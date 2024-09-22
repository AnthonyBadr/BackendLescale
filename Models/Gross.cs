using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Gross
    {

        public int GrossNumber { get; set; }
        public double TotalGross { get; set; }

        public string DateOfCreation { get; set; }

        public string DateOfClose { get; set; }

        public string Status { get; set; }

        
    }
}
    