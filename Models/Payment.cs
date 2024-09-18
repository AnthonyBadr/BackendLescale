using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Payment
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Type { get; set; }
        public double amount { get; set; }
        public string reason { get; set; }


        public string date {  get; set; }

        public int grossnumber {  get; set; }



    }
}
