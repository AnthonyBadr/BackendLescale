using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using backend.Services;
using System.Xml.Linq;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections;
using System.Text.RegularExpressions;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;
        public OrderController(ILogger<OrderController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }
   
        public IActionResult Index()
        {
            int x = 0;
            var collection = _database.GetCollection<BsonDocument>("Order");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to JSON
            var jsonResult = documents.Select(doc => doc.ToJson()).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        //        {

        //  "type": "dine in",
        //  "status": "pending",
        //  "tablenumber":"2",
        //  "items": [
        //    {

        //      "Category": "Beverages",
        //      "ItemName": "Coffee",
        //      "Description": "Freshly brewed coffee",
        //      "price": 3.5,
        //      "Ingredients": ["Water", "Coffee beans", "Sugar"],
        //      "Type": "drink"
        //    },
        //    {

        //      "Category": "Snacks",
        //      "ItemName": "Muffin",
        //      "Description": "Blueberry muffin",
        //      "price": 2.0,
        //      "Ingredients": ["Flour", "Sugar", "Blueberries", "Butter"],
        //      "Type": "food"
        //    }
        //  ]
        //}



        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] JsonElement jsonElement)
        {
            // Convert the JSON element to a JSON string
            string jsonString = jsonElement.GetRawText();

            // Parse the JSON string to a BsonDocument
            BsonDocument document = BsonDocument.Parse(jsonString);

            // Get the MongoDB collection for Orders
            var collection = _database.GetCollection<BsonDocument>("Orders");

            string tableNumber = "0";
            int grossnumber =0;
            if (jsonElement.TryGetProperty("tablenumber", out JsonElement tableNumberElement))
            {
                tableNumber = tableNumberElement.GetString();
            }
            string stype = "";
            if (jsonElement.TryGetProperty("type", out JsonElement typeElement))
            {
                stype = typeElement.GetString();
            }
            // Get the MongoDB collection for Table
            var tableCollection = _database.GetCollection<Table>("Table");

          
            var filter = Builders<Table>.Filter.Eq(t => t.tableNumber, int.Parse(tableNumber));

           
            var GrossCollection = _database.GetCollection<Gross>("Gross");

           
            var filtergross = Builders<Gross>.Filter.Eq(g => g.status, "Pending");

            var theGross = GrossCollection.Find(filtergross).FirstOrDefault();

            if (theGross != null)
            {
            
                 grossnumber = theGross.grossNumber;
                Console.WriteLine($"Gross Number: {grossnumber}");
            }
            else
            {
                return Ok(new { success = false, message = "Start your day please" });
            }

            if (stype == "Dine in")
            {
                var update = Builders<Table>.Update.Set(t => t.Status, "Taken");

                // Update the document in the Table collection
                var result = await tableCollection.UpdateOneAsync(filter, update);
            }else if (stype == "Delivery")
            {

            }
         
            // Define the update to change the status
           

            // Get the MongoDB collection for Orders
            var ordersCollection = _database.GetCollection<Order>("Orders");

            // Define the sort operation to get the largest orderNumber
            var sort = Builders<Order>.Sort.Descending(o => o.ordernumber);

            // Find the document with the largest orderNumber
            var largestOrder = await ordersCollection.Find(new BsonDocument())
                                                     .Sort(sort)
                                                     .Limit(1)
                                                     .FirstOrDefaultAsync();

            int newOrderNumber = largestOrder != null ? int.Parse(largestOrder.ordernumber) + 1 : 0;

          
            document["ordernumber"] = newOrderNumber.ToString();

            document["dateofOrder"] = DateTime.Now.ToString();

            document["grossNumber"] = grossnumber;

            double totalPrice = CalculateTotalPrice(jsonElement ,stype);

            document["totalprice"] = totalPrice;
      
            await collection.InsertOneAsync(document);

         
            var collectionOrdernew = _database.GetCollection<BsonDocument>("Orders");

            var filterOrderGross = Builders<BsonDocument>.Filter.Eq("grossNumber", grossnumber);
            var documents = await collectionOrdernew.Find(filterOrderGross).ToListAsync();

            double TP = 0;

            foreach (var item in documents) // Use 'documents' instead of 'document'
            {
                
                if (item.Contains("totalprice") && item["totalprice"].IsDouble)
                {
                    TP += item["totalprice"].AsDouble; // Accumulate the total price
                }
                else
                {
                    
                }
            }


            var updategross = Builders<Gross>.Update.Set(t => t.totalGross, TP);

            // Update the document in the Table collection
            var resultgross = await GrossCollection.UpdateOneAsync(filtergross, updategross);

            // TP now contains the sum of all 'totalprice' values

           
         
            _globalService.LogAction($"Order '{tableNumber}' Created with total price {totalPrice}.", "Created");

            return Ok(new { message = "Order created successfully", orderId = document["_id"].ToString(), totalPrice });
        }

        private double CalculateTotalPrice(JsonElement jsonElement, string stype)
        {
            double total = 0;
            if (stype=="Dine in")
            {
               
                if (jsonElement.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in itemsElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("price", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                        {
                            total += price;
                        }
                    }
                }
            }else if (stype == "Delivery")
            {
                if (jsonElement.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in itemsElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("pricedel", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                        {
                            total += price;
                        }
                    }
                }
            }
          
            return total;
        }














            [HttpPost("CreateGross")]
        public async Task<IActionResult> CreateGross([FromBody] JsonElement jsonElement)
        {
            try
            {
                var grossCollection = _database.GetCollection<Gross>("Gross");

                // Filter the collection to find entries where status is "Pending"
                var pendingGross = await grossCollection.Find(g => g.status == "Pending").FirstOrDefaultAsync();

                if (pendingGross != null)
                {
                    return BadRequest("There is already a gross entry with 'Pending' status.");
                }

                var sort = Builders<Gross>.Sort.Descending(g => g.grossNumber);
                var largestGross = await grossCollection.Find(new BsonDocument())
                                                        .Sort(sort)
                                                        .Limit(1)
                                                        .FirstOrDefaultAsync();

                string jsonString = jsonElement.GetRawText();
                BsonDocument document = BsonDocument.Parse(jsonString);

                document["dateofGrossPay"] = DateTime.Now.ToString();
                document["status"] = "Pending";

                if (largestGross != null)
                {
                    document["grossNumber"] = largestGross.grossNumber + 1;
                }
                else
                {
                    document["grossNumber"] = 1;
                }

                var bsonCollection = _database.GetCollection<BsonDocument>("Gross");
                await bsonCollection.InsertOneAsync(document);

                return Ok("Gross entry created successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating gross entry: {ex.Message}");
            }
        }




        [HttpGet("PrintOrders")]
        public IActionResult PrintOrders()
        {
            // Get the MongoDB collection
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Retrieve all documents (orders) from the collection
            var orders = collection.Find(new BsonDocument()).ToList();

            // Loop through each order and print the order details and item descriptions to the console
            foreach (var order in orders)
            {
                Console.WriteLine($"Order ID: {order["_id"]}");
                Console.WriteLine($"Type: {order["type"]}");
                Console.WriteLine($"Status: {order["status"]}");
                Console.WriteLine("Items:");

                var items = order["items"].AsBsonArray;
                foreach (var item in items)
                {
                    Console.WriteLine($"\tItem Name: {item["ItemName"]}");
                    Console.WriteLine($"\tDescription: {item["Description"]}");
                    Console.WriteLine($"\tPrice: {item["price"]}");
                    Console.WriteLine();
                }
            }

            // Return a simple success message
            return Ok("Orders printed to console.");
        }


        [HttpGet("GetOrdersinProcess")]
        public IActionResult GetOrdersinProcess()
        {
            var collection = _database.GetCollection<Order>("Order");
            int x = 0;
            // Find orders with status "Pending"
            var pendingOrders = collection.Find(order => order.status == "Pending").ToList();

            // Print the status of each pending order to the console
            foreach (var order in pendingOrders)
            {
                Console.WriteLine($"Order Status: {order.status}");
            }

            // Convert pending orders to JSON
            var jsonResult = pendingOrders.Select(order => JsonSerializer.Serialize(order)).ToList();

            // Return the data as JSON
            return Json(jsonResult);

        }
        [HttpPut("UpdateOrder/{ordernumber}")]
        public async Task<IActionResult> UpdateOrder(string ordernumber, [FromBody] JsonElement jsonElement)
        {
            int grossnumber;
            string tableNumberValue;
            string tablenumberstring="";
            try
            {
                string jsonString = jsonElement.GetRawText();
                BsonDocument updatedDocument = BsonDocument.Parse(jsonString);
                _logger.LogInformation($"Received JSON: {jsonString}");
                string a=updatedDocument["type"].AsString;

               double pricer= CalculateTotalPrice(jsonElement,a);
                var collection = _database.GetCollection<BsonDocument>("Orders");


                var filter = Builders<BsonDocument>.Filter.Eq("ordernumber", ordernumber);
                var document = await collection.Find(filter).FirstOrDefaultAsync();

                double totalnewpayment =CalculateTotalPrice(jsonElement,"jgjhdgjhgfj");

                if (document.Contains("tablenumber"))
                {
                    var tablenumbereElement = document.GetElement("tablenumber");
    
                    tablenumberstring = tablenumbereElement.Value.ToString();
                    


                   
                }
                else
                {
                    // Handle the case where the attribute does not exist
                    Console.WriteLine("The attribute 'customerName' does not exist.");
                }




                if (document != null)
                {
                    updatedDocument["totalprice"] = totalnewpayment;
                    string orderNumberValue = document["ordernumber"].AsString;
                     tableNumberValue = updatedDocument["tablenumber"].AsString;
                    double beforetotalprice = document["totalprice"].AsDouble;
                    grossnumber = document["grossNumber"].AsInt32;
                    _logger.LogInformation($"Order Number: {orderNumberValue}");
                    _logger.LogInformation($"Table Number: {tableNumberValue}");

                    //var items = document["items"].AsBsonArray;
                    //foreach (var item in items)
                    //{
                    //    string category = item["Category"].AsString;
                    //    string itemName = item["ItemName"].AsString;
                    //    string description = item["Description"].AsString;
                    //    double price = item["price"].AsDouble;
                    //    var ingredients = item["Ingredients"].AsBsonArray.Select(i => i.AsString).ToList();
                    //    string type = item["Type"].AsString;

                    //    _logger.LogInformation($"Category: {category}");
                    //    _logger.LogInformation($"Item Name: {itemName}");
                    //    _logger.LogInformation($"Description: {description}");
                    //    _logger.LogInformation($"Price: {price}");
                    //    _logger.LogInformation($"Ingredients: {string.Join(", ", ingredients)}");
                    //    _logger.LogInformation($"Type: {type}");
                    //}
                }
                else
                {
                    _logger.LogWarning("Document not found.");
                    return NotFound(new { message = "Order not found" });
                }




                var result = await collection.ReplaceOneAsync(filter, updatedDocument);


                if (a == "Delivery")
                {
                    var tableCollection = _database.GetCollection<Table>("Table");


                    var filtertablold = Builders<Table>.Filter.Eq(t => t.tableNumber, int.Parse(tablenumberstring));

                    // Define the update to change the status
                    var updateold = Builders<Table>.Update.Set(t => t.Status, "Available");

                    // Update the document in the Table collection
                    var resultold = await tableCollection.UpdateOneAsync(filtertablold, updateold);
                }else if(a=="Dine in")
                {
                    if (tablenumberstring != tableNumberValue)
                    {
                        var tableCollection = _database.GetCollection<Table>("Table");


                        var filtertablold = Builders<Table>.Filter.Eq(t => t.tableNumber, int.Parse(tablenumberstring));

                        // Define the update to change the status
                        var updateold = Builders<Table>.Update.Set(t => t.Status, "Available");

                        // Update the document in the Table collection
                        var resultold = await tableCollection.UpdateOneAsync(filtertablold, updateold);

                        var filtertabnew = Builders<Table>.Filter.Eq(t => t.tableNumber, int.Parse(tableNumberValue));

                        // Define the update to change the status
                        var updateoldnew = Builders<Table>.Update.Set(t => t.Status, "Taken");

                        // Update the document in the Table collection
                        var resultoldnew = await tableCollection.UpdateOneAsync(filtertabnew, updateoldnew);

                    }

                }




                // Update the document in the Table collection
                updatetheGross(grossnumber);

                if (result.ModifiedCount > 0)
                {
                    return Ok(updatedDocument.ToJson());
                }
                else
                {
                    return NotFound(new { message = "Order not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the order.");
                return StatusCode(500, "Internal server error");
            }
        }



        [HttpDelete("DeleteOrder/{ordernumber}")]
        public async Task<IActionResult> DeleteOrder(string ordernumber)
        {
            var collectionOrdernew = _database.GetCollection<Order>("Orders"); // Use Order type instead of BsonDocument

            // Filter for orders that match the given ordernumber
            var filterOrder = Builders<Order>.Filter.Eq(o => o.ordernumber, ordernumber);

            // Find the order matching the ordernumber
            var order = await collectionOrdernew.Find(filterOrder).FirstOrDefaultAsync();

            // If the order is not found, return a 404 NotFound result
            if (order == null)
            {
                return NotFound("Order not found");
            }

            // Store the grossNumber before deleting
            int x = order.grossNumber;
          

           // Delete the order from the collection
           await collectionOrdernew.DeleteOneAsync(filterOrder);
            updatetheGross(x);

            // Log the deletion
            _globalService.LogAction($"Order '{ordernumber}' deleted.", "Deleted");

            return Ok("Order deleted successfully");
        }


        public async Task updatetheGross(int grossNumber)
        {
            var GrossCollection = _database.GetCollection<Gross>("Gross");
            var collectionOrdernew = _database.GetCollection<Order>("Orders"); // Use Order type instead of BsonDocument

            // Filter for orders that match the grossNumber using the Order class
            var filterOrderGross = Builders<Order>.Filter.Eq(o => o.grossNumber, grossNumber);
            var documents = await collectionOrdernew.Find(filterOrderGross).ToListAsync();

            double TP = 0; // Total price accumulator

            if (documents.Count() == 0)
            {
                TP = 0;
            }else
            {
                foreach (var item in documents)
                {
                    // Accumulate the totalprice from each Order
                    TP += item.totalprice;
                }
            }

           

            // Update the totalGross field in the Gross collection where status is "Pending"
            var updategross = Builders<Gross>.Update.Set(t => t.totalGross, TP);
            var filtergross = Builders<Gross>.Filter.Eq(g => g.status, "Pending");

            var resultgross = await GrossCollection.UpdateOneAsync(filtergross, updategross);
        }








    }
}
