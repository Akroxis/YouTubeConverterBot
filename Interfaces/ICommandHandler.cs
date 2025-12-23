using YouTubeConverterBot.Models.Telegram;

namespace YouTubeConverterBot.Interfaces
{
    public interface ICommandHandler
    {
        /// <summary>
        /// Проверяет, может ли этот обработчик обработать команду.
        /// </summary>
        bool CanHandle(string command);
        
        /// <summary>
        /// Обрабатывает команду.
        /// </summary>
        Task HandleAsync(TelegramMessage message, CancellationToken cancellationToken);
        
        /// <summary>
        /// Описание команды для /help.
        /// </summary>
        string GetDescription();
    }
}