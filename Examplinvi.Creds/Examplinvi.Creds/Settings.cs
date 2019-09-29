using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Examplinvi.Creds
{
    public class SettingsConstants
    {
        public const string CONSUMER_KEY = nameof(CONSUMER_KEY);
        public const string CONSUMER_SECRET = nameof(CONSUMER_SECRET);
        public const string ACCESS_TOKEN = nameof(ACCESS_TOKEN);
        public const string ACCESS_TOKEN_SECRET = nameof(ACCESS_TOKEN_SECRET);
    }
    public class Settings
    {
        public static readonly string CONSUMER_KEY = SettingsProvider.Default.GetValue(nameof(CONSUMER_KEY));
        public static readonly string CONSUMER_SECRET = SettingsProvider.Default.GetValue(nameof(CONSUMER_SECRET));
        public static readonly string ACCESS_TOKEN = SettingsProvider.Default.GetValue(nameof(ACCESS_TOKEN));
        public static readonly string ACCESS_TOKEN_SECRET = SettingsProvider.Default.GetValue(nameof(ACCESS_TOKEN_SECRET));
    }
    public interface ISettingsProvider
    {
        string GetValue(string key);
    }
    public class SettingsProvider : ISettingsProvider
    {
        public static SettingsProvider Default = new SettingsProvider(new ApiSecretsKeyValueStore());
        private KeyValueStore keyValueStore;
        public SettingsProvider(KeyValueStore store)
        {
            this.keyValueStore = store;
        }

        public string GetValue(string key) => keyValueStore.GetKey(key);

    }
    public abstract class KeyValueStore
    {
        public abstract string GetKey(string key);
    }
    public class ApiSecretsKeyValueStore : KeyValueStore
    {
        Dictionary<string, string> secrets;
        public ApiSecretsKeyValueStore()
        {
            var settingsFile = GetSettingsFile();

            string settingsJson = string.Empty;
            if (!settingsFile.Exists)
            {
                settingsFile.Directory.Create();
                settingsJson = GetDefaultJson();
                File.WriteAllText(settingsFile.FullName, settingsJson);
                Prompt(settingsFile);

            }
            settingsJson = File.ReadAllText(settingsFile.FullName);

            secrets = JsonConvert.DeserializeObject<Dictionary<string, string>>(settingsJson);
            while (secrets.ToList().Any(x => string.IsNullOrEmpty(x.Value)))
            {
                Prompt(settingsFile);
            }
        }
        private FileInfo GetSettingsFile()
        {
            var current = Process.GetCurrentProcess().MainModule.FileName;
            var dir = new DirectoryInfo(current);
            var root = dir.Root;
            var settingsDirectoy = new DirectoryInfo(Path.Combine(root.FullName, "invisettings"));
            var settingsFile = new FileInfo(Path.Combine(settingsDirectoy.FullName, "settings.json"));
            return settingsFile;
        }

        private void Prompt(FileInfo settingsFile)
        {
            settingsFile.Delete();
            File.WriteAllText(settingsFile.FullName, GetDefaultJson());
            System.Threading.Thread.Sleep(1000);
            Console.WriteLine($"Settings file must be completed at '{settingsFile.FullName}'");
            Console.WriteLine("Program will continue when notepad has been closed.");
            var p = Process.Start("notepad", settingsFile.FullName);
            p.WaitForExit();

        }

        private string GetDefaultJson()
        {
            var d = new Dictionary<string, string>
            {
                { Creds.SettingsConstants.ACCESS_TOKEN, "" },
                { Creds.SettingsConstants.ACCESS_TOKEN_SECRET, "" },
                { Creds.SettingsConstants.CONSUMER_KEY, "" },
                { Creds.SettingsConstants.CONSUMER_SECRET, "" },
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(d, Formatting.Indented);
            return json;
        }

        public override string GetKey(string key) => secrets[key];

    }

}
