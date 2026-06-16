public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public ICollection<State> States { get; set; } = new List<State>();
}