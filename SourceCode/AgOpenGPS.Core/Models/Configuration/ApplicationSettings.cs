using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace AgOpenGPS.Core.Models
{
    public class ApplicationSettings
    {
        private const string SettingsFileName = "settings.json";

        public bool IsMetric { get; set; } = true;
        public bool IsDay { get; set; } = true;

        public static ApplicationSettings Load(DirectoryInfo baseDirectory)
        {
            try
            {
                var file = new FileInfo(Path.Combine(baseDirectory.FullName, SettingsFileName));
                if (file.Exists)
                {
                    using FileStream stream = file.OpenRead();
                    var settings = JsonSerializer.Deserialize<ApplicationSettings>(stream);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore and fall back to defaults
            }

            return new ApplicationSettings
            {
                IsMetric = CultureInfo.CurrentCulture.Name switch
                {
                    string culture when culture.StartsWith("en-US", StringComparison.OrdinalIgnoreCase) => false,
                    string culture when culture.StartsWith("en-GB", StringComparison.OrdinalIgnoreCase) => true,
                    _ => true
                },
                IsDay = true
            };
        }

        public void Save(DirectoryInfo baseDirectory)
        {
            var file = new FileInfo(Path.Combine(baseDirectory.FullName, SettingsFileName));
            file.Directory?.Create();

            using FileStream stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
