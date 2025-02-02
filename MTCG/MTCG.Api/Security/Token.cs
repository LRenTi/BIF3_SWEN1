using System.Collections.Generic;
using MTCG;
using MTCG.Core.Entities;

namespace MTCG.Security
{
    public static class Token
    {
        private static Dictionary<string, User> _tokens = new();

        public static async Task<string> GenerateToken(User user)
        {
            
            string randomToken = await Task.FromResult(Guid.NewGuid().ToString());
            _tokens[randomToken] = user;
            return randomToken;
        }

        public static async Task<(bool Success, User? User)> Authenticate(string token)
        {
            if (_tokens.TryGetValue(token, out User? user))
            {
                return (true, user);
            }
            return (false, null);
        }

        public static async Task<(bool Success, User? User)> Authenticate(HttpSvrEventArgs e)
        {
            foreach (var header in e.Headers)
            {
                if (header.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) &&
                    header.Value.StartsWith("Bearer "))
                {
                    string token = header.Value.Substring(7).Trim();
                    return await Authenticate(token);
                }
            }
            return (false, null);
        }
    }
}