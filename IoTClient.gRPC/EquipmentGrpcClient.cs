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
using static CommonModule.Protos.EdgeGateway;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IoTClient.gRPC.Equipment
{
    internal class EquipmentGrpcClient : IDisposable
    {
        private readonly GrpcChannel _clientChannel;
        private readonly EdgeGatewayClient _client;
        public List<string> logs = new List<string>();
        public EquipmentGrpcClient(IConfiguration configuration)
        {
            var certificateHelper = new CertificateHelper(configuration);
            // create a httpHandler
            var httpHandler = new HttpClientHandler();
            //ignore certificate validations
            // httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            //add the certificate for authentication
            httpHandler.ClientCertificates.Add(certificateHelper.getAuthenticationCertificate());
            var httpClient = new HttpClient(httpHandler);
            var gRPCConfig = configuration.GetSection("grpc");
            if (gRPCConfig == null)
            {
                throw new Exception("gRPC server details not configured in app settings file");
            }
            var gRPCEdgeServer = gRPCConfig.GetSection("server").Value;
            // The port number must match the port of the gRPC server.
            _clientChannel = GrpcChannel.ForAddress(gRPCEdgeServer, new GrpcChannelOptions { HttpClient = httpClient });
            _client = new EdgeGatewayClient(_clientChannel);
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
                var data = DataGenerator.GenerateData(payloadSize);
                var startTime = DateTime.UtcNow;
                var reply = await _client.SendEquipmentAsync(data);
                var receivedTime = DateTime.UtcNow;
                TimeSpan ts = receivedTime - startTime;
                Console.WriteLine("Unary RTT=" + ts.TotalMilliseconds);
                string jsonData = JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={payloadSize},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
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
                var data = DataGenerator.GenerateData(i);
                var startTime = DateTime.UtcNow;
                var reply = await _client.SendEquipmentAsync(data);
                var receivedTime = DateTime.UtcNow;
                TimeSpan ts = receivedTime - startTime;
                Console.WriteLine("Unary RTT=" + ts.TotalMilliseconds);
                string jsonData = JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={i},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="numberOfRuns"></param>
        /// <returns></returns>
        public async Task SendConstantPayload_StreamAsync(int payloadSize, int numberOfRuns)
        {
            using var streamCall = _client.SendEquipmentStream();
            for (int i = 1; i <= numberOfRuns; i++)
            {
                var data = DataGenerator.GenerateData(payloadSize);
                var startTime = DateTime.UtcNow;
                await streamCall.RequestStream.WriteAsync(data);
                var receivedTime = DateTime.UtcNow;
                TimeSpan ts = receivedTime - startTime;
                Console.WriteLine("Stream RTT=" + ts.TotalMilliseconds);
                string jsonData = JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={payloadSize},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
            }
            await streamCall.RequestStream.CompleteAsync();
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
            using var streamCall = _client.SendEquipmentStream();
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                var data = DataGenerator.GenerateData(i);
                var startTime = DateTime.UtcNow;
                await streamCall.RequestStream.WriteAsync(data);
                var receivedTime = DateTime.UtcNow;
                TimeSpan ts = receivedTime - startTime;
                Console.WriteLine("Stream RTT=" + ts.TotalMilliseconds);
                string jsonData = JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={i},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
            }
            await streamCall.RequestStream.CompleteAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="numberOfRuns"></param>
        /// <returns></returns>
        public async Task SendConstantPayload_BiStreamAsync(int payloadSize, int numberOfRuns)
        {
            using var streamCall = _client.SendEquipmentBiDirectionalStream();
            // register all response calls 
            var readResponsesTask = Task.Run(async () =>
            {
                await foreach (var responseMessage in streamCall.ResponseStream.ReadAllAsync())
                {
                    //get the response for the message
                    logs.Add($",,,ReceivedTime={DateTime.UtcNow},messageId={responseMessage.MessageId}");
                }
            });
            for (int i = 1; i <= numberOfRuns; i++)
            {
                var data = DataGenerator.GenerateData(payloadSize);
                var startTime = DateTime.UtcNow;
                await streamCall.RequestStream.WriteAsync(data);
                string jsonData = JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={payloadSize},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},SendTime={startTime},messageId={data.MessageId}");
            }
            await streamCall.RequestStream.CompleteAsync();
            await readResponsesTask;
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
            using var streamCall = _client.SendEquipmentBiDirectionalStream();
            // register all response calls 
            var readResponsesTask = Task.Run(async () =>
            {
                await foreach (var responseMessage in streamCall.ResponseStream.ReadAllAsync())
                {
                    //get the response for the message
                    logs.Add($",,,ReceivedTime={DateTime.UtcNow},messageId={responseMessage.MessageId}");
                }
            });
            for (int i = payloadMin; i <= payloadMax; i += increment)
            {
                var data = DataGenerator.GenerateData(i);
                var startTime = DateTime.UtcNow;
                await streamCall.RequestStream.WriteAsync(data);
                string jsonData = JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={i},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},SendTime={startTime},messageId={data.MessageId}");
            }
            await streamCall.RequestStream.CompleteAsync();
            await readResponsesTask;
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
