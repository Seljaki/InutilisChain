// See https://aka.ms/new-console-template for more information

using InutilisChain;
using MPI;

new MPI.Environment(ref args);
int rank = Communicator.world.Rank;
Console.WriteLine("Rank: " + rank);
if (rank == 0)
{
    var blockchainServer = new BlockchainServer();
    var mqttBlockchainServer = new MqttBlockchainServer(blockchainServer);
    RESTServer restServer = new RESTServer(blockchainServer);
    mqttBlockchainServer.StartServer();
    Console.WriteLine("Started mqtt server");
    var serverThread = new Thread(() => blockchainServer.StartServer());
    serverThread.Start();
    Console.WriteLine("Started blockchain server");
    blockchainServer.StartMining(rank);
    var restThread = new Thread(() => restServer.Start());
    restThread.Start();
    Console.WriteLine("Started mining");
    Console.WriteLine("Press any key to stop the server");
    Console.ReadLine();

    serverThread.Abort();
} else
{
    BlockchainServer.MPIClient(rank);
}
return 0;