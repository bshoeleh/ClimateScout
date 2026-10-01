namespace A_U_ClimateScout.Services
{
    // Picks black or white text for a #RRGGBB background, whichever contrasts more (WCAG relative luminance).
    // Used for the zone colour label.
    public static class ColorContrast
    {
        public static string TextColorFor(string hexColor)
        {
            var luminance = 0.2126 * Channel(hexColor, 1) + 0.7152 * Channel(hexColor, 3) + 0.0722 * Channel(hexColor, 5);
            var withBlack = (luminance + 0.05) / 0.05;
            var withWhite = 1.05 / (luminance + 0.05);
            return withBlack >= withWhite ? "#000000" : "#FFFFFF";
        }

        private static double Channel(string hexColor, int start)
        {
            var value = Convert.ToInt32(hexColor.Substring(start, 2), 16) / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
