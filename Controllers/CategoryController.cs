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
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public CategoryController(IMongoDatabase database, GlobalService globalService)
        {

            _database = database;
            _globalService = globalService;
        }

        // Get all categories
        [HttpGet("GetAllCategories")]
        public IActionResult GetAllCategories()
        {
            var collection = _database.GetCollection<BsonDocument>("Category");
            var documents = collection.Find(new BsonDocument())
                .Sort(Builders<BsonDocument>.Sort.Ascending("Order"))
                .ToList();


            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        public class CategoryOrderRequest
        {
            public List<CategoryOrderUpdate> Categories { get; set; }
        }
        public class CategoryOrderUpdate
        {
            public string Name { get; set; }
            public int Order { get; set; }
        }

        [HttpPost("OrderCategories")]
        public async Task<IActionResult> UpdateCategoryOrder([FromBody] CategoryOrderRequest request)
        {
            // Log the incoming request
            Console.WriteLine("Received UpdateCategoryOrder request");
            Console.WriteLine($"Request Body: {JsonSerializer.Serialize(request)}");

            if (request == null || request.Categories == null || request.Categories.Count == 0)
            {
                Console.WriteLine("Error: No categories provided in the request");
                return BadRequest("No categories provided.");
            }

            var categoryCollection = _database.GetCollection<BsonDocument>("Category");

            foreach (var categoryOrder in request.Categories)
            {
                // Log each category update attempt
                Console.WriteLine($"Processing category with Name: {categoryOrder.Name}, New Order: {categoryOrder.Order}");

                var filter = Builders<BsonDocument>.Filter.Eq("Name", categoryOrder.Name);
                var update = Builders<BsonDocument>.Update.Set("Order", categoryOrder.Order);

                try
                {
                    var result = await categoryCollection.UpdateOneAsync(filter, update);

                    if (result.ModifiedCount == 0)
                    {
                        Console.WriteLine($"Warning: Category with Name {categoryOrder.Name} not found or not modified.");
                    }
                    else
                    {
                        Console.WriteLine($"Success: Updated category {categoryOrder.Name} to order {categoryOrder.Order}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating category with Name {categoryOrder.Name}: {ex.Message}");
                    return StatusCode(500, $"Error updating category with Name {categoryOrder.Name}: {ex.Message}");
                }
            }

            Console.WriteLine("All categories processed successfully");
            return Ok("Category order updated successfully.");
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
                return Conflict(new { message = $"Category '{categoryName}' already exists. Bro shu bek man              fdfdfdf" });
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
            // Validate the name parameter
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Category name cannot be null or empty.");
            }

            var categoryCollection = _database.GetCollection<BsonDocument>("Category");
            var itemCollection = _database.GetCollection<BsonDocument>("Item");

            var filter2 = Builders<BsonDocument>.Filter.Eq("CategoryName", name);
            var itemExists = itemCollection.Find(filter2).Any();

            if (itemExists)
            {
                return BadRequest($"Cannot delete category '{name}' because items are associated with it.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("Name", name);
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


            if (!document.Contains("Name"))
            {
                return BadRequest("The 'name' field is required in the document.");
            }


            string newCategoryName = document["Name"].AsString;


            var collection = _database.GetCollection<BsonDocument>("Category");


            var existingCategory = collection.Find(Builders<BsonDocument>.Filter.Eq("Name", newCategoryName)).FirstOrDefault();
            if (existingCategory != null && newCategoryName != name)
            {
                return Conflict(new { message = $"Category '{newCategoryName}' already exists." });
            }


            var filter = Builders<BsonDocument>.Filter.Eq("Name", name);


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