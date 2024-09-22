using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Category
    {
        public string Name {  get; set; }
        public string Description { get; set; }
    }
}
