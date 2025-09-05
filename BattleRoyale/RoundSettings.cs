using MelonLoader;
using Newtonsoft.Json;

namespace NPCBattleRoyale.BattleRoyale
{
    [Serializable]
    public class RoundSettings
    {
        public string PresetName = "Default";
        public int ArenaIndex = 0;
        public List<string> SelectedGroups = new List<string>();
        public int ParticipantsPerGroup = 6;
        public bool ShuffleParticipants = true;
        public bool KnockoutCountsAsElim = true;
        public float MatchTimeoutSeconds = 180f;
        public int FinalsParticipants = 0; // 0 => winners only; >1 => top N per group
    }

    public static class PresetConfig
    {
        public const string PresetsFileName = "BattleRoyalePresets.json";
        public static string ConfigDirectory => GroupConfig.ConfigDirectory;
        public static string PresetsFilePath => Path.Combine(ConfigDirectory, PresetsFileName);

        public static List<RoundSettings> LoadOrCreateDefaults()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
                if (!File.Exists(PresetsFilePath))
                {
                    var defaults = CreateDefaultPresets();
                    var json = JsonConvert.SerializeObject(defaults, Formatting.Indented);
                    File.WriteAllText(PresetsFilePath, json);
                    return defaults;
                }
                var text = File.ReadAllText(PresetsFilePath);
                var list = JsonConvert.DeserializeObject<List<RoundSettings>>(text);
                return list ?? CreateDefaultPresets();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] Failed to load presets: {ex}");
                return CreateDefaultPresets();
            }
        }

        public static void SavePresets(List<RoundSettings> presets)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
                var json = JsonConvert.SerializeObject(presets, Formatting.Indented);
                File.WriteAllText(PresetsFilePath, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] Failed to save presets: {ex}");
            }
        }

        private static List<RoundSettings> CreateDefaultPresets()
        {
            return new List<RoundSettings>
            {
                new RoundSettings
                {
                    PresetName = "Default",
                    ArenaIndex = 1,
                    SelectedGroups = new List<string>{ "Westville", "Northtown" },
                    ParticipantsPerGroup = 6,
                    ShuffleParticipants = true,
                    KnockoutCountsAsElim = true,
                    MatchTimeoutSeconds = 180f,
                    FinalsParticipants = 0
                }
            };
        }
    }
}


