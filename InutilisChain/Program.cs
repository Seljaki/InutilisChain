// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using InutilisChain;

Console.WriteLine("Hello, World!");
Prediction prediction = new Prediction(0.1,0.4,0.5);
Data data = new Data(Guid.NewGuid().ToString(),13, 45.62131, 16.3454, prediction);
Console.WriteLine(data.Serialize());


Block currentBlock = new Block(new Data(Guid.NewGuid().ToString(), 0,0,0, new Prediction(1,0,0)), 0);
currentBlock.previousHash = new byte[]{0x0};
using (var sha256 = SHA256.Create())
    currentBlock.calculateAndSetHash(sha256);
Console.WriteLine(Block.Deserialize(currentBlock.Serialize()).Serialize());
;


/*Server server = new Server();
for (int i = 0; i < 10000; i++)
{
    server.dataQueue.Enqueue(new Data(Guid.NewGuid().ToString(), 13, 45.62131, 16.3454, prediction));
}


server.StartMining();
server.StartServer();*/
TerminalControl.Main(args);
