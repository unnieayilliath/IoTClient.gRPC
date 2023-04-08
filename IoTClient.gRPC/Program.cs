using System.Threading.Tasks;
using Grpc.Net.Client;
using IoTClient.gRPC;
// create a httpHandler
var httpHandler = new HttpClientHandler();
//ignore certificate validations
httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

var httpClient = new HttpClient(httpHandler);
// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7003", new GrpcChannelOptions { HttpClient = httpClient });
var client = new IoTMessage.IoTMessageClient(channel);
var reply = await client.SendAsync(
                  new EdgeRequest { Data = "GreeterClient" });
Console.WriteLine("Greeting: " + reply.Message);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();