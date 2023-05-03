using CommonModule.Protos;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static CommonModule.Protos.Equipment;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IoTClient.gRPC.Equipment
{
    internal class EquipmentGrpcClient : IDisposable
    {
        private readonly GrpcChannel _clientChannel;
        private readonly EquipmentClient _client;
        public List<string> logs = new List<string>();
        public EquipmentGrpcClient(IConfiguration configuration)
        {
            var certificateHelper = new CertificateHelper(configuration);
            // create a httpHandler
            var httpHandler = new HttpClientHandler();
            //add the certificate for authentication
            httpHandler.ClientCertificates.Add(certificateHelper.
                                                getAuthenticationCertificate());
            var httpClient = new HttpClient(httpHandler);
            var gRPCConfig = configuration.GetSection("grpc");
            if (gRPCConfig == null)
            {
                throw new Exception(@"gRPC server details not 
                            configured in app settings file");
            }
            var gRPCEdgeServer = gRPCConfig.GetSection("server").Value;
            // The port number must match the port of the gRPC server.
            _clientChannel = GrpcChannel.ForAddress(gRPCEdgeServer,
                new GrpcChannelOptions { HttpClient = httpClient });
            _client = new EquipmentClient(_clientChannel);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="numberOfRuns"></param>
        /// <returns></returns>
        public async Task SendConstantPayload_UnaryAsync(int payloadSize, int numberOfRuns)
        {
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await Send_UnaryAsync(payloadSize);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadMin"></param>
        /// <param name="payloadMax"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        public async Task SendVariablePayload_UnaryAsync(int payloadMin, int payloadMax, int increment = 1)
        {
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                await Send_UnaryAsync(i);
            }
        }
        /// <summary>
        /// This method makes a unary call
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <returns></returns>
        private async Task Send_UnaryAsync(int payloadSize)
        {
            var data = DataGenerator.GenerateData(payloadSize);
            var startTime = DateTime.UtcNow;
            var reply = await _client.SendAsync(data);
            var receivedTime = DateTime.UtcNow;
            TimeSpan ts = receivedTime - startTime;
            string jsonData = JsonSerializer.Serialize(data);
            logs.Add($"PayloadSize={payloadSize},ProtocolBufferSize={data.CalculateSize()}," +
                $"JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="numberOfRuns"></param>
        /// <returns></returns>
        public async Task SendConstantPayload_StreamAsync(int payloadSize, int numberOfRuns)
        {
            using var streamCall = _client.SendStream();
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await Send_ClientStreamingAsync(payloadSize, streamCall);
            }
            await streamCall.RequestStream.CompleteAsync();
            var response = await streamCall.ResponseAsync;
        }
        /// <summary>
        /// This method makes a client streaming call
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="streamCall"></param>
        /// <returns></returns>
        private async Task Send_ClientStreamingAsync(int payloadSize, AsyncClientStreamingCall<EquipmentMessage, EdgeResponse> streamCall)
        {
            var data = DataGenerator.GenerateData(payloadSize);
            var startTime = DateTime.UtcNow;
            await streamCall.RequestStream.WriteAsync(data);
            var receivedTime = DateTime.UtcNow;
            TimeSpan ts = receivedTime - startTime;
            string jsonData = JsonSerializer.Serialize(data);
            logs.Add($"PayloadSize={payloadSize},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadMin"></param>
        /// <param name="payloadMax"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        public async Task SendVariablePayload_StreamAsync(int payloadMin, int payloadMax, int increment = 1)
        {
            using var streamCall = _client.SendStream();
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                await Send_ClientStreamingAsync(i, streamCall);
            }
            await streamCall.RequestStream.CompleteAsync();
            var response = await streamCall.ResponseAsync;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="numberOfRuns"></param>
        /// <returns></returns>
        public async Task SendConstantPayload_BiStreamAsync(int payloadSize, int numberOfRuns)
        {
            using var streamCall = _client.SendBiDirectionalStream();
            Task readResponsesTask = RegisterResponseCalls(streamCall);
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await Send_BiStreamAsync(payloadSize, streamCall);
            }
            await streamCall.RequestStream.CompleteAsync();
            await readResponsesTask;
        }

        private Task RegisterResponseCalls(AsyncDuplexStreamingCall<EquipmentMessage, EdgeResponse> streamCall)
        {
            // register all response calls 
            var readResponsesTask = Task.Run(async () =>
            {
                await foreach (var responseMessage in streamCall.ResponseStream.ReadAllAsync())
                {
                    //get the response for the message
                    logs.Add($",,,ReceivedTime={DateTime.UtcNow},messageId={responseMessage.MessageId}");
                }
            });
            return readResponsesTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadMin"></param>
        /// <param name="payloadMax"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        public async Task SendVariablePayload_BiStreamAsync(int payloadMin, int payloadMax, int increment = 1)
        {
            using var streamCall = _client.SendBiDirectionalStream();
            Task readResponsesTask = RegisterResponseCalls(streamCall);
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                await Send_BiStreamAsync(i,streamCall);
            }
            await streamCall.RequestStream.CompleteAsync();
            await readResponsesTask;
        }
        /// <summary>
        /// This method makes bi-directional stream call
        /// </summary>
        /// <param name="payloadsize"></param>
        /// <param name="streamCall"></param>
        /// <returns></returns>
        private async Task Send_BiStreamAsync(int payloadsize,AsyncDuplexStreamingCall<EquipmentMessage, EdgeResponse> streamCall)
        {
            var data = DataGenerator.GenerateData(payloadsize);
            var startTime = DateTime.UtcNow;
            await streamCall.RequestStream.WriteAsync(data);
            string jsonData = JsonSerializer.Serialize(data);
            logs.Add($"PayloadSize={payloadsize},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},SendTime={startTime},messageId={data.MessageId}");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _clientChannel.Dispose();
        }
    }
}
