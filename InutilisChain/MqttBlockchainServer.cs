using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Server;

namespace InutilisChain;

public class MqttBlockchainServer
{
    private IMqttServer mqttServer;

    public MqttBlockchainServer()
    {
    }

    public async void StartServer()
    {
        Console.WriteLine("Starting MQTT Blockchain Server...");
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

        Console.WriteLine("MQTT Blockchain Server is running");
    }

    public void StopServer()
    {
        mqttServer.StopAsync();
    }

    static void AddDataToBlockchain(string jsonData)
    {
        try
        {
            //Console.WriteLine(jsonData);
            var data = Data.Deserialize(jsonData);
            Console.WriteLine(data.UUID);
            //var newData = JsonSerializer.Deserialize<BlockData>(jsonData);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add data to blockchain: {ex.Message}");
        }
    }

    static void SendBlockchain(string clientId, IMqttServer mqttServer)
    {
        try
        {
            var blockchainJson = "test_data";//JsonSerializer.Serialize(Blockchain);
            Console.WriteLine("client: " + clientId);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"blockchain/response/{clientId}")
                .WithPayload(blockchainJson)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            mqttServer.PublishAsync(message);
            Console.WriteLine("Blockchain sent to client.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send blockchain: {ex.Message}");
        }
    }
}