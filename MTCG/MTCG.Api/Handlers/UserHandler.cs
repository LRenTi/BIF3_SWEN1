using System;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using MTCG.Core.Entities;
using System.Text.Json;
using System.Security;
using MTCG.Models;
using MTCG.Security;

namespace MTCG;


public class UserHandler : Handler, IHandler
{
    [Route("POST", "users")]
    private async Task<(int Status, JsonObject? Reply)> _CreateUser(HttpSvrEventArgs e)
    {
        JsonObject reply = new() { ["success"] = false, ["message"] = "Ungültige Anfrage." };
        int status = HttpStatusCode.BAD_REQUEST;

        try
        {
            JsonNode? json = JsonNode.Parse(e.Payload);
            if (json != null)
            {
                string username = (string)json["username"]!;
                string password = (string)json["password"]!;

                await User.Register(username, password);

                return Ok("User created.");
            }
            else
            {
                return BadRequest("Invalid payload.");
            }
        }
        catch (SecurityException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest("Unexpected error: " + ex);
        }

    }

    [Route("GET", "users/{username}")]
    private async Task<(int status, JsonObject? Reply)> _QueryUser(HttpSvrEventArgs e)
    {
        try
        {
            
            if (!e.RouteParams.TryGetValue("username", out var username))
                return BadRequest("No username provided?");

            (bool Success, User? User) auth = await Token.Authenticate(e);
            
            JsonObject? reply = null;

            if (!auth.Success)
            {
                return Unauthorized();
            }

            User? user = User.Get(username);
            if (user == null)
            {
                reply["message"] = "User not found!";
                return (404, reply);
                
            }
            else
            {
                JsonObject json = new()
                {
                    ["username"] = user.Username,
                    ["coins"] = user.Coins,
                    ["elo"] = user.Elo,
                    ["wins"] = user.Wins,
                    ["losses"] = user.Losses,
                    ["games"] = user.Games,
                };
                return (HttpStatusCode.OK, json);
            }
        }
        catch (Exception ex)
        {
            return BadRequest("Something went wrong: " + ex.Message);
        }
    }
    
}