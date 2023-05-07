using CommonModule.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Quic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
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
        public EquipmentGrpcClient(IConfiguration configuration,string protocol)
        {
            var certificateHelper = new CertificateHelper(configuration);
            // create a httpHandler
            var httpHandler = new HttpClientHandler();
            //ignore certificate validations
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            //add the certificate for authentication
            httpHandler.ClientCertificates.Add(certificateHelper.
                                                getAuthenticationCertificate());
            var httpClient = new HttpClient(httpHandler);
            httpClient.DefaultRequestVersion = protocol=="h3"?HttpVersion.Version30: HttpVersion.Version20;
            httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            var gRPCConfig = configuration.GetSection("grpc");
            if (gRPCConfig == null)
            {
                throw new Exception(@"gRPC server details not 
                            configured in app settings file");
            }
            var gRPCEdgeServer = gRPCConfig.GetSection("server").Value;
            // Channel is actually created on the first call to the server.
            _clientChannel = GrpcChannel.ForAddress(gRPCEdgeServer,
                new GrpcChannelOptions { HttpClient = httpClient });
            var end=DateTime.UtcNow;
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
            var data = DataGenerator.GenerateData(payloadSize);
            var startTime = DateTime.Now;
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await Send_UnaryAsync(payloadSize, data);
            }
            var endTime = DateTime.Now;
            TimeSpan ts = endTime - startTime;
            logs.Add($"Total Runs={numberOfRuns}, Timespan={ts.TotalMilliseconds}");
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
            var messageList = new List<EquipmentMessage>();
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                messageList.Add(DataGenerator.GenerateData(i));
            }
            foreach (var message in messageList)
            {
                await Send_UnaryAsync(payloadMin, message);
            }

        }
        /// <summary>
        /// This method makes a unary call
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <returns></returns>
        private async Task Send_UnaryAsync(int payloadSize, EquipmentMessage data)
        {
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
            var data = DataGenerator.GenerateData(payloadSize);
            using var streamCall = _client.SendStream();
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await Send_ClientStreamingAsync(payloadSize, streamCall, data);
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
        private async Task Send_ClientStreamingAsync(int payloadSize, AsyncClientStreamingCall<EquipmentMessage, EdgeResponse> streamCall, EquipmentMessage data)
        {
            var startTime = DateTime.UtcNow;
            await streamCall.RequestStream.WriteAsync(data);
            var receivedTime = DateTime.UtcNow;
            TimeSpan ts = receivedTime - startTime;
            string jsonData = JsonSerializer.Serialize(data);
            logs.Add($"PayloadSize={payloadSize},ProtocolBufferSize={data.CalculateSize()}," +
                $"JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
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
            var payloadList = new List<EquipmentMessage>();
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                payloadList.Add(DataGenerator.GenerateData(i));
            }
            logs.Add($",,,StreamStartTime={DateTime.UtcNow},");
            using var streamCall = _client.SendStream();
            foreach (var payload in payloadList)
            {
                var i = 1;
                await Send_ClientStreamingAsync(i, streamCall, payload);
                i++;
            }
            await streamCall.RequestStream.CompleteAsync();

            var response = await streamCall.ResponseAsync;
            logs.Add($",,,StreamComplete={DateTime.UtcNow},");
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
                var data = DataGenerator.GenerateData(payloadSize);
                await Send_BiStreamAsync(payloadSize, streamCall, data);
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
                    logs.Add($",,,ReceivedTime={DateTime.UtcNow.ToTimestamp()},messageId={responseMessage.MessageId}");
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
            var messageList = new List<EquipmentMessage>();
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                messageList.Add(DataGenerator.GenerateData(i));
            }
            using var streamCall = _client.SendBiDirectionalStream();
            Task readResponsesTask = RegisterResponseCalls(streamCall);
            var k = 0;
            foreach (var message in messageList)
            {
                k++;
                await Send_BiStreamAsync(k, streamCall, message);
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
        private async Task Send_BiStreamAsync(int payloadsize, AsyncDuplexStreamingCall<EquipmentMessage, EdgeResponse> streamCall, EquipmentMessage data)
        {
            var startTime = DateTime.UtcNow;
            await streamCall.RequestStream.WriteAsync(data);
            string jsonData = JsonSerializer.Serialize(data);
            logs.Add($"PayloadSize={payloadsize},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},SendTime={startTime.ToTimestamp()},messageId={data.MessageId}");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _clientChannel.ShutdownAsync();
            _clientChannel.Dispose();
        }
    }
}
