using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DotNetEnv;
using SteamGameAnalyzerConsole.Helpers;
using SteamGameAnalyzerConsole.Results;

namespace SteamGameAnalyzerConsole
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            LoadEnvFile();
            string? apiKey = Environment.GetEnvironmentVariable("STEAM_API_KEY");
            string? steamId = Environment.GetEnvironmentVariable("STEAM_ID");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(steamId))
            {
                Console.WriteLine("Missing STEAM_API_KEY or STEAM_ID. Create a .env file (see .env.example) with these values.");
                return;
            }

            //var newsTest = await GetNewsForAppAsync(apiKey, steamId, "440");
            //foreach (var newsItem in newsTest.AppNews.NewsItems)
            //{
            //    Console.WriteLine($"News Item: {newsItem.Title} \n\nNews Content: {newsItem.Contents}");
            //}

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nShutting down tracker...");
                cts.Cancel();
            };
            Console.WriteLine("Starting background Steam tracker... Press CTRL+C to exit.\n");

            //GetOwnedGames(apiKey, steamId);

            RecentGamesResult recentGames = await GetRecentGamesAsync(apiKey, steamId);
            SteamPlayer player = null;

            string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={steamId}";
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                PlayerSummaryResult apiResult = JsonSerializer.Deserialize<PlayerSummaryResult>(responseBody);

                player = apiResult?.Response?.Players?.FirstOrDefault();
            }
            catch (Exception err)
            {
                Console.WriteLine($"EXCEPTION: {err.Message}");
                return;
            }


            while (!cts.Token.IsCancellationRequested)
            {
                await MenuFunc(apiKey, steamId, recentGames, player, cts.Token);
            }
        }

        static void LoadEnvFile()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidate))
                {
                    Env.Load(candidate);
                    return;
                }
                dir = dir.Parent;
            }
        }

        static async Task<RecentGamesResult> GetRecentGamesAsync(string apiKey, string steamId)
        {
            string url = $"https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v0001/?key={apiKey}&steamid={steamId}&format=json";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RecentGamesResult>(responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Recent Games API Error: {e.Message}");
                return null;
            }
        }

        static async Task<bool> GetPlayerSummaryAsync(string apiKey, string steamId, RecentGamesResult recentGames)
        {
            string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={steamId}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                PlayerSummaryResult? apiResult = JsonSerializer.Deserialize<PlayerSummaryResult>(responseBody);

                var player = apiResult?.Response?.Players?.FirstOrDefault();

                if (player != null && !string.IsNullOrEmpty(player.GameId))
                {
                    int activeAppId = int.Parse(player.GameId);

                    var activeGame = recentGames?.Response?.Games?.FirstOrDefault(g => g.AppId == activeAppId);

                    if (activeGame != null)
                    {
                        Console.WriteLine($"Currently Playing: {activeGame.Name}");
                        Console.WriteLine($"- Last 2 Weeks: {(activeGame.Playtime2Weeks / 60.0):F1} hours");
                        Console.WriteLine($"- Total Playtime: {(activeGame.PlaytimeForever / 60.0):F1} hours");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Currently Playing a game (AppID: {activeAppId}), but it wasn't found in your recent games list. \n Fetching the new list... \n");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("You are not currently playing any game on Steam.");
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Player Summary Error: {e.Message}");
                return false;
            }
        }

        static async Task MenuFunc(string apiKey, string steamId, RecentGamesResult recentGames, SteamPlayer player, CancellationToken token)
        {
            RecentGamesResult currentRecent = recentGames;
            try
            {
                if (player != null)
                {
                    Console.WriteLine($"\n\nSTEAM TRACKER \nUser Id: {steamId} \nUsername: {player.Username} \n\nSelect Function:\n1 - Show Recent Games List\n2 - Monitor Mode\n3 - Get All Owned Games\n\n Press Ctrl + C to exit");
                    string? input = null;

                    while (!token.IsCancellationRequested)
                    {
                        if (Console.KeyAvailable)
                        {
                            input = Console.ReadLine();
                            break;
                        }
                        try
                        {
                            await Task.Delay(100, token);
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                    }

                    if (token.IsCancellationRequested) return;

                    switch (input)
                    {
                        case "1":
                            if (currentRecent != null)
                            {
                                Console.WriteLine("\n--- Recent Games ---");
                                foreach (var game in currentRecent.Response.Games)
                                {
                                    Console.WriteLine($"Game: {game.Name}\nLast 2 Weeks: {(game.Playtime2Weeks / 60.0):F1} hours\nTotal Playtime: {(game.PlaytimeForever / 60.0):F1} hours\n");
                                }
                            }
                            break;

                        case "2":
                            try
                            {
                                Console.WriteLine("\nEntering Monitor Mode...");

                                while (!token.IsCancellationRequested)
                                {
                                    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Fetching data from Steam...");

                                    bool recentSame = await GetPlayerSummaryAsync(apiKey, steamId, currentRecent);
                                    if (!recentSame)
                                    {
                                        currentRecent = await GetRecentGamesAsync(apiKey, steamId);
                                    }

                                    Console.WriteLine("\nWaiting 60 seconds before next scan... \nPress ESC to return to menu.");

                                    DateTime endTime = DateTime.Now.AddSeconds(60);
                                    bool exitMonitor = false;

                                    while (DateTime.Now < endTime && !token.IsCancellationRequested)
                                    {
                                        if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                                        {
                                            exitMonitor = true;
                                            break;
                                        }
                                        await Task.Delay(100, token);
                                    }

                                    if (exitMonitor)
                                    {
                                        Console.WriteLine("\nExiting monitor mode...");
                                        break;
                                    }
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                Console.WriteLine("\nMonitor mode interrupted by user.");
                            }
                            break;

                        case "3":
                        {
                            var ownedGames = await GetOwnedGamesAsync(apiKey, steamId);
                            var games = ownedGames?.Response?.Games ?? new List<OwnedGame>();
                            var paginationHelper = new PaginationHelper<OwnedGame>(games, 10);

                            if (games.Count == 0)
                            {
                                Console.WriteLine("No owned games were found.");
                                break;
                            }

                            bool returnToMenu = false;
                            while (!returnToMenu)
                            {
                                var paginatedGames = paginationHelper.Paginate().ToList();
                                Console.WriteLine($"\n--- Owned Games (Page {paginationHelper.CurrentPage}/{paginationHelper.TotalPages}) ---");
                                for (int i = 0; i < paginatedGames.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}. {paginatedGames[i].Name}");
                                }

                                Console.WriteLine("\nEnter a game number to select it, N for next page, P for previous page, or M to return to menu.");
                                var subInput = Console.ReadLine()?.Trim();

                                if (string.Equals(subInput, "M", StringComparison.OrdinalIgnoreCase))
                                {
                                    returnToMenu = true;
                                }
                                else if (string.Equals(subInput, "N", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (paginationHelper.CurrentPage < paginationHelper.TotalPages)
                                    {
                                        paginationHelper.NextPage();
                                    }
                                    else
                                    {
                                        Console.WriteLine("You are already on the last page.");
                                    }
                                }
                                else if (string.Equals(subInput, "P", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (paginationHelper.CurrentPage > 1)
                                    {
                                        paginationHelper.PreviousPage();
                                    }
                                    else
                                    {
                                        Console.WriteLine("You are already on the first page.");
                                    }
                                }
                                else if (int.TryParse(subInput, out int gameNumber) && gameNumber >= 1 && gameNumber <= paginatedGames.Count)
                                {
                                    var selectedGame = paginatedGames[gameNumber - 1];
                                    Console.WriteLine("1 - Get News for App\n2 - Get Achievement Information\n3 - Get Game Information\n4 - Return to game list");
                                    var subSubInput = Console.ReadLine();
                                    switch (subSubInput)
                                    {
                                        case "1":
                                            var newsForApp = await GetNewsForAppAsync(apiKey, steamId, selectedGame.AppId.ToString());
                                            Console.WriteLine($"\n--- News for {selectedGame.Name} ---");
                                            foreach (var newsItem in newsForApp.AppNews.NewsItems)
                                            {
                                                Console.WriteLine($"Title: {newsItem.Title}\nContent: {newsItem.Contents}\nDate: {newsItem.Date}\n\n");
                                            }
                                            break;
                                            case "2":
                                                await GetPlayerAchievementsForGame(apiKey,steamId,selectedGame.AppId.ToString());
                                                break;
                                            default:
                                                Console.WriteLine("Wrong input");
                                                break;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Invalid input. Please enter a number on this page, N, P, or M.");
                                }
                            }
                            break;
                        }

                        default:
                            Console.WriteLine("Wrong command input... Try Again...");
                            break;
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine($"EXCEPTION: {err.Message}");
            }
        }
    

        static async Task<NewsForAppResult> GetNewsForAppAsync(string apiKey, string steamId, string appId)
        {
            string url = $"https://api.steampowered.com/ISteamNews/GetNewsForApp/v0002/?appid={appId}&count=3&maxlength=300&format=json";
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NewsForAppResult>(responseBody);
            }
            catch (HttpRequestException err)
            {
                Console.WriteLine($"News For App API Exception. Message: {err.Message}");
                throw;
            }
            
        }

        static async Task<OwnedGamesResult> GetOwnedGamesAsync(string apiKey, string steamId)
        {
            string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={apiKey}&steamid={steamId}&include_appinfo=true&format=json";
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var responseContent = JsonSerializer.Deserialize<OwnedGamesResult>(responseBody);
                return responseContent!;
            }
            catch (HttpRequestException err)
            {
                Console.WriteLine($"Owned Games API Exception. Message: {err.Message}");
                throw;
            }
        }

        static async Task GetPlayerAchievementsForGame(string apiKey, string steamId,string appId, string? lang="english")
        {
            string url = $"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={appId}&key={apiKey}&steamid={steamId}&l={lang}";
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                var responseContent = JsonSerializer.Deserialize<GetAchievementInformationResult>(responseBody);
                List<GameAchievement> achievements = responseContent!.PlayerStats.Achievements;

                Console.WriteLine($"\n{responseContent.PlayerStats.GameName} Achievements:");
                foreach (var item in achievements)
                {
                    string isAchieved;
                    if (item.IsAchieved==1)
                    {
                        isAchieved = "Yes";
                    }
                    else
                    {
                        isAchieved = "No";
                    }
                    DateTimeOffset utcDate = DateTimeOffset.FromUnixTimeSeconds(item.UnlockDate);
                    DateTime localDate = utcDate.LocalDateTime;
                    Console.WriteLine($"\n{item.AchievementName}\nDescription: {item.AchievementDescription}\nAchieved:{isAchieved}\nUnlock Date:{localDate.ToString("dd-MM-yyyy")}" );
                }
            }
            catch (HttpRequestException err)
            {

                throw;
            }
        }
    }
    
}