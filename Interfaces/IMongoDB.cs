
using MongoDB.Driver;

namespace DigitalTwinApi.Interfaces {
    public interface IMongoDB {
        public IMongoDatabase getDatabase();

    }
}