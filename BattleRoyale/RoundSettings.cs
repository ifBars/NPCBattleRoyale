using MelonLoader;
using Newtonsoft.Json;

namespace NPCBattleRoyale.BattleRoyale
{
    public enum ETournamentMode
    {
        GroupsThenFinals = 0, // current default: each selected group runs an FFA, then winners bracket
        SingleEliminationAll = 1 // seed all selected participants directly into a bracket
    }

    public enum EArenaRotation
    {
        Fixed = 0,      // Use the selected arena for all matches
        Alternate = 1,  // Alternate among arenas in order
        Random = 2      // Random arena per match
    }

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
        public int FinalsParticipants = 0; // 0 => winners only; >1 => top N per group (back-compat)

        // New customization options
        public ETournamentMode TournamentMode = ETournamentMode.GroupsThenFinals;
        public EArenaRotation ArenaRotation = EArenaRotation.Fixed;
        public int AdvancePerGroup = 1; // Number of winners to advance from each group's stage
        public int MaxFFASize = 0;      // 0 = no cap; otherwise chunk FFAs to this size (sequentially)
        public bool HealBetweenRounds = true; // Revive/heal winners between rounds
        public float InterRoundDelaySeconds = 1.0f; // Delay between sequential matches
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
                var list = JsonConvert.DeserializeObject<List<RoundSettings>>(text) ?? CreateDefaultPresets();
                // Back-compat: ensure new fields are initialized
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].AdvancePerGroup = Mathf.Max(1, list[i].AdvancePerGroup);
                    if (list[i].InterRoundDelaySeconds <= 0f) list[i].InterRoundDelaySeconds = 1.0f;
                }
                return list;
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
                    FinalsParticipants = 0,
                    TournamentMode = ETournamentMode.GroupsThenFinals,
                    ArenaRotation = EArenaRotation.Fixed,
                    AdvancePerGroup = 1,
                    MaxFFASize = 0,
                    HealBetweenRounds = true,
                    InterRoundDelaySeconds = 1.0f
                }
            };
        }
    }
}


