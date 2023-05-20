using IoTClient.gRPC;
using IoTClient.gRPC.Equipment;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json");
var configuration = builder.Build();
var loop = true;
while (loop)
{
    var streamingOption = Show_TopLevelChoices();
    if (streamingOption == '4')
    {
        await ConnectionMigration(configuration);
        continue;
    }else if (streamingOption == '5')
    {
        await ProtocolEfficiency(configuration);
        continue;
    }
    using (var grpcClient = new EquipmentGrpcClient(configuration))
    {

        var payloadOption = Show_PayloadOptions();
        string? payloadSize, numberOfRuns, numberOfStreams;
        Get_ConstantPayload_Parameters(out payloadSize, out numberOfRuns, out numberOfStreams);
        var logs = new List<string>();
        var logFilePrefix = streamingOption == '1' ? "Unary_" : (streamingOption == '2' ? "ClientStreaming_" : streamingOption == '3' ? "bidirectional_" : "connectionmigration_");
        for (var i = 1; i <= int.Parse(numberOfRuns); i++)
        {
            var currentlog = await RunClient(grpcClient, configuration, streamingOption, payloadOption, payloadSize, numberOfStreams);
            logs.AddRange(currentlog);
            Console.WriteLine("Press any key to start next run.");
            Console.ReadKey();
        }
        // write the logs to file
        PerformanceLogger.WriteDataToFile($"{logFilePrefix}_{payloadSize}_${DateTime.Now.Ticks}.txt", logs);
        Console.WriteLine("Do you want to run the client again?(y/n)");
        var choice = Console.ReadKey();
        loop = choice.KeyChar == 'y';
    }
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();



static async Task ProtocolEfficiency(IConfiguration configuration)
{
    Console.WriteLine("\n==========Running Protocol Effeciency Test=============");
    var data = DataGenerator.GenerateData_Tests(10);
    var client = new EquipmentGrpcClient(configuration);
    // this will be http/2 since first request
    await client.Send_UnaryAsync(100, data, "WiFi1-Req1");
    // second request is heavy coz of QUIC handshakes
    await client.Send_UnaryAsync(100, data, "WiFi1-Req2");

    for (int i = 1; i <=10; i++)
    {
        data = DataGenerator.GenerateData_Tests(i * 10);
        Console.WriteLine($"\nStart packet capture for ${i*10} and press any key..");
        Console.ReadKey();
        await client.Send_UnaryAsync(100, data, "WiFi1-Req2");
        Console.WriteLine($"\nSent {i * 10} bytes of application data.");
    }
    Console.WriteLine("\n==========Completed Test=============");
}

static async Task ConnectionMigration(IConfiguration configuration)
{
    Console.WriteLine("\n==========Running Connection Migration Test=============");
    var logs = new List<string>();
    var logFilePrefix = "ConnectionMigration_";
    // create 1000 channels
    var data = DataGenerator.GenerateData(100);

    List<EquipmentGrpcClient> clients = new List<EquipmentGrpcClient>();

    for (int i = 1; i <= 10; i++)
    {
        var client = new EquipmentGrpcClient(configuration);
        // this will be http/2 since first request
        await client.Send_UnaryAsync(100, data, "WiFi1-Req1");
        // send 2 requests 
        await client.Send_UnaryAsync(100 * i, data, "WiFi1-Req2");
        clients.Add(client);
    }
    // change wifi
    Console.WriteLine("\nPlease change WiFi and press any key to continue");
    Console.ReadKey();
    //send 2 requests
    foreach (var client in clients)
    {
        // send 2 more requests 
        await client.Send_UnaryAsync(200, data, "WiFi2-Req1");
        await client.Send_UnaryAsync(100, data, "WiFi2-Req2");
        logs.AddRange(client.logs);
    }
    // write the logs to file
    PerformanceLogger.WriteDataToFile($"{logFilePrefix}_${DateTime.Now.Ticks}.txt", logs);
    Console.WriteLine("\n==========Completed Connection Migration Test=============");
}
static async Task<List<string>> RunClient(EquipmentGrpcClient grpcClient, IConfiguration configuration, char streamingOption, char payloadOption, string payloadSize, string numberOfRuns)
{
    var logs = new List<string>();
    switch (streamingOption)
    {
        case '4':
            {
                await ConnectionMigration(configuration);
                break;
            }

        default:

            switch (streamingOption)
            {
                case '1':
                    {
                        // unary
                        switch (payloadOption)
                        {
                            case '1':
                                await Run_Unary_ConstantPayloadTestAsync(grpcClient, payloadSize, numberOfRuns);
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case '2':
                    {
                        //client streaming
                        switch (payloadOption)
                        {
                            case '1':
                                await Run_ClientStreaming_ConstantPayloadTestAsync(grpcClient, payloadSize, numberOfRuns);
                                break;
                            default:
                                break;
                        }
                        break;
                    }
                case '3':
                    {
                        // bi-directional
                        switch (payloadOption)
                        {
                            case '1':
                                await Run_BiStreaming_ConstantPayloadTestAsync(grpcClient, payloadSize, numberOfRuns);
                                break;
                            default:
                                break;
                        }
                        break;
                    }

            }
            logs = grpcClient.logs;
            // reset client logs
            grpcClient.logs = new List<string>();

            break;
    }
    return logs;
}


static async Task Run_Unary_ConstantPayloadTestAsync(EquipmentGrpcClient grpcClient, string payloadSize, string numberOfRuns)
{

    await grpcClient.SendConstantPayload_UnaryAsync(int.Parse(payloadSize), int.Parse(numberOfRuns));
}




static async Task Run_ClientStreaming_ConstantPayloadTestAsync(EquipmentGrpcClient grpcClient, string payloadSize, string numberOfRuns)
{

    await grpcClient.SendConstantPayload_StreamAsync(int.Parse(payloadSize), int.Parse(numberOfRuns));
}

static async Task Run_BiStreaming_ConstantPayloadTestAsync(EquipmentGrpcClient grpcClient, string payloadSize, string numberOfRuns)
{
    await grpcClient.SendConstantPayload_BiStreamAsync(int.Parse(payloadSize), int.Parse(numberOfRuns));
}
static char Show_TopLevelChoices()
{
    Console.WriteLine("\nSelect testing options:\n" +
            "1. Unary " +
            "2. Client Streaming " +
            "3. Bi-directional " +
            "4. Channel Creation test "+
            "5. Protocol effeciency test");
    var streamingOption = Console.ReadKey();
    return streamingOption.KeyChar;
}

static char Show_PayloadOptions()
{
    // only choose constant
    return '1';
    //Console.WriteLine("\nSelect payload options:\n" +
    //            "1. Constant payload " +
    //            "2. Variable payload (Incremental)");
    //var payloadOption = Console.ReadKey();
    //return payloadOption.KeyChar;
}

static void Get_ConstantPayload_Parameters(out string? payloadSize, out string? numberOfRuns, out string? numberOfStreams)
{
    Console.WriteLine("\nEnter the payload size:");
    payloadSize = Console.ReadLine();
    Console.WriteLine("\nEnter the number of runs:");
    numberOfRuns = Console.ReadLine();
    Console.WriteLine("\nEnter the number of streams per run:");
    numberOfStreams = Console.ReadLine();
}
