#define ApprovalEnvironmentPaaSChina

namespace DigitalTwinApi {

#if ApprovalEnvironmentPaaSChina
    public static partial class Constants {

        public const string miracleEndpoint= "https://miracle.apr.digitaltwin.vwcloud.cn/insert";

        // default values
        public const int DEFAULT_TTL_VALUE = 60;
        public const string DEVICE_NAME = "edge1";
        public const string COMMAND_NAME = "DigitalTwinCommandHandler";
        public static string[] VALID_OWNERS = {"fleet", "containerupd", "testcloud", "oemil", "admin"};


        // VWAC/MCVP API
        public const string VWAC_API_DOMAIN_NAME = "apr-vwacv-a72-apim.vwcloud.cn";
        public const string VWAC_API_USER_API_VERSION = "2019-09-01";
        public const string VWAC_API_COMMAND_API_VERSION = "2021-09-01";
        public const string VWAC_API_VEHICLE_API_VERSION = "2020-07-01";

        // VWAC Bearer Token Generation
        public const string domainName = "login.partner.microsoftonline.cn";
        public const string scope_user = "api://china-apr-vwacv-a72-apim-backend-sp/.default";
        public const string scope_command = "api://vwac-CommandService-cn-Approval-spn/.default";
        public const string grantType = "client_credentials";


        // Authorization keys
        public const string APIM_AUTHORIZATION_HEADER = "Authorization";
        public const string APIM_AUTHORIZATION_KEY = "ApiKey";
        public const string APIM_AUTHORIZATION_KEY_VALUE = "60b41dd7-28c8-4df6-9c66-f68069d6d68c";
    }
#endif
}
