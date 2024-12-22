using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Warp9
{
    public static class Options
    {
        static Lazy<OptionsInst> inst = new Lazy<OptionsInst>(() => Load() ?? new OptionsInst());


        public static string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "warp9.json");
        public static OptionsInst Instance => inst.Value;

        private static OptionsInst? Load()
        {
            try
            {
                using FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                return JsonSerializer.Deserialize<OptionsInst>(fs);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static void Save()
        {
            string fileTemp = FilePath + ".temp";

            using (FileStream fs = new FileStream(fileTemp, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                JsonSerializer.Serialize(fs, Instance);

            File.Move(fileTemp, FilePath, true);
        }

        public static void Set(OptionsInst oi)
        {
            inst = new Lazy<OptionsInst>(oi);
            Apply(oi);
        }

        private static void Apply(OptionsInst oi)
        {
            Themes.ThemesController.SetTheme((Themes.ThemeType)oi.ThemeIndex);
        }
    }

    public class OptionsInst
    {
        [JsonPropertyName("theme-index")]
        public int ThemeIndex { get; set; } = 0;
    }
}
