using Microsoft.Extensions.Logging;
using YouTubeConverterBot.Models.Telegram;

namespace YouTubeConverterBot.Services.Handlers
{
    /// <summary>
    /// Обработчик команды /convert (заглушка).
    /// Позже будет заниматься конвертацией видео в аудио.
    /// </summary>
    public class ConvertCommandHandler : BaseCommandHandler
    {
        public override string Command => "/convert";
        public override string Description => "Конвертировать YouTube видео в аудио";
        
        public ConvertCommandHandler(
            ILogger<ConvertCommandHandler> logger,
            MessageSenderService messageSender)
            : base(logger, messageSender)
        {
        }
        
        public override async Task HandleAsync(TelegramMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Обработка команды /convert от {UserId}", message.From?.Id);
            
            var responseText = """
                               <b>Функция в разработке</b>

                               <b>Планируемый синтаксис:</b>
                               <code>/convert https://youtube.com/watch?v=...</code>

                               <b>Что будет уметь:</b>
                               • Конвертация в MP3, AAC, FLAC
                               • Выбор качества (128kbps, 192kbps, 320kbps)
                               • Добавление метаданных
                               """;
            
            await SendResponseAsync(message.Chat.Id, responseText, cancellationToken);
        }
    }
}