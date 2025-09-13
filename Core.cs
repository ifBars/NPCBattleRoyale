using MelonLoader;
using NPCBattleRoyale.Integrations;
using NPCBattleRoyale.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(NPCBattleRoyale.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]
[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]

namespace NPCBattleRoyale
{
    public class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            HarmonyPatches.SetModInstance(this);
        }

        public override void OnApplicationQuit()
        {
            Instance = null;
        }

        public override void OnUpdate()
        {
            // Toggle the Battle Royale configuration UI with F8
            if (Input.GetKeyDown(KeyCode.F8))
            {
                try { BattleRoyale.ConfigPanel.Toggle(); }
                catch { }
            }
        }
    }
}