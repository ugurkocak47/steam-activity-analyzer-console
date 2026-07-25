using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SteamGameAnalyzerConsole
{
    public class RecentGamesResult
    {
        [JsonPropertyName("response")]
        public RecentGamesResponse Response { get; set; }
    }

    public class RecentGamesResponse
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("games")]
        public List<SteamGame> Games { get; set; }
    }

    public class SteamGame
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("playtime_2weeks")]
        public int Playtime2Weeks { get; set; }

        [JsonPropertyName("playtime_forever")]
        public int PlaytimeForever { get; set; }
    }
}
