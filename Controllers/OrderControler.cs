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
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization;

namespace backend.Controllers
{

    [Route("api/[controller]")]
    public class OrderController : Controller
    {

        List<double> itemprice = new List<double>();



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




        [HttpGet("MergeOrderByOrderNumber")]
        public async Task<IActionResult> MergeTablesOrder([FromQuery] string[] values)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Convert the string values to int
            int[] intValues = values.Select(int.Parse).ToArray();

            // Build the filter to retrieve orders by OrderNumber
            var filter = Builders<BsonDocument>.Filter.In("OrderNumber", intValues);

            // Retrieve the documents matching the filter
            var orders = await collection.Find(filter).ToListAsync();

            // Check if we have at least two orders to merge
            if (orders.Count < 2)
            {
                return BadRequest("At least two orders are required for merging.");
            }

            // Extract "Items" from each order and combine them
            var combinedItems = new List<object>();

            foreach (var order in orders)
            {
                if (order.Contains("Items") && order["Items"].IsBsonArray)
                {
                    var items = order["Items"].AsBsonArray;
                    combinedItems.AddRange(items.Select(item => BsonTypeMapper.MapToDotNetValue(item)));
                }
            }

            // Use the first order's OrderNumber for the new combined order
            int newOrderNumber = orders.First()["OrderNumber"].AsInt32;
            double totalprice= orders.First()["TotalPrice"].AsDouble;
            string tablenumber = orders.First()["TableNumber"].AsString;
            // Create the new combined order
            var response = new
            {
                TableNumber= tablenumber,
                Description = $"Combination of {orders.Count} Orders",
                Type = "Dine In",
                Status = "Pending",
                Items = combinedItems,
                Location = orders.First()["Location"].AsString, // Use the first order's location
                DeliveryCharge = orders.First()["DeleiveryCharge"].AsDouble, // Use the first order's delivery charge
                DateOfOrder = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), // Set the current time for the combined order
                TotalPrice= totalprice
            };

            return Ok(response);
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

                List<double> TotalPrice = new List<double>();
                int GrossNumber = 0;
                string jsonString = jsonElement.GetRawText();
                BsonDocument document = BsonDocument.Parse(jsonString);
                var collection = _database.GetCollection<BsonDocument>("Orders");

            var newOrder = new Order
            {
                TableNumber = document["TableNumber"].AsString,
                Type = "Dine In",
                    DeleiveryCharge = document["DeliveryCharge"].AsDouble
                    // Add other properties as needed
                };
          
            TotalPrice = CalculateTotalPrice(jsonElement);
            double test = TotalPrice[0];
            string name = _globalService.username;
                GrossNumber = UpdateTheGrossNew(TotalPrice[TotalPrice.Count() - 1]).Result;
            int index = 0;
            foreach (var item in document["Items"].AsBsonArray)
            {
                if(index< TotalPrice.Count()-1)
                {
                    item["ItemPrice"] = TotalPrice[index];
                }
                else
                {
                    item["ItemPrice"] = 0; 
                }

                index++;


            }

            if (newOrder.TableNumber!="NA")
            {
                UpdateTableStatus(newOrder.TableNumber, "Taken");
                document.Add("TypeOfOrder", "Dine In");

            }
            else if (newOrder.DeleiveryCharge > 0)
            {
                document.Add("TypeOfOrder", "Delivery");

            }else
            {
                document.Add("TypeOfOrder", "Take Away");

            }


            // Add a new sequence value for the table ID

            int newSequenceValue = _globalService.SequenceIncrement("OrderNumber").GetAwaiter().GetResult();
                document.Add("OrderNumber", newSequenceValue);
                document.Add("DateOfOrder", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                document.Add("GrossNumber", GrossNumber);
            document.Add("TotalPrice", TotalPrice[TotalPrice.Count() - 1]);
                document.Add("Created_by", _globalService.username);
                await collection.InsertOneAsync(document);
                return Ok(new { message = "Order created successfully", OrderNumber = newSequenceValue.ToString(), TotalPrice });



            }


        [HttpPut("AddAnItem/{ordernumber}")]
        public async Task<IActionResult> AddAnItem(int ordernumber, [FromBody] JsonElement jsonElement)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");
            var filter = Builders<BsonDocument>.Filter.Eq("OrderNumber", ordernumber);


            var existingDocument = await collection.Find(filter).FirstOrDefaultAsync();
            if (existingDocument == null)
            {
                return NotFound($"Order with number {ordernumber} not found.");
            }

     
            var newItem = BsonDocument.Parse(jsonElement.ToString());

