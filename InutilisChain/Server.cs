using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using static InutilisChain.Common;

namespace InutilisChain;

public class Miner
{
    
}

public class Server
{
    private const int STD_PORT = 6969;
    
    private List<Peer> Peers;
    //public List<Block> Blocks;
    public BlockChain blockChain;

    public bool doMining = true;
    
    public delegate void NewPortSetHandler(int port);
    public delegate void BlockMinedEventHandler(Block newBlock);
    public delegate void OnLog(string log);
    public event BlockMinedEventHandler OnBlockMined;
    public event OnLog OnNewLog;
    public event NewPortSetHandler OnNewPortSet;
    private bool listenToNewPears = true;
    TcpListener serverListener = null;
    public string minerName = "Miner" + Guid.NewGuid().ToString();

    public const int NUM_OF_THREADS = 12;
    public ConcurrentQueue<Data> dataQueue = new ConcurrentQueue<Data>();
        
    public Server()
    {
        OnBlockMined += OnNewBlockRecived;
        //Blocks = new List<Block>();
        blockChain = new BlockChain();
        Peers = new List<Peer>();
    }
    
    public void StartServer()
    {
        Console.WriteLine("Server");
        OnNewLog?.Invoke("Starting server!");
        listenToNewPears = true;
        bool foundPort = false;
        int rubbish = 0;
        serverListener = null;
        while (!foundPort)
        {
            try
            {
                serverListener = new TcpListener(IPAddress.Any, (STD_PORT + rubbish)); // začnemo server, ki posluša na TCP zahteve na danem IP ju ter portu
                serverListener.Start();
                foundPort = true;
            }
            catch (Exception e)
            {
                rubbish++;
            }
        }

        Console.WriteLine("Listening on address: " + IPAddress.Any + ":" + (STD_PORT + rubbish));
        OnNewPortSet?.Invoke((STD_PORT + rubbish));
        while (listenToNewPears)
        {
            try
            {
                Peer peer = new Peer();
                peer.client = serverListener.AcceptTcpClient();
                peer.clientThread = new Thread(new ParameterizedThreadStart(ListenToPeer));
                peer.clientThread.IsBackground = true;
                peer.clientThread.Start(peer);
                Peers.Add(peer);
                OnNewLog?.Invoke("new peer connected!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    
    public void ConnectToPeer(IPAddress ipAddress, int port)
    {
        try
        {
            Console.WriteLine("Connecting to server");
            OnNewLog?.Invoke("Connecting to server");
            Peer peer = new Peer();
            peer.IpAddress = ipAddress;
            peer.port = port;
            peer.client = new TcpClient(); // odpremo TPC client ter se povežemo na server
            peer.client.Connect(peer.IpAddress, peer.port);
            Console.WriteLine("Connected to server with addreas: " + ipAddress.ToString() + ":" + port.ToString());

            syncBlockChainWithPeer(peer);
            
            peer.clientThread = new Thread(new ParameterizedThreadStart(ListenToPeer));
            peer.clientThread.IsBackground = true;
            peer.clientThread.Start(peer);
            Peers.Add(peer);
        } catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    void syncBlockChainWithPeer(Peer peer)
    {
        if (blockChain.getCount() == 0)
        {
            RequestBlockChain(peer);
            RequestData(peer);
        }
        else
        {
            ulong my_difficulty = calculateChainDifficulty(blockChain.getBlockChain());
            SendEncryptedPacket(peer, Command.EXCHANGE_BLOCKCHAIN_DIFFICULTY, my_difficulty.ToString());
            ulong other_difficulty = ulong.Parse(ReceiveAndDecryptPacket(peer).message);
            if (other_difficulty > my_difficulty)
            {
                RequestBlockChain(peer);
                RequestData(peer);
            }
        }
    }
    
    void ListenToPeer(object oPeer)
    {
        Peer peer = (Peer)oPeer;
        TcpClient client = peer.client;
        using NetworkStream ns = client.GetStream();
        Console.WriteLine("Client has connected: " + client.Client.RemoteEndPoint?.ToString());
        
        try
        {
            while (client.Connected)
            {
                Packet packet = ReceiveAndDecryptPacket(peer);

                Console.WriteLine("Recived network packet, with command: " + packet.command.ToString());
                Console.WriteLine("With message: " + packet.message);
                OnNewLog?.Invoke("Recived network packet, with command: " + packet.command.ToString());

                if (packet.command == Command.NEW_BLOCK)
                {
                    Block block = Block.Deserialize(packet.message);
                    if (!blockChain.addBlock(block) && !blockChain.isInBlockchain(block))
                    {
                        syncBlockChainWithPeer(peer);
                    }
                    else
                        BroadCastPacket(Peers, Command.NEW_BLOCK, packet.message);
                    if (dataQueue.TryPeek(out Data queueData) && block.data.UUID == queueData.UUID)
                    {
                        dataQueue.TryDequeue(out _);
                    }
                    //OnBlockMined?.Invoke(block);
                }
                else if (packet.command == Command.REQUEST_BLOCKCHAIN)
                {
                    SendBlockChainToPeer(peer);
                }
                else if (packet.command == Command.REQUEST_DATA)
                {
                    SendDataToPeer(peer);
                }
                else if (packet.command == Command.DISSCONECT)
                    break;
                else if (packet.command == Command.EXCHANGE_BLOCKCHAIN_DIFFICULTY)
                {
                    ulong diff = ulong.Parse(packet.message);
                    ulong my_diff = calculateChainDifficulty(blockChain.getBlockChain());
                    SendEncryptedPacket(peer, Command.EXCHANGE_BLOCKCHAIN_DIFFICULTY, my_diff.ToString());
                    if (my_diff < diff)
                    {
                        RequestBlockChain(peer);
                        dataQueue.Clear();
                        RequestData(peer);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        Console.WriteLine("Client has disconnected");
        DissconectPeer(peer);
    }

    void SendBlockChainToPeer(Peer peer)
    {
        //List<Block> old = new List<Block>();
        var Blocks = blockChain.getBlockChain();
        //do
        //{
            //old.Clear();
        foreach (Block block in Blocks)
            SendEncryptedPacket(peer, Command.REQUESTED_BLOCK, block.Serialize());
        SendEncryptedPacket(peer, Command.END_OF_SYNC);
            //Blocks = blockChain.getBlockChain();
       // } while (old != Blocks);
    }
    
    void SendDataToPeer(Peer peer)
    {
        var datas = dataQueue.ToList();
        foreach (Data data in datas)
            SendEncryptedPacket(peer, Command.REQUESTED_DATA, data.Serialize());
        SendEncryptedPacket(peer, Command.END_OF_SYNC);
    }

    void RequestBlockChain(Peer peer)
    {
        SendEncryptedPacket(peer, Command.REQUEST_BLOCKCHAIN);
        List<Block> blocks = new List<Block>();
        List<Block> missed = new List<Block>();
        while (true)
        {
            Packet packet = ReceiveAndDecryptPacket(peer);
            if (packet.command == Command.END_OF_SYNC || packet.command == Command.ERROR)
                break;
            if (packet.command == Command.REQUESTED_BLOCK)
                blocks.Add(Block.Deserialize(packet.message));
            if (packet.command == Command.NEW_BLOCK)
                missed.Add(Block.Deserialize(packet.message));
        }

        foreach (var block in missed)
            blocks.Add(block);

        blockChain.setBlockChain(blocks);
    }
    
    void RequestData(Peer peer)
    {
        SendEncryptedPacket(peer, Command.REQUEST_DATA);
        List<Data> missed = new List<Data>();
        while (true)
        {
            Packet packet = ReceiveAndDecryptPacket(peer);
            if (packet.command == Command.END_OF_SYNC || packet.command == Command.ERROR)
                break;
            if (packet.command == Command.REQUESTED_DATA)
                dataQueue.Enqueue(Data.Deserialize(packet.message));
            if (packet.command == Command.NEW_DATA)
                missed.Add(Data.Deserialize(packet.message));
        }

        foreach (var data in missed)
            dataQueue.Enqueue(data);
    }
    
    void CreateNewChain()
    {
        Console.WriteLine("Creating new blockchain");
        Block currentBlock = new Block(new Data(Guid.NewGuid().ToString(), 0,0,0, new Prediction(1,0,0)), 0);
        currentBlock.previousHash = new byte[]{0x0};
        using (var sha256 = SHA256.Create())
            currentBlock.calculateAndSetHash(sha256);
        OnBlockMined?.Invoke(currentBlock);
        //blockChain.addBlock(currentBlock);
    }

    public void StartMining()
    {
        doMining = true;
        if(blockChain.getCount() == 0)
            CreateNewChain();

        for (int i = 0; i < NUM_OF_THREADS; i++)
        {
            Thread miningThread = new Thread(Mine);
            miningThread.IsBackground = true;
            miningThread.Start();
        }
    }

    void OnNewBlockRecived(Block newBlock)
    {
        Console.WriteLine("New block received: " + newBlock.Serialize());

        if (blockChain.canBeAdded(newBlock))
        {
            Console.WriteLine("New block is valid, adding it to the chain");
            PrintByteArray(newBlock.hash);

            // Safely check and remove data from the queue if it matches
            if (dataQueue.TryPeek(out Data currentData) && currentData.UUID == newBlock.data.UUID)
            {
                dataQueue.TryDequeue(out _);
            }

            if (blockChain.addBlock(newBlock))
            {
                BroadCastPacket(Peers, Command.NEW_BLOCK, newBlock.Serialize());
            
                // Safely check and remove data from the queue again if necessary
                if (dataQueue.TryPeek(out currentData) && currentData.UUID == newBlock.data.UUID)
                {
                    dataQueue.TryDequeue(out _);
                }

                Console.WriteLine("Blockchain length: " + blockChain.getCount());
            }
        }
    }
    
    int GetCurrentDifficulty()
    {
        Block prevAdjustmentBlock = blockChain.getBlockAt(blockChain.getCount() - DIFFICULTY_ADJUSTMENT_INTERVAL);
        long timeExpected = BLOCK_GENERATION_INTERVAL * DIFFICULTY_ADJUSTMENT_INTERVAL;
        long timeTaken = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - prevAdjustmentBlock.timestamp;
        if (timeTaken < (timeExpected / 2))
            return Math.Clamp(prevAdjustmentBlock.difficulty + 1, 1, 20);
        if (timeTaken > (timeExpected * 2))
            return Math.Clamp(prevAdjustmentBlock.difficulty - 1, 1, int.MaxValue);
        return prevAdjustmentBlock.difficulty;
    }

    List<Block> solveConflict(List<Block> first, List<Block> second)
    {
        ulong firstDifficulty = calculateChainDifficulty(first);
        ulong secondDifficulty = calculateChainDifficulty(second);
        if (firstDifficulty > secondDifficulty)
            return first;
        return second;
    }

    ulong calculateChainDifficulty(List<Block> blocks)
    {
        ulong difficulty = 0;
        foreach (Block block in blocks)
            difficulty += (ulong)Math.Pow(2, block.difficulty);

        return difficulty;
    }
    
    void Mine()
    {
        Console.WriteLine("Started mining");
        Random rnd = new Random();
        SHA256 sha256 = SHA256.Create();
        while (doMining)
        {
            if (doMining && dataQueue.TryPeek(out Data data))
            {
                Data dataCopy = Data.Deserialize(data.Serialize()); // Make a copy if necessary

                Block block = new Block(dataCopy, blockChain.getLastIndex() + 1, blockChain.getLastBlock().hash);
                block.miner = minerName;
                block.nonce = rnd.Next();

                while (doMining && dataQueue.TryPeek(out Data currentData) && currentData.UUID == data.UUID)
                {
                    block.previousHash = blockChain.getLastBlock().hash;
                    block.index = blockChain.getLastIndex() + 1;
                    block.difficulty = GetCurrentDifficulty();
                    block.setTimeStamp();
                    block.calculateAndSetHash(sha256);

                    if (blockChain.canBeAdded(block))
                    {
                        OnBlockMined?.Invoke(block);

                        // Remove the processed data from the queue
                        dataQueue.TryDequeue(out _);

                        break;
                    }

                    block.nonce++;
                }
            }
            else
            {
                // Optional: Add a small delay to prevent busy-waiting
                Thread.Sleep(1000);
            }
        }
    }

    public void DissconectFromAllPeers()
    {
        try
        {
            OnNewPortSet?.Invoke(0);
            listenToNewPears = false;
            serverListener.Stop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        foreach (Peer peer in Peers)
        {
            try
            {
                SendEncryptedPacket(peer, Command.DISSCONECT);
                DissconectPeer(peer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        Peers.Clear();
    }
    
    void DissconectPeer(Peer peer)
    {
        Console.WriteLine("Dissconecting peer");
        peer.client.Close();
        Peers.Remove(peer);
    }
}