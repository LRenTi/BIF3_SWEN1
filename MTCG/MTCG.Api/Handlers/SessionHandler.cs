using MTCG.Data.Repository;
using MTCG.Core.Entities;
using MTCG;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;

namespace MTCG;

    public class SessionHandler : Handler, IHandler
    {
        
        [Route("POST", "sessions")]
        private async Task<(int Status, JsonObject? Reply)> _CreateSession(HttpSvrEventArgs e)
        {
            JsonObject? reply = null;

            try
            {
                JsonNode? json = JsonNode.Parse(e.Payload);
                if (json != null)
                {
                    string username = (string)json["username"]!;
                    string password = (string)json["password"]!;

                    (bool success, User? user) = await User.Authenticate(username, password);

                    if (success)
                    {
                        string token = await Security.Token.GenerateToken(user!);
                        
                            reply = new JsonObject()
                            {
                                ["token"] = token
                            };

                            return Ok(reply);
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
            return Ok(reply);
        }
    }
