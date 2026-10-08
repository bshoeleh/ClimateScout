using AngleSharp.Html.Dom;
using Ganss.Xss;

namespace A_U_ClimateScout.Services
{
    // Cleans HTML from the admin rich-text editor before it is saved (plan §9): only the tags the site styles are
    // kept — paragraphs, headings, lists, links, bold/italic, sub/superscript — and links only to http(s) and mailto.
    // Everything else (scripts, styles, classes, event handlers, Quill's data-* attributes) is removed.
    public static class HtmlCleaner
    {
        private static readonly HtmlSanitizer Sanitizer = Create();

        public static string Clean(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return "";
            }

            var clean = Sanitizer.Sanitize(html).Trim();
            // An editor left empty saves as <p><br></p>.
            return clean is "<p><br></p>" or "<p></p>" ? "" : clean;
        }

        private static HtmlSanitizer Create()
        {
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            foreach (var tag in new[] { "p", "br", "h1", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "sub", "sup", "ul", "ol", "li", "a" })
            {
                sanitizer.AllowedTags.Add(tag);
            }

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("href");
            sanitizer.AllowedAttributes.Add("target");

            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.Add("http");
            sanitizer.AllowedSchemes.Add("https");
            sanitizer.AllowedSchemes.Add("mailto");

            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedAtRules.Clear();
            sanitizer.AllowedClasses.Clear();

            // Links that open a new tab must not give that page access to ours.
            sanitizer.PostProcessNode += (_, e) =>
            {
                if (e.Node is IHtmlAnchorElement link && link.HasAttribute("target"))
                {
                    link.SetAttribute("target", "_blank");
                    link.SetAttribute("rel", "noopener");
                }
            };

            return sanitizer;
        }
    }
}
