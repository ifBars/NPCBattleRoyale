using HarmonyLib;
using NPCBattleRoyale.BattleRoyale;
using FishNet.Connection;
#if MONO
using ScheduleOne.NPCs;
using BehaviourType = ScheduleOne.NPCs.Behaviour.Behaviour;
#else
using Il2CppScheduleOne.NPCs;
using BehaviourType = Il2CppScheduleOne.NPCs.Behaviour.Behaviour;
#endif

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

        /// <summary>
        /// Guard against redundant re-enables which can cause visible stutter.
        /// If the behaviour is already enabled, skip the original Enable_Networked call.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BehaviourType), nameof(BehaviourType.Enable_Networked))]
        private static bool Behaviour_EnableNetworked_Prefix(BehaviourType __instance, NetworkConnection conn)
        {
            try
            {
                if (__instance.Enabled)
                {
                    return false; // Skip original; already enabled
                }
            }
            catch { }
            return true; // Run original
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
