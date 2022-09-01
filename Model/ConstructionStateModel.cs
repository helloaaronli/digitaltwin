using System;
using System.Text.Json;

namespace DigitalTwinApi.Model {
    public class ConstructionStateModel {
        public Guid Id { get; set; }
        public JsonDocument CsModel { get; set; }

        public ConstructionStateModel (JsonDocument constructionStateModel) {
            Id = new Guid();
            CsModel = constructionStateModel;
        }
    }
}