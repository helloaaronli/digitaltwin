using System.Threading.Tasks;
using DigitalTwinApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DigitalTwinApi.Interfaces;


namespace DigitalTwinApi.Controllers {
    [Route("[controller]")]
    [ApiController]
    public class ConstructionStateController : Microsoft.AspNetCore.Mvc.ControllerBase {
        private readonly ILogger<ConstructionStateController> _logger;
        private readonly IConstructionState _constructionstate;

        public ConstructionStateController (ILogger<ConstructionStateController> logger, IConstructionState constructionstate) {
            _logger = logger;
            _constructionstate = constructionstate;
        }

        /// <summary>
        /// getConstructionState:
        /// retrieves construction state for a given vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("get")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetConstructionState (string vehicleId) {
            var constructionState = await _constructionstate.GetConstructionStateForVehicleFromMongoDb(vehicleId, false);
            if (constructionState == null) {
                return BadRequest();
            } else if(constructionState.CsModel.RootElement.ToString() == "{}") {
                return NoContent();
            } else {
                return Ok(constructionState.CsModel.RootElement);
            }
        }

        /// <summary>
        /// getConstructionStateJSON:
        /// retrieves construction state as JSON for a given vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("getJSON")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetConstructionStateJson (string vehicleId) {
            var constructionState = await _constructionstate.GetConstructionStateForVehicleFromMongoDb(vehicleId, true);
            if (constructionState == null) {
                return BadRequest();
            } else if(constructionState.CsModel.RootElement.ToString() == "{}") {
                return NoContent();
            } else {
                return Ok(constructionState.CsModel.RootElement);
            }
        }

        /// <summary>
        /// QueryConstructionState:
        /// get vehicle list by key-value pairs
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("listByKeyValue")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> QueryVehiclesList (string key, string value) {
             _logger.LogInformation($"Triggered QueryVehiclesList Endpoint --> using key: {key} and value: {value}");

            var vehicleList = await _constructionstate.QueryVehiclesList(key, value);
            if (vehicleList.ValueKind == JsonValueKind.Null) {
                return BadRequest();
            }
            else {
                return Ok(vehicleList);
            }
        }

        /// <summary>
        /// QueryConstructionState:
        /// get vehicle count by key-value pairs
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("countByKeyValue")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> QueryVehiclesCount (string key, string value) {
             _logger.LogInformation($"Triggered QueryVehiclesCount Endpoint --> using key: {key} and value: {value}");
            var vehicleList = await _constructionstate.QueryVehiclesCount(key, value);
            if (vehicleList.ValueKind == JsonValueKind.Null) {
                return BadRequest();
            } else {
                return Ok(vehicleList);
            }
        }

        /// <summary>
        /// QueryConstructionState:
        /// Execute a custom MongoDB query list
        /// </summary>
        /// <remarks>
        /// Logical selectors {and, nor, or}, evaluation selectors {mod, where}, comparison selectors {in, nin} and array selector {all} 
        /// follow the same notation {"{selector}" : ["{value1}", "{value2}", ... ]} where value can be an array, a string or another expression.
        /// The evaluation, comparison and array selectors need a "{field}" value in front of the expression (like color in IN query example). 
        ///
        ///     OR query example:
        ///     We want to query all vehicles that are either blue or Audi cars
        ///     {"or": [{"color": "blue"}, {"brand_name": "Audi"}] }
        ///
        ///     AND query example:
        ///     We want to query all blue Audi vehicles
        ///     {"and": [{"color": "blue"}, {"brand_name": "Audi"}] }
        ///     We want to query all blue Porsche vehicles where the color attribute has been changed after the given date
        ///     {"and": [{"color": "blue", "lastModified":{ "gt": "2022-03-11T12:41:35Z" }}, {"brand_name": "Porsche"}] }
        ///     We want to query all vehicles that are either blue or Audi cars and at the same time either red or Porsche cars
        ///     {"and": [{"or": [{"color": "blue"}, {"brand_name": "Audi"}]}, {"or": [{"color": "red"}, {"brand_name": "Porsche"}]}] }
        ///
        ///     IN query example:
        ///     We want to query all vehicles with color equals on of the colors blue and red.
        ///     {"color": {"in":["blue", "red"]} }
        ///     We want to query all vehicles which match one out of the colors blue and red. Additionally the color has been changed after the given date.
        ///     { "color": { "in":["blue", "red"], "lastModified":{ "gt": "2022-03-11T12:41:35Z" } } }
        ///     We want to query all vehicles which match one out of the colors blue and red. Additionally the color has been changed in between the two given dates.
        ///     { "color": { "in":["blue", "red"], "lastModified":{ "gt": "2022-03-11T12:41:35Z", "lt":"2022-03-12T12:41:35Z" } } }
        ///
        ///
        /// Comparison selectors {eq, gt, gte, lt, lte}, element {exists, type}, array {size} and evaluation {regex} selectors
        /// follow the notation {"{field}" : {"{selector}": "{value}"}} where value can be a string, number, boolean or another expression.
        ///
        ///     EQ query example:
        ///     We want to query all vehicles with color equals blue.
        ///     {"color":{"eq":"blue"}}
        ///
        ///     EXISTS query example:
        ///     We want to query all vehicles containing the field color
        ///     {"color":{"exists":"true"}}
        ///
        ///     REGEX query example:
        ///     We want to query all vehicles containing the regular expression "bl" in field color
        ///     {"color":{"regex":"bl"}}
        ///
        /// 
        /// Logical selector {not} 
        /// follow the notation { "{selector}": { "{value}" } } where value has to be an expression.
        ///
        ///     NOT query example:
        ///     We want to query all vehicles not containing the color blue.
        ///     {"color":{"not": {"eq": "blue"}}}
        ///
        ///
        /// Make sure to pass your key value pairs in this format "{your_key}": "{your_value}".
        /// Geospatial and bitwise query selectors are not supported.
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost]
        [Route("listByQuery")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ExecuteCustomQueryList (JsonElement query) {
            
            _logger.LogInformation($"Triggered CustomQuery: {query.ToString()}");
            if(!query.ValueKind.Equals(JsonValueKind.Object)) {
                return BadRequest();
            }    
            var result = await _constructionstate.ExecuteCustomQueryList(query);
            if (result.ValueKind == JsonValueKind.Null) {
                return BadRequest();
            } else {
                return Ok(result);
            }
        }

        /// <summary>
        /// QueryConstructionState:
        /// Execute a custom MongoDB query count
        /// </summary>
        /// <remarks>
        /// Logical selectors {and, nor, or}, evaluation selectors {mod, where}, comparison selectors {in, nin} and array selector {all} 
        /// follow the same notation {"{selector}" : ["{value1}", "{value2}", ... ]} where value can be an array, a string or another expression.
        /// The evaluation, comparison and array selectors need a "{field}" value in front of the expression (like color in IN query example). 
        ///
        ///     OR query example:
        ///     We want to query all vehicles that are either blue or Audi cars
        ///     {"or": [{"color": "blue"}, {"brand_name": "Audi"}] }
        ///
        ///     AND query example:
        ///     We want to query all blue Audi vehicles
        ///     {"and": [{"color": "blue"}, {"brand_name": "Audi"}] }
        ///     We want to query all blue Porsche vehicles where the color attribute has been changed after the given date
        ///     {"and": [{"color": "blue", "lastModified":{ "gt": "2022-03-11T12:41:35Z" }}, {"brand_name": "Porsche"}] }        
        ///     We want to query all vehicles that are either blue or Audi cars and at the same time either red or Porsche cars
        ///     {"and": [{"or": [{"color": "blue"}, {"brand_name": "Audi"}]}, {"or": [{"color": "red"}, {"brand_name": "Porsche"}]}] }
        ///
        ///     IN query example:
        ///     We want to query all vehicles with color equals on of the colors blue and red.
        ///     {"color": {"in":["blue", "red"]} }
        ///     We want to query all vehicles which match one out of the colors blue and red. Additionally the color has been changed after the given date.
        ///     { "color": { "in":["blue", "red"], "lastModified":{ "gt": "2022-03-11T12:41:35Z" } } }
        ///     We want to query all vehicles which match one out of the colors blue and red. Additionally the color has been changed in between the two given dates.
        ///     { "color": { "in":["blue", "red"], "lastModified":{ "gt": "2022-03-11T12:41:35Z", "lt":"2022-03-12T12:41:35Z" } } }
        ///
        ///
        /// Comparison selectors {eq, gt, gte, lt, lte}, element {exists, type}, array {size} and evaluation {regex} selectors
        /// follow the notation {"{field}" : {"{selector}": "{value}"}} where value can be a string, number, boolean or another expression.
        ///
        ///     EQ query example:
        ///     We want to query all vehicles with color equals blue.
        ///     {"color":{"eq":"blue"}}
        ///
        ///     EXISTS query example:
        ///     We want to query all vehicles containing the field color
        ///     {"color":{"exists":"true"}}
        ///
        ///     REGEX query example:
        ///     We want to query all vehicles containing the regular expression "bl" in field color
        ///     {"color":{"regex":"bl"}}
        ///
        /// 
        /// Logical selector {not} 
        /// follow the notation { "{selector}": { "{value}" } } where value has to be an expression.
        ///
        ///     NOT query example:
        ///     We want to query all vehicles not containing the color blue.
        ///     {"color":{"not": {"eq": "blue"}}}
        ///
        ///
        /// Make sure to pass your key value pairs in this format "{your_key}": "{your_value}".
        /// Geospatial and bitwise query selectors are not supported.
        /// </remarks>
        /// <response code="200">Success</response>
        /// <response code="400">BadRequest</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost]
        [Route("countByQuery")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ExecuteCustomQueryCount (JsonElement query) {
            
            _logger.LogInformation($"Triggered CustomQuery: {query.ToString()}");
            var result = await _constructionstate.ExecuteCustomQueryCount(query);
            if (result.ValueKind == JsonValueKind.Null) {
                return BadRequest();
            } else {
                return Ok(result);
            }
        }

        /// <summary>
        /// insertConstructionState:
        /// inserts construction state info for a given vehicle and account
        /// </summary>
        /// <remarks>
        /// Payload example:
        /// 
        ///     {
        ///         "color": "blue",
        ///         "brand_name": "Porsche"
        ///     }
        /// 
        /// </remarks>
        /// <param name="owner">owner of keyvalue pair(s) - default to fleet. You can also use vehicleId as owner</param>
        /// <param name="vehicleId">please insert vehicleId</param>
        /// <param name="payload">please insert payload</param>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound</response>
        [HttpPost]
        [Route("insert")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> InsertConstructionState (string vehicleId, JsonElement payload, string? owner = "fleet") {
            //TODO: Prevent misuse and determine if an insert comes in from offboard or onboard (Api key, header value or both)
            /*Microsoft.Extensions.Primitives.StringValues telemetryHeader;
            Request.Headers.TryGetValue("Telemetry", out telemetryHeader);*/

            var result = await _constructionstate.InsertConstructionStateForVehicle(owner, vehicleId, payload, false);
            if (result != System.Net.HttpStatusCode.OK) {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// insertConstructionStateJSON:
        /// inserts construction state info as JSON for a given vehicle and account
        /// </summary>
        /// <remarks>
        /// Payload example:
        /// 
        ///        {
        ///            "modules": {
        ///                "CommandModule": {
        ///                "created": "2021-07-09T11:35:30.9639857+02:00",
        ///                "image": "ce6ba475containerrepo.azurecr.io/command_module",
        ///                "status": "running",
        ///                "version": "1.0.1"
        ///                },
        ///                "EdgeTwin": {
        ///                "created": "2021-12-06T15:24:22.4082123+01:00",
        ///                "image": "ce6ba475containerrepo.azurecr.io/edge_twin",
        ///                "status": "running",
        ///                "version": "0.2.0"
        ///                }
        ///            }
        ///        }
        /// 
        /// </remarks>
         /// <param name="owner">owner of keyvalue pair(s) - default to fleet. You can also use vehicleId as owner</param>
        /// <param name="vehicleId">please insert vehicleId</param>
        /// <param name="payload">please insert payload</param>
        /// <returns>List of objects returned</returns>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound</response>
        [HttpPost]
        [Route("insertJSON")]
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> InsertConstructionStateJSON (string vehicleId, JsonElement payload, string? owner = "fleet") {
            var result = await _constructionstate.InsertConstructionStateForVehicle(owner, vehicleId, payload, true);
            if (result != System.Net.HttpStatusCode.OK) {
                return NotFound();
            }
            return Ok();
        }

        [HttpGet]
        [Route("flush")]
        [AuthorizationKeyFilterMaster]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FlushConstructionState (string vehicleId) {
            var result = await _constructionstate.FlushConstructionStateForVehicle(vehicleId);
            if (result != System.Net.HttpStatusCode.Accepted) {
                return NotFound();
            }
            return Ok();
        }
    }
}