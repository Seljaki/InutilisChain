using System.Net;
using System.Net.Sockets;

namespace InutilisChain;

public class Peer
{
    public IPAddress IpAddress;
    public int port;
    public TcpClient client;
    public Thread clientThread;
}

public enum Command
{
    CONNECT,
    DISSCONECT,
    NEW_BLOCK,
    NEW_DATA,
    REQUESTED_BLOCK,
    REQUEST_BLOCKCHAIN,
    REQUEST_DATA,
    REQUESTED_DATA,
    REQUEST_LAST_BLOCK,
    EXCHANGE_BLOCKCHAIN_DIFFICULTY,
    END_OF_SYNC,
    AKNOWLDEGE,
    ERROR
}