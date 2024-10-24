using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Category
    {
        public string? Name { get; set; } // Nullable string
        public string? Description { get; set; } // Nullable string
    }
}
