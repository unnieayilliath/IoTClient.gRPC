﻿using IoTClient.gRPC.Equipment;
using Microsoft.Extensions.Configuration;

//sleep 5 secs to ensure that facility client is running
Thread.Sleep(5000);
var logs = new List<string>();
var logFilePrefix = "";

var builder = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
               .AddJsonFile("appsettings.json");
var configuration = builder.Build();
using (EquipmentGrpcClient grpcClient = new EquipmentGrpcClient(configuration))
{
    var streamingOption = Show_TopLevelChoices();
    var payloadOption = Show_PayloadOptions();
    switch (streamingOption)
    {
        case '1':
            {
                // unary
                switch (payloadOption)
                {
                    case '1':
                        await Run_Unary_ConstantPayloadTestAsync(grpcClient);
                        logFilePrefix = "Unary_Constant";
                        break;
                    case '2':
                        await Run_Unary_VariablePayloadTests_Async(grpcClient);
                        logFilePrefix = "Unary_Variable";
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
                        await Run_ClientStreaming_ConstantPayloadTestAsync(grpcClient);
                        logFilePrefix = "ClientStreaming_Constant";
                        break;
                    case '2':
                        await Run_ClientStreaming_VariablePayloadTests_Async(grpcClient);
                        logFilePrefix = "ClientStreaming_Variable";
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
                        await Run_BiStreaming_ConstantPayloadTestAsync(grpcClient);
                        logFilePrefix = "BiStreaming_Constant";
                        break;
                    case '2':
                        await Run_BiStreaming_VariablePayloadTests_Async(grpcClient);
                        logFilePrefix = "BiStreaming_Variable";
                        break;
                    default:
                        break;
                }
                break;
            }

        default:
            break;
    }
    logs = grpcClient.logs;
}
// write the logs to file
PerformanceLogger.WriteDataToFile($"{logFilePrefix}_${DateTime.Now.Ticks.ToString()}.txt", logs);
Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

static async Task Run_Unary_ConstantPayloadTestAsync(EquipmentGrpcClient grpcClient)
{
    string? payloadSize, numberOfRuns;
    Get_ConstantPayload_Parameters(out payloadSize, out numberOfRuns);
    await grpcClient.SendConstantPayload_UnaryAsync(int.Parse(payloadSize), int.Parse(numberOfRuns));
}

static async Task Run_Unary_VariablePayloadTests_Async(EquipmentGrpcClient grpcClient)
{
    string? payloadMinSize, payloadMaxSize, increment;
    Get_VariablePayload_Parameters(out payloadMinSize, out payloadMaxSize, out increment);
    await grpcClient.SendVariablePayload_UnaryAsync(int.Parse(payloadMinSize), int.Parse(payloadMaxSize), int.Parse(increment));
}

static async Task Run_ClientStreaming_VariablePayloadTests_Async(EquipmentGrpcClient grpcClient)
{
    string? payloadMinSize, payloadMaxSize, increment;
    Get_VariablePayload_Parameters(out payloadMinSize, out payloadMaxSize, out increment);
    await grpcClient.SendVariablePayload_StreamAsync(int.Parse(payloadMinSize), int.Parse(payloadMaxSize), int.Parse(increment));
}
static async Task Run_ClientStreaming_ConstantPayloadTestAsync(EquipmentGrpcClient grpcClient)
{
    string? payloadSize, numberOfRuns;
    Get_ConstantPayload_Parameters(out payloadSize, out numberOfRuns);
    await grpcClient.SendConstantPayload_StreamAsync(int.Parse(payloadSize), int.Parse(numberOfRuns));
}
static async Task Run_BiStreaming_VariablePayloadTests_Async(EquipmentGrpcClient grpcClient)
{
    string? payloadMinSize, payloadMaxSize, increment;
    Get_VariablePayload_Parameters(out payloadMinSize, out payloadMaxSize, out increment);
    await grpcClient.SendVariablePayload_BiStreamAsync(int.Parse(payloadMinSize), int.Parse(payloadMaxSize), int.Parse(increment));
}
static async Task Run_BiStreaming_ConstantPayloadTestAsync(EquipmentGrpcClient grpcClient)
{
    string? payloadSize, numberOfRuns;
    Get_ConstantPayload_Parameters(out payloadSize, out numberOfRuns);
    await grpcClient.SendConstantPayload_BiStreamAsync(int.Parse(payloadSize), int.Parse(numberOfRuns));
}
static char Show_TopLevelChoices()
{
    Console.WriteLine("\nSelect testing options:\n" +
            "1. Unary " +
            "2. Client Streaming " +
            "3. Bi-directional");
    var streamingOption = Console.ReadKey();
    return streamingOption.KeyChar;
}

static char Show_PayloadOptions()
{
    Console.WriteLine("\nSelect payload options:\n" +
                "1. Constant payload " +
                "2. Variable payload (Incremental)");
    var payloadOption = Console.ReadKey();
    return payloadOption.KeyChar;
}

static void Get_ConstantPayload_Parameters(out string? payloadSize, out string? numberOfRuns)
{
    Console.WriteLine("\nEnter the payload size:");
    payloadSize = Console.ReadLine();
    Console.WriteLine("\nEnter the number of runs:");
    numberOfRuns = Console.ReadLine();
}

static void Get_VariablePayload_Parameters(out string? payloadMinSize, out string? payloadMaxSize, out string? increment)
{
    Console.WriteLine("\nEnter the payload minimum size:");
    payloadMinSize = Console.ReadLine();
    Console.WriteLine("\nEnter the payload maximum size:");
    payloadMaxSize = Console.ReadLine();
    Console.WriteLine("\nEnter the increment on each run:");
    increment = Console.ReadLine();
}