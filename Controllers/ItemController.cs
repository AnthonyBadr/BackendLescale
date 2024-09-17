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

        //start of cat

        public IActionResult Index()
        {
      
            var collection = _database.GetCollection<BsonDocument>("Category");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to JSON
            var jsonResult = documents.Select(doc => doc.ToJson()).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        public IActionResult Privacy()
        {
            return View();
        }




        //if category exist 

//        {
//    "message": "Category 'Burgers' already exists."
//}

    // Create a new document
    [HttpPost("CreateCategory")]
        public IActionResult CreateCategory([FromBody] JsonElement jsonElement)
        {
           
            string jsonString = jsonElement.GetRawText();

           
            BsonDocument document = BsonDocument.Parse(jsonString);

          
            string categoryName = document["name"].AsString;

         
            var collection = _database.GetCollection<BsonDocument>("Category");

          
            var existingCategory = collection.Find(Builders<BsonDocument>.Filter.Eq("name", categoryName)).FirstOrDefault();

            if (existingCategory != null)
            {
               
                return Conflict(new { message = $"Category '{categoryName}' already exists." });
            }

           
            collection.InsertOne(document);
            _globalService.LogAction($"Category '{categoryName}' created.", "Created");


            return Ok(new { message = "Category created successfully." });
        }



        [HttpPut("UpdateCategory/{name}")]
        public IActionResult UpdateCategory(string name, [FromBody] JsonElement jsonElement)
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
                return BadRequest("Category name is required.");
            }

           
            if (!document.Contains("name"))
            {
                return BadRequest("The 'name' field is required in the document.");
            }

        
            string newCategoryName = document["name"].AsString;

         
            var collection = _database.GetCollection<BsonDocument>("Category");

            
            var existingCategory = collection.Find(Builders<BsonDocument>.Filter.Eq("name", newCategoryName)).FirstOrDefault();
            if (existingCategory != null && newCategoryName != name)
            {
                return Conflict(new { message = $"Category '{newCategoryName}' already exists." });
            }

         
            var filter = Builders<BsonDocument>.Filter.Eq("name", name);

            
            var updateResult = collection.ReplaceOne(filter, document);

            if (updateResult.MatchedCount == 0)
            {
                return NotFound($"Category '{name}' not found.");
            }
            _globalService.LogAction($"Category '{name}' updated.", "Update");

            return Ok("Category updated successfully.");
        }

        [HttpGet("GetCategory/{name}")]
        public IActionResult GetCategory(string name)
        {
            // Ensure that 'name' is not null or empty
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Category name is required.");
            }

            // Get the collection
            var collection = _database.GetCollection<BsonDocument>("Category");

            // Create a filter to find the category by name
            var filter = Builders<BsonDocument>.Filter.Eq("name", name);

            // Find the category
            var category = collection.Find(filter).FirstOrDefault();

            if (category == null)
            {
                return NotFound($"Category '{name}' not found.");
            }

            // Return the found category
            return Ok(category);
        }






        [HttpDelete("DeleteCategoryByName/{name}")]
        public IActionResult DeleteCategoryByName(string name)
        {
            var categoryCollection = _database.GetCollection<BsonDocument>("Category");
            var itemCollection = _database.GetCollection<BsonDocument>("Item");

            // Filter to check if items exist with the given category name
            var filter2 = Builders<BsonDocument>.Filter.Eq("Category", name);
            var itemExists = itemCollection.Find(filter2).Any(); // Check if any items are associated with this category

            if (itemExists)
            {
                return BadRequest($"Cannot delete category '{name}' because items are associated with it.");
            }

            // Proceed with deleting the category
            var filter = Builders<BsonDocument>.Filter.Eq("name", name); // Filter by name
            var deleteResult = categoryCollection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound($"Category '{name}' not found.");
            }

            _globalService.LogAction($"Category '{name}' deleted.", "Delete");

            return Ok($"Category '{name}' successfully deleted.");
        }



        //get all items based on this category
        [HttpGet("GetItemsByCategory/{categoryName}")]
        public IActionResult GetItemsByCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                return BadRequest("Category name is required.");
            }

            var collection = _database.GetCollection<Item>("Item");
            var filter = Builders<Item>.Filter.Eq(item => item.Category, categoryName);
            var items = collection.Find(filter).ToList();

            if (items == null || items.Count == 0)
            {
                return NotFound($"No items found for category '{categoryName}'.");
            }

            return Ok(items);
        }



        //end of cat


        //Json set to item example 
        //   {
        //"Category": "Food",
        //"ItemName": "Cheeseburger",
        //"Description": "A juicy beef patty topped with melted cheese, lettuce, and tomato.",
        //"price": 8.99,
        //"priceDelivery":8,
        //"Type":"1",
        //"Ingredients": ["Beef", "Cheese", "Lettuce", "Tomato", "Bun"]
        //}
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

            string itemName = document.Contains("ItemName") ? document["ItemName"].AsString : null;
            string itemType = document.Contains("Type") ? document["Type"].AsString : null;

            if (string.IsNullOrEmpty(itemName))
            {
                return BadRequest("ItemName is required.");
            }

            if (string.IsNullOrEmpty(itemType))
            {
                return BadRequest("Type is required.");
            }

            var collection = _database.GetCollection<BsonDocument>("Item");

            // Create a filter to check for existing item with the same ItemName and Type
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("ItemName", itemName),
                Builders<BsonDocument>.Filter.Eq("Type", itemType)
            );

            var existingItem = collection.Find(filter).FirstOrDefault();

            if (existingItem != null)
            {
                return Conflict("Item with the same name and type already exists.");
            }

            _globalService.LogAction($"Item '{itemName}' created.", "Create");

            collection.InsertOne(document);

            return Ok("Item created successfully.");
        }



        [HttpPut("UpdateItem/{name}")]
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

            if (!document.Contains("ItemName") || !document.Contains("Type"))
            {
                return BadRequest("The 'ItemName' and 'Type' fields are required.");
            }

            string newItemName = document["ItemName"].AsString;
            string itemType = document["Type"].AsString;

            var collection = _database.GetCollection<BsonDocument>("Item");

            var existingItem = collection.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("ItemName", newItemName),
                Builders<BsonDocument>.Filter.Eq("Type", itemType)
            )).FirstOrDefault();

            if (existingItem != null && newItemName != name)
            {
                return Conflict(new { message = "Item with the same name and type already exists." });
            }

            var filter = Builders<BsonDocument>.Filter.Eq("ItemName", name);

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
            var filter = Builders<BsonDocument>.Filter.Eq("ItemName", name);

            // Retrieve the documents that match the filter
            var documents = collection.Find(filter).ToList();

            if (documents.Count == 0)
            {
                return NotFound("Item not found.");
            }

            // Convert documents to JSON format
            var jsonResult = documents.Select(doc => doc.ToJson()).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }


        [HttpGet("GetItemsByType/{type}")]
        public IActionResult GetItemsByType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return BadRequest("type is required.");
            }

            var collection = _database.GetCollection<Item>("Item");
            var filter = Builders<Item>.Filter.Eq(item => item.Type, type);
            var items = collection.Find(filter).ToList();

            if (items == null || items.Count == 0)
            {
                return NotFound($"No items found for type '{type}'.");
            }

            return Ok(items);
        }
   

        [HttpDelete("DeleteItem/{name}")]
        public IActionResult DeleteItem(string name)
        {
            // Get the collection for items
            var collection = _database.GetCollection<BsonDocument>("Item");

            // Create a filter to find the document by 'ItemName'
            var filter = Builders<BsonDocument>.Filter.Eq("ItemName", name);

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
