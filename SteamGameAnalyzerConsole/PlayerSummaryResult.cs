using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SteamGameAnalyzerConsole
{
    public class PlayerSummaryResult
    {
        [JsonPropertyName("response")]
        public PlayerSummaryResponse Response { get; set; }
    }

    public class PlayerSummaryResponse
    {
        // Steam puts the player data inside an array
        [JsonPropertyName("players")]
        public List<SteamPlayer> Players { get; set; }
    }

    public class SteamPlayer
    {
        [JsonPropertyName("gameextrainfo")]
        public string GameName { get; set; }

        // Steam returns gameid as a STRING ("730"), not an integer!
        [JsonPropertyName("gameid")]
        public string GameId { get; set; }

        [JsonPropertyName("personaname")]
        public string Username { get; set; }
    }
}
