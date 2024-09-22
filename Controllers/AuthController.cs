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
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;
        public AuthController(ILogger<AuthController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] JsonElement jsonElement)
        {
            try
            {
                // Parse the JSON element manually to avoid issues
                string json = jsonElement.GetRawText();
                var document = BsonDocument.Parse(json);
                string Username = document["Username"].AsString;
                string Pin = document["Pin"].AsString;

                // Retrieve the stored hashed password from the database
                var collection = _database.GetCollection<BsonDocument>("User");
                var filter = Builders<BsonDocument>.Filter.Eq("Username", Username);
                var userDocument = await collection.Find(filter).FirstOrDefaultAsync();

                if (userDocument == null)
                {
                    return Unauthorized(new { Message = "Invalid username or password." });
                }

                string storedHashedPassword = userDocument["Pin"].AsString;

                // Hash the provided pin using SHA-256
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(Pin));
                    StringBuilder builder = new StringBuilder();
                    foreach (var b in bytes)
                    {
                        builder.Append(b.ToString("x2"));
                    }
                    string hashedPassword = builder.ToString();

                    // Compare the hashed passwords
                    if (hashedPassword == storedHashedPassword)
                    {
                        _globalService.username = Username;
                        // Return the user document without any deserialization into booleans
                        return Ok(userDocument.ToJson());
                    }
                    else
                    {
                        return Unauthorized(new { Message = "Invalid username or pin." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}


