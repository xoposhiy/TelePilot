using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TL;
using WTelegram;
using Message = TL.Message;
using Timer = System.Windows.Forms.Timer;

namespace TelePilot;

internal class TelegramManagerService
{
    private readonly ILogger<TelegramManagerService> logger;
    private readonly Config config;
    private readonly Func<string, string?> promptSecret;
    private readonly Client telegramClient;
    private readonly TelegramBotClient botClient;

    public TelegramManagerService(ILogger<TelegramManagerService> logger, Config config, Func<string, string?> promptSecret)
    {
        this.logger = logger;
        this.config = config;
        this.promptSecret = promptSecret;
        
        // Check if config is properly loaded
        if (IsConfigInvalid(config))
        {
            HandleInvalidConfig();
            // Since we can't proceed without a valid config, we'll set up minimal clients
            // that won't connect but will prevent null reference exceptions
            telegramClient = new Client(_ => null);
            botClient = new TelegramBotClient("invalid_token");
            return;
        }
        
        telegramClient = new Client(ConfigValue);
        botClient = new TelegramBotClient(config.Telegram.BotToken);
    }

    private bool IsConfigInvalid(Config config)
    {
        return config == null || config.Telegram == null || 
               string.IsNullOrEmpty(config.Telegram.BotToken) || 
               string.IsNullOrEmpty(config.Telegram.ApiId) ||
               string.IsNullOrEmpty(config.Telegram.ApiHash) ||
               string.IsNullOrEmpty(config.Telegram.PhoneNumber) ||
               config.Telegram.Me <= 0;
    }

    private void HandleInvalidConfig()
    {
        const string configFilePath = "config.json";
        const string templateFilePath = "config.template.json";
        
        logger.LogWarning("Invalid or missing configuration detected!");
        
        if (!File.Exists(configFilePath) && File.Exists(templateFilePath))
        {
            logger.LogInformation("Creating config.json from template...");
            File.Copy(templateFilePath, configFilePath);
            logger.LogInformation("Config file created. Please edit it with your Telegram credentials.");
        }
        
        // Open the config file in the default text editor
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = configFilePath,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
            logger.LogInformation("Opening config file for editing.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open config file for editing.");
            logger.LogInformation($"Please manually edit the config file at: {Path.GetFullPath(configFilePath)}");
        }
        
