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
    var payloadOption = Show_PayloadOptions();
    string? payloadSize, numberOfRuns, numberOfStreams;
    Get_ConstantPayload_Parameters(out payloadSize, out numberOfRuns, out numberOfStreams);
    var logs = new List<string>();
    var logFilePrefix = streamingOption == '1' ? "Unary_" : (streamingOption == '2' ? "ClientStreaming_" : streamingOption == '3' ? "bidirectional_" : "ChannelCreation_");
    for (var i = 1; i <= int.Parse(numberOfRuns); i++)
    {
        var currentlog = await RunClient(configuration, streamingOption, payloadOption, payloadSize, numberOfStreams);
        logs.AddRange(currentlog);
    }
    // write the logs to file
    PerformanceLogger.WriteDataToFile($"{logFilePrefix}_{payloadSize}_${DateTime.Now.Ticks}.txt", logs);
    Console.WriteLine("Do you want to run the client again?(y/n)");
    var choice = Console.ReadKey();
    loop = choice.KeyChar == 'y';
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();


static async Task RunChannelCreationTestsAsync(IConfiguration configuration)
{
    var logs = new List<string>();
    var logFilePrefix = "ChannelCreation";
    for (int i = 0; i < 100; i++)
    {
        using (EquipmentGrpcClient grpcClient = new EquipmentGrpcClient(configuration))
        {
            // run the test wih 100 bytes 
            await grpcClient.SendConstantPayload_UnaryAsync(100, 3);
            logs.AddRange(grpcClient.logs);
        }
    }
    // write the logs to file
    PerformanceLogger.WriteDataToFile($"{logFilePrefix}_${DateTime.Now.Ticks}.txt", logs);
}
static async Task<List<string>> RunClient(IConfiguration configuration, char streamingOption, char payloadOption, string payloadSize, string numberOfRuns)
{
    var logs = new List<string>();
    switch (streamingOption)
    {
        case '4':
            {
                await RunChannelCreationTestsAsync(configuration);
                break;
            }

        default:
            using (EquipmentGrpcClient grpcClient = new EquipmentGrpcClient(configuration))
            {
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
            }
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
            "3. Bi-directional" +
            "4. Channel Creation test");
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
