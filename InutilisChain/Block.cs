using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InutilisChain;

public class Block
{
    public int index { get; set; }
    public long timestamp { get; set; }
    public byte[] hash { get; set; }
    public byte[] previousHash { get; set; }
    public int nonce { get; set; }
    public int difficulty { get; set; }
    public string miner { get; set; }
    public Data data { get; set; }
    public Block()
    {
    }
    public Block(Data data, int index)
    {
        this.index = index;
        nonce = 0;
        difficulty = 1;
        this.data = data;
        setTimeStamp();
        //setData();
    }
    
    public Block(Data data, int index, byte[] previousHash) : this(data, index)
    {
        this.previousHash = previousHash;
    }
    
    public void setTimeStamp()
    {
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
    
    public void calculateAndSetHash(SHA256 sha256)
    {
        hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(index + data.Serialize() + timestamp + previousHash + nonce + difficulty));
    }
    
    public string Serialize() => JsonSerializer.Serialize(this);
    public static Block Deserialize(string json) => JsonSerializer.Deserialize<Block>(json);
}