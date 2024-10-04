using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using backend.Services;
using System.Xml.Linq;
namespace backend.Controllers
{
    [Route("api/[controller]")]

    public class ItemController : Controller
    {

        private readonly ILogger<ItemController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public ItemController(ILogger<ItemController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }

        //Anthony Badr finialised this
        [HttpGet("GetAllItems")]
        public IActionResult GetAllItems([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var collection = _database.GetCollection<BsonDocument>("Item");

            var sortDefinition = Builders<BsonDocument>.Sort.Ascending("CategoryName");

            var documents = collection.Find(new BsonDocument())
                                      .Sort(sortDefinition)
                                      .Skip((pageNumber - 1) * pageSize)
                                      .Limit(pageSize)
                                      .ToList();

            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            return Json(jsonResult);
        }



        [HttpGet("GetItemsByCategory/{categoryName}")]
        public IActionResult GetItemsByCategory(string categoryName, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                return BadRequest("Category name is required.");
            }

            var collection = _database.GetCollection<BsonDocument>("Item");
            var filter = Builders<BsonDocument>.Filter.Eq("CategoryName", categoryName);

            // Apply pagination using Skip and Limit
            var items = collection.Find(filter)
                                  .Skip((pageNumber - 1) * pageSize)
                                  .Limit(pageSize)
                                  .ToList();

            if (items == null || items.Count == 0)
            {
                return NotFound($"No items found for category '{categoryName}'.");
            }

            var jsonDocuments = items.Select(doc => BsonTypeMapper.MapToDotNetValue(doc));
            return Ok(jsonDocuments);
        }




        [HttpPost("CreateItem")]
            public IActionResult CreateItem([FromBody] JsonElement jsonElement)
            {
                string jsonString = jsonElement.GetRawText();

                BsonDocument document;
                try
                {
                    document = BsonDocument.Parse(jsonString);
                }
                catch (Exception ex)
                {
                    return BadRequest($"Invalid JSON format: {ex.Message}");
                }

                string itemName = document.Contains("Name") ? document["Name"].AsString : null;
            
                if (string.IsNullOrEmpty(itemName))
                {
                    return BadRequest("Name is required.");
                }


                var collection = _database.GetCollection<BsonDocument>("Item");

                var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("Name", itemName)
            );


                var existingItem = collection.Find(filter).FirstOrDefault();

                if (existingItem != null)
                {
                    return Conflict("Item  already exists.");
                }

                _globalService.LogAction($"Item '{itemName}' created.", "Create");

                collection.InsertOne(document);

                return Ok("Item created successfully.");
            }                                                                                               



        [HttpPut("UpdateItemByName/{name}")]
        public IActionResult UpdateItem(string name, [FromBody] JsonElement jsonElement)
        {
            string jsonString = jsonElement.GetRawText();

            BsonDocument document;
            try
            {
                document = BsonDocument.Parse(jsonString);
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }

            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Invalid name.");
            }

            if (!document.Contains("Name"))
            {
                return BadRequest("The 'Name' field is required.");
            }

            string newItemName = document["Name"].AsString;
            

            var collection = _database.GetCollection<BsonDocument>("Item");

            var existingItem = collection.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("Name", newItemName)
            )).FirstOrDefault();

            if (existingItem != null && newItemName != name)
            {
                return Conflict(new { message = "Item with the same name already exists." });
            }

            var filter = Builders<BsonDocument>.Filter.Eq("Name", name);

            var updateResult = collection.ReplaceOne(filter, document);

            if (updateResult.MatchedCount == 0)
            {
                return NotFound("Item not found.");
            }

            _globalService.LogAction(name, "Update");

            return Ok("Item updated successfully.");
        }


        [HttpGet("GetItembyName/{name}")]
        public IActionResult GetItems(string name)
        {
            // Get the collection for items
            var collection = _database.GetCollection<BsonDocument>("Item");

            // Create a filter to find the document by 'ItemName'
            var filter = Builders<BsonDocument>.Filter.Eq("Name", name);

            // Retrieve the documents that match the filter
            var documents = collection.Find(filter).FirstOrDefault();

            if (!documents.Any())
            {
                return NotFound("Item not found.");
            }

            // Convert BSON documents to JSON
            var jsonDocuments = BsonTypeMapper.MapToDotNetValue(documents);

            // Return the data as JSON
            return Ok(jsonDocuments);
        }



        [HttpDelete("DeleteItemByName/{name}")]
        public IActionResult DeleteItem(string name)
        {
            // Get the collection for items
            var collection = _database.GetCollection<BsonDocument>("Item");

            // Create a filter to find the document by 'ItemName'
            var filter = Builders<BsonDocument>.Filter.Eq("Name", name);

            // Perform the deletion
            var deleteResult = collection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound("Item not found.");
            }


            _globalService.LogAction(name,"Delete");

            return Ok($"Item '{name}' deleted successfully.");
        }

       




    }
}
