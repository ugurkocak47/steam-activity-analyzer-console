using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SteamGameAnalyzerConsole.Results
{
    public class GetAchievementInformationResult
    {
        [JsonPropertyName("playerstats")]
        public AchievementPlayerStats PlayerStats { get; set; }
    }

    public class AchievementPlayerStats
    {
        [JsonPropertyName("steamID")]
        public string SteamID { get; set; }
        [JsonPropertyName("gameName")]
        public string GameName { get; set; }
        [JsonPropertyName("achievements")]
        public List<GameAchievement> Achievements { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class GameAchievement
    {
        [JsonPropertyName("apiname")]
        public string AchievementAPIName { get; set; }
        [JsonPropertyName("achieved")]
        public int IsAchieved { get; set; }
        [JsonPropertyName("unlocktime")]
        public int UnlockDate { get; set; }
        [JsonPropertyName("name")]
        public string AchievementName { get; set; }
        [JsonPropertyName("description")]
        public string AchievementDescription { get; set; }

    }
}
