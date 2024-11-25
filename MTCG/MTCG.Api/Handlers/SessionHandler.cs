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
            if (userDto == null) 
                return BadRequest("Invalid request.");

            var (Success, Token) = User.Logon(
                userDto.Username,
                userDto.Password
            );

            if (Success)
            {
                return TokenOk(Token, "User logged in successfully.");
            }

            return Unauthorized("Invalid username/password.");
        }
        catch (Exception)
        {
            return BadRequest("Invalid request.");
        }
    }
}