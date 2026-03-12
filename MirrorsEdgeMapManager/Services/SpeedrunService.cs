using System.Net.Http;
using System.Text.Json;

namespace MirrorsEdgeMapManager.Services;

public class SpeedrunService
{
    private readonly HttpClient _httpClient;
    private const string SpeedrunApiBase = "https://www.speedrun.com/api/v1";
    private const string AnyPercentCategoryId = "jdzzpy6d";
    private const string FullLeaderboardUrl = "https://www.speedrun.com/mecm";

    public SpeedrunService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MirrorsEdgeMapManager/2.0");
    }

    public async Task<SpeedrunStats?> GetLevelStatsAsync(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
            return null;

        try
        {
            var url = $"{SpeedrunApiBase}/levels/{levelId}/records?top=3&skip-empty=true";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            var data = json.RootElement.GetProperty("data");
            if (data.GetArrayLength() == 0)
                return null;

            // find any% records
            foreach (var leaderboard in data.EnumerateArray())
            {
                if (!leaderboard.TryGetProperty("category", out var category))
                    continue;

                if (category.GetString() != AnyPercentCategoryId)
                    continue;

                if (!leaderboard.TryGetProperty("runs", out var runs) || runs.GetArrayLength() == 0)
                    continue;

                var stats = new SpeedrunStats();
                stats.LeaderboardUrl = FullLeaderboardUrl;

                var runCount = Math.Min(3, runs.GetArrayLength());
                for (int i = 0; i < runCount; i++)
                {
                    var runData = runs[i];
                    var run = runData.GetProperty("run");
                    var place = runData.GetProperty("place").GetInt32();

                    var entry = new SpeedrunEntry
                    {
                        Place = place
                    };

                    var time = run.GetProperty("times").GetProperty("primary_t").GetDouble();
                    entry.Time = FormatTime(time);

                    if (run.TryGetProperty("date", out var dateElement))
                    {
                        entry.Date = dateElement.GetString() ?? "N/A";
                    }

                    var players = run.GetProperty("players");
                    if (players.GetArrayLength() > 0)
                    {
                        var player = players[0];
                        
                        if (player.TryGetProperty("id", out var userId))
                        {
                            var userName = await GetUserNameAsync(userId.GetString() ?? "");
                            entry.PlayerName = userName ?? "Unknown";
                        }
                        else if (player.TryGetProperty("name", out var guestName))
                        {
                            entry.PlayerName = guestName.GetString() ?? "Unknown";
                        }
                    }

                    stats.TopRuns.Add(entry);
                }

                return stats;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetUserNameAsync(string userId)
    {
        try
        {
            var url = $"{SpeedrunApiBase}/users/{userId}";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            
            var data = json.RootElement.GetProperty("data");
            if (data.TryGetProperty("names", out var names))
            {
                if (names.TryGetProperty("international", out var intName))
                {
                    return intName.GetString();
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private string FormatTime(double seconds)
    {
        var timeSpan = TimeSpan.FromSeconds(seconds);
        
        var hours = (int)timeSpan.TotalHours;
        var minutes = timeSpan.Minutes;
        var secs = timeSpan.Seconds;
        var milliseconds = timeSpan.Milliseconds;

        if (hours > 0)
            return $"{hours}h:{minutes}m:{secs}s:{milliseconds}ms";
        else if (minutes > 0)
            return $"{minutes}m:{secs}s:{milliseconds}ms";
        else
            return $"{secs}s:{milliseconds}ms";
    }
}

public class SpeedrunStats
{
    public List<SpeedrunEntry> TopRuns { get; set; } = new();
    public string LeaderboardUrl { get; set; } = "";
}

public class SpeedrunEntry
{
    public int Place { get; set; }
    public string PlayerName { get; set; } = "Unknown";
    public string Time { get; set; } = "N/A";
    public string Date { get; set; } = "N/A";
}

