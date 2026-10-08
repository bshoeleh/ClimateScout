using System.Buffers.Binary;
using A_U_ClimateScout.Models;

namespace A_U_ClimateScout.Services
{
    // Saves an uploaded image under wwwroot/img/media/{folder}/ and describes it as a MediaAsset (plan §9).
    // The type is read from the file's first bytes, never trusted from its name or the browser: only PNG, JPEG and
    // WebP are accepted (SVG needs its own cleaning, Phase 9). Files get a new random name, so an upload can never
    // overwrite another file or choose its own path.
    public class ImageUploads(IWebHostEnvironment environment)
    {
        public const long MaxBytes = 2 * 1024 * 1024;
        public const string Accept = "image/png,image/jpeg,image/webp";   // for <input type="file" accept="…">

        public record Result(MediaAsset? Asset, string? Error);

        public async Task<Result> SaveAsync(IFormFile file, string folder, string? altText, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                return new Result(null, "The file is empty.");
            }
            if (file.Length > MaxBytes)
            {
                return new Result(null, $"The file is {file.Length / 1024.0 / 1024.0:0.0} MB; the limit is 2 MB.");
            }

            using var memory = new MemoryStream();
            await file.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();

            var image = Identify(bytes);
            if (image is null)
            {
                return new Result(null, "Only PNG, JPEG or WebP images can be uploaded.");
            }

            var (extension, contentType, width, height) = image.Value;
            var storagePath = $"{folder}/{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(environment.WebRootPath, "img", "media", folder, Path.GetFileName(storagePath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

            return new Result(new MediaAsset
            {
                FileName = Path.GetFileName(file.FileName),
                ContentType = contentType,
                StoragePath = storagePath,
                SizeBytes = bytes.Length,
                AltText = altText,
                Width = width,
                Height = height,
                UploadedAt = DateTimeOffset.UtcNow,
            }, null);
        }

        // The image type and size from the file's header, or null if it isn't a PNG, JPEG or WebP.
        public static (string Extension, string ContentType, int? Width, int? Height)? Identify(ReadOnlySpan<byte> b)
        {
            // PNG: 8-byte signature, then the IHDR chunk with width and height (big-endian).
            if (b.Length >= 24 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            {
                return (".png", "image/png", BinaryPrimitives.ReadInt32BigEndian(b[16..]), BinaryPrimitives.ReadInt32BigEndian(b[20..]));
            }

            // JPEG: starts FF D8; the size is in the first "start of frame" segment (C0–CF, except C4, C8, CC).
            if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8)
            {
                var i = 2;
                while (i + 9 < b.Length && b[i] == 0xFF)
                {
                    var marker = b[i + 1];
                    var length = BinaryPrimitives.ReadUInt16BigEndian(b[(i + 2)..]);
                    if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
                    {
                        return (".jpg", "image/jpeg", BinaryPrimitives.ReadUInt16BigEndian(b[(i + 7)..]), BinaryPrimitives.ReadUInt16BigEndian(b[(i + 5)..]));
                    }
                    i += 2 + length;
                }
                return (".jpg", "image/jpeg", null, null);
            }

            // WebP: "RIFF" …. "WEBP", then a VP8 / VP8L / VP8X chunk holding the size.
            if (b.Length >= 30 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8))
            {
                var chunk = b[12..16];
                if (chunk.SequenceEqual("VP8 "u8))
                {
                    return (".webp", "image/webp", BinaryPrimitives.ReadUInt16LittleEndian(b[26..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(b[28..]) & 0x3FFF);
                }
                if (chunk.SequenceEqual("VP8L"u8))
                {
                    var bits = BinaryPrimitives.ReadUInt32LittleEndian(b[21..]);
                    return (".webp", "image/webp", (int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
                }
                if (chunk.SequenceEqual("VP8X"u8))
                {
                    return (".webp", "image/webp", (b[24] | b[25] << 8 | b[26] << 16) + 1, (b[27] | b[28] << 8 | b[29] << 16) + 1);
                }
                return (".webp", "image/webp", null, null);
            }

            return null;
        }
    }
}
