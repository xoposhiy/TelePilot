# TelePilot

## Problem

Hi! I have a lot of subscriptions in the Telegram and to organize chats and channels, I rely on folders: Personal, Work, Interesting, etc.
It works perfectly, and helps me not to miss any important messages, except for notification about adding me to some new chats. 
New chat notifications go to unread folder, which is the least priority folder for me, containing a lot of notifications from low important chats and channels. 
Missing these notifications become a real problem for me. THat's why I created this small app.

If you also don't want to miss new chat notifications, as I do, use this app.

It is a Windows system tray application that monitors your Telegram account for new chats and automatically notifies you via telegram bot and automatically add new chats to special folder, specified in the config.

## Features

- 🔍 **New Chat Detection**: Working as another one Telegram Client, it automatically detects when you're added to new chats, groups, or channels
- 📁 **Auto-Folder Organization**: Automatically adds new chats to a specified Telegram folder
- 📬 **Smart Notifications**: Sends notifications to yourself via a Telegram bot about new chat activity
- 🔄 **Real-time Monitoring**: Continuously monitors for new chats (checks every minute)
- 🎯 **System Tray Integration**: Runs quietly in your system tray
- 📊 **Logging**: Comprehensive logging for debugging and monitoring

## Requirements

- Windows (built for .NET 8.0-windows)
- Telegram account
- Telegram bot token (from @BotFather)
- Telegram API credentials (from https://my.telegram.org/apps)

## Quick Start

### 1. Get Telegram Credentials

1. **Get API credentials**:
   - Go to https://my.telegram.org/apps
   - Create a new application to get `api_id` and `api_hash`

2. **Create a Telegram bot**:
   - Message @BotFather on Telegram
   - Create a new bot with `/newbot`
   - Save the bot token

3. **Get your Telegram ID**:
   - Message @getmyidbot to get your user ID

### 2. Configure the Application

1. Run TelePilot.exe once - it will create `config.json` from the template
2. Edit `config.json` with your credentials:

```json
{
  "new_chats_folder": "New Chats",
  "telegram": {
    "bot_token": "your_bot_token_here",
    "api_id": "your_api_id_here", 
    "api_hash": "your_api_hash_here",
    "phone_number": "your_phone_number",
    "me": your_telegram_user_id
  }
}
```

### 3. Run the Application

1. Double-click `TelePilot.exe`.
2. On first run, you may need to verify your phone number
3. The app will minimize to system tray and start monitoring
4. Look for the TelePilot icon in your system tray

## Configuration Options

| Field | Description | Required |
|-------|-------------|----------|
| `new_chats_folder` | Name of the Telegram folder where new chats will be organized. If empty, auto-organization is disabled. | Optional |
| `telegram.bot_token` | Token from @BotFather for sending notifications | Yes |
| `telegram.api_id` | API ID from my.telegram.org | Yes |
| `telegram.api_hash` | API hash from my.telegram.org | Yes |
| `telegram.phone_number` | Your Telegram phone number | Yes |
| `telegram.me` | Your Telegram user ID (get from @getmyidbot) | Yes |

## How It Works

1. **Chat Detection**: TelePilot monitors your Telegram dialogs and maintains a list of known chats in `known-chats.txt`
2. **New Chat Processing**: When a new chat is detected:
   - Logs the discovery with chat details
   - Sends you a notification via your bot (unless it's the first run)
   - Automatically adds the chat to your specified folder (if configured)
3. **Folder Management**: Creates the target folder if it doesn't exist and organizes chats automatically

## File Structure

```
TelePilot/
├── TelePilot.exe           # Main executable
├── config.json             # Your configuration (created from template)
├── config.template.json    # Configuration template
├── known-chats.txt         # List of known chats (auto-generated)
├── logs/                   # Application logs (auto-generated)
└── tg-{userid}.session     # Telegram session file (auto-generated)
```

## Building from Source

### Prerequisites
- .NET 8.0 SDK

### Build Steps

```bash
# Clone the repository
git clone <repository-url>
cd TelePilot

# Restore packages
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --project TelePilot
```
---

**Made with ❤️ for Telegram power users**