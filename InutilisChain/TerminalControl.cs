using System.Linq;
using System.Net;

namespace InutilisChain;

using System;
using System.Threading;

class TerminalControl
{
    private static BlockchainServer blockchainServer = new BlockchainServer();
    private static bool isRunning = true;
    private static MqttBlockchainServer mqttBlockchainServer = new MqttBlockchainServer(blockchainServer);

    public static void Main(string[] args)
    {
        Console.WriteLine("Blockchain BlockchainServer Control Terminal");
        Console.WriteLine("Type 'help' for a list of commands.");
        PrintHelp();

        Thread serverThread = null;
        Thread miningThread = null;

        while (isRunning)
        {
            Console.Write("> ");
            string input = Console.ReadLine()?.Trim().ToLower();

            switch (input)
            {
                case "help":
                    PrintHelp();
                    break;

                case "1":
                    if (serverThread == null || !serverThread.IsAlive)
                    {
                        serverThread = new Thread(() => blockchainServer.StartServer());
                        serverThread.Start();
                        Console.WriteLine("BlockchainServer started.");
                    }
                    else
                    {
                        Console.WriteLine("BlockchainServer is already running.");
                    }
                    break;

                case "2":
                    if (miningThread == null || !miningThread.IsAlive)
                    {
                        //blockchainServer.StartMining();
                       // miningThread = new Thread(() => blockchainServer.StartMining());
                        //miningThread.Start();
                        Console.WriteLine("Mining started.");
                    }
                    else
                    {
                        Console.WriteLine("Mining is already running.");
                    }
                    break;

                case "3":
                    FillTestData();
                    break;

                case "4":
                    Console.Write("Enter peer address (localhost): ");
                    string peerAddress = Console.ReadLine();
                    Console.Write("Enter peer port: ");
                    string peerPort = Console.ReadLine();
                    if (string.IsNullOrEmpty(peerAddress))
                        peerAddress = "127.0.0.1";
                    if (string.IsNullOrEmpty(peerPort))
                        peerPort = "6969";

                    blockchainServer.ConnectToPeer(IPAddress.Parse(peerAddress), int.Parse(peerPort));
                    Console.WriteLine($"Connected to peer: {peerAddress}");
                    break;

                case "5":
                    if (serverThread != null && serverThread.IsAlive)
                    {
                        //blockchainServer.StopServer();
                        serverThread.Join(); // Wait for the thread to stop
                        Console.WriteLine("BlockchainServer stopped.");
                    }
                    else
                    {
                        Console.WriteLine("BlockchainServer is not running.");
                    }
                    break;

                case "6":
                    if (blockchainServer.doMining)
                    {
                        blockchainServer.doMining = false;
                        Console.WriteLine("Mining stopped.");
                    }
                    else
                    {
                        Console.WriteLine("Mining is not running.");
                    }
                    break;

                case "7":
                    Console.WriteLine("Current Blockchain:");
                    blockchainServer.blockChain.Print();
                    break;
                case "8":
                    Console.WriteLine("Current Data:");
                    var datas = blockchainServer.dataQueue.ToList();
                    foreach (var data in datas)
                        Console.WriteLine(data.Serialize());
                    break;
                case "9":
                    mqttBlockchainServer.StartServer();
                    break;
                case "0":
                    isRunning = false;
                    StopAll(serverThread, miningThread);
                    Console.WriteLine("Exiting terminal.");
                    break;

                default:
                    Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
                    break;
            }
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine(" 1  enable blockchainServer    - Starts the blockchainServer.");
        Console.WriteLine(" 2  start mining     - Starts mining blocks.");
        Console.WriteLine(" 3  fill test data   - Fills the data queue with test data.");
        Console.WriteLine(" 4  connect peer     - Connects to another peer.");
        Console.WriteLine(" 5  stop blockchainServer      - Stops the blockchainServer.");
        Console.WriteLine(" 6  stop mining      - Stops mining.");
        Console.WriteLine(" 7  output blockchain- Displays the current blockchain.");
        Console.WriteLine(" 8  output data      - Displays the current data.");
        Console.WriteLine(" 9  start mqtt blockchainServer");
        Console.WriteLine(" 0  exit             - Exits the terminal.");
    }

    private static void FillTestData()
    {
        Random random = new Random();
        for (int i = 0; i < 5; i++)
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

    private static double GetRandomDouble(Random random, double minValue, double maxValue)
    {
        return random.NextDouble() * (maxValue - minValue) + minValue;
    }
    
    private static void StopAll(Thread serverThread, Thread miningThread)
    {
        if (serverThread != null && serverThread.IsAlive)
        {
            //blockchainServer.StopServer();
            serverThread.Join();
        }

        if (miningThread != null && miningThread.IsAlive)
        {
            blockchainServer.doMining = false;
            miningThread.Join();
        }
    }
}
