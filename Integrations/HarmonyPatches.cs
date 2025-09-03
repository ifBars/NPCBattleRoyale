#if MONO
using ScheduleOne;
#else
using Il2CppScheduleOne;
#endif
using HarmonyLib;
using NPCBattleRoyale.BattleRoyale;

namespace NPCBattleRoyale.Integrations
{
    [HarmonyPatch]
    public static class HarmonyPatches
    {
        private static Core? _modInstance;

        /// <summary>
        /// Set the mod instance for patch callbacks
        /// </summary>
        public static void SetModInstance(Core modInstance)
        {
            _modInstance = modInstance;
            EnsureManager();
        }

#if MONO
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScheduleOne.NPCs.NPC), nameof(ScheduleOne.NPCs.NPC.Start))]
        private static void NPC_Start_Postfix_Mono(ScheduleOne.NPCs.NPC __instance)
        {
            EnsureManager();
            BattleRoyaleManager.Instance?.OnNPCStart(__instance);
        }
#else
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Il2CppScheduleOne.NPCs.NPC), nameof(Il2CppScheduleOne.NPCs.NPC.Start))]
        private static void NPC_Start_Postfix_Il2cpp(Il2CppScheduleOne.NPCs.NPC __instance)
        {
            EnsureManager();
            BattleRoyaleManager.Instance?.OnNPCStart(__instance);
        }
#endif

        private static void EnsureManager()
        {
            if (BattleRoyaleManager.Instance == null)
            {
                var go = new UnityEngine.GameObject("NPCBattleRoyale_Manager");
                go.hideFlags = UnityEngine.HideFlags.DontUnloadUnusedAsset;
                go.AddComponent<BattleRoyaleManager>();
            }
        }
    }
}
