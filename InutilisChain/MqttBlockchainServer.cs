using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Server;

namespace InutilisChain;

public class MqttBlockchainServer
{
    private IMqttServer? mqttServer;
    private BlockchainServer blockChainServer;

    public MqttBlockchainServer(BlockchainServer blockChainServer)
    {
        this.blockChainServer = blockChainServer;
        blockChainServer.OnBlockMined += NotifySubscribersOfNewBlock;
    }

    public void Deconstruct()
    {
        blockChainServer.OnBlockMined -= NotifySubscribersOfNewBlock;
    }

    public async void StartServer()
    {
        Console.WriteLine("Starting MQTT Blockchain BlockchainServer...");
        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(3001)
            .Build();

        mqttServer = new MqttFactory().CreateMqttServer();

        mqttServer.UseApplicationMessageReceivedHandler(e =>
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            if (topic == "blockchain/add")
            {
                AddDataToBlockchain(payload);
            }
            else if (topic == "blockchain/get")
            {
                SendBlockchain(e.ClientId, mqttServer);
            }
        });

        await mqttServer.StartAsync(mqttServerOptions);

        Console.WriteLine("MQTT Blockchain BlockchainServer is running");
    }

    public void StopServer()
    {
        mqttServer.StopAsync();
    }

    void AddDataToBlockchain(string jsonData)
    {
        try
        {
            //Console.WriteLine(jsonData);
            var data = Data.Deserialize(jsonData);
            Console.WriteLine(data.Serialize());
            blockChainServer.onNewData(data);
            //var newData = JsonSerializer.Deserialize<BlockData>(jsonData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add data to blockchain: {ex.Message}");
        }
    }

    void SendBlockchain(string clientId, IMqttServer mqttServer)
    {
        try
        {
            var blockchainJson = JsonSerializer.Serialize(blockChainServer.blockChain.getBlockChain());
            Console.WriteLine("client: " + clientId);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"blockchain/response/{clientId}")
                .WithPayload(blockchainJson)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .Build();

            mqttServer.PublishAsync(message);
            Console.WriteLine("Blockchain sent to client.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send blockchain: {ex.Message}");
        }
    }
    
    public void NotifySubscribersOfNewBlock(Block newBlock)
    {
        if(mqttServer == null) return;
        try
        {
            var messageJson = newBlock.Serialize();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("blockchain/newBlock")
                .WithPayload(messageJson)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .Build();

            mqttServer.PublishAsync(message).Wait(); // Ensure the message is sent
            Console.WriteLine("All subscribers notified of new block.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to notify subscribers: {ex.Message}");
        }
    }
}