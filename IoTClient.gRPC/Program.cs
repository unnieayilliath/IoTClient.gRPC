using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using IoTClient.gRPC;
// create a httpHandler
var httpHandler = new HttpClientHandler();
//ignore certificate validations
httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

var httpClient = new HttpClient(httpHandler);
// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });
var client = new IoTMessage.IoTMessageClient(channel);

for (int i = 0; i < 10; i++)
{
    var data= DataGenerator.GenerateData(1);
    var request = new EdgeRequest { Data = data, SendTime = DateTime.UtcNow.ToTimestamp() };
    var reply = await client.SendAsync(request);
    TimeSpan ts = reply.ReceivedTime.ToDateTime() - request.SendTime.ToDateTime();
    Console.WriteLine("Latency : " + ts.Microseconds);
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();