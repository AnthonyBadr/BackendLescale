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

        public IActionResult Index()
        {
            int x = 0;
            var collection = _database.GetCollection<BsonDocument>("User");
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Create a new document
        [HttpPost("Create")]
        public IActionResult Create([FromBody] JsonElement jsonElement)
        {
            // Convert the JSON element to a JSON string
            string jsonString = jsonElement.GetRawText();

            // Parse the JSON string to a BsonDocument
            BsonDocument document = BsonDocument.Parse(jsonString);

            // Get the collection and insert the document
            var collection = _database.GetCollection<BsonDocument>("User");
            collection.InsertOne(document);

            return Ok();
        }

        [HttpGet]
        public IActionResult Hello()
        {
            // Convert the JSON element to a JSON string
           

            return Ok();
        }


        // Update an existing document
        [HttpPut("Update/{id}")]
        public IActionResult Update(string id, [FromBody] JsonElement jsonElement)
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

            // Ensure id is a valid ObjectId
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest("Invalid ID format.");
            }

            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

            // Replace the document
            var updateResult = collection.ReplaceOne(filter, document);

            if (updateResult.MatchedCount == 0)
            {
                return NotFound("Document not found.");
            }

            return Ok("Document updated successfully.");
        }


        // Get a specific document by ID
        [HttpGet("GetById/{id}")]
        public IActionResult GetById(string id)
        {
            // Ensure id is a valid ObjectId
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest("Invalid ID format.");
            }

            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

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
        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(string id)
        {
            var collection = _database.GetCollection<BsonDocument>("User");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            var deleteResult = collection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
