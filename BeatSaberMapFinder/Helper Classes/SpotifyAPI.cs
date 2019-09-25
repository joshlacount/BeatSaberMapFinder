using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Data;
using System.Timers;

namespace BeatSaberMapFinder
{
    public static class SpotifyAPI
    {
        private static readonly string _clientID = "b7a2eaf30f7344b29856972ced3daf11";
        private static readonly string _clientSecret = "55796c5350cf4a769cd669d69f35f8a4";
        private static readonly string _redirectURI = "http://localhost:4200/";

        private static HttpClient _client = new HttpClient();
        private static string _token;
        /*
        static SpotifyAPI()
        {
            var table = SQLite.ExecuteQuery("select * from api where service='spotify'");
            Console.WriteLine(table.Rows.Count);

            if (table.Rows.Count > 0)
            {
                var tokenExpirationDT = DateTime.FromBinary(Convert.ToInt64(table.Rows[0]["token_expiration"]));
                if (DateTime.Compare(DateTime.Now, tokenExpirationDT) < 0)
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (string)table.Rows[0]["access_token"]);
                else
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientID}:{_clientSecret}")));

                    var content = new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", (string)table.Rows[0]["refresh_token"] }
                    };

                    var json = PostRequest("https://accounts.spotify.com/api/token", content);
                    string accessToken = json["access_token"];
                    string tokenExpiration = DateTime.Now.AddSeconds((double)json["expires_in"]).ToBinary().ToString();

                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    SQLite.ExecuteNonQuery($"update api set access_token = '{accessToken}', token_expiration = '{tokenExpiration}' where service='spotify';");
                }
                Task.Run(() =>
                {
                    bool localLoaded = false;
                    var subToken = EventSystem.Subscribe<PlaylistsFromLocalMessage>(a =>
                    {
                        Console.WriteLine("received local load message");
                        localLoaded = true;
                    });
                    Console.WriteLine("Waiting for local load");
                    while (!localLoaded)
                    { }
                    EventSystem.Publish<SpotifyAccessTokenMessage>();
                    EventSystem.Unsubscribe<PlaylistsFromLocalMessage>(subToken);

                });
            }

            else
            {
                EventSystem.Publish<SpotifySignInMessage>();
            }
        }
        */
        public static void Start()
        {
            var table = SQLite.ExecuteQuery("select * from api where service='spotify'");
            Console.WriteLine(table.Rows.Count);

            if (table.Rows.Count > 0)
            {
                var tokenExpirationDT = DateTime.FromBinary(Convert.ToInt64(table.Rows[0]["token_expiration"]));
                if (DateTime.Compare(DateTime.Now, tokenExpirationDT) < 0)
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (string)table.Rows[0]["access_token"]);
                else
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientID}:{_clientSecret}")));

                    var content = new Dictionary<string, string>
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", (string)table.Rows[0]["refresh_token"] }
                    };

