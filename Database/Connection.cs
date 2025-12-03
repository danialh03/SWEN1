using System;
using Npgsql;

namespace DhProjekt.Database
{
    public class DatabaseConnection
    {

        private readonly string _connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=dhp";


        public NpgsqlConnection OpenConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public void TestConnection()
        {
            try
            {
                using var connection = OpenConnection();
                Console.WriteLine("Datenbankverbindung OK.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Datenbankverbindung fehlgeschlagen:");
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
