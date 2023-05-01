using System.Threading.Tasks;
using CommonModule.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using static System.Runtime.InteropServices.JavaScript.JSType;
// create a httpHandler
var httpHandler = new HttpClientHandler();
//ignore certificate validations
httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

var httpClient = new HttpClient(httpHandler);
// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions { HttpClient = httpClient });
var client = new Facility.FacilityClient(channel);
var i = 1;
while(true)
{
    var data = new FacilityMessage
    {
        Humidity = i + 5,
        Temperature = i + 10,
        TimestampStart = DateTime.UtcNow.ToTimestamp(),
    };
    Thread.Sleep(2000);
    data.TimestampEnd = DateTime.UtcNow.ToTimestamp();
    var reply = await client.SendAsync(data);
    TimeSpan ts = reply.ReceivedTime.ToDateTime() - data.TimestampStart.ToDateTime();
    Console.WriteLine($"Sent in  {ts.Microseconds}");
    i++;
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();