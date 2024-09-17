using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using MongoDB.Bson.Serialization.IdGenerators;
using static System.Runtime.InteropServices.JavaScript.JSType;

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


    //    {
    //"Dairy": []
    //}

    [HttpDelete("Remove")]
        public async Task<IActionResult> Remove([FromBody] JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Get the first property from the JSON object
                var property = jsonElement.EnumerateObject().FirstOrDefault();
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // Get the variable name from the JSON
                    string variableName = property.Name;

                    // Get the collection
                    var collection = _database.GetCollection<BsonDocument>("Ingredients");

                    // Create a filter to find documents with the specified variable name
                    var filter = Builders<BsonDocument>.Filter.Exists($"Ingredients.{variableName}");

                    // Delete the document
                    var deleteResult = await collection.DeleteManyAsync(filter);

                    if (deleteResult.DeletedCount > 0)
                    {
                        return Ok($"Document(s) with the variable name '{variableName}' have been removed.");
                    }
                    else
                    {
                        return NotFound($"No document found with the variable name '{variableName}'.");
                    }
                }
                else
                {
                    return BadRequest("The provided JSON does not contain an array value for removal.");
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

                    // Get the collection
                    var collection = _database.GetCollection<BsonDocument>("Ingredients");

                    // Create an update filter to match documents containing the old key
                    var filter = Builders<BsonDocument>.Filter.Exists($"Ingredient.{oldKey}");

                    // Create an update definition to rename the old key to the new key
                    var update = Builders<BsonDocument>.Update.Rename($"Ingredient.{oldKey}", $"Ingredient.{newKey}");

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



        //        {
        //  "Ingredient": {
        //    "Dairy": ["cheese", "milk"]
        //    }
        //}


        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                // Get the 'Ingredients' property
                if (jsonElement.TryGetProperty("Ingredient", out JsonElement ingredients))
                {
                    // Check if 'Ingredients' is an object
                    if (ingredients.ValueKind == JsonValueKind.Object)
                    {
                        // Get the first property name
                        var property = ingredients.EnumerateObject().FirstOrDefault();
                        string typeOfIngredient = property.Name;

                        // Convert the JSON element to a JSON string
                        string jsonString = jsonElement.GetRawText();

                        // Parse the JSON string to a BsonDocument
                        BsonDocument document = BsonDocument.Parse(jsonString);

                        // Get the collection
                        var collection = _database.GetCollection<BsonDocument>("Ingredients");

                        // Check if the ingredient type already exists
                        var filter = Builders<BsonDocument>.Filter.Exists($"Ingredients.{typeOfIngredient}");
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
                        return BadRequest("The 'Ingredients' property is not an object.");
                    }
                }
                else
                {
                    return BadRequest("The 'Ingredients' property is missing.");
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
    //}
        [HttpPost("UpdateASpecifiedIngredient")]
        public async Task<IActionResult> UpdateASpecifiedIngredientAsync([FromBody] JsonElement jsonElement)
        {
            
            string type_of_Ingredient = jsonElement.GetProperty("type_of_Ingredient").GetString();
            string name = jsonElement.GetProperty("name").GetString();
            var filter = Builders<BsonDocument>.Filter.Exists($"Ingredient.{type_of_Ingredient}");
            // Define the update to add a new sauce to the 'Sauces' array
            var update = Builders<BsonDocument>.Update.AddToSet($"Ingredient.{type_of_Ingredient}", name);
            var collection = _database.GetCollection<BsonDocument>("Ingredients");

            // Update the document
            var result = await collection.UpdateOneAsync(filter, update);

            // Output the result
            Console.WriteLine(result.ModifiedCount > 0 ? "Sauce added successfully!" : "No document was updated.");

            return Ok();
        }

        //    {
        //    type_of_Ingredient:
        //        name:
        //}
        [HttpPost("removeASpecifiedIngredient")]
        public async Task<IActionResult> removeASpecifiedIngredientAsync([FromBody] JsonElement jsonElement)
        {

            // Ensure that these properties are present in the JSON request
            if (!jsonElement.TryGetProperty("type_of_Ingredient", out JsonElement typeElement) ||
                !jsonElement.TryGetProperty("name", out JsonElement nameElement))
            {
                return BadRequest("Invalid input.");
            }

            string type_of_Ingredient = typeElement.GetString();
            string name = nameElement.GetString();
            var filter = Builders<BsonDocument>.Filter.Exists($"Ingredient.{type_of_Ingredient}");

            // Define the update to remove one occurrence of the ingredient from the specified type
            var update = Builders<BsonDocument>.Update.Pull($"Ingredient.{type_of_Ingredient}", name);
            var collection = _database.GetCollection<BsonDocument>("Ingredients");

            // Update the document
            var result = await collection.UpdateOneAsync(filter, update);

            // Output the result
            Console.WriteLine(result.ModifiedCount > 0 ? "Ingredient removed successfully!" : "No document was updated.");

            return Ok();
        }


        //      {
        //"Sauces": ["ketchup", "mustard"]
        //  }


    }
}
