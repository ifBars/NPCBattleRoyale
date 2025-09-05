#if MONO
using ScheduleOne.NPCs;
#else
using Il2CppScheduleOne.NPCs;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NPC), nameof(NPC.Start))]
        private static void NPC_Start_Postfix_Mono(NPC __instance)
        {
            EnsureManager();
            BattleRoyaleManager.Instance?.OnNPCStart(__instance);
        }

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
