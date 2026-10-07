namespace Backend.Models.Sistemas
{
    public class BackendGenerateResult
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public string? OutputPath { get; set; }
        public bool? RestoreOk { get; set; }
        public string? RestoreOutput { get; set; }
        public string? RestoreError { get; set; }
    }
}
