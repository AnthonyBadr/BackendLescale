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

