// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using InutilisChain;
using MPI;


void FillTestData(BlockchainServer blockchainServer)
{
    Random random = new Random();
    for (int i = 0; i < 1000; i++)
    {
        blockchainServer.onNewData(
            new Data(
                Guid.NewGuid().ToString(),                       // Random unique identifier
                random.Next(0, 100),                             // Random integer between 0 and 100
                GetRandomDouble(random, 0.0, 90.0),                      // Random latitude
                GetRandomDouble(random, 0.0, 180.0),                     // Random longitude
                new Prediction(
                    GetRandomDouble(random, 0.0, 1.0),                   // Random double between 0.0 and 1.0
                    GetRandomDouble(random, 0.0, 1.0),                   // Random double between 0.0 and 1.0
                    GetRandomDouble(random, 0.0, 1.0)                    // Random double between 0.0 and 1.0
                )
            )
        );
    }
    Console.WriteLine("Test data filled.");
}

double GetRandomDouble(Random random, double minValue, double maxValue)
{
    return random.NextDouble() * (maxValue - minValue) + minValue;
}

new MPI.Environment(ref args);
int rank = Communicator.world.Rank;
Console.WriteLine("Rank: " + rank);
if (rank == 0)
{
    var blockchainServer = new BlockchainServer();
    FillTestData(blockchainServer);
    blockchainServer.StartMining(rank);
}
else
{
    BlockchainServer.MPIClient(rank);
}

return 0;
/*
if (args.Contains("-server"))
{
    var blockchainServer = new BlockchainServer();
    var mqttBlockchainServer = new MqttBlockchainServer(blockchainServer);
    mqttBlockchainServer.StartServer();
    Console.WriteLine("Started mqtt server");
    var serverThread = new Thread(() => blockchainServer.StartServer());
    serverThread.Start();
    Console.WriteLine("Started blockchain server");
    blockchainServer.StartMining();
    Console.WriteLine("Started mining");
    Console.WriteLine("Press any key to stop the server");
    Console.ReadLine();

    serverThread.Abort();
}
else
{
    TerminalControl.Main(args);
}
*/