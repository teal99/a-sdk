namespace A.Core.Common;

public static class Config
{
    public static bool RelayTokens { get; private set; } = false;
    public static bool RelayOpcodes { get; private set; } = false;

    static Config()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "config.json");

            for (int i = 0; i < 4 && !File.Exists(configPath); i++)
            {
                baseDir = Path.GetDirectoryName(baseDir)!;
                if (string.IsNullOrEmpty(baseDir)) break;
                configPath = Path.Combine(baseDir, "config.json");
            }

            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);

                RelayTokens = json.Contains("\"relaytokens\": true") || json.Contains("\"relaytokens\":true");
                RelayOpcodes = json.Contains("\"relayopcodes\": true") || json.Contains("\"relayopcodes\":true");
            }
        }
        catch
        {
            /* soft failure mode: keep default tracking values if config are missing */
        }
    }
}