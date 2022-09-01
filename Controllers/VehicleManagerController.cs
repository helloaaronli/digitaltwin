using System;
using System.Text.Json;
using System.Threading.Tasks;
using DigitalTwinApi.Services;
using DigitalTwinApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DigitalTwinApi.Interfaces;



namespace DigitalTwinApi.Controllers {
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    [ApiController]
    public class VehicleManagerController : Microsoft.AspNetCore.Mvc.ControllerBase {
        private readonly ILogger<VehicleManagerController> _logger;
        private readonly IVehicleManager vehicleManagerService;

        public VehicleManagerController (ILogger<VehicleManagerController> logger, IVehicleManager vehicleManagerService) {
            _logger = logger;
            this.vehicleManagerService = vehicleManagerService;
        }

        /// <summary>
        /// getVehicleList:
        /// Returns all vehicles linked to Digital Twin user
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        [Route("VehicleList")] // e.g. http://localhost:5000/VehicleManager/VehicleList
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<JsonElement>> GetVehicleList () {
            JsonElement vehicleList = await vehicleManagerService.GetVehicleList();
            return vehicleList;
        }

        /// <summary>
        /// getTopicListForVehicle:
        /// Returns all topics available for the vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet]
        [Route("TopicList/{vehicleId}")] // e.g. http://localhost:5000/VehicleManager/TopicList/vehicle01
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JsonElement>> GetTopicListForVehicle (string vehicleId) {
            try {
                JsonElement topicList = await vehicleManagerService.GetTopicListForVehicle(vehicleId);

                return topicList;
            } catch (Exception) {
                return NotFound("Vehicle '" + vehicleId + "' Not found!");
            }
        }

        /// <summary>
        /// getTopicForVehicle:
        /// Returns topic information for the vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle not found</response>
        [HttpGet]
        [Route("TopicList/{vehicleId}/{topicName}")] // e.g. http://localhost:5000/VehicleManager/TopicList/vehicle01/topic01
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<JsonElement>> GetTopicForVehicle (string vehicleId, string topicName) {
            try {
                JsonElement topic = await vehicleManagerService.GetTopicForVehicle(vehicleId, topicName);
                return topic;
            } catch (Exception) {
                return NotFound("Vehicle '" + vehicleId + "' Not found!\nOR\nTopicName '" + topicName + "' does not exists for Vehicle '" + vehicleId + "'");
            }
        }

        /// <summary>
        /// updateLists:
        /// Updates vehicle list and all topic lists
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">No Vehicle List</response>
        [HttpGet]
        [Route("UpdateLists")] // e.g. http://localhost:5000/VehicleManager/UpdateLists
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateLists () {
            if (!await vehicleManagerService.UpdateLists()) {
                return NotFound("No Vehicle List received!");
            }
            return Ok("Lists updated!");
        }

        /// <summary>
        /// addVehicle:
        /// Adds a vehicle to the vehicle list
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="400">Parameter 'vehicleId' must not be null</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle already exists</response>
        [HttpPost]
        [Route("VehicleList")] // e.g. http://localhost:5000/VehicleManager/VehicleList
        [AuthorizationKeyFilterMaster]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AddVehicle (VehicleItem vehicleItem) {
            if (vehicleItem.vehicleId == null) {
                return BadRequest("Parameter 'vehicleId' must not be null!");
            }

            if (!await vehicleManagerService.AddVehicle(vehicleItem.vehicleId)) {

                return NotFound("Vehicle '" + vehicleItem.vehicleId + "' already exists!");
            }

            return Ok("Vehicle '" + vehicleItem.vehicleId + "' added!");
        }

        /// <summary>
        /// removeVehicle:
        /// Removes a vehicle from the vehicle list
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle doesn't exist</response>
        [HttpDelete]
        [Route("VehicleList/{vehicleId}")] // e.g. http://localhost:5000/VehicleManager/VehicleList/vehicle01
        [AuthorizationKeyFilterMaster]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveVehicle (string vehicleId) {
            if (!await vehicleManagerService.RemoveVehicle(vehicleId)) {

                return NotFound("Vehicle '" + vehicleId + "' does not exists!");
            }

            return Ok("Vehicle '" + vehicleId + "' deleted!");
        }

        /// <summary>
        /// addTopicForVehicle
        /// Adds a topic to the topic list of the vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="400">Parameter 'topicName' must not be null</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle not found or Topic already exist</response>
        [HttpPost]
        [Route("TopicList/{vehicleId}")] // e.g. http://localhost:5000/VehicleManager/TopicList/vehicle01
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AddTopicForVehicle (string vehicleId, TopicItem topicItem) {
            if (topicItem.topicName == null) {
                return BadRequest("Parameter 'name' must not be null!");
            }
            if (!await vehicleManagerService.AddTopicForVehicle(vehicleId, topicItem.topicName, topicItem.topic, topicItem.priority, topicItem.ttl)) {
                return NotFound("Vehicle '" + vehicleId + "' does not exist!\nOR\nTopicName '" + topicItem.topicName + "' already exists for Vehicle '" + vehicleId + "'");
            }

            return Ok("TopicName '" + topicItem.topicName + "' for Vehicle '" + vehicleId + "' created!");
        }

        /// <summary>
        /// updateTopicForVehicle:
        /// Updates topic information of the vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle or Topic not found</response>
        [HttpPut]
        [Route("TopicList/{vehicleId}/{topicName}")] // e.g. http://localhost:5000/VehicleManager/TopicList/vehicle01/topic01
        [AuthorizationKeyFilter]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateTopicForVehicle (string vehicleId, string topicName, TopicItem topicItem) {
            if (!await vehicleManagerService.UpdateTopicForVehicle(vehicleId, topicName, topicItem.topic, topicItem.priority, topicItem.ttl)) {
                return NotFound("Vehicle '" + vehicleId + "' does not exist!\nOR\nTopicName '" + topicName + "' does not exist for Vehicle '" + vehicleId + "'");
            }

            return Ok("TopicName '" + topicName + "' for Vehicle '" + vehicleId + "' updated!");
        }

        /// <summary>
        /// removeTopicForVehicle:
        /// Removes a topic from the topic list of the vehicle
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">Vehicle or Topic not found</response>
        [HttpDelete]
        [Route("TopicList/{vehicleId}/{topicName}")] // e.g. http://localhost:5000/VehicleManager/TopicList/vehicle01/topic01
        [AuthorizationKeyFilterMaster]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RemoveTopicForVehicle (string vehicleId, string topicName) {
            if (!await vehicleManagerService.RemoveTopicForVehicle(vehicleId, topicName)) {
                return NotFound("Vehicle '" + vehicleId + "' does not exist!\nOR\nTopicName '" + topicName + "' does not exist for Vehicle '" + vehicleId + "'");
            }

            return Ok("TopicName '" + topicName + "' for Vehicle '" + vehicleId + "' removed!");
        }

        public class VehicleItem {
            public string vehicleId { get; set; }
        }

        public class TopicItem {
            public string topicName { get; set; }
            public string topic { get; set; }
            public int priority { get; set; }
            public int ttl { get; set; }
        }
    }
}