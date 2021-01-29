using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace DatabaseBackup
{
    public static class AppSettings
    {
        private static JToken _appSettingsJSON;

        public static void InitializeAppSettings()
        {
            var JSON = System.IO.File.ReadAllText("appsettings.json");
            _appSettingsJSON = JObject.Parse(JSON);
        }

        public static T GetConfiguration<T>(string ConfigurationKey)
        {
            string value = (string)_appSettingsJSON.SelectToken(string.Format("Configuration.{0}", ConfigurationKey));
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
