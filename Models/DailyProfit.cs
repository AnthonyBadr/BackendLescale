using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace backend.Models
{
    public class DailyProfit
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id { get; set; }
        public DateOnly DateOnly { get; set; }
     

        public double amount { get; set; }

    }
}

