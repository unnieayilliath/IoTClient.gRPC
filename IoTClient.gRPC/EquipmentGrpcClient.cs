using CommonModule.Protos;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<string> logs=new List<string>();
        public EquipmentGrpcClient()
        {
            // create a httpHandler
            var httpHandler = new HttpClientHandler();
            //ignore certificate validations
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var httpClient = new HttpClient(httpHandler);
            // The port number must match the port of the gRPC server.
            _clientChannel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });
            _client = new EdgeGateway.EdgeGatewayClient(_clientChannel);
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
                string jsonData= JsonSerializer.Serialize(data);
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
        public async Task SendVariablePayload_UnaryAsync(int payloadMin, int payloadMax, int increment=1)
        {
            for (int i = payloadMin; i <= payloadMax; i+= increment)
            {
                var data = DataGenerator.GenerateData(i);
                var startTime = DateTime.UtcNow;
                var reply = await _client.SendEquipmentAsync(data);
                var receivedTime = DateTime.UtcNow;
                TimeSpan ts = receivedTime - startTime;
                Console.WriteLine("Unary RTT=" + ts.TotalMilliseconds);
                string jsonData= JsonSerializer.Serialize(data);
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
                string jsonData= JsonSerializer.Serialize(data);
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
        public async Task SendVariablePayload_StreamAsync(int payloadMin, int payloadMax, int increment=1)
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
                string jsonData= JsonSerializer.Serialize(data);
                logs.Add($"PayloadSize={i},ProtocolBufferSize={data.CalculateSize()},JSONSize={Encoding.UTF8.GetByteCount(jsonData)},RTT={ts.TotalMilliseconds}");
            }
            await streamCall.RequestStream.CompleteAsync();
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
