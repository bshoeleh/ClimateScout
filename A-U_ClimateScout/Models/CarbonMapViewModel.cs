namespace A_U_ClimateScout.Models
{
    // The carbon map page: the data the map script needs (serialised into the page as JSON), and the note shown
    // under the map (content block carbon.map.note, editable in Admin later).
    public record CarbonMapViewModel(object MapData, string? NoteHtml);
}
