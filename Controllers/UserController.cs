using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using backend.Services;

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



        [HttpGet]
        public IActionResult Index()
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
        [HttpGet("GetUser")]
        public IActionResult GetUser()
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

        //// Create a new document
        //[HttpPost("Create")]
        //public IActionResult Create([FromBody] JsonElement jsonElement)
        //{
        //    // Convert the JSON element to a JSON string
        //    string jsonString = jsonElement.GetRawText();

        //    // Parse the JSON string to a BsonDocument
        //    BsonDocument document = BsonDocument.Parse(jsonString);

        //    // Get the collection and insert the document
        //    var collection = _database.GetCollection<BsonDocument>("User");
        //    collection.InsertOne(document);

        //    return Ok();
        //}


        // Create a new document with a sequential ID
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] JsonElement jsonElement)
        {
            // Convert the JSON element to a JSON string
            string jsonString = jsonElement.GetRawText();

            // Parse the JSON string to a BsonDocument
            BsonDocument document = BsonDocument.Parse(jsonString);

            // Get the collection for the items
            var collection = _database.GetCollection<BsonDocument>("User");

            // Get the collection for the sequence counter
            var counterCollection = _database.GetCollection<BsonDocument>("Sequence");

            // Increment the sequence value and retrieve the new value
            var filter = Builders<BsonDocument>.Filter.Eq("_id", "UserId");
            var update = Builders<BsonDocument>.Update.Inc("sequenceValue", 1);
            var options = new FindOneAndUpdateOptions<BsonDocument>
            {
                ReturnDocument = ReturnDocument.After // Return the updated document
            };

            // Find and increment the sequence value
            var counterDocument = await counterCollection.FindOneAndUpdateAsync(filter, update, options);
            var newSequenceValue = counterDocument["sequenceValue"].AsInt32;

            // Add the sequence value to the new document (as a sequential 'count' field)
            document.Add("Count", newSequenceValue);

            // Insert the new document with the sequence value
            await collection.InsertOneAsync(document);

            return Ok();
        }



        [HttpGet]
        public IActionResult Hello()
        {
            // Convert the JSON element to a JSON string
           

            return Ok();
        }

        // Update an existing document without touching the Count field
        [HttpPut("Update/{Count}")]
        public IActionResult Update(int Count, [FromBody] JsonElement jsonElement)
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

            // Remove the 'Count' field from the document if it exists, so it won't be updated
            document.Remove("Count");

            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("Count", Count);

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
        [HttpGet("GetById/{Count}")]
        public IActionResult GetById(int Count)
        {
            // Ensure id is a valid ObjectId
            
            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("Count", Count);

            // Find the document
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null)
            {
                return NotFound("Document not found.");
            }

            // Return the document as JSON
            return Json(document.ToJson());
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] Login loginRequest)
        {
            // Check if the request body is null
            if (loginRequest == null)
            {
                return BadRequest("Invalid login request.");
            }

            // Get the collection from the database
                var collection = _database.GetCollection<User>("User");
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq("username", loginRequest.Username),
                Builders<User>.Filter.Eq("pin", loginRequest.Pin)
            );
            var user = collection.Find(filter).FirstOrDefault();

            if (user == null)
            {
                return NotFound("No matching documents found.");
            }
            _globalService.username = user.username;

            // Return the matching user
            return Ok(user);
        }



        // Delete a document
        [HttpDelete("Delete/{Count}")]
        public IActionResult Delete(int Count)
        {
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("Count", Count);
            var deleteResult = collection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
