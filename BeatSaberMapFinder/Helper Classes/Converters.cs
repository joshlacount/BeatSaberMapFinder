using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BeatSaberMapFinder
{
    [ValueConversion(typeof(List<string>), typeof(string))]
    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("The target must be a string");

            return string.Join(", ", ((List<string>)value).ToArray());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(List<string>))
                throw new InvalidOperationException("The target must be a List<string>");

            return ((string)value).Split(new string[] { ", " }, StringSplitOptions.None).ToList();
        }
    }
    /*
    public class BeatsaverMapConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(BeatsaverMap));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            BeatsaverMap map = new BeatsaverMap();

            Dictionary<string, bool> difficulties = new Dictionary<string, bool>();
            foreach (var kvp in (JObject)obj["metadata"]["difficulties"])
            {
                difficulties.Add(kvp.Key, (bool)kvp.Value);
            }

            Dictionary<string, Difficulty> characteristics = new Dictionary<string, Difficulty>(); // TODO: add Difficulty converter
            foreach (var d in (JObject)obj["metadata"]["characteristics"][0]["difficulties"])
            {
                Console.WriteLine(obj["_id"]);
                if (difficulties[d.Key])
                {
                    try
                    {
                        characteristics.Add(d.Key, new Difficulty()
                        {
                            Duration = Convert.ToInt32(d.Value["duration"]),
                            Length = Convert.ToInt32(d.Value["length"]),
                            Bombs = Convert.ToInt32(d.Value["bombs"]),
                            Notes = Convert.ToInt32(d.Value["notes"]),
                            Obstacles = Convert.ToInt32(d.Value["obstacles"])
                        });
                    }
                    catch
                    {
                        difficulties[d.Key] = false;
                    }
                }
            }

            map.Difficulties = difficulties;
            map.Characteristics = characteristics;
            map.LevelAuthorName = (string)obj["metadata"]["levelAuthorName"];
            map.SongAuthorName = (string)obj["metadata"]["songAuthorName"];
            map.SongName = (string)obj["metadata"]["songName"];
            map.SongSubName = (string)obj["metadata"]["songSubName"];
            map.Bpm = Convert.ToInt32(obj["metadata"]["bpm"]);
            map.Downloads = Convert.ToInt32(obj["stats"]["downloads"]);
            map.Plays = Convert.ToInt32(obj["stats"]["plays"]);
            map.Downvotes = Convert.ToInt32(obj["stats"]["downVotes"]);
            map.Upvotes = Convert.ToInt32(obj["stats"]["upVotes"]);
            map.Heat = Convert.ToDouble(obj["stats"]["heat"]);
            map.Rating = Convert.ToDouble(obj["stats"]["rating"]);
            map.Description = (string)obj["description"];
            map.Id = (string)obj["_id"];
            map.Key = (string)obj["key"];
            map.Name = (string)obj["name"];
            map.Uploader = new Uploader() { Id = (string)obj["uploader"]["_id"], Username = (string)obj["uploader"]["username"] };
            map.Hash = (string)obj["hash"];
            map.Uploaded = DateTime.Parse((string)obj["uploaded"]);
            map.DirectDownload = (string)obj["directDownload"];
            map.DownloadUrl = (string)obj["downloadURL"];
            map.CoverUrl = (string)obj["coverURL"];

            return map;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }*/
}
