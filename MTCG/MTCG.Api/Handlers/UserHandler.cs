using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;

namespace MTCG;

    public class UserHandler : Handler, IHandler
    {
        [Route("POST","users")]
        private (int Status, JsonObject? Reply) CreateUser(HttpSvrEventArgs e)
        {
            try
            {
                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json == null) return (HttpStatusCode.BAD_REQUEST, null);

                User.Create(
                    (string)json["username"]!,
                    (string)json["password"]!,
                    (string?)json["fullname"] ?? "",
                    (string?)json["email"] ?? ""
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

        [Route("GET", "users/me")]
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

            return (HttpStatusCode.OK, new JsonObject 
            { 
                ["success"] = true,
                ["username"] = User!.UserName,
                ["fullname"] = User!.FullName,
                ["email"] = User!.EMail 
            });
        }

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