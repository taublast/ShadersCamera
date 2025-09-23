using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadersCamera.Helpers
{
    public class UserSettings
    {
        public UserSettings()
        {
            Formats = new();
            Fill = false;
            Lang = "en";
        }

        public string Lang { get; set; }
        public bool Fill { get; set; }
        public bool Mirror { get; set; }
        public Dictionary<string, int> Formats { get; set; }
        public string Filter { get; set; }
        public bool ShownWelcome { get; set; }
        public int Flash { get; set; }
        public int Source { get; set; }

        private static UserSettings _loaded;

        public static void Save()
        {
            var json = string.Empty;
            try
            {
                json = JsonConvert.SerializeObject(Current);
                Preferences.Default.Set("setts", json);
            }
            catch
            {
                // ignored
            }
        }

        public static UserSettings Current
        {
            get
            {
                if (_loaded == null)
                {
                    try
                    {
                        var json = Preferences.Default.Get("setts", string.Empty);
                        _loaded = JsonConvert.DeserializeObject<UserSettings>(json);
                    }
                    catch
                    {
                        // ignored
                    }

                    if (_loaded == null)
                    {
                        _loaded = new();
                    }
                }
                return _loaded;
            }
            set
            {
                _loaded = value;
                var json = string.Empty;
                try
                {
                    json = JsonConvert.SerializeObject(value);
                    Preferences.Default.Set("setts", json);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
