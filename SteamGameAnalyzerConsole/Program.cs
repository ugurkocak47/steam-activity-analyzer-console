using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SteamGameAnalyzerConsole
{
    internal class Program
    {
        // Use one HttpClient for the whole application
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            string apiKey = "***REMOVED-STEAM-API-KEY***";
            string steamId = "76561198818174359";

            // 1. Create ONE cancellation token for the whole app
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent instant crash
                Console.WriteLine("\nShutting down tracker safely...");
                cts.Cancel(); // Signal the loop to stop
            };

            Console.WriteLine("Starting background Steam tracker... Press CTRL+C to exit.\n");

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
                return; // Exit safely if the initial fetch fails
            }

            // 2. Pass the cancellation token to the MenuFunc
            while (!cts.Token.IsCancellationRequested)
            {
                await MenuFunc(apiKey, steamId, recentGames, player, cts.Token);
            }
        }

        static async Task<RecentGamesResult> GetRecentGamesAsync(string apiKey, string steamId)
        {
            // Corrected URL for Recently Played Games
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
            // URL for Player Summaries (requires 'steamids' plural)
            string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={apiKey}&steamids={steamId}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                PlayerSummaryResult? apiResult = JsonSerializer.Deserialize<PlayerSummaryResult>(responseBody);

                // Safely grab the first player from the array
                var player = apiResult?.Response?.Players?.FirstOrDefault();

                if (player != null && !string.IsNullOrEmpty(player.GameId))
                {
                    // Steam returns GameId as a string, so we parse it to an int to match your RecentGames list
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

        // Add CancellationToken token as the final parameter
        static async Task MenuFunc(string apiKey, string steamId, RecentGamesResult recentGames, SteamPlayer player, CancellationToken token)
        {
            RecentGamesResult currentRecent = recentGames;
            try
            {
                if (player != null)
                {
                    // Note: Ensure your SteamPlayer class actually has a 'Username' property mapped to "personaname"
                    Console.WriteLine($"\n\nSTEAM TRACKER \nUser Id: {steamId} \nUsername: {player.Username} \n\nSelect Function:\n1 - Show Recent Games List\n2 - Monitor Mode\n\n Press Ctrl + C to exit");
                    string? input = null;

                    // A safer read loop that respects cancellation immediately
                    while (!token.IsCancellationRequested)
                    {
                        if (Console.KeyAvailable)
                        {
                            input = Console.ReadLine();
                            break;
                        }

                        // Check every 100ms if Ctrl+C was hit while waiting
                        try
                        {
                            await Task.Delay(100, token);
                        }
                        catch (TaskCanceledException)
                        {
                            return; // Exit the method immediately if cancelled
                        }
                    }

                    // If cancellation was requested during the wait, bail out
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
                            // REMOVED the duplicate CancellationTokenSource and Console.CancelKeyPress here!

                            try
                            {
                                Console.WriteLine("\nEntering Monitor Mode...");

                                // Use the token passed from Main
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

                                    // Use the token passed from Main
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
                                // Safely catches the Ctrl+C interrupt during Task.Delay
                                Console.WriteLine("\nMonitor mode interrupted by user.");
                            }
                            break;

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
    }

    
}