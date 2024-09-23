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
    public class CategoryController : Controller
    {
        private readonly ILogger<CategoryController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public CategoryController(ILogger<CategoryController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }

        // Get all categories
        [HttpGet("GetAllCategories")]
        public IActionResult GetAllCategories()
        {
            var collection = _database.GetCollection<BsonDocument>("Category");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to .NET objects for proper serialization
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        // Create a new category
        [HttpPost("CreateCategory")]
        public IActionResult CreateCategory([FromBody] JsonElement jsonElement)
        {
            string jsonString = jsonElement.GetRawText();
            BsonDocument document = BsonDocument.Parse(jsonString);
            string categoryName = document["Name"].AsString;

            var collection = _database.GetCollection<BsonDocument>("Category");
            var existingCategory = collection.Find(Builders<BsonDocument>.Filter.Eq("Name", categoryName)).FirstOrDefault();

            if (existingCategory != null)
            {
                return Conflict(new { message = $"Category '{categoryName}' already exists." });
            }

            collection.InsertOne(document);
            _globalService.LogAction($"Category '{categoryName}' created.", "Created");

            return Ok(new { message = "Category created successfully." });
        }

        // Get a category by name
        [HttpGet("GetCategoryByName/{name}")]
        public IActionResult GetCategory(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest(new { message = "Category name is required." });
            }

            var collection = _database.GetCollection<BsonDocument>("Category");
            var filter = Builders<BsonDocument>.Filter.Eq("Name", name);

            // Find the category
            var category = collection.Find(filter).FirstOrDefault();

            if (category == null)
            {
                return NotFound(new { message = $"Category '{name}' not found." });
            }

            // Return the found category as a JSON object
            return Ok(BsonTypeMapper.MapToDotNetValue(category));
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




        [HttpPut("UpdateCategoryByName/{name}")]
        public IActionResult UpdateCategoryByName(string name, [FromBody] JsonElement jsonElement)
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
    }
}
