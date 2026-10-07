namespace Backend.Models.Sistemas
{
    public class BackendSystemConfig
    {
        public string ApiBase { get; set; } = "api/v1";
        public bool RequireAuth { get; set; } = true;
        public string SchemaPrefix { get; set; } = "sys";
        public string Persistence { get; set; } = "sql";
        public int DefaultPageSize { get; set; } = 50;
        public int MaxPageSize { get; set; } = 200;

        public string AudioStorageProvider { get; set; } = "local";
        public bool AudioTranscodeEnabled { get; set; } = false;
        public string AudioTranscodeFormat { get; set; } = "opus";
        public string AudioTranscodeBitrate { get; set; } = "32k";
        public bool AudioTranscodeDeleteOriginal { get; set; } = true;
        public int AudioRetentionSoftDays { get; set; } = 0;
        public int AudioRetentionPurgeDays { get; set; } = 0;
        public int AudioRetentionRunMinutes { get; set; } = 60;
    }
}
