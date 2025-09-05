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
        public List<string> NPCIDs = new List<string>();
        // Optional: include any NPC whose ID contains one of these case-insensitive terms
        public List<string> IdContainsAny = new List<string>();
        // Optional: exclude specific NPC IDs
        public List<string> ExcludeNPCIDs = new List<string>();
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
                    return groups ?? new List<GroupDefinition>();
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
            // Ship a few sample groups using ID substring rules. Users can edit JSON to refine.
            return new List<GroupDefinition>
            {
                new GroupDefinition { Name = "Westville", IdContainsAny = new List<string>{ "westville" } },
                new GroupDefinition { Name = "Northtown", IdContainsAny = new List<string>{ "north" , "northtown" } },
                new GroupDefinition { Name = "Warehouse", IdContainsAny = new List<string>{ "warehouse" } },
                new GroupDefinition { Name = "Suppliers", IdContainsAny = new List<string>{ "supplier" } },
                new GroupDefinition { Name = "Dealers", IdContainsAny = new List<string>{ "dealer" } },
            };
        }

        public static bool IsMember(GroupDefinition group, NPC npc)
        {
            if (npc == null) return false;
            var id = npc.ID ?? string.Empty;
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


