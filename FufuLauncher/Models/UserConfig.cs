using System.Text.Json.Serialization;

namespace FufuLauncher.Models;

public class UserConfig
{
    [JsonPropertyName("cookie")]
    public string Cookie { get; set; } = "";
    
    [JsonPropertyName("stuid")]
    public string Stuid { get; set; } = "";
    
    [JsonPropertyName("stoken")]
    public string Stoken { get; set; } = "";
    
    [JsonPropertyName("mid")]
    public string Mid { get; set; } = "";
}