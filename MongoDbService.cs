using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MongoDbConsoleApp
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;

        public MongoDbService(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        // Make this method public to access it from other services
        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        public async Task PingAsync()
        {
            try
            {
                var pingCommand = new BsonDocument("ping", 1);
                var result = await _database.RunCommandAsync<BsonDocument>(pingCommand);
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ping failed: {ex.Message}");
            }
        }

        public async Task InsertSampleDocumentAsync()
        {
            var document = new BsonDocument
            {
                { "name", "MongoDB Example" },
                { "type", "Database" },
                { "count", 1 },
                { "info", new BsonDocument { { "x", 203 }, { "y", 102 } } }
            };

            var userCollection = GetCollection<BsonDocument>("User"); // Or any other collection you want to insert into
            await userCollection.InsertOneAsync(document);
            Console.WriteLine("Inserted sample document.");
        }
    }
}
