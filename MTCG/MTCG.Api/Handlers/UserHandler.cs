using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;
using MTCG.Models;

namespace MTCG;

public class UserHandler : Handler
{
    [Route("POST", "users")]
    private (int Status, JsonObject? Reply) CreateUser(HttpSvrEventArgs e)
    {
        try
        {
            var userDto = JsonSerializer.Deserialize<UserDto>(e.Payload);
            if (userDto == null) 
                return BadRequest("Invalid request.");

            User.Create(
                userDto.Username,
                userDto.Password,
                userDto.Fullname,
                userDto.Email
            );

            return Ok("User created.");
        }
        catch (UserException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return BadRequest("Invalid request.");
        }
    }

    [Route("GET","users/me")]
    private (int Status, JsonObject? Reply) GetUserProfile(HttpSvrEventArgs e)
    {
        var (Success, User) = Token.Authenticate(e);
        
        if (!Success)
        {
            return Unauthorized();
        }

        var userDto = new UserDto
        {
            Username = User!.UserName,
            Fullname = User.FullName,
            Email = User.EMail
        };

        return Ok(userDto);
    }
}