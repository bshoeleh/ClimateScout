namespace A_U_ClimateScout.Options
{
    // Outgoing email (plan §8). SMTP covers Mailjet (in-v3.mailjet.com, port 587, API key + secret as user name and
    // password) and a local test mailbox such as Mailpit (localhost, port 1025, no sign-in). The password belongs in
    // user secrets / the CS__Email__Password environment variable, never in appsettings.json.
    // While Host is empty, nothing is sent: the site logs that an email would have gone out (never its text).
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "Arcadis ClimateScout";

        // Who gets "new contact message" emails; empty = every Admin.
        public List<string> NotifyAddresses { get; set; } = [];
    }
}
