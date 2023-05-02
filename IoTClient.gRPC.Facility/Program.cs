using System.Threading.Tasks;
using CommonModule.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using IoTClient.gRPC.Facility;
using Microsoft.Extensions.Configuration;

var httpHandler = new HttpClientHandler();
//ignore certificate validations
httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

var builder = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json");

var configuration = builder.Build();
using (var facilityClient = new FacilityGrpcClient(configuration))
{
    await facilityClient.SendDataAsync();
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();