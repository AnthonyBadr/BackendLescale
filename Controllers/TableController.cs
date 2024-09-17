using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using backend.Models;
using MongoDB.Bson;
using System.Linq;
using Microsoft.Extensions.Logging;
using backend.Services;

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

        [HttpGet]
        public IActionResult Index()
        {
            var collection = _database.GetCollection<BsonDocument>("Table");
            var documents = collection.Find(new BsonDocument()).ToList();

            // Convert documents to JSON
            var jsonResult = documents.Select(doc => doc.ToJson()).ToList();

            // Return the data as JSON
            return Json(jsonResult);
        }

        [HttpPost("CreateTable")]
        public IActionResult CreateTable([FromBody] Table newTable)
        {
            if (newTable == null)
            {
                return BadRequest("Table data is null");
            }

            if (string.IsNullOrEmpty(newTable.Status))
            {
                newTable.Status = "Available";
            }

           
            var tableCollection = _database.GetCollection<Table>("Table");

           


            var existingTable = tableCollection.Find(t => t.tableNumber == newTable.tableNumber).FirstOrDefault();

            if (existingTable != null)
            {
                return Conflict("A table with this table number already exists");
            }

            _globalService.LogAction($"Table '{newTable.tableNumber}' created.", "Created");
            tableCollection.InsertOne(newTable);

            return Ok("Table added successfully");
        }







        [HttpPut("UpdateTable/{tableNumber}")]
        public IActionResult UpdateTable(int tableNumber, [FromBody] Table updatedTable)
        {
            if (updatedTable == null)
            {
                return BadRequest("Table data is null");
            }

            var tableCollection = _database.GetCollection<Table>("Table");
            var filter = Builders<Table>.Filter.Eq(t => t.tableNumber, tableNumber);
            var existingTable = tableCollection.Find(filter).FirstOrDefault();

            if (existingTable == null)
            {
                return NotFound("Table not found");
            }

            // Update the fields
            existingTable.tableNumber = updatedTable.tableNumber;
            existingTable.Status = updatedTable.Status;
            existingTable.tabletype = updatedTable.tabletype;

            tableCollection.ReplaceOne(filter, existingTable);
            _globalService.LogAction($"Table '{tableNumber}' updated.", "Updated");

            return Ok("Table updated successfully");
        }

        [HttpDelete("DeleteTable/{tableNumber}")]
        public IActionResult DeleteTable(int tableNumber)
        {
            var tableCollection = _database.GetCollection<Table>("Table");
            var filter = Builders<Table>.Filter.Eq(t => t.tableNumber, tableNumber);
            var existingTable = tableCollection.Find(filter).FirstOrDefault();

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
