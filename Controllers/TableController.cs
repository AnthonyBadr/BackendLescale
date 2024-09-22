using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Linq;
using Microsoft.Extensions.Logging;
using backend.Services;
using System.Text.Json;
using MongoDB.Bson.Serialization;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class TableController : Controller
    {
        private readonly ILogger<TableController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public TableController(ILogger<TableController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }
        [HttpGet("GetAllTables")]
        public IActionResult GetAllTables()
        {
            var collection = _database.GetCollection<BsonDocument>("Table");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to a list of JSON objects
            var jsonResult = documents.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        [HttpGet("GetTableById/{Id}")]
        public IActionResult GetTableById(int Id)
        {
            var collection = _database.GetCollection<BsonDocument>("Table");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", Id);
            var document = collection.Find(filter).FirstOrDefault();

            if (document == null)
            {
                return NotFound("Table not found");
            }

            // Convert the document to a .NET type
            var jsonResult = BsonTypeMapper.MapToDotNetValue(document);

            // Return the data as JSON
            return Json(jsonResult);
        }


        [HttpPost("CreateTable")]
        public async Task<IActionResult> CreateTable([FromBody] JsonElement jsonElement)
        {
            try
            {
                // Parse the JSON element
                var document = BsonDocument.Parse(jsonElement.ToString());

                // Manually create the Table object from the document
                string tableNumber = document["TableNumber"].AsString;

                // Check if a table with the same table number already exists
                var tableCollection = _database.GetCollection<BsonDocument>("Table");
                var existingTable = await tableCollection.Find(Builders<BsonDocument>.Filter.Eq("TableNumber", tableNumber)).FirstOrDefaultAsync();

                if (existingTable != null)
                {
                    return Conflict("A table with this table number already exists");
                }

                // Add a new sequence value for the table ID
                int newSequenceValue = _globalService.SequenceIncrement("TableId").GetAwaiter().GetResult();
                document.Add("Id", newSequenceValue);

                document["Status"] = "Available";

                // Log the action and insert the new table
                _globalService.LogAction($"Table '{tableNumber}' created.", "Created");
                await tableCollection.InsertOneAsync(document);

                return Ok("Table added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }



        [HttpPut("UpdateTableById/{Id}")]
        public async Task<IActionResult> UpdateTableById(int Id, [FromBody] JsonElement jsonElement)
        {
            try
            {
                // Parse the JSON element
                var document = BsonDocument.Parse(jsonElement.ToString());

                // Manually create the Table object from the document
                var updatedTable = new Table
                {
                    TableNumber = document["TableNumber"].AsString,
                    Status = document["Status"].AsString
                    // Add other properties as needed
                };

                // Remove the 'Id' field from the document if it exists, so it won't be updated
                document.Remove("Id");

                var collection = _database.GetCollection<BsonDocument>("Table");
                var filter = Builders<BsonDocument>.Filter.Eq("Id", Id); // Use _id field for filtering

                // Create an update definition with the remaining fields in the document
                var updateDefinition = new BsonDocument("$set", document);

                // Update the document
                var updateResult = await collection.UpdateOneAsync(filter, updateDefinition);

                if (updateResult.ModifiedCount == 0)
                {
                    return NotFound("Table not found");
                }

                _globalService.LogAction($"Table '{updatedTable.TableNumber}' updated.", "Updated");

                return Ok("Table updated successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpDelete("DeleteTablebyId/{Id}")]
        public IActionResult DeleteTable(int Id)
        {
            var tableCollection = _database.GetCollection<BsonDocument>("Table");
            var filter = Builders<BsonDocument>.Filter.Eq("Id", Id); // Use _id field for filtering
            var existingTable = tableCollection.Find(filter).FirstOrDefault();
            var tableNumber = existingTable["TableNumber"].AsInt32; // Replace "TableType" with the actual field name
            if (existingTable == null)
            {
                return NotFound("Table not found");
            }

            // Delete the table from the collection
            tableCollection.DeleteOne(filter);

            // Log the deletion
            _globalService.LogAction($"Table '{tableNumber}' deleted.", "Deleted");

            return Ok("Table deleted successfully");
        }


        

    }
}
