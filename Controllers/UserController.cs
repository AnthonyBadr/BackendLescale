using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using backend.Services;
using System.Text;
using System.Security.Cryptography;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;
        public UserController(ILogger<UserController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //use this 
        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            try
            {
                // Get the 'User' collection from the database
                var collection = _database.GetCollection<BsonDocument>("User");

                // Find all documents in the 'User' collection
                var users = collection.Find(new BsonDocument()).ToList();

                // Check if there are any users
                if (users == null || users.Count == 0)
                {
                    return NotFound("No users found.");
                }

                // Convert BsonDocuments to dynamic objects and return them as JSON
                var jsonUsers = users.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

                return Ok(jsonUsers);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 internal server error
                _logger.LogError($"Error fetching users: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

   
        // Create a new document with a sequential ID
        [HttpPost("CreateUser")]
        public async Task<IActionResult> Create([FromBody] JsonElement jsonElement)
        {
            try
            {
                // Parse the JSON element
                var document = BsonDocument.Parse(jsonElement.ToString());

                // Extract the password
                if (document.TryGetValue("Pin", out BsonValue passwordValue))
                {
                    string password = passwordValue.AsString;

                    // Hash the password using SHA-256
                    using (SHA256 sha256Hash = SHA256.Create())
                    {
                        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                        StringBuilder builder = new StringBuilder();
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            builder.Append(bytes[i].ToString("x2"));
                        }
                        string hashedPassword = builder.ToString();

                        // Replace the password in the document with the hashed password
                        document.Set("Pin", hashedPassword);
                    }
                }

                // Add a new sequence value for the user ID
                int newSequenceValue = _globalService.SequenceIncrement("UserId").GetAwaiter().GetResult();
                document.Add("Id", newSequenceValue);

                // Insert the document into the collection
                var collection = _database.GetCollection<BsonDocument>("User");
                await collection.InsertOneAsync(document);

                return Ok(new { Id = newSequenceValue, Message = "User created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }




        // Update an existing document without touching the Count field
        [HttpPut("UpdateUserById/{Id}")]
        public IActionResult UpdateUserById(int Id, [FromBody] JsonElement jsonElement)
        {
            // Convert the JSON element to a JSON string
            string jsonString = jsonElement.GetRawText();

            // Parse the JSON string to a BsonDocument
            BsonDocument document;
            try
            {
                document = BsonDocument.Parse(jsonString);
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }

            // Remove the 'Id' field from the document if it exists, so it won't be updated
            document.Remove("Id");

            // Check if the document contains a password field and hash it
            if (document.Contains("Pin"))
            {
                string pin = document["Pin"].AsString;

                // Hash the password using SHA-256
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(pin));
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("x2"));
                    }
                    string hashedPin = builder.ToString();

                    // Replace the password in the document with the hashed password
                    document.Set("Pin", hashedPin);
                }
            }

            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", Id);

            // Create an update definition with the remaining fields in the document
            var updateDefinition = new BsonDocument("$set", document);

            // Update the document, keeping the 'Count' field unchanged
            var updateResult = collection.UpdateOne(filter, updateDefinition);

            if (updateResult.MatchedCount == 0)
            {
                return NotFound("Document not found.");
            }

            return Ok("Document updated successfully.");
        }


        // Get a specific document by ID
        [HttpGet("GetUserById/{Id}")]
        public IActionResult GetUserById(int Id)
        {
            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", Id);

            // Find the document
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null)
            {
                return NotFound(new { Message = "Document not found." });
            }

            // Return the document as a JSON object
            return Ok(MongoDB.Bson.BsonTypeMapper.MapToDotNetValue(document));
        }





        // Delete a document
        [HttpDelete("DeleteUserByUser/{Id}")]
        public IActionResult DeleteUserByUser(int Id)
        {
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", Id);
            var deleteResult = collection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