        logger.LogWarning("Application will not function correctly until configuration is properly set up!");
    }

    private string? ConfigValue(string what)
    {
        return what switch
        {
            "api_id" => config.Telegram.ApiId,
            "api_hash" => config.Telegram.ApiHash,
            "phone_number" => config.Telegram.PhoneNumber,
            "verification_code" => promptSecret("Verification code") ?? "",
            "password" => promptSecret("Password") ?? "",
            "session_key" => null,
            "session_pathname" => Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? ".", $"tg-{config.Telegram.Me}.session"),
            "server_address" => null,
            "init_params" => null,
            _ => null
        };
    }

    public async void Start()
    {
        logger.LogInformation("Starting telegram manager");

        // If config is invalid, don't try to connect
        if (IsConfigInvalid(config))
        {
            logger.LogWarning("Cannot start Telegram manager with invalid configuration.");
            return;
        }

        try
        {
            var user = await telegramClient.LoginUserIfNeeded();
            logger.LogInformation("Logged in as: {User}", user.username ?? user.first_name);
            await CheckForUnreadChats();
            var timer = new Timer();
            timer.Interval = 60 * 1000;
            timer.Tick += async (_, _) => await CheckForUnreadChats();
            timer.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start TelegramManagerService");
        }
    }

    private async Task CheckForUnreadChats()
    {
        try
        {
            logger.LogInformation("Checking for new unread chats...");
            var knownChats = (await File.ReadAllLinesAsync("known-chats.txt"))
                .Select(line => line.Split(' ', 2))
                .ToDictionary(p => long.Parse(p[0]), p => p[1]);
            var firstRun = knownChats.Count == 0;
            if (firstRun)
                logger.LogInformation("First run. Collecting all chats to known-chats.txt file...");
            var ans = firstRun ? await telegramClient.Messages_GetAllDialogs() : await telegramClient.Messages_GetDialogs();
            if (ans is not Messages_Dialogs dialogs) throw new Exception($"Unknown answer {ans.GetType()}");
            var messages = dialogs.messages
                .GroupBy(m => m.ID)
                .ToDictionary(g => g.Key, g => g.First());
            var newUnknown = 0;
            foreach (var dialog in dialogs.dialogs.OfType<Dialog>())
            {
                //if (dialog.unread_count == 0) continue;
                if (!dialogs.chats.TryGetValue(dialog.peer.ID, out ChatBase? chat)) continue;
                if (!messages.TryGetValue(dialog.top_message, out var message)) continue;
                if (!knownChats.ContainsKey(chat.ID))
                {
                    logger.LogInformation($"NEW CHAT {chat.Title} has {dialog.unread_count} unread messages. Last: {message.Date}");
                    if (!firstRun)
                    {
                        var notificationMessage = 
                            $"🆕 New chat: <a href=\"https://t.me/c/{chat.ID}\"><b>{chat.Title}</b></a>\n" +
                            $"📬 Unread messages: {dialog.unread_count}\n" +
                            $"⌚ Last message: {FormatRelativeTime(message.Date)}";
    
                        await SendNotificationAsync(notificationMessage, enableHtml: true);
                    }

                    knownChats.Add(chat.ID, chat.Title);
                    newUnknown++;
                    
                    // Add new chat to specified folder if configured
                    if (!string.IsNullOrEmpty(config.NewChatsFolder))
                    {
                        await AddChatToFolder(chat, config.NewChatsFolder);
                    }
                }
            }
            await File.WriteAllLinesAsync("known-chats.txt",
                knownChats.OrderBy(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value}"));
            logger.LogInformation("Checked {checkedCount} Found {newCount} new chats!", dialogs.dialogs.Length, newUnknown);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to check for new unread chats");
        }
    }

    public async Task SendNotificationAsync(string message, bool enableHtml = false)
    {
        var chatId = new Telegram.Bot.Types.ChatId(config.Telegram.Me);
        await botClient.SendMessage(
            chatId, 
            message,
            parseMode: enableHtml ? ParseMode.Html : ParseMode.None);
    }

    private string FormatRelativeTime(DateTime dateTime)
    {
        var now = DateTime.Now;
        var span = now - dateTime;
        if (span.TotalSeconds < 60)
            return "just now";
        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} minute{(span.TotalMinutes >= 2 ? "s" : "") } ago";
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "") } ago";
        if (span.TotalDays < 2)
            return "yesterday";
        if (span.TotalDays < 7)
            return $"{(int)span.TotalDays} day{(span.TotalDays >= 2 ? "s" : "") } ago";
        return dateTime.ToString("g");
    }

    private async Task AddChatToFolder(ChatBase chat, string folderName)
    {
        try
        {
            logger.LogInformation($"Adding chat {chat.Title} (ID: {chat.ID}) to folder '{folderName}'");
            
            // Get current folders
            var filters = await telegramClient.Messages_GetDialogFilters();
            
            // Find or create the target folder
            DialogFilter? targetFolder = null;
            foreach (var filter in filters.filters)
            {
                if (filter is DialogFilter dialogFilter && dialogFilter.Title.text == folderName)
                {
                    targetFolder = dialogFilter;
                    break;
                }
            }
            
            // If folder doesn't exist, create it
            if (targetFolder == null)
            {
                logger.LogInformation($"Creating new folder '{folderName}'");
                targetFolder = new DialogFilter
                {
                    id = filters.filters.Length > 0 ? filters.filters.Where(f => f is DialogFilter).Cast<DialogFilter>().Max(f => f.id) + 1 : 1,
                    title = new TextWithEntities { text = folderName },
                    flags = DialogFilter.Flags.contacts | DialogFilter.Flags.non_contacts | 
                           DialogFilter.Flags.groups | DialogFilter.Flags.broadcasts | DialogFilter.Flags.bots,
                    include_peers = new InputPeer[0],
                    exclude_peers = new InputPeer[0]
                };
                
                await telegramClient.Messages_UpdateDialogFilter(targetFolder.id, targetFolder);
            }
            
            // Create the appropriate InputPeer based on chat type
            InputPeer chatPeer = chat switch
            {
                Chat => new InputPeerChat(chat.ID),
                Channel channel => new InputPeerChannel(chat.ID, channel.access_hash),
                _ => throw new InvalidOperationException($"Unknown chat type: {chat.GetType()}")
            };
            
            var currentIncludePeers = targetFolder.include_peers?.ToList() ?? new List<InputPeer>();
            
            // Check if chat is already in the folder
            bool alreadyInFolder = currentIncludePeers.Any(peer => 
                (peer is InputPeerChat inputPeerChat && inputPeerChat.chat_id == chat.ID) ||
                (peer is InputPeerChannel inputPeerChannel && inputPeerChannel.channel_id == chat.ID));
            
            if (!alreadyInFolder)
            {
                currentIncludePeers.Add(chatPeer);
                targetFolder.include_peers = currentIncludePeers.ToArray();
                
                await telegramClient.Messages_UpdateDialogFilter(targetFolder.id, targetFolder);
                logger.LogInformation($"Successfully added chat '{chat.Title}' to folder '{folderName}'");
            }
            else
            {
                logger.LogInformation($"Chat '{chat.Title}' is already in folder '{folderName}'");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to add chat '{chat.Title}' to folder '{folderName}': {ex.Message}");
        }
    }

    public void Stop()
    {
        logger.LogInformation("Stopping telegram manager");
    }
}