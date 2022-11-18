using System.Text.Json.Serialization;

namespace LibrelancerAuthentication.Captcha;

public class RenderedCaptcha
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("background")]
    public string Background { get; set; }
    
    [JsonPropertyName("piece")]
    public string Piece { get; set; }
    
    
    [JsonPropertyName("y")]
    public int Y { get; set; }
}