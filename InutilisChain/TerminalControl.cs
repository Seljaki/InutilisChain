using System.Linq;
using System.Net;

namespace InutilisChain;

using System;
using System.Threading;

class TerminalControl
{
    private static Server server = new Server();
    private static bool isRunning = true;

    public static void Main(string[] args)
    {
        Console.WriteLine("Blockchain Server Control Terminal");
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
                        serverThread = new Thread(() => server.StartServer());
                        serverThread.Start();
                        Console.WriteLine("Server started.");
                    }
                    else
                    {
                        Console.WriteLine("Server is already running.");
                    }
                    break;

                case "2":
                    if (miningThread == null || !miningThread.IsAlive)
                    {
                        server.StartMining();
                       // miningThread = new Thread(() => server.StartMining());
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

                    server.ConnectToPeer(IPAddress.Parse(peerAddress), int.Parse(peerPort));
                    Console.WriteLine($"Connected to peer: {peerAddress}");
                    break;

                case "5":
                    if (serverThread != null && serverThread.IsAlive)
                    {
                        //server.StopServer();
                        serverThread.Join(); // Wait for the thread to stop
                        Console.WriteLine("Server stopped.");
                    }
                    else
                    {
                        Console.WriteLine("Server is not running.");
                    }
                    break;

                case "6":
                    if (server.doMining)
                    {
                        server.doMining = false;
                        Console.WriteLine("Mining stopped.");
                    }
                    else
                    {
                        Console.WriteLine("Mining is not running.");
                    }
                    break;

                case "7":
                    Console.WriteLine("Current Blockchain:");
                    server.blockChain.Print();
                    break;
                case "8":
                    Console.WriteLine("Current Data:");
                    var datas = server.dataQueue.ToList();
                    foreach (var data in datas)
                        Console.WriteLine(data.Serialize());
                    break;
                case "9":
                    Console.WriteLine("Current Data:");
                    var block = server.blockChain.getBlockAt(0);
                    Console.WriteLine(block.Serialize());
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
        Console.WriteLine(" 1  enable server    - Starts the server.");
        Console.WriteLine(" 2  start mining     - Starts mining blocks.");
        Console.WriteLine(" 3  fill test data   - Fills the data queue with test data.");
        Console.WriteLine(" 4  connect peer     - Connects to another peer.");
        Console.WriteLine(" 5  stop server      - Stops the server.");
        Console.WriteLine(" 6  stop mining      - Stops mining.");
        Console.WriteLine(" 7  output blockchain- Displays the current blockchain.");
        Console.WriteLine(" 8  output data      - Displays the current data.");
        Console.WriteLine(" 0  exit             - Exits the terminal.");
    }

    private static void FillTestData()
    {
        Random random = new Random();
        for (int i = 0; i < 30; i++)
        {
            server.dataQueue.Enqueue(
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
            //server.StopServer();
            serverThread.Join();
        }

        if (miningThread != null && miningThread.IsAlive)
        {
            server.doMining = false;
            miningThread.Join();
        }
    }
}
