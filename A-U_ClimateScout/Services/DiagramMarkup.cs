using System.Net;
using System.Text.RegularExpressions;

namespace A_U_ClimateScout.Services
{
    // Prepares a building diagram SVG (an Illustrator export) for inlining in a zone page: drops the XML declaration
    // and comments, gives the <svg> a class and an accessible name, and marks every strategy layer (<g id="ds-{slug}">)
    // with the class cs-diagram-layer so CSS hides it until its strategy is selected. Other ds- layers
    // (ds-base-diagram, ds-black-background) stay visible. Returns the slugs that have a layer.
    public static partial class DiagramMarkup
    {
        public static (string Markup, IReadOnlySet<string> Layers) Prepare(string svg, IReadOnlySet<string> strategySlugs, string label)
        {
            var layers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var markup = XmlDeclarationOrComment().Replace(svg, "");
            markup = StrategyLayer().Replace(markup, match =>
            {
                if (!strategySlugs.Contains(match.Groups[1].Value))
                {
                    return match.Value;
                }

                layers.Add(match.Groups[1].Value);
                return $"{match.Value} class=\"cs-diagram-layer\"";
            });
            markup = SvgRoot().Replace(markup, $"<svg class=\"cs-diagram-svg\" role=\"img\" aria-label=\"{WebUtility.HtmlEncode(label)}\" ", 1);
            return (markup.Trim(), layers);
        }

        [GeneratedRegex(@"<\?xml.*?\?>|<!--.*?-->", RegexOptions.Singleline)]
        private static partial Regex XmlDeclarationOrComment();

        [GeneratedRegex("<g id=\"ds-([^\"]+)\"")]
        private static partial Regex StrategyLayer();

        [GeneratedRegex("<svg ")]
        private static partial Regex SvgRoot();
    }
}
