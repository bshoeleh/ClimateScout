using A_U_ClimateScout.Identity;

namespace A_U_ClimateScout.Areas.Admin
{
    // The Admin side menu (plan §9), in groups. Controller is null for screens not built yet: they show greyed out
    // with "soon". Policy decides who sees the item (Editors don't see Administration or carbon imports).
    public static class AdminMenu
    {
        public record Item(string Text, string? Controller, string Policy = Policies.AdminArea);

        public record Group(string Heading, IReadOnlyList<Item> Items);

        public static readonly IReadOnlyList<Group> Groups =
        [
            new("Content",
            [
                new("Climate zones", null),
                new("Zone groups", null),
                new("Diagrams", null),
                new("Design strategies", null),
                new("Content blocks", null),
                new("Media library", null),
            ]),
            new("Carbon",
            [
                new("Regions & aliases", null),
                new("Imports", null, Policies.AdminOnly),
                new("Equivalency factors", null),
            ]),
            new("Site",
            [
                new("Sponsors", null),
                new("Contact messages", null),
            ]),
            new("Administration",
            [
                new("Users", null, Policies.AdminOnly),
                new("Audit log", null, Policies.AdminOnly),
            ]),
        ];
    }
}
