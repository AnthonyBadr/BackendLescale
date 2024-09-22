using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace backend.Models
{
    public class Item
    {
       

        public string CategoryName {  get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public double PriceDineIn { get; set; }

        public double PriceDelivery { get; set; }

        public List<string> Ingredients = new List<string>();



        public  List<string> Removals = new List<string>();


        public List<ItemIng> AddOns = new List<ItemIng>();


    }
}


/*
 * {
  "Type": "Dine-In",
  "DateOfOrder": "2023-09-22T14:30:00Z",
  "OrderNumber": "ORD12345",
  "Status": "Pending",
  "Items": [
    {
      "CategoryName": "Appetizer",
      "Name": "Spring Rolls",
      "Description": "Crispy spring rolls with vegetables.",
      "PriceDineIn": 5.99,
      "PriceDelivery": 6.99,
      "Ingredients": [
        "Cabbage",
        "Carrots",
        "Glass Noodles"
      ],
      "Removals": [],
      "AddOns": []
    },
    {
      "CategoryName": "Main Course",
      "Name": "Chicken Curry",
      "Description": "Spicy chicken curry with rice.",
      "PriceDineIn": 12.99,
      "PriceDelivery": 13.99,
      "Ingredients": [
        "Chicken",
        "Curry Sauce",
        "Rice"
      ],
      "Removals": [],
      "AddOns": []
    }
  ],
  "TableNumber": "5",
  "TotoalPrice": 18.98,
  "GrossNumber": 1,
  "DeleiveryCharge": 2.50,
  "Location": "123 Main St, Cityville",
  "CreatedBy": "user@example.com",
  "Quantity": 2
}
*/