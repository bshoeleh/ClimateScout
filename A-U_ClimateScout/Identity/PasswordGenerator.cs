using System.Security.Cryptography;

namespace A_U_ClimateScout.Identity
{
    // Generates one-time passwords (tool init admin). Uses a cryptographic random source and always
    // includes an upper-case letter, lower-case letter, digit and symbol, so Identity's rules accept it.
    // Look-alike characters (0/O, 1/l/I) are left out, because the password is read off a screen.
    public static class PasswordGenerator
    {
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnpqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%*-_=+?";
        private const string All = Upper + Lower + Digits + Symbols;

        public static string Generate(int length = 20)
        {
            var chars = new char[length];
            chars[0] = RandomNumberGenerator.GetItems<char>(Upper, 1)[0];
            chars[1] = RandomNumberGenerator.GetItems<char>(Lower, 1)[0];
            chars[2] = RandomNumberGenerator.GetItems<char>(Digits, 1)[0];
            chars[3] = RandomNumberGenerator.GetItems<char>(Symbols, 1)[0];
            RandomNumberGenerator.GetItems<char>(All, length - 4).CopyTo(chars, 4);
            RandomNumberGenerator.Shuffle(chars.AsSpan());
            return new string(chars);
        }
    }
}
