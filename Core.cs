using MelonLoader;
using NPCBattleRoyale.Integrations;
using NPCBattleRoyale.Utils;

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
    }
}