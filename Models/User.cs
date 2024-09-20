using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class User
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _Id { get; set; }

        public string username { get; set; }
        public string pin { get; set; }

       public string role { get; set; }


        public int Id { get; set; }

    }
}





