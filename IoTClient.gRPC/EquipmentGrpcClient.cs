using CommonModule.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using static CommonModule.Protos.Equipment;
namespace IoTClient.gRPC.Equipment
{
    internal class EquipmentGrpcClient : IDisposable
    {
        private readonly GrpcChannel _clientChannel;
        private readonly EquipmentClient _client;
        public List<string> logs = new List<string>();
        public Dictionary<string, DateTime> biStreamClientRequests = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> biStreamClientResponses = new Dictionary<string, DateTime>();
        public EquipmentGrpcClient(IConfiguration configuration)
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
        public async Task Send_UnaryAsync(int payloadSize, EquipmentMessage data,string wifi="")
        {
            var startTime = DateTime.UtcNow;
            var reply = await _client.SendAsync(data);
            var receivedTime = DateTime.UtcNow;
            TimeSpan ts = receivedTime - startTime;
            string jsonData = JsonSerializer.Serialize(data);
            logs.Add($"Wifi={wifi},PayloadSize={payloadSize}," +
                $"ProtocolBufferSize={data.CalculateSize()}," +
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
            var startTime=DateTime.UtcNow;
            using var streamCall = _client.SendStream();
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await streamCall.RequestStream.WriteAsync(data);
            }
            await streamCall.RequestStream.CompleteAsync();
            //wait for response
            var response = await streamCall.ResponseAsync;
            var endTime = DateTime.UtcNow;
            var ts= endTime - startTime;
            logs.Add($"{payloadSize},{ts.TotalMilliseconds}");
        }
        /// <summary>
        /// This method makes a client streaming call
        /// </summary>
        /// <param name="payloadSize"></param>
        /// <param name="streamCall"></param>
        /// <returns></returns>
        private async Task Send_ClientStreamingAsync(int payloadSize, AsyncClientStreamingCall<EquipmentMessage, EdgeResponse> streamCall, EquipmentMessage data)
        {
            //store the send time
            data.Timestamp = DateTime.UtcNow.ToTimestamp();
            await streamCall.RequestStream.WriteAsync(data);
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
            var data = DataGenerator.GenerateData(payloadSize);
            Task readResponsesTask = RegisterResponseCalls(streamCall);
            for (int i = 1; i <= numberOfRuns; i++)
            {
                await Send_BiStreamAsync(streamCall, data);
            }
            await streamCall.RequestStream.CompleteAsync();
            await readResponsesTask;
            // log the bistream metrics
            LogBiStreamMetrics(payloadSize);
           
        }
        private void LogBiStreamMetrics(int payloadSize)
        {
            foreach (var request in biStreamClientRequests)
            {
                DateTime response;
                if (biStreamClientResponses.TryGetValue(request.Key, out response))
                {
                    TimeSpan ts = response - request.Value;
                    logs.Add($"{payloadSize},{request.Key},{ts.TotalMilliseconds}");
                }
            }
            //reset the request and response
            biStreamClientRequests = new Dictionary<string, DateTime>();
            biStreamClientResponses = new Dictionary<string, DateTime>();
        }
        private Task RegisterResponseCalls(AsyncDuplexStreamingCall<EquipmentMessage, EdgeResponse> streamCall)
        {
            // register all response calls 
            var readResponsesTask = Task.Run(async () =>
            {
                List<Task> tasks = new List<Task>();
                await foreach (var responseMessage in streamCall.ResponseStream.ReadAllAsync())
                {
                    biStreamClientResponses.Add(responseMessage.MessageId, DateTime.UtcNow);
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
                await Send_BiStreamAsync(streamCall, message);
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
        private async Task Send_BiStreamAsync(AsyncDuplexStreamingCall<EquipmentMessage, EdgeResponse> streamCall, EquipmentMessage data)
        {
            var sendTime= DateTime.UtcNow;
            data.Timestamp = sendTime.ToTimestamp();
            await streamCall.RequestStream.WriteAsync(data);
            biStreamClientRequests.Add(data.MessageId, sendTime);
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
