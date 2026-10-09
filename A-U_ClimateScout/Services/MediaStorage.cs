namespace A_U_ClimateScout.Services
{
    // Where media files (MediaAsset.StoragePath, e.g. "projects/abc.jpg") live on disk. By default that's
    // wwwroot/img/media; production can set Media:RootPath to a folder outside the site, so a deployment can never
    // overwrite uploaded files (plan Phase 9). Either way they're served at /img/media/… (see Program.cs).
    public class MediaStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        public string Root { get; } = Path.GetFullPath(
            configuration["Media:RootPath"] is { Length: > 0 } root ? root : Path.Combine(environment.WebRootPath, "img", "media"));

        public bool IsOutsideWebRoot => !Root.StartsWith(Path.GetFullPath(environment.WebRootPath), StringComparison.OrdinalIgnoreCase);

        // The full path for a storage path, or null if it would point outside the media folder ("../" tricks).
        public string? FullPath(string storagePath)
        {
            var path = Path.GetFullPath(Path.Combine(Root, storagePath));
            return path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? path : null;
        }
    }
}
