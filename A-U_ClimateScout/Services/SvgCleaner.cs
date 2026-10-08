using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace A_U_ClimateScout.Services
{
    // Makes an uploaded SVG safe to put inside our pages (plan §9): diagram SVGs are inlined in the zone pages, so
    // anything that can run code or load from elsewhere is removed — <script>, <foreignObject> and other embedding
    // elements, on… event attributes, and links (href) that don't point inside the file. Embedded raster images
    // (data:image/png|jpeg|gif|webp, as Illustrator exports them) are kept. Returns null if the file isn't an SVG.
    public static partial class SvgCleaner
    {
        private static readonly HashSet<string> RemovedElements =
            new(StringComparer.OrdinalIgnoreCase) { "script", "foreignObject", "iframe", "object", "embed", "handler", "listener" };

        public static string? Clean(string svg)
        {
            XDocument document;
            try
            {
                // No DTDs: they allow entity tricks (billion laughs, external entities).
                using var reader = XmlReader.Create(new StringReader(svg), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            }
            catch (XmlException)
            {
                return null;
            }

            if (document.Root?.Name.LocalName != "svg")
            {
                return null;
            }

            document.Root.DescendantsAndSelf().Where(e => RemovedElements.Contains(e.Name.LocalName)).ToList().ForEach(e => e.Remove());
            foreach (var element in document.Root.DescendantsAndSelf())
            {
                foreach (var attribute in element.Attributes().ToList())
                {
                    var name = attribute.Name.LocalName;
                    if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                        || (name == "href" && !IsSafeHref(attribute.Value))
                        || attribute.Value.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Remove();
                    }
                }

                // Stylesheets may not load anything from elsewhere.
                if (element.Name.LocalName == "style")
                {
                    element.Value = ExternalCss().Replace(element.Value, "");
                }
            }

            return document.Root.ToString(SaveOptions.DisableFormatting);
        }

        private static bool IsSafeHref(string value) =>
            value.StartsWith('#') || SafeDataImage().IsMatch(value);

        [GeneratedRegex(@"^data:image/(png|jpe?g|gif|webp);base64,", RegexOptions.IgnoreCase)]
        private static partial Regex SafeDataImage();

        [GeneratedRegex(@"@import[^;]*;?|url\(\s*['""]?(?!#|data:image/)[^)]*\)", RegexOptions.IgnoreCase)]
        private static partial Regex ExternalCss();
    }
}
