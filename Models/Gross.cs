using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Gross
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public double totalGross { get; set; }

        public string dateofGrossPay { get; set; }

        public string status { get; set; }

        public int grossNumber { get; set; }
    }
}
    