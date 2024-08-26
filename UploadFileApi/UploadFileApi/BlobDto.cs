namespace UploadFileApi
{
    public class BlobDto
    {
        public string? Uri { get; set; }
        public string? Name { get; set; }
        public string? ContentType { get; set; }
        public string Version { get; set; }
        public bool IsLatest { get; set; }
        public string Tier { get; set; }
        public Stream? Content { get; set; }
    }
}
