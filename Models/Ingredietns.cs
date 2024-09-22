using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Ingredietns
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string IngredientCategory { get; set; }
        public List<ItemIng> Items = new List<ItemIng>();
    }
}
