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
bool loop = true;
while (loop)
{
    using (var facilityClient = new FacilityGrpcClient(configuration))
    {
        await facilityClient.SendDataAsync();
    }
    Console.WriteLine("Do you want to exit?(y/n)");
    var input=Console.ReadKey();
    loop = input.KeyChar == 'n';
}


Console.WriteLine("Press any key to exit...");
Console.ReadKey();