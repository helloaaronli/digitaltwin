using MongoDB.Driver;
using System;
using Microsoft.Extensions.Configuration;

namespace DigitalTwinApi.Services {

public class MongodbService: Interfaces.IMongoDB
    {
        private static MongoClient _client;
        public static IMongoDatabase _db;
        public static IConfiguration Configuration;

        public MongodbService()
        {   
            try {

            _client = new MongoClient(
                Configuration["mongoDbConnectionString"]
            );

            _db = _client.GetDatabase("ditwin-state-dev");
            Console.WriteLine("Connected successfully to mongodb!");

            } catch(Exception ex) {
                Console.WriteLine("Failed to connect to mongodb: ", ex);
            }

        }

        public IMongoDatabase getDatabase() { return _db; }
    }

}