using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using MTCG.Models;
using MTCG.Security;

namespace MTCG;

/// <summary>
/// This class handles session-related requests.
/// </summary>
public class SessionHandler : Handler
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public static methods                                                                                            //
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>Creates a session.</summary>
    /// <param name="e">Event arguments.</param>
    /// <returns>Returns TRUE.</returns>
        
        [Route("POST", "sessions")]
        public static bool _CreateSession(HttpSvrEventArgs e)
        {
            JsonObject? reply = new JsonObject() { ["success"] = false, ["message"] = "Invalid request." };
            int status = HttpStatusCode.BAD_REQUEST;                            // initialize response

            try
            {
                JsonNode? json = JsonNode.Parse(e.Payload);                     // parse payload
                if(json != null)
                {
                    Session ses = Session.Create((string?) json["username"] ?? "", (string?) json["password"] ?? "");

                    if(ses.Valid)
                    {
                        status = HttpStatusCode.OK;
                        reply = new JsonObject() { ["success"] = true,
                                                   ["token"] = ses.Token };
                    }
                    else
                    {
                        reply = new JsonObject() { ["success"] = false,
                                                   ["message"] = "Invalid user name or password" };
                    }
                }
            }
            catch(Exception) 
            {                                                                   // handle unexpected exception
                reply = new JsonObject() { ["success"] = false, ["message"] = "Unexpected error." };
            }

            e.Reply(status, reply?.ToJsonString());                             // send response
            return true;
        }
}