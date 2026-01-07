using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace update
{
    public class UpdateInfo
    {
        public string version { get; set; }
        public string description { get; set; }
        public string sha256 { get; set; }
        public List<string> dependencies { get; set; }
        public DateTime serverTime { get; set; }
        public string downloadUrl { get; set; }
        public int downloadUrlExpireIn { get; set; }
    }

    public class NoticeInfo
    {
        public string message { get; set; }
        public bool enableClient { get; set; } 
        public bool enableUpdate { get; set; }
        public DateTime serverTime { get; set; }
    }
    
    [JsonSerializable(typeof(UpdateInfo))]
    [JsonSerializable(typeof(NoticeInfo))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}