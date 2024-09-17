using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Item
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Category {  get; set; }

        public string ItemName { get; set; }
        public string Description { get; set; }

        public double price { get; set; }

        public List<string> Ingredients = new List<string>();

        public string Type {  get; set; }




    }
}
