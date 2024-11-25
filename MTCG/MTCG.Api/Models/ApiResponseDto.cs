using System.Text.Json.Serialization;

namespace MTCG.Models;

/// <summary>
/// This class defines the structure of an API response.
/// </summary>
public class ApiResponseDto<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    // Hilfsmethoden für häufige Antworttypen
    public static ApiResponseDto<T> SuccessResponse(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ApiResponseDto<T> SuccessResponse(string message) => new()
    {
        Success = true,
        Message = message
    };

    public static ApiResponseDto<T> ErrorResponse(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static ApiResponseDto<T> TokenResponse(string token, string message) => new()
    {
        Success = true,
        Token = token,
        Message = message
    };
} 