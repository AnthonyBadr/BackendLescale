using backend.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.Controllers
{
    [Route("api/[controller]")]

    public class DashBoardController : Controller
    {

        private readonly ILogger<DashBoardController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public DashBoardController(ILogger<DashBoardController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }



        [HttpPost("CreateTotalRevenue")]
        public async Task<IActionResult> CreateTotalRevenue()
        {
            var grossCollection = _database.GetCollection<BsonDocument>("Gross");

            var result = await grossCollection.Aggregate()
                .Group(new BsonDocument
                {
            { "_id", BsonNull.Value },
            { "TotalGross", new BsonDocument("$sum", "$TotalGross") }
                })
                .FirstOrDefaultAsync();

            var totalGross = result?["TotalGross"].ToDouble() ?? 0;

            return Ok(new { TotalGross = totalGross });
        }










        }
}
