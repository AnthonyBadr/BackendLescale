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





        [HttpGet("GetPaymentByPaymentNumber/{paymentNumber}")]
        public async Task<IActionResult> GetOrderByOrderNumber(int paymentNumber)
        {
            var collection = _database.GetCollection<BsonDocument>("Payment");

            // Create a filter to find the order with the specified OrderNumber
            var filter = Builders<BsonDocument>.Filter.Eq("PaymentNumber", paymentNumber);

            // Find the order in the collection
            var paymentDocument = await collection.Find(filter).FirstOrDefaultAsync();
            var document = BsonTypeMapper.MapToDotNetValue(paymentDocument);
            if (paymentDocument == null)
            {
                // Return a 404 if the order is not found
                return NotFound($"Order with OrderNumber {paymentNumber} not found.");
            }

            // Return the found order
            return Json(document);
        }



        [HttpGet("GetAllPayment")]
        public async Task<IActionResult> GetAllPayment([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var collection = _database.GetCollection<BsonDocument>("Payment");

            // Apply pagination using Skip and Limit
            var documents = await collection.Find(new BsonDocument())
                                            .Skip((pageNumber - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

            if (documents == null || documents.Count == 0)
            {
                return NotFound("No payment records found.");
            }

            // Convert documents to a list of JSON objects
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the paginated data as JSON
            return Json(jsonResult);
        }



        [HttpPost("CreatePayment")]
        public async  Task<IActionResult> Create([FromBody] JsonElement jsonElement)
        {
            try
            {
                // Convert the JSON element to a JSON string
                int GrossNumber = 0;
                string jsonString = jsonElement.GetRawText();
                var GrossCollection = _database.GetCollection<BsonDocument>("Payment");
                BsonDocument document = BsonDocument.Parse(jsonString);

                GrossNumber = UpdateTheGrossNew(-document["Amount"].AsDouble).Result;

                int newSequenceValue = _globalService.SequenceIncrement("PaymentNumber").GetAwaiter().GetResult();
                document.Add("PaymentNumber", newSequenceValue);
                document.Add("Created_by", _globalService.username);
                document.Add("DateOfPayment", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                document.Add("GrossNumber", GrossNumber);
                await GrossCollection.InsertOneAsync(document);
                return Ok(new { message = "Order created successfully", PaymentNumber = newSequenceValue.ToString(), document["Amount"].AsDouble });

                // Find the first 'Gross' document with a status of 'Pending'

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating payment: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }



        [HttpPut("UpdatePaymentByPaymentNumber/{paymentNumber}")]
        public async Task<IActionResult> UpdatePaymentByPaymentNumber(int paymentNumber, [FromBody] JsonElement jsonElement)
        {
            string jsonString = jsonElement.GetRawText();
            BsonDocument document = BsonDocument.Parse(jsonString);
            var collection = _database.GetCollection<BsonDocument>("Payment");
            double AmountOld = 0;
            double AmountNew = 0;
            int GrossNumber = 0;

            // Create a filter to find the order with the specified OrderNumber
            var filter = Builders<BsonDocument>.Filter.Eq("PaymentNumber", paymentNumber);

            // Find the order in the collection
            var paymentDocument = await collection.Find(filter).FirstOrDefaultAsync();

            if (paymentDocument == null)
            {
                // Return a 404 if the payment is not found
                return NotFound($"Payment with PaymentNumber {paymentNumber} not found.");
            }

            // Get the old amount and the new amount from the request body
            AmountOld = paymentDocument.GetValue("Amount").AsDouble;
            AmountNew = document["Amount"].AsDouble;

            // Update the gross value by subtracting the old amount and adding the new amount
            GrossNumber = await UpdateTheGrossNew(+AmountOld);
            GrossNumber = await UpdateTheGrossNew(-AmountNew);

            // Prepare the update for the payment document in the collection
            var update = Builders<BsonDocument>.Update
                .Set("Amount", AmountNew)
                .Set("DateOfPayment", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")) // Update payment date if needed
                .Set("GrossNumber", GrossNumber); // Update GrossNumber if needed

            // Apply the update to the document in the collection
            var updateResult = await collection.UpdateOneAsync(filter, update);

            if (updateResult.ModifiedCount == 0)
            {
                return BadRequest("Failed to update the payment.");
            }

            // Return success response
            return Ok(new { message = "Payment updated successfully", PaymentNumber = paymentNumber, NewAmount = AmountNew });
        }







[HttpDelete("DeletePaymentBYPaymentNumber/{paymentNumber}")]
public IActionResult Delete(int paymentNumber)
{

    var GrossCollection = _database.GetCollection<BsonDocument>("Gross");


    double paymentAmount = 0;
 
 
    var collection = _database.GetCollection<BsonDocument>("Payment");
    var filter = Builders<BsonDocument>.Filter.Eq("PaymentNumber", paymentNumber);
    var payment = collection.Find(filter).FirstOrDefault();

            if (payment != null)
    {
         paymentAmount = payment["Amount"].AsDouble;
                UpdateTheGrossNew(-paymentAmount);

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

    return Ok();
}




        public async Task<int> UpdateTheGrossNew(double amount)
        {
            var grossCollection = _database.GetCollection<BsonDocument>("Gross");

            var filterGross = Builders<BsonDocument>.Filter.Eq("Status", "Open");
            var theGross = await grossCollection.Find(filterGross).FirstOrDefaultAsync();

            if (theGross == null)
            {
                throw new Exception("Start your day please.");
            }

            if (!theGross.Contains("GrossNumber"))
            {
                throw new Exception("GrossNumber field is missing in the document.");
            }

            int grossNumber = theGross["GrossNumber"].AsInt32;

            // Debug statement to log the document found by the filter
            Console.WriteLine(theGross.ToJson());

            var update = Builders<BsonDocument>.Update.Inc("TotalGross", amount);
            var updateResult = await grossCollection.UpdateOneAsync(filterGross, update);

            // Debug statement to log the update result
            Console.WriteLine($"Matched Count: {updateResult.MatchedCount}, Modified Count: {updateResult.ModifiedCount}");

            if (updateResult.ModifiedCount == 0)
            {
                throw new Exception("No updates were made to the gross document.");
            }

            return grossNumber;
        }







    }
}
