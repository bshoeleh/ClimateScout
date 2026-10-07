namespace A_U_ClimateScout.Identity
{
    /// <summary>Role names stored in the Identity tables.</summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Editor = "Editor";
    }

    /// <summary>Authorization policy names; see Program.cs for what each policy requires.</summary>
    public static class Policies
    {
        /// <summary>Anyone allowed into the Admin area (Admin or Editor).</summary>
        public const string AdminArea = "AdminArea";

        /// <summary>Admin-only screens: users, audit log, carbon imports (plan §9).</summary>
        public const string AdminOnly = "AdminOnly";
    }
}
