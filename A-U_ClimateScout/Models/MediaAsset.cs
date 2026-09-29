namespace A_U_ClimateScout.Models
{
    // An uploaded file (image, SVG, logo). The file itself lives in storage (IFileStorage, Phase 3);
    // this row holds what the site needs to know about it.
    public class MediaAsset
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";      // original name, shown in the media library
        public string ContentType { get; set; } = "";   // e.g. image/png, image/svg+xml
        public string StoragePath { get; set; } = "";   // key inside storage, e.g. 2026/09/cool-roof.png
        public long SizeBytes { get; set; }
        public string? AltText { get; set; }
        public int? Width { get; set; }                  // null for SVGs and non-images
        public int? Height { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }
}
