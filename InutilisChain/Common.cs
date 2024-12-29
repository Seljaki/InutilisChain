using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InutilisChain;

public class Packet
{
    public Command command { get; set; }
    public string message { get; set; }
}

public static class Common
{
    public const int STD_MSG_SIZE = 4096;
    public const string ENCRYPTION_KEY = "15-slc";
    public const int BLOCK_GENERATION_INTERVAL = 600; // v sekundah, 1 minuta
    public const int DIFFICULTY_ADJUSTMENT_INTERVAL = 20;

    public static bool isValidBlock(Block lastBlock, Block newBlock)
    {
        
        
        if (!VerifyTimeAgainstPreviousBlock(lastBlock, newBlock) || !VerifyTimeWithLocalTime(newBlock))
            return false;

        //PrintByteArray(newBlock.hash);
        for(int i = 0; i < newBlock.difficulty; i++)
            if (newBlock.hash[i] != '0')
                return false;


        if (newBlock.index != lastBlock.index + 1)
            return false;

        if (!newBlock.previousHash.SequenceEqual(lastBlock.hash))
            return false;
        
        return true;
    }

    public static bool VerifyTimeAgainstPreviousBlock(Block previousBlock, Block newBlock)
    {
        if (newBlock.timestamp - previousBlock.timestamp > 60)
            return false;
        return true;
    }
    
    public static bool VerifyTimeWithLocalTime(Block newBlock)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - newBlock.timestamp > 60)
            return false;
        return true;
    }
    
    public static void PrintByteArray(byte[] bytes)
    {
        var sb = new StringBuilder("new byte[] { ");
        foreach (var b in bytes)
        {
            sb.Append(b + ", ");
        }
        sb.Append("}");
        Console.WriteLine(sb.ToString());
    }
    
    public static void BroadCastPacket(List<Peer> peers, Command command, string message = "")
    {
        try
        {
            foreach (Peer peer in peers)
                SendEncryptedPacket(peer, command, message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public static void SendEncryptedPacket(Peer peer, Command command, string message = "")
    {
        string json = CreateJsonPacket(command, message);
        json = EncryptMessage(json, ENCRYPTION_KEY);
        SendMessage(peer.client.GetStream(), json);
        if(command != Command.AKNOWLDEGE)
            ReceiveAndDecryptPacket(peer);
        //Console.WriteLine("Sent packet: " + command.ToString());
    }
    
    public static Packet ReceiveAndDecryptPacket(Peer peer)
    {
        string json = ReceiveMessage(peer.client.GetStream());
        json = DecryptMessage(json, ENCRYPTION_KEY);
        Packet packet =  DeserializePacket(json);
        if (packet.command != Command.AKNOWLDEGE)
            SendEncryptedPacket(peer, Command.AKNOWLDEGE);
        //Console.WriteLine("Recived packet: " + packet.command.ToString());
        return packet;
    }
    
    public static Packet DeserializePacket(string json)
    {
        return JsonSerializer.Deserialize<Packet>(json);
    }
    
    public static string CreateJsonPacket(Command command, string message = "")
    {
        Packet packet = new Packet();
        packet.command = command;
        packet.message = message;

        return SerializePacket(packet);
    }
    
    public static string SerializePacket(Packet packet)
    {
        return JsonSerializer.Serialize(packet);
    }
    
    public static string EncryptMessage(string message, string key) // encrypts a message in trippleDES encryption
    {
        // vir: https://dotnetcodr.com/2015/10/23/encrypt-and-decrypt-plain-string-with-triple-des-in-c/
        TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
        MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

        byte[] byteHash;
        byte[] byteBuff;

        byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key)); // key pretvorimo v hash
        desCryptoProvider.Key = byteHash;
        desCryptoProvider.Mode = CipherMode.ECB; // CBC, CFB
        byteBuff = Encoding.UTF8.GetBytes(message); // pretvorimoi sporočilo v zloge

        return Convert.ToBase64String(desCryptoProvider.CreateEncryptor()
            .TransformFinalBlock(byteBuff, 0, byteBuff.Length));
    }

    public static string DecryptMessage(string message, string key)
    {
        // vir: https://dotnetcodr.com/2015/10/23/encrypt-and-decrypt-plain-string-with-triple-des-in-c/
        TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
        MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

        byte[] byteHash;
        byte[] byteBuff;

        byteHash = hashMD5Provider.ComputeHash(Encoding.UTF8.GetBytes(key)); // key pretvorimo v hash
        desCryptoProvider.Key = byteHash;
        desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
        byteBuff = Convert.FromBase64String(message); // dekodiramo zloge iz BASE64 formata

        return Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor()
            .TransformFinalBlock(byteBuff, 0, byteBuff.Length));
    }

    public static string? ReceiveMessage(NetworkStream ns) // dekodiramo zloge v string utf8 znakov
    {
        try
        {
            byte[] recv = new byte[STD_MSG_SIZE];
            int length = ns.Read(recv, 0, recv.Length);
            return Encoding.UTF8.GetString(recv, 0, length); // dekodiramo iz zlogov v UTF8 kopdirane znake
        }
        catch (Exception e)
        {
            Console.WriteLine("Error at receiving message: " + e.Message);
            Console.WriteLine(e.StackTrace);
            return null;
        }
    }
    
    
    public static void SendMessage(NetworkStream ns, string message)
    {
        try
        {
            if (message.Length == 0) throw new Exception("Message is empty!");
            byte[] send = Encoding.UTF8.GetBytes(message); // kodiramo znake v zloge
            if (send.Length > STD_MSG_SIZE)
                throw new Exception("Message too long to send: " + send.Length); // preverimo dolžino sporočila
            ns.Write(send, 0, send.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error at sending message: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }
}