using backend.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace backend.Controllers
{
    [Route("api/[controller]")]

    public class CustomerNumberController : Controller
    {
        private readonly ILogger<CustomerNumberController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public CustomerNumberController(ILogger<CustomerNumberController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }


    
        [HttpGet("GetAllCustomerNumbers")]
        public IActionResult GetAllCategories()
        {
            var collection = _database.GetCollection<BsonDocument>("CustomerNumber");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to .NET objects for proper serialization
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        [HttpPost("CreateCustomerNumber")]
        public IActionResult CreateCategory([FromBody] JsonElement jsonElement)
        {
            string jsonString = jsonElement.GetRawText();
            BsonDocument document = BsonDocument.Parse(jsonString);
            string PhoneNumber = document["PhoneNumber"].AsString;

            var collection = _database.GetCollection<BsonDocument>("PhoneNumber");
            var existingCategory = collection.Find(Builders<BsonDocument>.Filter.Eq("PhoneNumber", PhoneNumber)).FirstOrDefault();

            if (existingCategory != null)
            {
                return Conflict(new { message = $"Phone Number '{PhoneNumber}' already exists." });
            }

            collection.InsertOne(document);
            _globalService.LogAction($"Phone Number '{PhoneNumber}' created.", "Created");

            return Ok(new { message = "Phone Number created successfully." });
        }


        [HttpGet("GetCustomerNumberByPhoneNumber/{PhoneNumber}")]
        public IActionResult GetCategory(string PhoneNumber)
        {
            if (string.IsNullOrEmpty(PhoneNumber))
            {
                return BadRequest(new { message = "PhoneNumber  is required.asfsf" });
            }
            var collection = _database.GetCollection<BsonDocument>("PhoneNumber");
            var filter = Builders<BsonDocument>.Filter.Eq("PhoneNumber", PhoneNumber);        
            var category = collection.Find(filter).FirstOrDefault();
            if (category == null)
            {
                return NotFound(new { message = $"Phone Number '{PhoneNumber}' AINT THERE" });
            }
            return Ok(BsonTypeMapper.MapToDotNetValue(category));
        }


        [HttpDelete("DeleteCustomerNumberByPhoneNumber/{PhoneNumber}")]
        public IActionResult DeleteCategoryByName(string PhoneNumber)
        {
            var categoryCollection = _database.GetCollection<BsonDocument>("PhoneNumber");

            var filter = Builders<BsonDocument>.Filter.Eq("PhoneNumber", PhoneNumber); // Filter by name
            var deleteResult = categoryCollection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound($"Phone Number '{PhoneNumber}' not found.");
            }

            _globalService.LogAction($"Phone Number '{PhoneNumber}' deleted.", "Delete");

            return Ok($"Phone Number '{PhoneNumber}' successfully deleted.");
        }





        [HttpPut("UpdateCustomerNumberByName/{PhoneNumber}")]
        public IActionResult UpdateItem(string PhoneNumber, [FromBody] JsonElement jsonElement)
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

            if (string.IsNullOrEmpty(PhoneNumber))
            {
                return BadRequest("Invalid name.");
            }

            if (!document.Contains("PhoneNumber"))
            {
                return BadRequest("The 'PhoneNumber' field is required.");
            }

            string newCustomerItemName = document["PhoneNumber"].AsString;


            var collection = _database.GetCollection<BsonDocument>("CustomerNumber");

            var existingItem = collection.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("PhoneNumber", PhoneNumber)
            )).FirstOrDefault();

            if (existingItem != null && newCustomerItemName != PhoneNumber)
            {
                return Conflict(new { message = "Customer  with the same phonenumber already exists." });
            }

            var filter = Builders<BsonDocument>.Filter.Eq("PhoneNumber", PhoneNumber);

            var updateResult = collection.ReplaceOne(filter, document);

            if (updateResult.MatchedCount == 0)
            {
                return NotFound("Item not found.");
            }

            _globalService.LogAction(PhoneNumber, "Update");

            return Ok("CustomerPhoneNumber updated successfully.");
        }

    }
}
