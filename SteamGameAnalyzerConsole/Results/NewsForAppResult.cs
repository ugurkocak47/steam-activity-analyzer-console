using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SteamGameAnalyzerConsole.Results
{
    public class NewsForAppResult
    {
        [JsonPropertyName("appnews")]
        public AppNews AppNews { get; set; }
    }

    public class AppNews
    {
        [JsonPropertyName("appid")]
        public int AppId { get; set; }

        [JsonPropertyName("newsitems")]
        public List<NewsItem> NewsItems { get; set; }
    }

    public class NewsItem
    {
        [JsonPropertyName("gid")]
        public string GId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("is_external_url")]
        public bool IsExternalUrl { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("contents")]
        public string Contents { get; set; }

        [JsonPropertyName("feedlabel")]
        public string FeedLabel { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("feedname")]
        public string FeedName { get; set; }

        [JsonPropertyName("feed_type")]
        public short FeedType { get; set; }

        [JsonPropertyName("appid")]
        public int AppId { get; set; }
    }
}
