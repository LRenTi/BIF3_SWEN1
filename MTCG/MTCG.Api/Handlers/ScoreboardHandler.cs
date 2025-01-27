using System.Linq;
using System.Text.Json.Nodes;

using MTCG.Core.Entities;
using MTCG.Data.Repository;
using MTCG.Security;


namespace MTCG;

    public class ScoreBoardHandler : Handler, IHandler
    {

        [Route("GET", "scoreboard")]
        private (int Status, JsonObject? Reply) GetScoreboard(HttpSvrEventArgs e)
        {

            var users = UserRepository.GetAllUsers() ?? new List<User>();

            var sorted = users.OrderByDescending(u => u.Elo).ToList();
            JsonObject reply = new();
            JsonArray arr = new();
            foreach (var u in sorted)
            {
                var userObject = new JsonObject
                {
                    ["username"] = u.Username,
                    ["elo"] = u.Elo,
                    ["games"] = u.Games
                };

                if (u.Losses > 0)
                {
                    userObject["winrate"] = (double)u.Wins / u.Losses;
                }
                else
                {
                    userObject["winrate"] = u.Wins > 0 ? 1.0 : 0.0;
                }

                arr.Add(userObject);
            }
            reply["scoreboard"] = arr;
            
            return Ok(reply);
        }
    }