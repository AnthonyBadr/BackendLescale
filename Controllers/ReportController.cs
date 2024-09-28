using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Linq;
using System.Text.Json;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    public class ReportController : Controller
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IMongoDatabase _database;
        private readonly GlobalService _globalService;

        public ReportController(ILogger<ReportController> logger, IMongoDatabase database, GlobalService globalService)
        {
            _logger = logger;
            _database = database;
            _globalService = globalService;
        }

        [HttpPost("CreateReport")]
        public async Task<IActionResult> CreateReport()
        {
            List<ReportItemlist> RTL = new List<ReportItemlist>();
            var grossCollection = _database.GetCollection<BsonDocument>("Gross");
            var reportsCollection = _database.GetCollection<BsonDocument>("Reports");


            // Find all gross documents with status "Semi-Closed"
            var filterGross = Builders<BsonDocument>.Filter.Eq("Status", "Semi-Closed");
            var grossDocuments = grossCollection.Find(filterGross).FirstOrDefault();

            if (grossDocuments == null)
            {
                throw new Exception("Start your day please.");
            }

            if (!grossDocuments.Contains("GrossNumber"))
            {
                throw new Exception("GrossNumber field is missing in the document.");
            }
            int newSequenceValue = _globalService.SequenceIncrement("ReportNumber").GetAwaiter().GetResult();
            int grossNumber = grossDocuments["GrossNumber"].AsInt32;
            var orderCollection = _database.GetCollection<BsonDocument>("Orders");

            // Define your filter condition (adjust this to your needs)
            var filterOrder = Builders<BsonDocument>.Filter.Eq("GrossNumber", grossNumber);

            
            var aggregateResult = await orderCollection.Aggregate()
                .Match(filterOrder) 
                .Group(new BsonDocument
                {
                    { "_id", BsonNull.Value }, 
                    { "totalSum", new BsonDocument("$sum", "$TotalPrice") } 
                })
                .FirstOrDefaultAsync(); 

            var aggregateResult2 = await orderCollection.Aggregate()
               .Match(filterOrder) 
               .Unwind("Items")   
               .Group(new BsonDocument
               {
                { "_id", BsonNull.Value }, 
                { "totalCount", new BsonDocument("$sum", "$Items.Quantity") }
               })
               .FirstOrDefaultAsync();

           


            double totalSum = aggregateResult?["totalSum"]?.AsDouble ?? 0.0;
            int TotalC = aggregateResult2?["totalCount"]?.AsInt32 ?? 0;

//ByItemStart       
            var aggregationPipeline = new[]
            {
    new BsonDocument("$match", new BsonDocument("GrossNumber", grossNumber)), 
    new BsonDocument("$unwind", "$Items"), 
    new BsonDocument("$group", new BsonDocument
    {
        { "_id", new BsonDocument
            {
                { "ItemName", "$Items.Name" }  
            }
        },
        { "totalCount", new BsonDocument("$sum","$Items.Quantity") }, 
        { "ItemPrice", new BsonDocument("$sum", "$Items.ItemPrice") }  // Sum of ItemPrice
    }),

    new BsonDocument("$set", new BsonDocument
    {
        { "CalculatedPrice", new BsonDocument("$multiply", new BsonArray
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
            
var result = await orderCollection.Aggregate<BsonDocument>(aggregationPipeline).ToListAsync();
var document = result.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();
//ByItemEnd


//ByCategoryStart
            var aggregationCategory = new[]
            {
    new BsonDocument("$match", new BsonDocument("GrossNumber", grossNumber)),
    new BsonDocument("$unwind", "$Items"),
    new BsonDocument("$group", new BsonDocument
    {
        { "_id", new BsonDocument
            {
                { "CategoryName", "$Items.CategoryName" }
            }
        },
        { "totalCount", new BsonDocument("$sum","$Items.Quantity") },
        { "ItemPrice", new BsonDocument("$sum", "$Items.ItemPrice") }  // Sum of ItemPrice
    }),

    new BsonDocument("$set", new BsonDocument
    {
        { "CalculatedPrice", new BsonDocument("$multiply", new BsonArray
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
            //ByCategoryEnd



            //ByCategoryStart
            var aggregationType = new[]
            {
    new BsonDocument("$match", new BsonDocument("GrossNumber", grossNumber)),
    new BsonDocument("$unwind", "$Items"),
    new BsonDocument("$group", new BsonDocument
    {
        { "_id", new BsonDocument
            {
              { "Type", "$Type" }
            }
        },
        { "totalCount", new BsonDocument("$sum","$Items.Quantity") },
        { "ItemPrice", new BsonDocument("$sum", "$Items.ItemPrice") }  // Sum of ItemPrice
    }),

    new BsonDocument("$set", new BsonDocument
    {
        { "CalculatedPrice", new BsonDocument("$multiply", new BsonArray
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

            var resultType = await orderCollection.Aggregate<BsonDocument>(aggregationType).ToListAsync();
            var documentType = resultType.Select(doc => BsonTypeMapper.MapToDotNetValue(doc)).ToList();
            //ByCategoryEnd







            // Create a JSON object to hold the date and the document
            var jsonObject = new BsonDocument
            {
                { "Date", DateTime.UtcNow },
                { "ReportNumber", newSequenceValue },
                { "ItemReport", new BsonArray(document.Select(d => BsonDocument.Parse(JsonSerializer.Serialize(d)))) }, // Serialize each item to Bson
                 { "CategoryReport", new BsonArray(documentCat.Select(d => BsonDocument.Parse(JsonSerializer.Serialize(d)))) }, // Serialize each item to Bson
                 { "TypeReport", new BsonArray(documentType.Select(d => BsonDocument.Parse(JsonSerializer.Serialize(d)))) } // Serialize each item to Bson
            };


            // Serialize the JSON object to a JSON string

            var hey = BsonTypeMapper.MapToDotNetValue(jsonObject) as Dictionary<string, object>;

            await reportsCollection.InsertOneAsync(jsonObject);

            return Ok(hey);
        }
    }
}
