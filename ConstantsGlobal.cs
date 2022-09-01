namespace DigitalTwinApi {
    public static class ConstantsGlobal {
        // default values
        public const int DEFAULT_TTL_VALUE = 60;
        public const string DEVICE_NAME = "edge1";
        public const string COMMAND_NAME = "DigitalTwinCommandHandler";

        //Valid Construction State owners
        public static string[] VALID_OWNERS = {"fleet", "containerupd", "testcloud", "oemil", "admin"};

        // Authorization keys
        public const string APIM_AUTHORIZATION_HEADER = "Authorization";
        public const string APIM_AUTHORIZATION_KEY = "ApiKey";
        public const string APIM_AUTHORIZATION_KEY_VALUE = "60b41dd7-28c8-4df6-9c66-f68069d6d68c";

        // Large telemetry & large command
        public const int MAX_COMMAND_SIZE_IN_BYTE = 20000; // ca. 20 kB
        public const string METADATA = "metadata";
        public const string DATA = "data";
        public const string CHUNK = "chunk";
        public const string CHUNKS = "chunks";
        public const string LARGE_DTID = "largeDTID";
        public const string COMMAND_PAYLOAD = "commandPayload";
        public const string TOPIC = "topic";
    }
}