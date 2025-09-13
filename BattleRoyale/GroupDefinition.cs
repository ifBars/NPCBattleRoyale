using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

#if MONO
using ScheduleOne.NPCs;
#else
using Il2CppScheduleOne.NPCs;
#endif

namespace NPCBattleRoyale.BattleRoyale
{
    /// <summary>
    /// Declarative definition of a participant group for tournaments.
    /// Currently supports explicit NPC ID lists and simple ID substring filters.
    /// </summary>
    [Serializable]
    public class GroupDefinition
    {
        public string Name = "Group";
        // Explicit list of NPC IDs in this group
        public List<string> NPCIDs = new();
        // Optional: Regions by name (e.g., "Westville", "Northtown")
        // Preferred way to group NPCs; matches NPC.Region.ToString()
        public List<string> Regions = new();
        // Optional: include any NPC whose ID contains one of these case-insensitive terms
        public List<string> IdContainsAny = new();
        // Optional: exclude specific NPC IDs
        public List<string> ExcludeNPCIDs = new();
    }

    /// <summary>
    /// Loads and resolves groups from JSON configs.
    /// </summary>
    public static class GroupConfig
    {
        public const string GroupsFileName = "BattleRoyaleGroups.json";
        public static string ConfigDirectory
        {
            get
            {
                // Place alongside the mod directory by default (within NPCBattleRoyale/Config)
                var baseDir = Path.Combine(MelonEnvironment.UserDataDirectory, "NPCBattleRoyale", "Config");
                return baseDir;
            }
        }

        public static string GroupsFilePath => Path.Combine(ConfigDirectory, GroupsFileName);

        public static List<GroupDefinition> LoadOrCreateDefaults()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory)) Directory.CreateDirectory(ConfigDirectory);
                if (!File.Exists(GroupsFilePath))
                {
                    var defaults = CreateDefaultGroups();
                    var json = JsonConvert.SerializeObject(defaults, Formatting.Indented);
                    File.WriteAllText(GroupsFilePath, json);
                    return defaults;
                }
                else
                {
                    var json = File.ReadAllText(GroupsFilePath);
                    var groups = JsonConvert.DeserializeObject<List<GroupDefinition>>(json);
                    groups = groups ?? new List<GroupDefinition>();
                    // Back-compat: ensure Regions list exists
                    for (int i = 0; i < groups.Count; i++)
                    {
                        if (groups[i].Regions == null) groups[i].Regions = new List<string>();
                        if (groups[i].NPCIDs == null) groups[i].NPCIDs = new List<string>();
                        if (groups[i].IdContainsAny == null) groups[i].IdContainsAny = new List<string>();
                        if (groups[i].ExcludeNPCIDs == null) groups[i].ExcludeNPCIDs = new List<string>();
                    }
                    return groups;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] Failed to load group config: {ex}");
                return CreateDefaultGroups();
            }
        }

        private static List<GroupDefinition> CreateDefaultGroups()
        {
            // Ship a few sample groups using Region names for accurate selection.
            return new List<GroupDefinition>
            {
                new() { Name = "Westville", Regions = new List<string>{ "Westville" } },
                new() { Name = "Northtown", Regions = new List<string>{ "Northtown" } },
                new() { Name = "Downtown", Regions = new List<string>{ "Downtown" } },
                new() { Name = "Docks", Regions = new List<string>{ "Docks" } },
                new() { Name = "Suburbia", Regions = new List<string>{ "Suburbia" } },
                new() { Name = "Uptown", Regions = new List<string>{ "Uptown" } },
            };
        }

        public static bool IsMember(GroupDefinition group, NPC npc)
        {
            if (npc == null) return false;
            var id = npc.ID ?? string.Empty;
            var regionName = npc.Region.ToString();
            // Explicit ID include wins
            for (int i = 0; i < group.NPCIDs.Count; i++)
            {
                if (string.Equals(group.NPCIDs[i], id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Explicit ID exclude
            for (int i = 0; i < group.ExcludeNPCIDs.Count; i++)
            {
                if (string.Equals(group.ExcludeNPCIDs[i], id, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            // Region name match (preferred)
            for (int i = 0; i < group.Regions.Count; i++)
            {
                string r = group.Regions[i];
                if (!string.IsNullOrWhiteSpace(r) && string.Equals(r, regionName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Back-compat: if no Regions defined, match by group name against region
            if (group.Regions == null || group.Regions.Count == 0)
            {
                if (string.Equals(group.Name, regionName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            // Substring match
            for (int i = 0; i < group.IdContainsAny.Count; i++)
            {
                string term = group.IdContainsAny[i];
                if (!string.IsNullOrWhiteSpace(term) && id.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}