            if (existingDocument.Contains("Items") && existingDocument["Items"].IsBsonArray)
            {
          
                var itemsArray = existingDocument["Items"].AsBsonArray;
                itemsArray.Add(newItem);
            }
            else
            {
           
                existingDocument["Items"] = new BsonArray { newItem };
            }
            var document = BsonTypeMapper.MapToDotNetValue(existingDocument);

          
            return Ok(document);
        }




        [HttpPut("RemoveItem/{ordernumber}")]
        public async Task<IActionResult> RemoveItem(int ordernumber, [FromBody] JsonElement jsonElement)
        {
            var collection = _database.GetCollection<BsonDocument>("Orders");
            var filter = Builders<BsonDocument>.Filter.Eq("OrderNumber", ordernumber);

            var existingDocument = await collection.Find(filter).FirstOrDefaultAsync();

            if (existingDocument == null)
            {
                return NotFound("Order not found.");
            }

            if (existingDocument.Contains("Items"))
            {
                var itemsArray = existingDocument["Items"].AsBsonArray;

                for (int i = 0; i < itemsArray.Count; i++)
                {
                    var currentItem = itemsArray[i].ToJson();
                    JsonElement citem = JsonSerializer.Deserialize<JsonElement>(currentItem);


                    var citemWithoutPrice = RemoveField(citem, "ItemPrice");

                    if (citemWithoutPrice.GetRawText() == jsonElement.GetRawText()) // Assuming the structure is similar
                    {
                        itemsArray.RemoveAt(i);
                        break; 
                    }
                }

                // Replace the "Items" array in the existing document
                existingDocument["Items"] = itemsArray;

                // Update the document in the database (optional)
                await collection.ReplaceOneAsync(filter, existingDocument);

                // Return the modified document as JSON
                return Ok(existingDocument.ToJson()); // Convert the document to JSON and return it
            }

            return NotFound("Item not found in the order.");
        }

        private JsonElement RemoveField(JsonElement element, string fieldName)
        {
            using (JsonDocument doc = JsonDocument.Parse(element.GetRawText()))
            {
                var root = doc.RootElement;
                using (var memoryStream = new MemoryStream())
                using (var jsonWriter = new Utf8JsonWriter(memoryStream))
                {
                    jsonWriter.WriteStartObject();
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name != fieldName) 
                        {
                            property.WriteTo(jsonWriter);
                        }
                    }
                    jsonWriter.WriteEndObject();
                    jsonWriter.Flush();

                    memoryStream.Position = 0;

                    using (var reader = new StreamReader(memoryStream))
                    {
                        var newJson = reader.ReadToEnd();
                        return JsonSerializer.Deserialize<JsonElement>(newJson);
                    }
                }
            }
        }






        [HttpPut("UpdateOrderByOrderNumber/{ordernumber}")]
        public async Task<IActionResult> UpdateOrderByOrderNumber(int ordernumber, [FromBody] JsonElement jsonElement)
        {
            List<double> TotalPrice = new List<double>();
            double TotalOldPrice = 0;
            string TableNumberOld = "";
            string DateOfOrder = "";
            string jsonString = jsonElement.GetRawText();
            BsonDocument newDocument = BsonDocument.Parse(jsonString);

            // Get the collection
            var collection = _database.GetCollection<BsonDocument>("Orders");

            // Create a filter to find the order with the specified OrderNumber
            var filter = Builders<BsonDocument>.Filter.Eq("OrderNumber", ordernumber);

            // Find the order in the collection
            var existingDocument = await collection.Find(filter).FirstOrDefaultAsync();

            if (existingDocument == null)
            {
                // Return a 404 if the order is not found
                return NotFound($"Order with OrderNumber {ordernumber} not found.");
            }

            // Access the individual attributes of the existing order
            TableNumberOld = existingDocument.GetValue("TableNumber").AsString;
            TotalOldPrice = existingDocument.GetValue("TotalPrice").AsDouble;
            DateOfOrder = existingDocument.GetValue("DateOfOrder").AsString;

            // Prepare the updated order details from the new document
            var updatedOrder = new Order
            {
                TableNumber = newDocument["TableNumber"].AsString,
                Status = newDocument["Status"].AsString
            };

            // Update the table statuses based on changes
            if (updatedOrder.Status == "Closed" || updatedOrder.Status == "Altered")
            {
                UpdateTableStatus(TableNumberOld, "Available");
            }
            else
            {
                // Handle table status changes
                if (TableNumberOld != "N/A" && updatedOrder.TableNumber != "N/A")
                {
                    UpdateTableStatus(TableNumberOld, "Available");
                    UpdateTableStatus(updatedOrder.TableNumber, "Taken");
                }
                else if (TableNumberOld == "N/A" && updatedOrder.TableNumber != "N/A")
                {
                    UpdateTableStatus(updatedOrder.TableNumber, "Taken");
                }
                else if (TableNumberOld != "N/A" && updatedOrder.TableNumber == "N/A")
                {
                    UpdateTableStatus(TableNumberOld, "Available");
                }
            }

         
            TotalPrice = CalculateTotalPrice(jsonElement);

            int index = 0;
            foreach (var item in newDocument["Items"].AsBsonArray)
            {
                if (index < TotalPrice.Count)
                {
                    item["ItemPrice"] = TotalPrice[index];
                }
                else
                {
                    item["ItemPrice"] = 0; 
                }
                index++;
            }

            int grossNumber=await UpdateTheGrossNew(-TotalOldPrice);
            await UpdateTheGrossNew(TotalPrice.Last());

            newDocument.Add("TotalPrice", TotalPrice.Last());
            newDocument.Add("GrossNumber", grossNumber);
            newDocument.Add("OrderNumber", ordernumber);
            newDocument.Add("DateOfOrder", DateOfOrder);

            
            var replaceResult = await collection.ReplaceOneAsync(filter, newDocument);

            if (replaceResult.ModifiedCount == 0)
            {
                return StatusCode(500, "Nothing changed");
            }

            return Ok(new { message = "Order updated successfully" });
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




      




        private List<double> CalculateTotalPrice(JsonElement jsonElement)
        {
            double total = 0;
            string itemName = "";
            double priceOfItem = 0;
            double deleiveryCharge = 0;
            List<double> itemprice = new List<double>(); // Assuming you meant to store item prices here

            if (jsonElement.TryGetProperty("DeleiveryCharge", out JsonElement deleiveryChargeElement) && deleiveryChargeElement.ValueKind == JsonValueKind.Number)
            {
                 deleiveryCharge = deleiveryChargeElement.GetDouble();
            }


            if (jsonElement.TryGetProperty("Items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in itemsElement.EnumerateArray())
                {


                    if (item.TryGetProperty("Name", out JsonElement itemNameElement)) 
                    {
                        itemName = itemNameElement.GetString();
                    }

                    if (item.TryGetProperty("Quantity", out JsonElement quantityElement) && quantityElement.TryGetInt32(out int Quantity))
                    {
                        if (item.TryGetProperty("TypeItem", out JsonElement typeItemElement) && typeItemElement.ValueKind == JsonValueKind.String)
                        {
                            string typeItem = typeItemElement.GetString();


                            if (typeItem == "Dine In")
                            {
                       
                                if (item.TryGetProperty("PriceDineIn", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                                {
                                    total += price * Quantity; 
                                    priceOfItem = price * Quantity;

                            
                                    if (item.TryGetProperty("AddOns", out JsonElement addonsElement) && addonsElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (JsonElement addon in addonsElement.EnumerateArray())
                                        {
                                            if (addon.TryGetProperty("Price", out JsonElement addonPriceElement) && addonPriceElement.TryGetDouble(out double addonPrice))
                                            {
                                                total += addonPrice * Quantity; 
                                                priceOfItem += addonPrice * Quantity;
                                            }
                                        }
                                    }
                                    itemprice.Add(priceOfItem); 
                                }
                            }else if (typeItem == "Delivery" || typeItem == "TakeAway")
                            {
                                if (item.TryGetProperty("PriceDelivery", out JsonElement priceElement) && priceElement.TryGetDouble(out double price))
                                {
                                    total += price * Quantity;
                                    priceOfItem = price * Quantity;


                                    if (item.TryGetProperty("AddOns", out JsonElement addonsElement) && addonsElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (JsonElement addon in addonsElement.EnumerateArray())
                                        {
                                            if (addon.TryGetProperty("Price", out JsonElement addonPriceElement) && addonPriceElement.TryGetDouble(out double addonPrice))
                                            {
                                                total += addonPrice * Quantity;
                                                priceOfItem += addonPrice * Quantity;
                                            }
                                        }
                                    }
                                    itemprice.Add(priceOfItem);
                                }
                            }

                        }
                    }
                }

                total = total + deleiveryCharge;
                itemprice.Add(total);

            }

            return itemprice;
        }






    }
}




    
