#if MONO
using Newtonsoft.Json;
using Object = System.Object;
#else
using Il2CppNewtonsoft.Json;
using Object = Il2CppSystem.Object;
#endif

namespace NPCBattleRoyale.Utils
{
    /// <summary>
    /// Thin wrapper to shield call sites from Newtonsoft vs Il2CppNewtonsoft differences.
    /// </summary>
    internal static class JsonUtils
    {
        public static string Serialize(object obj, bool indented = true)
        {
            var formatting = indented ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(obj as Object, formatting);
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}


