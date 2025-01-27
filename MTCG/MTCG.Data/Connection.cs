using Npgsql;
using System;

namespace MTCG.Data;

    public static class Connection
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=MTCG;Username=postgres;Password=admin";
        
        public static NpgsqlConnection GetConnection()
        {
            var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }