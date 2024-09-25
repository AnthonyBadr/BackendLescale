using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson.Serialization.IdGenerators;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ZstdSharp.Unsafe;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class IngredientsController : Controller
    {

        private readonly ILogger<IngredientsController> _logger;
        private readonly IMongoDatabase _database;

        public IngredientsController(ILogger<IngredientsController> logger, IMongoDatabase database)
        {
            _logger = logger;
            _database = database;
        }
        public IActionResult Index()
        {
            var collection = _database.GetCollection<BsonDocument>("Ingredients");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Create a list to store the results
            var result = new List<object>();

            foreach (var doc in documents)
            {
                // Check if the document contains the "Ingredient" field and it is a document
                if (doc.Contains("Ingredient") && doc["Ingredient"].IsBsonDocument)
                {
                    var ingredientsDoc = doc["Ingredient"].AsBsonDocument;
                    var ingredientDict = new Dictionary<string, List<string>>();

                    // Iterate through each field in the "Ingredient" document
                    foreach (var ingredientField in ingredientsDoc.Elements)
                    {
                        // Check if the field value is an array
                        if (ingredientField.Value.IsBsonArray)
                        {
                            Console.WriteLine(ingredientField);
                            var ingredientValues = ingredientField.Value.AsBsonArray;
                            var valuesList = new List<string>();

                            // Collect all the values for this ingredient
                            foreach (var value in ingredientValues)
                            {
                                Console.WriteLine(value);
                                valuesList.Add(value.ToString());
                            }

                            // Add the ingredient and its values to the dictionary
                            ingredientDict.Add(ingredientField.Name, valuesList);
                        }
                    }

                    // Add the processed ingredient data to the result list
                    result.Add(ingredientDict);
                }
            }

            // Return the result as JSON
            return Json(result);
        }





        [HttpGet("GetIngredientsByCategory/{categoryName}")]
        public async Task<IActionResult> GetIngredientsByCategory(string categoryName)
        {
            var collection = _database.GetCollection<BsonDocument>("Ingredients");

            // Create a filter to find documents that have the category as a key (e.g., "Dairy")
            var filter = Builders<BsonDocument>.Filter.Exists(categoryName);

            // Find documents that match the filter
            var documents = await collection.Find(filter).ToListAsync();

            if (documents.Count == 0)
            {
                return NotFound($"No documents found for category '{categoryName}'.");
            }

            // Convert documents to a list of JSON objects
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }



        [HttpGet("GetAllIngredients")]
        public async Task<IActionResult> GetAllOrders()
        {
            var collection = _database.GetCollection<BsonDocument>("Ingredients");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to a list of JSON objects
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }



        //    {
        //"Dairy": []
        //}

        [HttpDelete("RemoveACategory")]
        public async Task<IActionResult> RemoveACategory([FromBody] JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Get the first property from the JSON object (the category name)
                var property = jsonElement.EnumerateObject().FirstOrDefault();
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // Get the category name from the JSON (e.g., "Dairy")
                    string categoryName = property.Name;

                    // Get the collection
                    var collection = _database.GetCollection<BsonDocument>("Ingredients");

                    // Create a filter to find documents with the specified category name
                    var filter = Builders<BsonDocument>.Filter.Exists(categoryName);

                    // Delete the document(s) containing the category
                    var deleteResult = await collection.DeleteManyAsync(filter);

                    if (deleteResult.DeletedCount > 0)
                    {
                        return Ok($"Document(s) with category '{categoryName}' have been deleted.");
                    }
                    else
                    {
                        return NotFound($"No document found with the category '{categoryName}'.");
                    }
                }
                else
                {
                    return BadRequest("The provided JSON does not contain an array value for the category.");
                }
            }
            else
            {
                return BadRequest("The JSON root is not an object.");
            }
        }


        //    {
        //    oldkey:
        //        newkey:
        //}

        [HttpPut("UpdateKeyName")]
        public async Task<IActionResult> UpdateKeyName([FromBody] JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Extract OldKey and NewKey from the JSON
                if (jsonElement.TryGetProperty("OldKey", out JsonElement oldKeyElement) &&
                    jsonElement.TryGetProperty("NewKey", out JsonElement newKeyElement))
                {
                    string oldKey = oldKeyElement.GetString();
                    string newKey = newKeyElement.GetString();

                    if (string.IsNullOrEmpty(oldKey) || string.IsNullOrEmpty(newKey))
                    {
                        return BadRequest("OldKey and NewKey cannot be null or empty.");
                    }

                    try
                    {
                        // Get the collection
                        var collection = _database.GetCollection<BsonDocument>("Ingredients");

                        // Create an update filter to match documents containing the old key
                        var filter = Builders<BsonDocument>.Filter.Exists(oldKey);

                        // Create an update definition to rename the old key to the new key
                        var update = Builders<BsonDocument>.Update.Rename(oldKey, newKey);

                        // Perform the update
                        var updateResult = await collection.UpdateManyAsync(filter, update);

                        if (updateResult.ModifiedCount > 0)
                        {
                            return Ok($"Key '{oldKey}' has been updated to '{newKey}' in {updateResult.ModifiedCount} document(s).");
                        }
                        else
                        {
                            return NotFound($"No documents found with the key '{oldKey}'.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the exception (you can use a logging framework)
                        return StatusCode(500, $"Internal server error: {ex.Message}");
                    }
                }
                else
                {
                    return BadRequest("The JSON must contain 'OldKey' and 'NewKey' properties.");
                }
            }
            else
            {
                return BadRequest("The JSON root is not an object.");
            }
        }


        [HttpPost("CreateIngredient")]
        public async Task<IActionResult> CreateIngredient([FromBody] JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Check if the JSON contains at least one ingredient category
                var firstProperty = jsonElement.EnumerateObject().FirstOrDefault();
                if (firstProperty.Value.ValueKind == JsonValueKind.Array)
                {
                    // Get the first property name (e.g., "Dairy")
                    string typeOfIngredient = firstProperty.Name;

                    // Convert the JSON element to a JSON string
                    string jsonString = jsonElement.GetRawText();

                    // Parse the JSON string to a BsonDocument
                    BsonDocument document = BsonDocument.Parse(jsonString);

                    // Get the collection
                    var collection = _database.GetCollection<BsonDocument>("Ingredients");

                    // Check if the ingredient type already exists
                    var filter = Builders<BsonDocument>.Filter.Exists(typeOfIngredient);
                    var existingDocument = await collection.Find(filter).FirstOrDefaultAsync();

                    if (existingDocument != null)
                    {
                        // Return a response if the ingredient type already exists
                        return Conflict($"Ingredient type '{typeOfIngredient}' already exists.");
                    }

                    // Insert the new document
                    await collection.InsertOneAsync(document);

                    return Ok("Document created successfully.");
                }
                else
                {
                    return BadRequest("The root element should contain an array of ingredients.");
                }
            }
            else
            {
                return BadRequest("The JSON root is not an object.");
            }
        }




        //    {
        //    type_of_Ingredient:
        //        name:
        //        price
        //}
        [HttpPost("AddanIngredientToALiSt")]
        public async Task<IActionResult> AddanIngredientToALiSt([FromBody] JsonElement jsonElement)
        {
            string type_of_Ingredient = jsonElement.GetProperty("type_of_Ingredient").GetString();
            string name = jsonElement.GetProperty("Name").GetString();
            double price = jsonElement.GetProperty("Price").GetDouble();

            var filter = Builders<BsonDocument>.Filter.Exists(type_of_Ingredient);

            // Define the update to add a new ingredient with both name and price
            var newIngredient = new BsonDocument
    {
        { "Name", name },
        { "Price", price }
    };

            var update = Builders<BsonDocument>.Update.AddToSet(type_of_Ingredient, newIngredient);
            var collection = _database.GetCollection<BsonDocument>("Ingredients");

            // Update the document
            var result = await collection.UpdateOneAsync(filter, update);

            // Output the result
            Console.WriteLine(result.ModifiedCount > 0 ? "Ingredient added successfully!" : "No document was updated.");

            return Ok();
        }

        //    {
        //    type_of_Ingredient:
        //        name:
        //}
        [HttpPost("removeASpecifiedIngredientFromTheList")]
        public async Task<IActionResult> removeASpecifiedIngredientFromTheList([FromBody] JsonElement jsonElement)
        {
            // Ensure that these properties are present in the JSON request
            if (!jsonElement.TryGetProperty("type_of_Ingredient", out JsonElement typeElement) ||
                !jsonElement.TryGetProperty("Name", out JsonElement nameElement))
            {
                return BadRequest("Invalid input.");
            }

            string type_of_Ingredient = typeElement.GetString();
            string name = nameElement.GetString();

            // Define a filter to find the document containing the specified ingredient type
            var filter = Builders<BsonDocument>.Filter.Exists(type_of_Ingredient);

            // Define the update to remove one occurrence of the ingredient from the specified type
            var update = Builders<BsonDocument>.Update.Pull(type_of_Ingredient, new BsonDocument { { "Name", name } });

            var collection = _database.GetCollection<BsonDocument>("Ingredients");

            // Update the document
            var result = await collection.UpdateOneAsync(filter, update);

            // Output the result
            Console.WriteLine(result.ModifiedCount > 0 ? "Ingredient removed successfully!" : "No document was updated.");

            return Ok();
        }

       



    }
}
