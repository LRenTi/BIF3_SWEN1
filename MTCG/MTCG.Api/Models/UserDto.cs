namespace MTCG;

public class CreateUserDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Fullname { get; set; }
    public string? Email { get; set; }
}

public class UserResponseDto
{
    public string Username { get; set; } = "";
    public string Fullname { get; set; } = "";
    public string Email { get; set; } = "";
} 