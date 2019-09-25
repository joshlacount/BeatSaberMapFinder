using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace BeatSaberMapFinder
{
    public static class BeatsaverAPI
    {
        private static readonly string _beatsaverURL = "https://beatsaver.com/api";
        private static readonly int rateLimit = 25;
        private static readonly int pageLimit = 3;

        private static List<BeatsaverMap> mapDump;

        public static string BeatSaberInstallFolder { get; set; }

        static BeatsaverAPI()
        {

        }

        public static void Start()
        {
            if (File.Exists("map_dump.json"))
                mapDump = LoadMapDump();
            else
                mapDump = DownloadMapDump();
            UpdateMapDump(ref mapDump);
            SaveMapDump(mapDump);
        }

        public static void DownloadMap(string mapKey, string mapSongName, string mapAuthor)
        {
            if (!Directory.Exists($"{BeatSaberInstallFolder}\\Beat Saber_Data\\CustomLevels\\{mapKey} ({mapSongName} - {mapAuthor})"))
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFile($"{_beatsaverURL}/download/key/{mapKey}", $"{mapKey}.zip");
                    }
                    ZipFile.ExtractToDirectory($"{mapKey}.zip", $"{BeatSaberInstallFolder}\\Beat Saber_Data\\CustomLevels\\{mapKey} ({mapSongName} - {mapAuthor})");
                    File.Delete($"{mapKey}.zip");
                }
                catch (WebException e)
                {
                    Console.WriteLine($"{e.Response} - {mapKey} - {mapSongName} - {mapAuthor}");
                }
            }
        }

        public static void DownloadMaps(SongModel song)
        {
            foreach (var map in song.MapMatches)
                DownloadMap(map.Key, map.Metadata.SongName, map.Metadata.LevelAuthorName);
        }

        public static void DownloadMaps(PlaylistModel playlist)
        {
            foreach (var song in playlist.Songs)
                DownloadMaps(song);
        }

        public static List<BeatsaverMap> DownloadMapDump()
        {
            var dumpJson = GetRequest("/download/dump/maps", returnString:true);
            Console.WriteLine("downloaded");

            List<BeatsaverMap> dump = JsonConvert.DeserializeObject<List<BeatsaverMap>>(dumpJson);

            return dump;
        }

        public static List<BeatsaverMap> LoadMapDump()
        {
            List<BeatsaverMap> dump = new List<BeatsaverMap>();

            using (FileStream fileStream = File.OpenRead("map_dump.json"))
            {
                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                var jsonString = Encoding.UTF8.GetString(buffer);
                dump = JsonConvert.DeserializeObject<List<BeatsaverMap>>(jsonString);
            }

            return dump;
        }

        public static void SaveMapDump(List<BeatsaverMap> dump)
        {
            File.WriteAllText("map_dump.json", JsonConvert.SerializeObject(dump, Formatting.Indented));
        }

        public static void UpdateMapDump(ref List<BeatsaverMap> dump)
        {
            var mostRecentDate = dump[0].Uploaded;
            int pageNum = 0;
            bool isUpdated = false;
            while (!isUpdated)
            {
                var mapsJson = JsonConvert.SerializeObject(GetRequest($"/maps/latest/{pageNum}", rateLimit:true)["docs"]);
                List<BeatsaverMap> maps = JsonConvert.DeserializeObject<List<BeatsaverMap>>(mapsJson);
                
                foreach (var m in maps)
                {
                    if (DateTime.Compare(m.Uploaded, mostRecentDate) < 0)
                    {
                        isUpdated = true;
                        break;
                    }
                    dump.Add(m);
                }
                pageNum++;
            }
        }

        public static List<BeatsaverMap> FindMapsSingleSong(SongModel song)
        {
            List<BeatsaverMap> matches = new List<BeatsaverMap>();
            var songTitleLower = song.SongTitle.ToLower();
            var songArtistLower = song.SongArtists[0].ToLower();
            
            foreach (var m in mapDump)
            {
                var mapAuthorName = m.Metadata.LevelAuthorName;
                var mapKey = m.Key;
                var mapSongName = m.Metadata.SongName;
                var mapSongSubName = m.Metadata.SongSubName;
                var mapSongNameLower = mapSongName.ToLower();
                var mapSongSubNameLower = mapSongSubName.ToLower();

                if ((mapSongNameLower.Contains(songTitleLower) && mapSongSubNameLower.Contains(songArtistLower)) || (mapSongNameLower.Contains(songTitleLower) && mapSongNameLower.Contains(songArtistLower)))
                    matches.Add(m);
            }

            if (matches.Count > 0)
            {
                var matchesJson = JsonConvert.SerializeObject(matches);
                matchesJson = matchesJson.Replace("'", "''");
                SQLite.ExecuteNonQuery($"update songs set matches='{matchesJson}' where id='{song.SongID}';");
            }
            
            return matches;
        }

        public static Dictionary<string, List<BeatsaverMap>> FindMapsPlaylist(PlaylistModel playlist)
        {
            Dictionary<string, List<BeatsaverMap>> playlistMapModels = new Dictionary<string, List<BeatsaverMap>>();

            foreach (var s in playlist.Songs)
            {
                if (s.SongID == null)
                    continue;
                var maps = FindMapsSingleSong(s);
                playlistMapModels.Add(s.SongID, maps);
            }
            Console.WriteLine("Done matching playlist");
            return playlistMapModels;
        }

            /*
            public static List<BeatsaverMap> FindMapsSingleSong(string songTitle, string songArtist)
            {
                List<BeatsaverMap> mapModels = new List<BeatsaverMap>();
                songTitle = songTitle.ToLower();
                songArtist = songArtist.ToLower();

                dynamic searchResponse = GetRequest($"/search/text/?q={songTitle}");
                int lastPage = Convert.ToInt32(searchResponse["lastPage"]) + 1;
                int totalPages = lastPage < pageLimit ? lastPage : pageLimit;

                List<int> pages = new List<int>();
                for (int i = 0; i < totalPages; i++)
                    pages.Add(i);

                TimeSpan MaxTime = new TimeSpan(0, 0, 5);
                var MaxTasksInTimeFrame = totalPages < rateLimit ? totalPages : rateLimit;
                var MaxConcurrentThreads = 4;

                BlockingCollection<BeatsaverMap> bc = new BlockingCollection<BeatsaverMap>();
                ManualResetEvent[] manualEvents = new ManualResetEvent[MaxTasksInTimeFrame];

                ThreadPool.SetMaxThreads(MaxConcurrentThreads, MaxConcurrentThreads);

                while (pages.Count > 0)
                {
                    var startTime = DateTime.Now;

                    for (int i = 0; i < MaxTasksInTimeFrame; i++)
                    {
                        try
                        {
                            var element = pages[0];
                            pages.RemoveAt(0);
                            manualEvents[i] = new ManualResetEvent(false);
                            SingleSongState state = new SingleSongState()
                            {
                                songTitle = songTitle,
                                songArtist = songArtist,
                                pageNum = element,
                                manualEvent = manualEvents[i],
                                bc = bc
                            };

                            ThreadPool.QueueUserWorkItem(new WaitCallback(SingleSongSearch), state);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            break;
                        }
                    }

                    WaitHandle.WaitAll(manualEvents);
                    var elapsedTime = DateTime.Now - startTime;
                    if (elapsedTime < MaxTime)
                    {
                        Console.WriteLine("sleeping for " + (MaxTime - elapsedTime).TotalSeconds.ToString());
                        Thread.Sleep(MaxTime - elapsedTime);
                    }
                }
                Console.WriteLine("done");
                mapModels = bc.ToList();
                return mapModels;
            }

            private static void SingleSongSearch(object input)
            {
                var state = (SingleSongState)input;
                dynamic maps = GetRequest($"/search/text/{state.pageNum}?q={state.songTitle}")["docs"];
                Console.WriteLine(state.pageNum);
                foreach (dynamic m in maps)
                {
                    var mapAuthorName = (string)m["metadata"]["levelAuthorName"];
                    var mapKey = (string)m["key"];
                    var mapSongName = ((string)m["metadata"]["songName"]).ToLower();
                    var mapSongSubName = ((string)m["metadata"]["songSubName"]).ToLower();

                    if ((mapSongName.Contains(state.songTitle) && mapSongSubName.Contains(state.songArtist)) || (mapSongName.Contains(state.songTitle) && mapSongName.Contains(state.songArtist)))
                        state.bc.Add(new BeatsaverMap() { LevelAuthorName = mapAuthorName, Key = mapKey, SongName = mapSongName });
                }

                state.manualEvent.Set();
            }

            public static Dictionary<string, List<BeatsaverMap>> FindMapsPlaylist(PlaylistModel playlist)
            {
                Dictionary<string, List<BeatsaverMap>> mapModels = new Dictionary<string, List<BeatsaverMap>>();

                List<Tuple<SongModel, int>> songPages = GetSongPages(playlist);

                TimeSpan MaxTime = new TimeSpan(0, 0, 5);
                var MaxTasksInTimeFrame = songPages.Count < rateLimit ? songPages.Count * pageLimit : rateLimit;
                var MaxConcurrentThreads = 4;

                BlockingCollection<Tuple<string, BeatsaverMap>> bc = new BlockingCollection<Tuple<string,BeatsaverMap>>();
                ManualResetEvent[] manualEvents = new ManualResetEvent[MaxTasksInTimeFrame];

                ThreadPool.SetMaxThreads(MaxConcurrentThreads, MaxConcurrentThreads);

                while (songPages.Count > 0)
                {
                    var startTime = DateTime.Now;

                    for (int i = 0; i < MaxTasksInTimeFrame; i++)
                    {
                        try
                        {
                            var element = songPages[0];
                            songPages.RemoveAt(0);
                            manualEvents[i] = new ManualResetEvent(false);
                            PlaylistState state = new PlaylistState()
                            {
                                songTitle = element.Item1.SongTitle,
                                songArtist = element.Item1.SongArtists[0],
                                songID = element.Item1.SongID,
                                pageNum = element.Item2,
                                manualEvent = manualEvents[i],
                                bc = bc
                            };

                            ThreadPool.QueueUserWorkItem(new WaitCallback(PlaylistSearch), state);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            break;
                        }
                    }

                    WaitHandle.WaitAll(manualEvents);
                    var elapsedTime = DateTime.Now - startTime;
                    if (elapsedTime < MaxTime)
                    {
                        Console.WriteLine("sleeping for " + (MaxTime - elapsedTime).TotalSeconds.ToString());
                        Thread.Sleep(MaxTime - elapsedTime);
                    }
                }

                Console.WriteLine($"done - {bc.Count}");

                foreach (var tuple in bc)
                {
                    if (mapModels.Keys.Contains(tuple.Item1))
                        mapModels[tuple.Item1].Add(tuple.Item2);
                    else
                        mapModels.Add(tuple.Item1, new List<BeatsaverMap>() { tuple.Item2 });
                }

                Console.WriteLine($"Playlist done {mapModels.Count}");
                return mapModels;
            }

            private static void PlaylistSearch(object input)
            {
                var state = (PlaylistState)input;
                dynamic maps = GetRequest($"/search/text/{state.pageNum}?q={state.songTitle}")["docs"];
                Console.WriteLine($"{state.songTitle} - {state.pageNum}");
                foreach (dynamic m in maps)
                {
                    var mapAuthorName = (string)m["metadata"]["levelAuthorName"];
                    var mapKey = (string)m["key"];
                    var mapSongName = ((string)m["metadata"]["songName"]).ToLower();
                    var mapSongSubName = ((string)m["metadata"]["songSubName"]).ToLower();
                    Console.WriteLine($"{mapSongName} - {mapSongSubName}");
                    if ((mapSongName.Contains(state.songTitle.ToLower()) && mapSongSubName.Contains(state.songArtist.ToLower())) || (mapSongName.Contains(state.songTitle.ToLower()) && mapSongName.Contains(state.songArtist.ToLower())))
                    {
                        Console.WriteLine($"Adding map for {state.songTitle}");
                        state.bc.Add(new Tuple<string, BeatsaverMap>(state.songID, new BeatsaverMap() { LevelAuthorName = mapAuthorName, Key = mapKey, SongName = mapSongName }));
                    }
                }

                state.manualEvent.Set();
            }

            private static List<Tuple<SongModel, int>> GetSongPages(PlaylistModel playlist)
            {
                List<Tuple<SongModel, int>> songPages = new List<Tuple<SongModel, int>>();
                List<SongModel> songs = playlist.Songs.ToList();

                TimeSpan MaxTime = new TimeSpan(0, 0, 5);
                var MaxTasksInTimeFrame = rateLimit;
                var MaxConcurrentThreads = 4;

                BlockingCollection<Tuple<SongModel, int>> bc = new BlockingCollection<Tuple<SongModel, int>>();
                ManualResetEvent[] manualEvents = new ManualResetEvent[MaxTasksInTimeFrame];

                ThreadPool.SetMaxThreads(MaxConcurrentThreads, MaxConcurrentThreads);

                while (songs.Count > 0)
                {
                    var startTime = DateTime.Now;

                    for (int i = 0; i < MaxTasksInTimeFrame; i++)
                    {
                        try
                        {
                            var element = songs[0];
                            songs.RemoveAt(0);
                            manualEvents[i] = new ManualResetEvent(false);
                            SongPagesState state = new SongPagesState()
                            {
                                song = element,
                                manualEvent = manualEvents[i],
                                bc = bc
                            };

                            ThreadPool.QueueUserWorkItem(new WaitCallback(GetSongPagesWorker), state);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            break;
                        }
                    }

                    WaitHandle.WaitAll(manualEvents);
                    var elapsedTime = DateTime.Now - startTime;
                    if (elapsedTime < MaxTime)
                    {
                        Console.WriteLine("sleeping for " + (MaxTime - elapsedTime).TotalSeconds.ToString());
                        Thread.Sleep(MaxTime - elapsedTime);
                    }
                }

                songPages = bc.ToList();
                Console.WriteLine($"Song pages done - {songPages.Count}");
                return songPages;
            }

            private static void GetSongPagesWorker(object input)
            {
                SongPagesState state = (SongPagesState)input;
                int totalPages = Convert.ToInt32(GetRequest($"/search/text/?q={state.song.SongTitle}")["lastPage"]) + 1;
                int numPages = totalPages < pageLimit ? totalPages : pageLimit;
                Console.WriteLine($"total pages - {totalPages}");
                Console.WriteLine($"num pages - {numPages}");
                for (int i = 0; i < numPages; i++)
                {
                    state.bc.Add(new Tuple<SongModel, int>(state.song, i));
                }
                state.manualEvent.Set();
            }
            */

            private static dynamic GetRequest(string endpoint, bool returnString = false, bool rateLimit = false)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_beatsaverURL + endpoint);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    if (rateLimit)
                    {
                        int rateLimitRemaining = Convert.ToInt32(response.Headers["Rate-Limit-Remaining"]);
                        double unixTimeStamp = Convert.ToDouble(response.Headers["Rate-Limit-Reset"]);
                        DateTime rateLimitReset = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        rateLimitReset = rateLimitReset.AddSeconds(unixTimeStamp + 5).ToLocalTime();

                        if (rateLimitRemaining == 0)
                        {
                            Console.WriteLine("waiting");
                            int dtCompare = -1;
                            while (dtCompare < 0)
                            {
                                dtCompare = DateTime.Compare(DateTime.Now, rateLimitReset);
                            }
                            Console.WriteLine("done waiting");
                        }
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string responseString = reader.ReadToEnd();
                        if (returnString)
                            return responseString;
                        return JsonConvert.DeserializeObject<dynamic>(responseString);
                    }
                }
            }
        }
    }

    class SingleSongState
    {
        public string songTitle { get; set; }
        public string songArtist { get; set; }
        public int pageNum { get; set; }
        public ManualResetEvent manualEvent { get; set; }
        public BlockingCollection<BeatsaverMap> bc { get; set; }
    }

    class PlaylistState
    {
        public string songTitle { get; set; }
        public string songArtist { get; set; }
        public string songID { get; set; }
        public int pageNum { get; set; }
        public ManualResetEvent manualEvent { get; set; }
        public BlockingCollection<Tuple<string, BeatsaverMap>> bc { get; set; }
    }

    class SongPagesState
    {
        public SongModel song { get; set; }
        public ManualResetEvent manualEvent { get; set; }
        public BlockingCollection<Tuple<SongModel, int>> bc { get; set; }
    }
}
