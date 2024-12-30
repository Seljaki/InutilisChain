// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using InutilisChain;

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
