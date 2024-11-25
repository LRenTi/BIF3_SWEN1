using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using MTCG.Models;

namespace MTCG;

public class SessionHandler : Handler
{
    [Route("POST", "sessions")]
    private (int Status, JsonObject? Reply) CreateSession(HttpSvrEventArgs e)
    {
        try
        {
            var userDto = JsonSerializer.Deserialize<UserDto>(e.Payload);
            if (userDto == null) return (HttpStatusCode.BAD_REQUEST, CreateErrorReply("Invalid request."));

            var (Success, Token) = User.Logon(
                userDto.Username,
                userDto.Password
            );

            if (Success)
            {
                return (HttpStatusCode.OK, new JsonObject 
                { 
                    ["success"] = true,
                    ["message"] = "User logged in successfully.",
                    ["token"] = Token 
                });
            }

            return (HttpStatusCode.UNAUTHORIZED, new JsonObject 
            { 
                ["success"] = false,
                ["message"] = "Invalid username/password." 
            });
        }
        catch (Exception)
        {
            return (HttpStatusCode.BAD_REQUEST, CreateErrorReply("Invalid request."));
        }
    }
}