                    var json = PostRequest("https://accounts.spotify.com/api/token", content);
                    string accessToken = json["access_token"];
                    string tokenExpiration = DateTime.Now.AddSeconds((double)json["expires_in"]).ToBinary().ToString();

                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    SQLite.ExecuteNonQuery($"update api set access_token = '{accessToken}', token_expiration = '{tokenExpiration}' where service='spotify';");
                }

                EventSystem.Publish<SpotifyAccessTokenMessage>();
            }

            else
            {
                EventSystem.Publish<SpotifySignInMessage>();
            }
        }

        public static void GetNewTokens()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(_redirectURI);
            listener.Start();
            var t = Task.Run(() =>
            {
                IAsyncResult asyncResult = listener.BeginGetContext(HttpListener_Callback, listener);
                asyncResult.AsyncWaitHandle.WaitOne();

                DateTime timeout = DateTime.Now.AddMinutes(2);
                while (!asyncResult.IsCompleted || _token == null)
                {
                    if (DateTime.Now > timeout)
                        return;
                }

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientID}:{_clientSecret}")));

                var content = new Dictionary<string, string>
                {
                    { "code", _token },
                    { "grant_type", "authorization_code" },
                    { "redirect_uri", "http://localhost:4200/" }
                };

                var json = PostRequest("https://accounts.spotify.com/api/token", content);
                string accessToken = json["access_token"];
                string refreshToken = json["refresh_token"];
                string tokenExpiration = DateTime.Now.AddSeconds((double)json["expires_in"]).ToBinary().ToString();

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer", accessToken);

                if (SQLite.ExecuteQuery("select * from api").Rows.Count > 0)
                    SQLite.ExecuteNonQuery($"update api set access_token = '{accessToken}', refresh_token = '{refreshToken}', token_expiration = '{tokenExpiration}' where service='spotify';");
                else
                    SQLite.ExecuteNonQuery($"insert into api (access_token, refresh_token, token_expiration, service) values ('{accessToken}', '{refreshToken}', '{tokenExpiration}', 'spotify');");

                EventSystem.Publish<SpotifyAccessTokenMessage>();
            });
        }

        public static List<PlaylistModel> GetPlaylists(bool fromLocal=false)
        {
            List<PlaylistModel> playlistModels = new List<PlaylistModel>();

            if (!fromLocal)
            {
                dynamic playlistGet = GetRequest("https://api.spotify.com/v1/me/playlists");
                dynamic spotifyPlaylists = playlistGet["items"];
                
                var localPlaylists = SQLite.ExecuteQuery("select * from playlists where platform='spotify';").Rows;
                int playlistDiff = localPlaylists.Count - spotifyPlaylists.Count;
                /*
                if (playlistDiff > 0)
                {
                    foreach (DataRow lP in localPlaylists)
                    {
                        foreach (var sP in spotifyPlaylists)
                        {
                            if (lP["id"] == sP["id"])
                                break;
                        }

                        SQLite.ExecuteNonQuery($"delete from playlists where id='{lP["id"]}' and platform='spotify';");
                        SQLite.ExecuteNonQuery($"delete from songs where playlist_id='{lP["id"]}' and platform='spotify';");
                        break;
                    }
                }
                */
                foreach (dynamic p in spotifyPlaylists)
                {
                    int numTracks = p["tracks"]["total"];

                    List<SongModel> songs = new List<SongModel>();

                    for (int i = 0; i < Math.Ceiling(numTracks / 100f); i++)
                    {
                        dynamic tracks = GetRequest($"https://api.spotify.com/v1/playlists/{p["id"]}/tracks?offset={i * 100}")["items"];
                        foreach (dynamic t in tracks)
                        {
                            if (t["track"] != null)
                            {
                                string songName = t["track"]["name"];
                                string songID = t["track"]["id"];
                                List<string> artists = new List<string>();
                                foreach (dynamic a in t["track"]["artists"])
                                {
                                    if (a["name"] != "Various Artists")
                                        artists.Add((string)a["name"]);
                                }
                                string artistsString = string.Join(", ", artists.ToArray());
                                
                                int numSongs = SQLite.ExecuteQuery($"select * from songs where id='{songID}' and playlist_id='{p["id"]}' and platform='spotify';").Rows.Count;
                                if (numSongs == 0)
                                {
                                    Console.WriteLine($"{songName} - {p["name"]}");
                                    SQLite.ExecuteNonQuery($"insert into songs (title, artists, id, playlist_id, platform) values (\"{songName}\", \"{artistsString}\", '{songID}', \"{p["id"]}\", 'spotify');");
                                }

                                List<BeatsaverMap> matches = new List<BeatsaverMap>();
                                var matchesJson = SQLite.ExecuteQuery($"select * from songs where id='{songID}' and platform='spotify';").Rows[0]["matches"];
                                if (matchesJson.GetType() != typeof(DBNull))
                                    matches = JsonConvert.DeserializeObject<List<BeatsaverMap>>((string)matchesJson);
                                songs.Add(new SongModel() { SongTitle = songName, SongArtists = artists, SongID = songID, MapMatches = matches });
                            }
                        }
                    }
                    if (SQLite.ExecuteQuery($"select * from playlists where id='{p["id"]}' and platform='spotify';").Rows.Count == 0)
                        SQLite.ExecuteNonQuery($"insert into playlists (id, name, view_order, platform) values ('{p["id"]}', \"{p["name"]}\", '{playlistModels.Count+1}', 'spotify');");
                    else
                        SQLite.ExecuteNonQuery($"update playlists set name=\"{p["name"]}\", view_order='{playlistModels.Count + 1}' where id='{p["id"]}' and platform='spotify';");
                    
                    playlistModels.Add(new PlaylistModel() { PlaylistName = p["name"], Songs = songs });
                }
            }
            else
            {
                var playlistRows = SQLite.ExecuteQuery("select * from playlists where platform='spotify';").Rows;
                PlaylistModel[] playlistArray = new PlaylistModel[playlistRows.Count];
                foreach(DataRow row in playlistRows)
                {
                    playlistArray[Convert.ToInt32(row["view_order"])-1] = new PlaylistModel() { PlaylistName = (string)row["name"], Songs = new List<SongModel>() };
                }

                var songRows = SQLite.ExecuteQuery("select * from songs order by playlist_id;").Rows;
                foreach(DataRow row in songRows)
                {
                    List<BeatsaverMap> matches = new List<BeatsaverMap>();
                    if (row["matches"].GetType() != typeof(DBNull))
                        matches = JsonConvert.DeserializeObject<List<BeatsaverMap>>((string)row["matches"]);
                    SongModel s = new SongModel() { SongTitle = (string)row["title"], SongArtists = ((string)row["artists"]).Split(new string[] { ", " }, StringSplitOptions.None).ToList(), SongID = (string)row["id"], MapMatches = matches };
                    int index = Convert.ToInt32(SQLite.ExecuteQuery($"select view_order from playlists where id='{row["playlist_id"]}'").Rows[0]["view_order"])-1;
                    playlistArray[index].Songs.Add(s);
                }

                playlistModels = playlistArray.ToList();
            }

            return playlistModels;
        }

        private static void HttpListener_Callback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            _token = request.QueryString["code"];
        }

        private static dynamic GetRequest(string url)
        {
            var responseTask = _client.GetAsync(url);
            responseTask.Wait();
            var responseStringTask = responseTask.Result.Content.ReadAsStringAsync();
            responseStringTask.Wait();
            var json = JsonConvert.DeserializeObject<dynamic>(responseStringTask.Result);
            return json;
        }

        private static dynamic PostRequest(string url, Dictionary<string, string> content)
        {
            var encodedContent = new FormUrlEncodedContent(content);
            var responseTask = _client.PostAsync(url, encodedContent);
            responseTask.Wait();
            var responseStringTask = responseTask.Result.Content.ReadAsStringAsync();
            responseStringTask.Wait();
            var json = JsonConvert.DeserializeObject<dynamic>(responseStringTask.Result);
            return json;
        }
    }

    public class SpotifyAccessTokenMessage { }

    public class SpotifySignInMessage { }
}
