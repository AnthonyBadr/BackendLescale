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

        public double pricedel { get; set; }

        public List<string> Ingredients = new List<string>();

        public string Type {  get; set; }

        public  List<string> Remnovals = new List<string>();


        public List<ItemIng> Addons = new List<ItemIng>();


    }
}


/*

{

    "Category": "Electronics",
  "ItemName": "Smartphone",
  "Description": "Latest model smartphone",
  "price": 999.99,
  "pricedel": 899.99,
  "Ingredients": [
    "Battery",
    "Screen",
    "Camera"
  ],
  "Type": "Gadget",
  "Remnovals": [
    "Old Battery",
    "Damaged Screen"
  ],
  "Addons": [
    {
        "id": 1,
      "name": "Wireless Charger",
      "price": 29.99
    },
    {
        "id": 2,
      "name": "Screen Protector",
      "price": 9.99
    }
  ]
}
*/