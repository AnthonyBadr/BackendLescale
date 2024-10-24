using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Table
    {
       public int ?Id { get; set; }

        public string ?TableNumber { get; set; }

        public string? Status { get; set; }

        public string ?TableType { get; set; }


    }
}
