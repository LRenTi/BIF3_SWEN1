using System.Net;
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
            if (userDto == null) return (HttpStatusCode.BAD_REQUEST, null);

            User.Create(
                userDto.Username,
                userDto.Password,
                userDto.Fullname,
                userDto.Email
            );

            return (HttpStatusCode.OK, new JsonObject 
            { 
                ["success"] = true,
                ["message"] = "User created."
            });
        }
        catch (UserException ex)
        {
            return (HttpStatusCode.BAD_REQUEST, new JsonObject 
            { 
                ["success"] = false,
                ["message"] = ex.Message 
            });
        }
        catch (Exception)
        {
            return (HttpStatusCode.BAD_REQUEST, new JsonObject 
            { 
                ["success"] = false,
                ["message"] = "Invalid request." 
            });
        }
    }

    [Route("GET","users/me")]
    private (int Status, JsonObject? Reply) GetUserProfile(HttpSvrEventArgs e)
    {
        var (Success, User) = Token.Authenticate(e);
        
        if (!Success)
        {
            return (HttpStatusCode.UNAUTHORIZED, new JsonObject 
            { 
                ["success"] = false,
                ["message"] = "Unauthorized." 
            });
        }

        var userDto = new UserDto
        {
            Username = User!.UserName,
            Fullname = User.FullName,
            Email = User.EMail
        };

        return (HttpStatusCode.OK, new JsonObject
        {
            ["success"] = true,
            ["data"] = JsonSerializer.SerializeToNode(userDto)
        });
    }
}