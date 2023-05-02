using CommonModule.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonModule.Protos.Equipment;
using static CommonModule.Protos.Facility;

namespace IoTClient.gRPC.Facility
{
    public class FacilityGrpcClient : IDisposable
    {
        private readonly GrpcChannel _clientChannel;
        private readonly FacilityClient _client;
        public FacilityGrpcClient(IConfiguration configuration)
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
            _client = new FacilityClient(_clientChannel);
        }

        public async Task SendDataAsync()
        {
            var i = 1;
            while (true)
            {
                var data = new FacilityMessage
                {
                    Humidity = i + 5,
                    Temperature = i + 10,
                    TimestampStart = DateTime.UtcNow.ToTimestamp(),
                };
                //sleep for 2 seconds
                Thread.Sleep(2000);
                data.TimestampEnd = DateTime.UtcNow.ToTimestamp();
                var reply = await _client.SendAsync(data);
                TimeSpan ts = reply.ReceivedTime.ToDateTime() - data.TimestampStart.ToDateTime();
                Console.WriteLine($"Sent in  {ts.Microseconds}");
                i++;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
