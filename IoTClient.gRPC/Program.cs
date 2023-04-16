using System.Threading.Tasks;
using CommonModule.Protos;
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
var client = new EdgeGateway.EdgeGatewayClient(channel);
//sleep 5 secs to ensure that facility client is running
Thread.Sleep(5000);
while (true)
{
    var data= DataGenerator.GenerateData(1);
    int byteSize = data.CalculateSize();
    var reply = await client.SendEquipmentAsync(data);
    TimeSpan ts = reply.ReceivedTime.ToDateTime() - data.Timestamp.ToDateTime();
    Console.WriteLine($"Sent  {byteSize} in  {ts.Microseconds}");
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();