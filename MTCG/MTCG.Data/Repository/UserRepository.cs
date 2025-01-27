using Npgsql;
using MTCG.Core.Entities;

namespace MTCG.Data.Repository;

    public static class UserRepository
    {
        /// <summary>
        /// Registriert einen neuen User in der Datenbank.
        /// </summary>
        public static void RegisterUser(string username, string password)
        {
            using var conn = Connection.GetConnection();

            //Password hashen
            string hashedPwd = BCrypt.Net.BCrypt.HashPassword(password);

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO users (username, password, coins, elo) 
                  VALUES (@uname, @pwd, 20, 100)", conn);

            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("pwd", hashedPwd);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException pex)
            {
                if (pex.SqlState == "23505")
                {
                    throw new Exception("Benutzername existiert bereits.");
                }
                throw;
            }
        }

        /// <summary>
        /// Prüft, ob ein User mit given username existiert.
        /// </summary>
        public static bool DoesUserExist(string username)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM users WHERE username = @uname;", conn);

            cmd.Parameters.AddWithValue("uname", username);

            var count = (long)cmd.ExecuteScalar()!;
            return (count > 0);
        }

        /// <summary>
        /// Liefert einen User per Username zurück (oder null wenn nicht existiert).
        /// </summary>
        public static User? GetUserByUsername(string username)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT username, password, coins, elo, is_admin, losses, wins, games 
                  FROM users
                  WHERE username = @uname;", conn);
            cmd.Parameters.AddWithValue("uname", username);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                var user = new User(
                    username: reader.GetString(0),
                    password: reader.GetString(1)
                );
                user.Coins = reader.GetInt32(2);
                user.Elo = reader.GetInt32(3);
                user.IsAdmin = reader.GetBoolean(4);
                user.Losses = reader.GetInt32(5);
                user.Wins = reader.GetInt32(6);
                user.Games = reader.GetInt32(7);

                return user;
            }
            return null;
        }

        /// <summary>
        /// Authentifiziert User (username/password).
        /// </summary>
        public static (bool Success, User? User) AuthenticateUser(string username, string password)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT username, password, coins, elo, is_admin, losses, wins, games
                  FROM users
                  WHERE username = @uname;", conn);
            cmd.Parameters.AddWithValue("uname", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string dbUser = reader.GetString(0);
                string dbHashedPwd = reader.GetString(1);
                int dbCoins = reader.GetInt32(2);
                int dbElo = reader.GetInt32(3);
                bool dbIsAdmin = reader.GetBoolean(4);
                int dbLosses = reader.GetInt32(5);
                int dbWins = reader.GetInt32(6);
                int dbGames = reader.GetInt32(7);

                bool pwOk = BCrypt.Net.BCrypt.Verify(password, dbHashedPwd);
                if (!pwOk) return (false, null);
                
                var user = new User(
                    username: dbUser,
                    password: dbHashedPwd
                );
                user.Coins = dbCoins;
                user.Elo = dbElo;
                user.IsAdmin = dbIsAdmin;
                user.Losses = dbLosses;
                user.Wins = dbWins;
                user.Games = dbGames;
                
                return (true, user);
            }
            return (false, null);
        }

        /// <summary>
        /// Speichert Änderungen eines Users.
        /// </summary>
        public static void UpdateUser(User user)
        {
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"UPDATE users
                  SET password = @pwd,
                      coins = @coins,
                      elo = @elo,
                      is_admin = @isAdmin,
                      losses = @losses,
                      wins = @wins,
                      games = @games
                  WHERE username = @uname;", conn);

            cmd.Parameters.AddWithValue("pwd", user.Password);
            cmd.Parameters.AddWithValue("coins", user.Coins);
            cmd.Parameters.AddWithValue("elo", user.Elo);
            cmd.Parameters.AddWithValue("isAdmin", user.IsAdmin);
            cmd.Parameters.AddWithValue("uname", user.Username);
            cmd.Parameters.AddWithValue("losses", user.Losses);
            cmd.Parameters.AddWithValue("wins", user.Wins);
            cmd.Parameters.AddWithValue("games", user.Games);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Gibt eine Liste aller User zurück.
        /// </summary>
        public static List<User> GetAllUsers()
        {
            var list = new List<User>();
            using var conn = Connection.GetConnection();
            using var cmd = new NpgsqlCommand(
                @"SELECT username, password, coins, elo, is_admin, losses, wins, games FROM users;", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var uname = reader.GetString(0);
                var pwd = reader.GetString(1);
                var coins = reader.GetInt32(2);
                var elo = reader.GetInt32(3);
                var isAdmin = reader.GetBoolean(4);
                int losses = reader.GetInt32(5);
                int wins = reader.GetInt32(6);
                int games = reader.GetInt32(7);

                var u = new User(uname, pwd);
                u.Coins = coins;
                u.Elo = elo;
                u.IsAdmin = isAdmin;
                u.Losses = losses;
                u.Wins = wins;
                u.Games = games;
                list.Add(u);
            }
            return list;
        }

    }