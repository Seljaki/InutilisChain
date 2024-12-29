using System.Text.Json;

namespace InutilisChain;

public class Prediction
{
    public Double clear { get; set; }
    public Double cloudy { get; set; }
    public Double rainy { get; set; }

    public Prediction(double clear, double cloudy, double rainy)
    {
        this.clear = clear;
        this.cloudy = cloudy;
        this.rainy = rainy;
    }
}

public class Data
{
    public string UUID { get; set; }
    public Double temperature { get; set; }
    public Double longitude { get; set; }
    public Double latitude { get; set; }
    public Prediction prediction { get; set; }

    public Data(string UUID, double temperature, double longitude, double latitude, Prediction prediction)
    {
        this.temperature = temperature;
        this.longitude = longitude;
        this.latitude = latitude;
        this.prediction = prediction;
        this.UUID = UUID;
    }
    
    public string Serialize() => JsonSerializer.Serialize(this);
    public static Data Deserialize(string json) => JsonSerializer.Deserialize<Data>(json);
}