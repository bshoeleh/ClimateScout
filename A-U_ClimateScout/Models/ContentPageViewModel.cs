namespace A_U_ClimateScout.Models
{
    // A page that is one content block (About now; later e.g. privacy or terms): the browser-tab title and the HTML,
    // which brings its own headings.
    public record ContentPageViewModel(string Title, string? Html);
}
