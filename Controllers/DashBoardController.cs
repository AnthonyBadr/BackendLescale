using backend.Models;
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

        [HttpPost("CreateTotalRevenueByDate")]
        public async Task<IActionResult> CreateTotalRevenueByDate()
        {
            var grossCollection = _database.GetCollection<BsonDocument>("Gross");

            var result = await grossCollection.Aggregate()
      .Sort(new BsonDocument("DateOfCreation", 1)) // Sort by DateOfCreation in ascending order
      .Group(new BsonDocument
      {
        { "_id", "$DateOfCreation" }, // Group by DateOfCreation
        { "TotalGross", new BsonDocument("$sum", "$TotalGross") }
      })
      .ToListAsync(); // Use ToListAsync to get all grouped results

            var totalGross = result.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            return Ok(totalGross);
        }




        public async Task<IActionResult> GetOrderAggregatesAsync()
        {
            // Access the collection
            var orderCollection = _database.GetCollection<BsonDocument>("Orders");

            // Calculate total sum of "TotalPrice"
            var aggregateResult = await orderCollection.Aggregate()
                .Group(new BsonDocument
                {
            { "_id", BsonNull.Value },
            { "totalSum", new BsonDocument("$sum", "$TotalPrice") }
                })
                .FirstOrDefaultAsync();

            // Calculate total count of "Items.Quantity"
            var aggregateResult2 = await orderCollection.Aggregate()
                .Unwind("Items")
                .Group(new BsonDocument
                {
            { "_id", BsonNull.Value },
            { "totalCount", new BsonDocument("$sum", "$Items.Quantity") }
                })
                .FirstOrDefaultAsync();

            // Extract results
            double totalSum = aggregateResult?["totalSum"]?.AsDouble ?? 0.0;
            int totalCount = aggregateResult2?["totalCount"]?.AsInt32 ?? 0;

            // Create aggregation pipeline for categories
            var aggregationCategory = new[]
            {
        new BsonDocument("$unwind", "$Items"),
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", new BsonDocument { { "CategoryName", "$Items.CategoryName" } } },
            { "totalCount", new BsonDocument("$sum", "$Items.Quantity") },
            { "ItemPrice", new BsonDocument("$sum", "$Items.ItemPrice") }
        }),
        new BsonDocument("$set", new BsonDocument
        {
            { "PercentagePrice", new BsonDocument("$multiply", new BsonArray
                {
                    new BsonDocument("$divide", new BsonArray
                    {
                        "$ItemPrice",
                        totalSum == 0 ? 1 : totalSum
                    }),
                    100
                })
            }
        }),
        new BsonDocument("$set", new BsonDocument
        {
            { "PercentageCount", new BsonDocument("$multiply", new BsonArray
                {
                    new BsonDocument("$divide", new BsonArray
                    {
                        "$totalCount",
                        totalCount == 0 ? 1 : totalCount
                    }),
                    100
                })
            }
        }),
        new BsonDocument("$sort", new BsonDocument("totalCount", -1))
    };

            // Execute the category aggregation
            var resultCat = await orderCollection.Aggregate<BsonDocument>(aggregationCategory).ToListAsync();

            // Map Bson results to .NET values and return them
            var documentCat = resultCat.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            return Ok(documentCat);
        }




        [HttpPost("CreateCategoryStats")]
        public async Task<IActionResult> CreateCategory()
        {


            var orderCollection = _database.GetCollection<BsonDocument>("Orders");

            var aggregateResult = await orderCollection.Aggregate()
               
               .Group(new BsonDocument
               {
                    { "_id", BsonNull.Value },
                    { "totalSum", new BsonDocument("$sum", "$TotalPrice") }
               })
               .FirstOrDefaultAsync();



            var aggregateResult2 = await orderCollection.Aggregate()
          
               .Unwind("Items")
               .Group(new BsonDocument
               {
                { "_id", BsonNull.Value },
                { "totalCount", new BsonDocument("$sum", "$Items.Quantity") }
               })
               .FirstOrDefaultAsync();

            double totalSum = aggregateResult?["totalSum"]?.AsDouble ?? 0.0;
            int TotalC = aggregateResult2?["totalCount"]?.AsInt32 ?? 0;




            var aggregationCategory = new[]
            {
                new BsonDocument("$unwind", "$Items"),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument {
                        { "CategoryName", "$Items.CategoryName" } 
                    } },
                    { "totalCount", new BsonDocument("$sum", "$Items.Quantity") },
                    { "ItemPrice", new BsonDocument("$sum", "$Items.ItemPrice") }
                }),
                new BsonDocument("$set", new BsonDocument
                {
                    { "PercentagePrice", new BsonDocument("$multiply", new BsonArray
                        {
                            new BsonDocument("$divide", new BsonArray
                            {
                                "$ItemPrice",
                                totalSum == 0 ? 1 : totalSum
                            }),
                            100
                        })
                    }
                }),
                new BsonDocument("$set", new BsonDocument
                {
                    { "PercentageCount", new BsonDocument("$multiply", new BsonArray
                        {
                            new BsonDocument("$divide", new BsonArray
                            {
                                "$totalCount",
                                TotalC == 0 ? 1 : TotalC
                            }),
                            100
                        })
                    }
                }),
                new BsonDocument("$sort", new BsonDocument("totalCount", -1))
            };

            var resultCat = await orderCollection.Aggregate<BsonDocument>(aggregationCategory).ToListAsync();
            var documentCat = resultCat.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();

            return Ok(documentCat);
        }





        [HttpPost("CreateTop3ItemInEachCategory")]
        public async Task<IActionResult> CreateTop3ItemInEachCategory()
        {
            var orderCollection = _database.GetCollection<BsonDocument>("Orders");

            var pipeline = new[]
            {
        new BsonDocument("$unwind", "$Items"),
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$Items.CategoryName" },
            { "items", new BsonDocument("$push", new BsonDocument
                {
                    { "name", "$Items.Name" },
                    { "totalItemPrice", "$Items.ItemPrice" },
                    { "totalQuantity", "$Items.Quantity" }
                })
            }
        }),
        new BsonDocument("$unwind", "$items"),
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", new BsonDocument
                {
                    { "category", "$_id" },
                    { "item", "$items.name" }
                }
            },
            { "totalItemPrice", new BsonDocument("$sum", "$items.totalItemPrice") },
            { "totalQuantity", new BsonDocument("$sum", "$items.totalQuantity") }
        }),
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$_id.category" },
            { "items", new BsonDocument("$push", new BsonDocument
                {
                    { "name", "$_id.item" },
                    { "totalItemPrice", "$totalItemPrice" },
                    { "totalQuantity", "$totalQuantity" }
                })
            }
        }),
        new BsonDocument("$project", new BsonDocument
        {
            { "items", new BsonDocument("$slice", new BsonArray
                {
                    new BsonDocument("$sortArray", new BsonDocument
                    {
                        { "input", "$items" },
                        { "sortBy", new BsonDocument("totalItemPrice", -1) }
                    }),
                    3
                })
            }
        })
    };

            var result = await orderCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            var documentCat = result.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();
            return Ok(documentCat);
        }







    }
}
