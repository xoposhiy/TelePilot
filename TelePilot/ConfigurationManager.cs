using System.Text.Json;

namespace TelePilot
{
    public class ConfigurationManager
    {
        private const string ConfigFilePath = "config.json";

        public static Config LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new Config();
            }

            var json = File.ReadAllText(ConfigFilePath);
            var options = GetJsonSerializerOptions();

            return JsonSerializer.Deserialize<Config>(json, options) ?? throw new Exception("Empty config?");
        }

        public static void SaveConfig(Config config)
        {
            var options = GetJsonSerializerOptions();
            var json = JsonSerializer.Serialize(config, options);

            File.WriteAllText(ConfigFilePath, json);
        }

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = true
            };

            return options;
        }

    }

    public class Config
    {
        public string NewChatsFolder { get; set; } = null!;
        public TelegramCredentials Telegram { get; set; } = null!;
    }

    public class TelegramCredentials
    {
        public string BotToken { get; set; } = null!;
        public string ApiId { get; set; } = null!;
        public string ApiHash { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public long Me { get; set; } = 0;
    }
}