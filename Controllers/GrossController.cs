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

    public class GrossController : Controller
    {

        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public GrossController( IMongoDatabase database, GlobalService globalService)
        {
            _database = database;
            _globalService = globalService;
        }



        [HttpGet("GetAllGross")]
        public IActionResult GetAllItems()
        {
            var collection = _database.GetCollection<BsonDocument>("Gross");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to a list of JSON objects
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        [HttpGet("GetLatestGross")]
        public IActionResult GetLatestGross()
        {
            var collection = _database.GetCollection<BsonDocument>("Gross");
            var sort = Builders<BsonDocument>.Sort.Descending("GrossNumber");
            var document = collection.Find(new BsonDocument()).Sort(sort).FirstOrDefault();

            if (document == null)
            {
                return NotFound("Table not found");
            }

            // Convert the document to a .NET type
            var jsonResult = BsonTypeMapper.MapToDotNetValue(document);

            // Return the data as JSON
            return Json(jsonResult);
        }




        [HttpPost("CreateGross")]
        public async Task<IActionResult> CreateTable([FromBody] JsonElement jsonElement)
        {
            try
            {
                // Parse the JSON element
                var document = BsonDocument.Parse(jsonElement.ToString());

                // Manually create the Table object from the document

                // Check if a table with the same table number already exists
                var grossCollection = _database.GetCollection<BsonDocument>("Gross");
                var existingPendingGross = await grossCollection.Find(Builders<BsonDocument>.Filter.Eq("Status", "Open")).FirstOrDefaultAsync();

                if (existingPendingGross != null)
                {
                    return Conflict("A gross with status pending already exists");
                }
                // Add a new sequence value for the table ID
                int newSequenceValue = _globalService.SequenceIncrement("GrossNumber").GetAwaiter().GetResult();
                document.Add("GrossNumber", newSequenceValue);

                document["DateOfCreation"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");


                // Log the action and insert the new table
                await grossCollection.InsertOneAsync(document);
                _globalService.LogAction($"Gross '{newSequenceValue}' created.", "Created");

                return Ok("Gross added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }




        [HttpPut("UpdateGrossByGrossNumber/{GrossNumber}")]
        public async Task<IActionResult> UpdateGrossByGrossNumber(int GrossNumber, [FromBody] JsonElement jsonElement)
        {
            try
            {
                // Extract the "Status" field from the JSON element
                var status = jsonElement.GetProperty("Status").GetString();

                // Get the "Gross" collection from the database
                var grossCollection = _database.GetCollection<BsonDocument>("Gross");

                // Create a filter to find the document by GrossNumber
                var filter = Builders<BsonDocument>.Filter.Eq("GrossNumber", GrossNumber);

                // Create the update definition to update only the "Status" field
                var updateDefinition = Builders<BsonDocument>.Update.Set("Status", status);

                // Update the document in the collection
                var updateResult = await grossCollection.UpdateOneAsync(filter, updateDefinition);

                // Check if the update was successful
                if (updateResult.MatchedCount == 0)
                {
                    return NotFound($"No document found with GrossNumber: {GrossNumber}");
                }

                // Log the update action
                _globalService.LogAction($"Gross document with GrossNumber '{GrossNumber}' status updated to '{status}'.", "Updated");

                return Ok("Status updated successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }




        [HttpDelete("DeleteGrossByGrossNumber/{GrossNumber}")]
        public async Task<IActionResult> DeleteGrossByGrossNumber(int GrossNumber)
        {
            try
            {
                // Get the "Gross" collection from the database
                var grossCollection = _database.GetCollection<BsonDocument>("Gross");

                // Create a filter to find the document by GrossNumber
                var filter = Builders<BsonDocument>.Filter.Eq("GrossNumber", GrossNumber);

                // Delete the document in the collection
                var deleteResult = await grossCollection.DeleteOneAsync(filter);

                // Check if any document was deleted
                if (deleteResult.DeletedCount == 0)
                {
                    return NotFound($"No document found with GrossNumber: {GrossNumber}");
                }

                // Log the delete action
                _globalService.LogAction($"Gross document with GrossNumber '{GrossNumber}' deleted.", "Deleted");

                return Ok("Gross document deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }







    }
}
