using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using backend.Services;
using static MongoDB.Driver.WriteConcern;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;
        public PaymentController(ILogger<PaymentController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("Create")]
        public IActionResult Create([FromBody] JsonElement jsonElement)
        {
            try
            {
                // Convert the JSON element to a JSON string
                string jsonString = jsonElement.GetRawText();
                var GrossCollection = _database.GetCollection<Gross>("Gross");

                // Find the first 'Gross' document with a status of 'Pending'
                var filtergross = Builders<Gross>.Filter.Eq(g => g.status, "Pending");
                var theGross = GrossCollection.Find(filtergross).FirstOrDefault();

                if (theGross == null)
                {
                    return NotFound("No 'Gross' document with 'Pending' status found.");
                }

                // Parse the JSON string to a BsonDocument
                BsonDocument document = BsonDocument.Parse(jsonString);
                document["date"] = DateTime.Now.ToString("MM/dd/yyyy");
                document["grossnumber"] = theGross.grossNumber; // Assuming 'grossnumber' exists in Gross
                double amount = double.Parse(document["amount"].ToString());
                // Get the 'Payment' collection and insert the new payment document
                var collection = _database.GetCollection<BsonDocument>("Payment");
                collection.InsertOne(document);

                // Increment the 'grossnumber' in the 'Gross' document by 5
                var update = Builders<Gross>.Update.Inc(g => g.totalGross, -amount);
                GrossCollection.UpdateOne(filtergross, update);

                return Ok("Payment created and gross number updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating payment: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }




        [HttpPut("Update/{id}")]
        public IActionResult Update(string id, [FromBody] JsonElement jsonElement)
        {

            var GrossCollection = _database.GetCollection<Gross>("Gross");

            // Find the first 'Gross' document with a status of 'Pending'
            var filtergross = Builders<Gross>.Filter.Eq(g => g.status, "Pending");
            var theGross = GrossCollection.Find(filtergross).FirstOrDefault();
            if (theGross == null)
            {
                return NotFound("No 'Gross' document with 'Pending' status found.");
            }

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


            double amount = double.Parse(document["amount"].ToString());

            if(amount > 0)
            {
                UpdateGross(amount);
            }
            else
            {
                UpdateGross(-amount);
            }


            // Ensure id is a valid ObjectId
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest("Invalid ID format.");
            }

            // Get the collection and create the filter
            var collection = _database.GetCollection<BsonDocument>("Payment");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

            // Replace the document
            var updateResult = collection.ReplaceOne(filter, document);

            
          


            if (updateResult.MatchedCount == 0)
            {
                return NotFound("Document not found.");
            }

            return Ok("Document updated successfully.");
        }

        private void UpdateGross(double amount)
        {

            var GrossCollection = _database.GetCollection<Gross>("Gross");

            // Find the first 'Gross' document with a status of 'Pending'
            var filtergross = Builders<Gross>.Filter.Eq(g => g.status, "Pending");
            var theGross = GrossCollection.Find(filtergross).FirstOrDefault();
            // Increment the 'grossnumber' in the 'Gross' document by 5
            var update = Builders<Gross>.Update.Inc(g => g.totalGross, -amount);
            GrossCollection.UpdateOne(filtergross, update);

          
        }

        [HttpGet("GetAllPayments")]
        public IActionResult GetAllPayments()
        {
            try
            {
                // Get the 'User' collection from the database
                var collection = _database.GetCollection<BsonDocument>("Payment");

                // Find all documents in the 'User' collection
                var payment = collection.Find(new BsonDocument()).ToList();

                // Check if there are any users
                if (payment == null || payment.Count == 0)
                {
                    return NotFound("No users found.");
                }

                // Convert BsonDocuments to dynamic objects and return them as JSON
                var jsonUsers = payment.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

                return Ok(jsonUsers);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 internal server error
                _logger.LogError($"Error fetching users: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }



        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(string id)
        {

            var GrossCollection = _database.GetCollection<Gross>("Gross");


            double paymentAmount = 0;
            // Find the first 'Gross' document with a status of 'Pending'
            var filtergross = Builders<Gross>.Filter.Eq(g => g.status, "Pending");
            var theGross = GrossCollection.Find(filtergross).FirstOrDefault();
            if (theGross == null)
            {
                return NotFound("No 'Gross' document with 'Pending' status found.");
            }
            var collection = _database.GetCollection<BsonDocument>("Payment");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            var payment = collection.Find(filter).FirstOrDefault();

            if (payment != null)
            {
                 paymentAmount = payment["amount"].AsDouble;
                Console.WriteLine($"Payment amount: {paymentAmount}");
            }
            else
            {
                Console.WriteLine("Payment not found.");
            }

            var deleteResult = collection.DeleteOne(filter);

            if (deleteResult.DeletedCount == 0)
            {
                return NotFound();
            }
         

            if (paymentAmount > 0)
            {
                UpdateGross(paymentAmount);
            }
            else
            {
                UpdateGross(-paymentAmount);
            }


            return Ok();
        }



    }
}
