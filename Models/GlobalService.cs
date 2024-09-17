using MongoDB.Driver;
using System;
using backend.Models;

namespace backend.Services
{
    public class GlobalService
    {
        private readonly IMongoDatabase _database;
        public  string username { get; set; } = "N/A";

        

        public GlobalService(IMongoDatabase database)
        {
            _database = database;
        }

        public void LogAction(string description, string action)
        {
            if (_database == null)
            {
                throw new InvalidOperationException("Database is not initialized.");
            }

            // Create a log entry
            var log = new Logs
            {
                action = action,
                description = description,
                bywho = username,
                date = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            // Get the collection for logs
            var logCollection = _database.GetCollection<Logs>("Logs");

            // Insert the log into the collection
            logCollection.InsertOne(log);
        }
    }
}
