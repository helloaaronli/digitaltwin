[[_TOC_]]

DigitalTwin API
===============

# Purpose
DigitalTwin API enables various requests that could be sent to the EdgeTwin module or to the corresponding extensions.

# Accessibility
The sources of the online documentation for accessing the DigitalTwin API are described below.

__Important notes__:
 - If the authorization header is omitted or if a wrong API key is provided, the response is `401, Unauthorized`.
- The attribute `vehicleId` corresponds to the vehicles that are correctly provisioned in VWAC and that have correct permission claims (please read [this tutorial](https://dev.azure.com/vwac/Digital%20Twin/_wiki/wikis/Digital-Twin.wiki/27699/Provision-a-vehicle-in-VWAC-and-set-user-claims-for-permissions) for more details). For invalid attribute values, the response is `404, Not found`.

## API key
 Almost all of the requests require an authorization header (key-based). Please set an API key in the header of your request:
| Header        | Value           |
| ------------- |:-------------:| 
| Authorization      | ApiKey 60b41dd7-28c8-4df6-9c66-f68069d6d68c |


## Swagger UI
The Swagger UI for the API can be found [here](https://api-digitaltwin.azurewebsites.net/swagger/index.html). This documentation offers a full range of possible requests and their detailed description including parameters specification and responses.

![DigitalTwin-API](docs/assets/dtapi_swagger.png =70%x)

 For a one-off authorization please use the "Authorize" button and fill in the value field. After that, you can use the "Try it out" button for making the requests.

![DigitalTwin-API](docs/assets/dtapi_key.png =40%x)

# Examples
## Send command to a vehicle
Please find a detailed description of different command types for a subscription [here](https://api-digitaltwin.developer.azure-api.net/api-details#api=apim-digitaltwin&operation=setvehiclecommand). For a telemetry subscription please make a `POST` request to
 ```
 https://api-digitaltwin.azure-api.net/vehicles/{VehicleId}/VehicleCommand
 ```
with a specific `vehicleId`.

Telemetry subscription example for `vehicleId` "f10808627aa812de273c23e68064db8b":

![DigitalTwin-API](docs/assets/dtapi_sendcommand.png =70%x)

Response:

![DigitalTwin-API](docs/assets/dtapi_sendcommand_response.png =70%x)

#Troubleshooting
If you are not able to build or publish the API:
- Install dotnet SDK 5.0 (e.g. for Ubuntu distros: https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu)
- Optional: remove bin, obj and .vscode folders and restart VS Code or reload the window. After a few seconds a warning popup should appear at the bottom right of the screen, asking for required assets. Click the "yes" button. As soon as you want to deploy another popup should open, asking you to add a deployment config. Click "yes" and the deployment process should start.
- Add these tasks to your tasks.json file located in the .vscode folder:
``` json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/DigitalTwinApi.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "publish",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "${workspaceFolder}/DigitalTwinApi.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "watch",
            "command": "dotnet",
            "type": "process",
            "args": [
                "watch",
                "run",
                "${workspaceFolder}/DigitalTwinApi.csproj",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "clean",
            "command": "dotnet",
            "type": "process",
            "args": [
                "clean",
                "${workspaceFolder}",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "publish-release",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "${workspaceFolder}",
                "--configuration",
                "Release",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "problemMatcher": "$msCompile",
            "dependsOn": "clean"
        }
    ]
} 
```