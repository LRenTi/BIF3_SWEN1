using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Reflection;

namespace MTCG;

public class SessionHandler : Handler
{
    [Route("POST", "sessions")]
    private (int Status, JsonObject? Reply) CreateSession(HttpSvrEventArgs e)
    {
        try
        {
            JsonNode? json = JsonNode.Parse(e.Payload);
            if (json == null) return (HttpStatusCode.BAD_REQUEST, CreateErrorReply("Invalid request."));

            var (Success, Token) = User.Logon(
                (string)json["username"]!,
                (string)json["password"]!
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

    private static JsonObject CreateErrorReply(string message) => new()
    {
        ["success"] = false,
        ["message"] = message
    };

    public override bool Handle(HttpSvrEventArgs e)
    {
        var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<RouteAttribute>() != null);

        foreach (var method in methods)
        {
            var route = method.GetCustomAttribute<RouteAttribute>()!;
            var normalizedPath = e.Path.TrimEnd('/', ' ', '\t').TrimStart('/');
            
            if (route.Path == normalizedPath && route.Method == e.Method)
            {
                var result = ((int Status, JsonObject? Reply))method.Invoke(this, new[] { e })!;
                e.Reply(result.Status, result.Reply?.ToJsonString());
                return true;
            }
        }

        return false;
    }
}