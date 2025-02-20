﻿using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using MPI;
using static InutilisChain.Common;

namespace InutilisChain;

public class Miner
{
    
}

public class BlockchainServer
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

    public const int NUM_OF_THREADS = 1;
    public Queue<Data> dataQueue = new Queue<Data>();
        
    public BlockchainServer()
    {
        OnBlockMined += OnNewBlockRecived;
        //Blocks = new List<Block>();
        blockChain = new BlockChain();
        Peers = new List<Peer>();
    }
    
    public void StartServer()
    {
        Console.WriteLine("BlockchainServer");
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

    public void onNewData(Data data)
    {
        if (dataQueue.Count == 0 || dataQueue.Count > 0 && dataQueue.Peek().UUID != data.UUID)
        {
            BroadCastPacket(Peers, Command.NEW_DATA, data.Serialize());
            dataQueue.Enqueue(data);
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
                    if (dataQueue.Count > 0 && block.data.UUID == dataQueue.Peek().UUID)
                        dataQueue.Dequeue();
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
                else if (packet.command == Command.NEW_DATA)
                {
                    onNewData(Data.Deserialize(packet.message));
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

    public void StartMining(int rank)
    {
        doMining = true;
        if(blockChain.getCount() == 0)
            CreateNewChain();
        MPIServer(rank);
    }

    void OnNewBlockRecived(Block newBlock)
    {
        //if(Blocks.Last().index =< newBlock.index)
        Console.WriteLine("New block recieved: " + newBlock.Serialize());
        if (blockChain.canBeAdded(newBlock))
        {
            Console.WriteLine("New block is valid, adding it to the chain");
            PrintByteArray(newBlock.hash);
            if (dataQueue.Count > 0 && dataQueue.Peek().UUID == newBlock.data.UUID)
                dataQueue.Dequeue();
            if(blockChain.addBlock(newBlock))
            {
                BroadCastPacket(Peers, Command.NEW_BLOCK, newBlock.Serialize());
                // remove data from the stack
                if (dataQueue.Count > 0 && dataQueue.Peek().UUID == newBlock.data.UUID)
                    dataQueue.Dequeue();
                Console.WriteLine("Blockchain lenght: " + blockChain.getCount());
            }
        }
    }
    
    static int GetCurrentDifficulty(Block prevAdjustmentBlock)
    {
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

    private void MPIServer(int rank)
    {
        using (var comm = Intracommunicator.world)
        {
            while (true)
            {
                try
                {
                    CompletedStatus status;
                    string request;
                    comm.Receive<string>(Communicator.anySource, Communicator.anyTag, out request, out status); // Listen for messages with tag 100
                    int senderRank = status.Source;
                    //Console.WriteLine("Got request " + request);

                    if (request == "get_data")
                    {
                        if (doMining && dataQueue.Count > 0)
                        {
                            Data data = dataQueue.Peek();
                            Block block = blockChain.getLastBlock();
                            DataAndBlock db = new DataAndBlock(data, block);
                            //Console.WriteLine("Sending data block " + db.Serialize());
                            comm.Send(db.Serialize(), senderRank, 1);
                        }
                        else
                        {
                            comm.Send("", senderRank, 1);
                        }
                    }
                    else if (request.StartsWith("block_mined"))
                    {
                        string jsonBlock = request.Substring("block_mined".Length);
                        Block minedBlock = Block.Deserialize(jsonBlock);
                        if (dataQueue.Peek().UUID == minedBlock.data.UUID && blockChain.canBeAdded(minedBlock))
                        {
                            OnBlockMined?.Invoke(minedBlock);
                            for (int i = 1; i < Communicator.world.Size; i++)
                                if (i != senderRank)
                                    comm.Send("stop", i, 300); // Tag 300 for stop signal

                            dataQueue.Dequeue();
                            Console.WriteLine($"Block added from miner with rank: {minedBlock.miner}");
                        }
                        else
                        {
                            //Console.WriteLine($"Got invalid block from rank: {minedBlock.miner}");
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e);
                }
            }
        }
        Console.WriteLine("Stopped server");
    }
    
    public static void MPIClient(int rank)
    {
        Random rnd = new Random();
        SHA256 sha256 = SHA256.Create();
        Console.WriteLine("Starting client " + rank);
        using (var comm = Intracommunicator.world)
        {
            while (true)
            {
                //Console.WriteLine("Sending get data request " + rank);
                comm.Send("get_data", 0, 100);
                string data = comm.Receive<string>(0, Communicator.anyTag);
                //Console.WriteLine("Got data " + data);

                if (string.IsNullOrEmpty(data) || data == "stop")
                {
                    continue;
                }

                DataAndBlock db = DataAndBlock.Deserialize(data);
                
                Block block = new Block(db.data, db.block.index + 1, db.block.hash);
                block.miner = "miner" + rank;

                Boolean doMining = true;
                int threadCount = System.Environment.ProcessorCount;
                Thread[] miners = new Thread[threadCount];

                // Start mining threads
                for (int i = 0; i < threadCount; i++)
                {
                    int threadIndex = i; // To avoid closure issue
                    miners[i] = new Thread(() => Miner(block.Serialize(), db.block, comm, ref doMining));
                    miners[i].Start();
                }
                string stopSignal = comm.Receive<string>(0, 300);
                if (stopSignal == "stop")
                {
                    //Console.WriteLine($"Miner {rank} received stop signal. Halting mining.");
                    doMining = false;
                }
                foreach (var miner in miners)
                {
                    miner?.Join();
                }

                //Console.WriteLine($"Miner {rank} stopped all threads.");
            }
        }
        Console.WriteLine("Stopped client");
    }

    private static void Miner(String blockJson, Block lastBlock, Intracommunicator comm, ref Boolean doMining)
    {
        Random rnd = new Random();
        SHA256 sha256 = SHA256.Create();
        Block block = Block.Deserialize(blockJson);
        //Console.WriteLine("Starting thread");
        while (doMining)
        {
            block.nonce = rnd.Next();
            block.difficulty = GetCurrentDifficulty(lastBlock);
            block.setTimeStamp();
            block.calculateAndSetHash(sha256);

            if (BlockChain.isValidBlock(lastBlock, block) && doMining)
            {
                doMining = false; // Stop other threads
                comm.Send($"block_mined{block.Serialize()}", 0, 200);
                //Console.WriteLine($"Miner {rank} found a valid block.");
            }
        }
        //Console.WriteLine("Stopping thread");
    }
    void DissconectPeer(Peer peer)
    {
        Console.WriteLine("Dissconecting peer");
        peer.client.Close();
        Peers.Remove(peer);
    }
}