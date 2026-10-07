using System.Net;
using System.Text.RegularExpressions;

namespace A_U_ClimateScout.Services
{
    // Turns HTML or long text from the database into a short plain-text summary, e.g. for <meta name="description">.
    public static partial class PlainText
    {
        public static string? Summarize(string? text, int maxLength = 160)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Drop headings (a page's own title, e.g. "Why ClimateScout") and the other tags, decode entities (&amp; → &),
            // then collapse runs of spaces and line breaks.
            var plain = Whitespace().Replace(WebUtility.HtmlDecode(Tags().Replace(Headings().Replace(text, " "), " ")), " ").Trim();
            if (plain.Length <= maxLength)
            {
                return plain.Length == 0 ? null : plain;
            }

            // Cut at the last space that leaves room for the "…".
            var cut = plain.LastIndexOf(' ', maxLength - 1);
            return (cut > 0 ? plain[..cut] : plain[..(maxLength - 1)]).TrimEnd(' ', ',', ';', ':', '.') + "…";
        }

        [GeneratedRegex(@"<h[1-6][^>]*>.*?</h[1-6]\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex Headings();

        [GeneratedRegex("<[^>]*>")]
        private static partial Regex Tags();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Whitespace();
    }
}
