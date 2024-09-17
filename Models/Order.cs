using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace backend.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
     public  string Id { get; set; }
        public string type { get; set; }

        public string dateofOrder { get; set; }


        public string ordernumber { get; set; }
        public string status { get; set; }

        public List<Item> items { get; set; }

        public string tablenumber {  get; set; }

        public double totalprice { get; set; }

        public int grossNumber { get; set; }



    }
}
