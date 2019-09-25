using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMapFinder
{
    public class BeatsaverMap
    {
        public Metadata Metadata { get; set; }
        public Stats Stats { get; set; }
        

        public string Description { get; set; }
        public string _Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public Uploader Uploader { get; set; }
        public string Hash { get; set; }
        public DateTime Uploaded { get; set; }
        public string DirectDownload { get; set; }
        public string DownloadUrl { get; set; }
        public string CoverUrl { get; set; }
    }

    public struct Metadata
    {
        public Dictionary<string, bool> Difficulties { get; set; }
        public List<Characteristic> Characteristics { get; set; }
        public string LevelAuthorName { get; set; }
        public string SongAuthorName { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public double Bpm { get; set; }
    }

    public struct Characteristic
    {
        public Dictionary<string, Difficulty?> Difficulties { get; set; }
        public string Name { get; set; }
    }

    public struct Difficulty
    {
        public double? Duration { get; set; }
        public int? Length { get; set; }
        public int? Bombs { get; set; }
        public int? Notes { get; set; }
        public int? Obstacles { get; set; }
    }

    public struct Stats
    {
        public int Downloads { get; set; }
        public int Plays { get; set; }
        public int Downvotes { get; set; }
        public int Upvotes { get; set; }
        public double Heat { get; set; }
        public double Rating { get; set; }
    }

    public struct Uploader
    {
        public string _Id { get; set; }
        public string Username { get; set; }
    }
}
