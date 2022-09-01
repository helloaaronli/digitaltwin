
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace DigitalTwinApi.Model
{
    [BsonIgnoreExtraElements]
    public class State
    {

        [BsonId]
        public ObjectId _id { get; set; }

        public string vehicleId { get; set; }
        public object state { get; set; }

    }

}