using System.Text.Json.Serialization;

namespace MTCG.Models;


/// <summary>
/// This class represents a user.
/// </summary>
public class UserDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("fullname")]
    public string Fullname { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}