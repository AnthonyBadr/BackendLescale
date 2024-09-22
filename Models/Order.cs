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
        public string Type { get; set; }

        public string DateOfOrder { get; set; }


        public int OrderNumber { get; set; }
        public string Status { get; set; }

        public List<Item> Items { get; set; }   

        public string TableNumber {  get; set; }

        public double TotoalPrice { get; set; }

        public int GrossNumber { get; set; }


        public double DeleiveryCharge { get; set; }

        public string Location { get; set; }


        public string Created_by {  get; set; }

        public int Quantity { get; set; }   

        public double TotalOldPrice {  get; set; }


    }
}
