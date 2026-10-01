using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SteamGameAnalyzerConsole.Results
{
    public class OwnedGamesResult
    {
        [JsonPropertyName("response")]
        public OwnedGamesResponse Response { get; set; }
    }

    public class OwnedGamesResponse
    {
        [JsonPropertyName("game_count")]
        public int GameCount { get; set; }

        [JsonPropertyName("games")]
        public List<OwnedGame> Games { get; set; }
    }

    public class OwnedGame
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("playtime_forever")]
        public int PlaytimeTotal { get; set; }

        [JsonPropertyName("image_icon_url")]
        public string ImageIconUrl { get; set; }

        [JsonPropertyName("playtime_windows_forever")]
        public int PlaytimeWindows { get; set; }

        [JsonPropertyName("playtime_mac_forever")]
        public int PlaytimeMac { get; set; }

        [JsonPropertyName("playtime_linux_forever")]
        public int PlaytimeLinux { get; set; }

        [JsonPropertyName("playtime_deck_forever")]
        public int PlaytimeDeck { get; set; }

        [JsonPropertyName("playtime_disconnected")]
        public int PlaytimeOffline { get; set; }
    }
}
