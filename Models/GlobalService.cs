using MongoDB.Driver;
using System;
using backend.Models;
using MongoDB.Bson;

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

        public async Task<int> SequenceIncrement(string _id)
        {
            var counterCollection = _database.GetCollection<BsonDocument>("Sequence");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", _id); // Use _id parameter
            var update = Builders<BsonDocument>.Update.Inc("sequenceValue", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                ReturnDocument = ReturnDocument.After // Return the updated document
            };

            // Find and increment the sequence value
            var counterDocument = await counterCollection.FindOneAndUpdateAsync(filter, update, options);
            var newSequenceValue = counterDocument["sequenceValue"].AsInt32;

            return newSequenceValue;
        }




    }
}
