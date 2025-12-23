namespace YouTubeConverterBot.Models
{
    public class BotConfiguration
    {
        public string BotToken { get; set; } = string.Empty;
        public IEnumerable<int> AdminIds { get; set; } = [];
    }
}