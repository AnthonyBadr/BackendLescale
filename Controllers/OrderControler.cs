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



        [HttpGet("GetOrderByOrderNumber/{orderNumber}")]
        public async Task<IActionResult> GetOrderByOrderNumber(int orderNumber)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Create a filter to find the order with the specified OrderNumber
            var filter = Builders<BsonDocument>.Filter.Eq("OrderNumber", orderNumber);

            // Find the order in the collection
            var orderDocument = await collection.Find(filter).FirstOrDefaultAsync();
            var document = BsonTypeMapper.MapToDotNetValue(orderDocument);
            if (orderDocument == null)
            {
                // Return a 404 if the order is not found
                return NotFound($"Order with OrderNumber {orderNumber} not found.");
            }

            // Return the found order
            return Json(document);
        }


        [HttpGet("GetOrderByDate")]
        public async Task<IActionResult> GetOrderByOrderNumber([FromQuery] string date)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Create a filter to find the order with the specified date (first 10 characters)
            var filter = Builders<BsonDocument>.Filter.Regex("DateOfOrder", new BsonRegularExpression($"^{date}"));

            // Find the order in the collection
            var orderDocuments =  collection.Find(filter).ToList();
            if (orderDocuments == null)
            {
                // Return a 404 if the order is not found
                return NotFound($"Order with date {date} not found.");
            }

            // Map the BSON document to a .NET object
            var document = orderDocuments.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the found order
            return Json(document);
        }


        [HttpGet("GetOrderByStatus/{Status}")]
        public async Task<IActionResult> GetOrderByStatus(string Status)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Create a filter to find the order with the specified OrderNumber
            var filter = Builders<BsonDocument>.Filter.Eq("Status", Status);

            // Find the order in the collection
            var orderDocuments =  collection.Find(filter).ToList();
            var document = orderDocuments.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();
            if (orderDocuments == null)
            {
                // Return a 404 if the order is not found
                return NotFound($"Order with Status {Status} not found.");
            }

            // Return the found order
            return Json(document);
        }



        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders(int pageNumber = 1, int pageSize = 10)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Calculate the number of documents to skip
            int skip = (pageNumber - 1) * pageSize;

            // Retrieve the documents with pagination
            var documents = collection.Find(new BsonDocument())
                                      .Skip(skip)
                                      .Limit(pageSize)
                                      .ToList();

            // Convert documents to a list of JSON objects
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }




        [HttpPost("CreateOrder")]
            public async Task<IActionResult> CreateOrder([FromBody] JsonElement jsonElement)
            {

                double TotalPrice = 0;
                int GrossNumber = 0;
                string jsonString = jsonElement.GetRawText();
                BsonDocument document = BsonDocument.Parse(jsonString);
                var collection = _database.GetCollection<BsonDocument>("Orders");

                var newOrder = new Order
                {
                    TableNumber = document["TableNumber"].AsString,
                    Type = document["Type"].AsString
                    // Add other properties as needed
                };

                TotalPrice = CalculateTotalPrice(jsonElement, newOrder.Type);

                GrossNumber = UpdateTheGrossNew(TotalPrice).Result;



                if (newOrder.Type == "Dine In")
                {
                    UpdateTableStatus(newOrder.TableNumber, "Taken");
                }
                else if (newOrder.Type == "Delivery")
                {

                }

                // Add a new sequence value for the table ID

                int newSequenceValue = _globalService.SequenceIncrement("OrderNumber").GetAwaiter().GetResult();
                document.Add("OrderNumber", newSequenceValue);
                document.Add("DateOfOrder", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                document.Add("GrossNumber", GrossNumber);
                document.Add("TotalPrice", TotalPrice);
                document.Add("Created_by", _globalService.username);
                await collection.InsertOneAsync(document);
                return Ok(new { message = "Order created successfully", OrderNumber = newSequenceValue.ToString(), TotalPrice });



            }


            [HttpPut("UpdateOrderByOrderNumber/{ordernumber}")]
            public async Task<IActionResult> UpdateOrderByOrderNumber(int ordernumber, [FromBody] JsonElement jsonElement)
            {
                double TotalNewPrice = 0;
                double TotalOldPrice = 0;
                int GrossNumber = 0;
                string oldType = "";
                string TableNumberOld = "";
                string jsonString = jsonElement.GetRawText();
                BsonDocument document = BsonDocument.Parse(jsonString);

                var Order = new Order
                {
                    TableNumber = document["TableNumber"].AsString,
                    Type = document["Type"].AsString,
                    TotalOldPrice = document["TotalPrice"].AsDouble,
                };

                var collection = _database.GetCollection<BsonDocument>("Orders");

                // Create a filter to find the order with the specified OrderNumber
                var filter = Builders<BsonDocument>.Filter.Eq("OrderNumber", ordernumber);

                // Find the order in the collection
                var orderDocument = await collection.Find(filter).FirstOrDefaultAsync();

                if (orderDocument == null)
                {
                    // Return a 404 if the order is not found
                    return NotFound($"Order with OrderNumber {ordernumber} not found.");
                }

                // Access the individual attributes of the existing order
                TableNumberOld = orderDocument.GetValue("TableNumber").AsString;
                oldType = orderDocument.GetValue("Type").AsString;
                TotalOldPrice = orderDocument.GetValue("TotalPrice").AsDouble;

                // Update table statuses based on changes
                if (TableNumberOld != Order.TableNumber)
                {
                    if (Order.Type == oldType)
                    {
                        UpdateTableStatus(TableNumberOld, "Available");
                        UpdateTableStatus(Order.TableNumber, "Taken");
                    }
                    else
                    {
                        UpdateTableStatus(Order.TableNumber, "Taken");
                    }
                }
                else if (Order.Type != oldType)
                {
                    if (Order.Type == "Dine In")
                    {
                        UpdateTableStatus(Order.TableNumber, "Taken");
                    }
                    else if (Order.Type == "Delivery")
                    {
                        UpdateTableStatus(TableNumberOld, "Available");
                    }
                }

                // Calculate new total price
                TotalNewPrice = CalculateTotalPrice(jsonElement, Order.Type);

                // Update gross values
                await UpdateTheGrossNew(-TotalOldPrice);
                await UpdateTheGrossNew(TotalNewPrice);

                // Update the order with new values
                var updateDefinition = Builders<BsonDocument>.Update
                    .Set("TableNumber", Order.TableNumber)
                    .Set("Type", Order.Type)
                    .Set("TotalPrice", TotalNewPrice);

                var updateResult = await collection.UpdateOneAsync(filter, updateDefinition);

                if (updateResult.ModifiedCount == 0)
                {
                    return StatusCode(500, "Nothing Chnanged");
                }

                return Ok(new { message = "Order updated successfully", TotalNewPrice });
            }




            [HttpDelete("DeleteOrderByOrderNumber/{orderNumber}")]
            public IActionResult DeleteOrderByOrderNumber(int orderNumber)
            {

                var GrossCollection = _database.GetCollection<BsonDocument>("Orders");


                double orderAmount = 0;


                var collection = _database.GetCollection<BsonDocument>("Orders");
                var filter = Builders<BsonDocument>.Filter.Eq("OrderNumber", orderNumber);
                var payment = collection.Find(filter).FirstOrDefault();

                if (payment != null)
                {
                    orderAmount = payment["TotalPrice"].AsDouble;
                    UpdateTheGrossNew(-orderAmount);

                    Console.WriteLine($"Order amount: {orderAmount}");
                }
                else
                {
                    Console.WriteLine("Order not found.");
                }

                var deleteResult = collection.DeleteOne(filter);

                if (deleteResult.DeletedCount == 0)
                {
                    return NotFound();
                }

                return Ok();
            }








            public async Task<int> UpdateTheGrossNew(double amount)
            {
                var grossCollection = _database.GetCollection<BsonDocument>("Gross");

                var filterGross = Builders<BsonDocument>.Filter.Eq("Status", "Open");
                var theGross = await grossCollection.Find(filterGross).FirstOrDefaultAsync();

                if (theGross == null)
                {
                    throw new Exception("Start your day please.");
                }

                if (!theGross.Contains("GrossNumber"))
                {
                    throw new Exception("GrossNumber field is missing in the document.");
                }

                int grossNumber = theGross["GrossNumber"].AsInt32;

                // Debug statement to log the document found by the filter
                Console.WriteLine(theGross.ToJson());

                var update = Builders<BsonDocument>.Update.Inc("TotalGross", amount);
                var updateResult = await grossCollection.UpdateOneAsync(filterGross, update);

                // Debug statement to log the update result
                Console.WriteLine($"Matched Count: {updateResult.MatchedCount}, Modified Count: {updateResult.ModifiedCount}");

                if (updateResult.ModifiedCount == 0)
                {
                    throw new Exception("No updates were made to the gross document.");
                }

                return grossNumber;
            }






            private async void UpdateTableStatus(string TableNumber, string Status)
            {
                var tableCollection = _database.GetCollection<BsonDocument>("Table");
                var update = Builders<BsonDocument>.Update.Set("Status", Status);
                var filter = Builders<BsonDocument>.Filter.Eq("TableNumber", TableNumber);
                var result = await tableCollection.UpdateOneAsync(filter, update);

            }



            private double CalculateTotalPrice(JsonElement jsonElement, string stype)
            {
                double total = 0;

                if (stype == "Dine In")
                {

                    if (jsonElement.TryGetProperty("Items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in itemsElement.EnumerateArray())
                        {
                            // Get the main item's price
                            if (item.TryGetProperty("PriceDineIn", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                            {
                                total += price; // Add main item price to total
                            }

                            // Check for addons and sum their prices
                            if (item.TryGetProperty("AddOns", out JsonElement addonsElement) && addonsElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement addon in addonsElement.EnumerateArray())
                                {
                                    if (addon.TryGetProperty("Price", out JsonElement addonPriceElement) && addonPriceElement.TryGetDouble(out double addonPrice))
                                    {
                                        total += addonPrice; // Add addon price to total
                                        int x = 0;
                                    }
                                }
                            }
                        }
                    }

                }
                else if (stype == "Delivery")
                {
                    if (jsonElement.TryGetProperty("Items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in itemsElement.EnumerateArray())
                        {
                            // Get the main item's price
                            if (item.TryGetProperty("PriceDelivery", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                            {
                                total += price; // Add main item price to total
                            }

                            // Check for addons and sum their prices
                            if (item.TryGetProperty("AddOns", out JsonElement addonsElement) && addonsElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement addon in addonsElement.EnumerateArray())
                                {
                                    if (addon.TryGetProperty("Price", out JsonElement addonPriceElement) && addonPriceElement.TryGetDouble(out double addonPrice))
                                    {
                                        total += addonPrice; // Add addon price to total
                                        int x = 0;
                                    }
                                }
                            }
                        }
                    }
                }

                return total;
            }







        }
    }




    
