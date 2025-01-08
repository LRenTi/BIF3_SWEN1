using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Security;
using MTCG.Models;
using MTCG.Security;

namespace MTCG;

/// <summary>
/// This class handles user-related requests.
/// </summary>
public class UserHandler : Handler, IHandler
{
    [Route("POST", "users")]
    private (int Status, JsonObject? Reply) _CreateUser(HttpSvrEventArgs e)
        {
            try
            {
                JsonNode? json = JsonNode.Parse(e.Payload);                     // parse payload
                if(json != null)
                {
                    User user = new()                                           // create user object
                    {
                        UserName = (string?) json["username"] ?? "",
                        FullName = (string?) json["name"] ?? "",
                        EMail = (string?) json["email"] ?? ""
                    };
                    user.BeginEdit(Session.From(e));                            // edit user with session

                    user.IsAdmin = (bool?) json["admin"] ?? false;              // set admin flag                    
                    user.Save((string?) json["password"] ?? "12345");           // save user object
                    
                    user.EndEdit();                                             // end editing
                    
                    return Ok("User created.");
                }
                
                return BadRequest(e.Payload);
            }
            catch(SecurityException ex)
            {
                return BadRequest(ex.Message);
            }
            catch(Exception ex)
            {
                return BadRequest("Unexpected error: " + ex);
            }
            
        }
    
    [Route("GET","users/me")]
    private (int Status, JsonObject? Reply) GetUserProfile(HttpSvrEventArgs e)
    {
        /*var (Success, User) = .Authenticate(e);
        
        if (!Success)
        {
            return Unauthorized();
        }*/
        Console.WriteLine("Start");

        var userDto = new UserDto
        {
            Username = "test"
        };

        return Ok(userDto);
    }
}