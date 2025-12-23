using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YouTubeConverterBot.Interfaces;
using YouTubeConverterBot.Models;
using YouTubeConverterBot.Services;
using YouTubeConverterBot.Services.Handlers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(configure =>
        {
            configure.ClearProviders();
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });
        
        services.Configure<BotConfiguration>(
            context.Configuration.GetSection("BotConfig"));
        
        // 3. HttpClient для Telegram API с именем "Telegram"
        services.AddHttpClient("Telegram", (provider, client) =>
        {
            var config = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{config.BotToken}/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        
        // 4. Отдельный HttpClient для TelegramBotService (без имени)
        services.AddHttpClient<TelegramBotService>((provider, client) =>
        {
            var config = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{config.BotToken}/");
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        
        // 5. Сервис отправки сообщений (Scoped или Transient)
        services.AddScoped<MessageSenderService>();
        
        // 6. Обработчики команд
        services.AddScoped<ICommandHandler, StartCommandHandler>();
        services.AddScoped<ICommandHandler, ConvertCommandHandler>();
        services.AddScoped<ICommandHandler, HelpCommandHandler>();
        
        // 7. Основной сервис бота
        services.AddHostedService<TelegramBotService>();
        
        Console.WriteLine("Конфигурация DI завершена");
    })
    .UseConsoleLifetime()
    .Build();

try
{
    Console.WriteLine("Запуск YouTube Converter Bot...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Критическая ошибка: {ex.Message}");
    throw;
}
finally
{
    Console.WriteLine("Бот остановлен");
}