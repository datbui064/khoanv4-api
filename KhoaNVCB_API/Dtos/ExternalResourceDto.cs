namespace KhoaNVCB_API.Dtos
{
    public class ExternalResourceDto
    {
        public int ResourceId { get; set; }
        public int PostId { get; set; }
        public string ResourceType { get; set; } = null!;
        public string SourceUrl { get; set; } = null!;
    }
}
