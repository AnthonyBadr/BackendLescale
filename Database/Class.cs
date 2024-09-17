using MongoDB.Driver;

public class MongoDBContext
{
    private readonly IMongoDatabase _database;
    private readonly string _collectionName = "mycollection"; // Define your collection name here

    public MongoDBContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<MyDocument> GetCollection()
    {
        return _database.GetCollection<MyDocument>(_collectionName);
    }
}

public class MyDocument
{
    public string Id { get; set; }
    
}
