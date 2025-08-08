using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WTelegram;

namespace TelePilot;

class TrayApp : Form
{
    private readonly TelegramManagerService telegramManagerService;
    private readonly ILogger<TrayApp> logger;
    private readonly NotifyIcon trayIcon;
    private readonly ContextMenuStrip trayMenu;

    public TrayApp(TelegramManagerService telegramManagerService, ILogger<TrayApp> logger, Config config)
    {
        logger.LogInformation("Starting TrayApp in dir: {Dir}", Directory.GetCurrentDirectory());
        logger.LogInformation("Run Telegram for Phone: {Phone}", config.Telegram.PhoneNumber);
        this.telegramManagerService = telegramManagerService;
        this.logger = logger;
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Show Log", null, OnOpenLog);
        trayMenu.Items.Add("Run on startup", null, OnRunOnStartup);
        trayMenu.Items.Add("Exit", null, OnExit);

        trayIcon = new NotifyIcon
        {
            Text = "TelePilot",
            Icon = new Icon("app.ico", SystemInformation.SmallIconSize),
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        trayIcon.DoubleClick += OnOpenLog;

        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        Visible = false;
    }

    private void OnRunOnStartup(object? sender, EventArgs e)
    {
        try
        {
            var appPath = $"\"{Process.GetCurrentProcess().MainModule!.FileName}\"";

            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                       keyName, true))
            {
                key?.SetValue("TelegramMonitorApp", appPath);
            }
            logger.LogInformation("Application has been added to startup with path: {AppPath}. Registry Key: {KeyName}", appPath, keyName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"An error occurred while adding to startup: {ex.Message}");
        }

    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
        logger.LogInformation("Starting telegram service");
        telegramManagerService.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        logger.LogInformation("Stopping telegram service");
        telegramManagerService.Stop();
    }

    private void OnOpenLog(object? sender, EventArgs e)
    {
        if (!File.Exists("log.txt"))
            File.WriteAllText("log.txt", "");
        Process.Start(new ProcessStartInfo("log.txt")
        {
            UseShellExecute = true
        });
    }

    private void OnExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            trayIcon.Dispose();
            trayMenu.Dispose();
        }
        base.Dispose(disposing);
    }
}