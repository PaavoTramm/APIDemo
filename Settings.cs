using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiDemo
{
    internal class Settings
    {
        #region Singleton
        public static Settings Instance
        {
            get { return Nested.instance; }
        }

        public static Data Current
        {
            get { return Nested.instance.Loaded; }
        }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {

            }

            internal static readonly Settings instance = new Settings();
        }
        #endregion

        Settings()
        {
            Load();
        }

        public void Load()
        {
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApiDemo.js");
            if (File.Exists(file))
            {
                Data? data = JsonSerializer.Deserialize<Data>(System.IO.File.ReadAllText(file));
                if (null != data)
                    Loaded = data;
            }
        }

        public void Save()
        {
            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApiDemo.js");
            File.WriteAllText(file, JsonSerializer.Serialize(Loaded));
        }

        public class Data
        {
            public string UserName { get; set; } = "";
            public string Password { get; set; } = "";
            public string ShapeFile { get; set; } = "";
            public string ScriptFile { get; set; } = "";
            public string DataFile { get; set; } = "";
            public string ResultFile { get; set; } = "";
        }

        Data Loaded { get; set; } = new Data();
    }
}
