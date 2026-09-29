namespace A_U_ClimateScout.Data.Configurations
{
    // Column lengths shared by the entity configurations (and later by form validation),
    // so the same kind of field has the same limit everywhere.
    public static class FieldLengths
    {
        public const int Name = 200;
        public const int Slug = 100;
        public const int Url = 500;
        public const int Color = 7;            // #RRGGBB
        public const int FileName = 260;
        public const int ContentType = 100;
        public const int StoragePath = 400;
        public const int AltText = 500;
        public const int Summary = 1000;
        public const int Code = 20;            // region codes: US, US-AL, AGG-ASEAN
        public const int EnumText = 20;        // enums stored as text

        // SQL check for a #RRGGBB colour, used in CHECK constraints ({0} = column name).
        public const string ColorCheckSql = "[{0}] LIKE '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]'";
    }
}
