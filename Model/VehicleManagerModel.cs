using System.Collections.Generic;

namespace DigitalTwinApi.Model {

public class VehicleManagerModel {
    public List<string> vehicleList { get; set; } // key: vehicleId
    public Dictionary<string, List<TopicObject>> topicList { get; set; } // key: vehicleId

    public VehicleManagerModel () {
        vehicleList = new List<string>();
        topicList = new Dictionary<string, List<TopicObject>>();
    }
}

public class TopicObject {
    public string name { get; set; }
    public string topic { get; set; }
    public int priority { get; set; }
    public int ttl { get; set; }
}

}