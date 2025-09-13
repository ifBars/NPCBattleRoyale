#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppSystem.Collections.Generic;
#endif
using System.Collections.Generic;

namespace NPCBattleRoyale.Utils
{
    /// <summary>
    /// Cross-branch helpers to bridge between System.Collections.Generic.List<T>
    /// and Il2CppSystem.Collections.Generic.List<T>, inspired by S1MelonModTemplate Utils
    /// (add-range, conversions) [reference](https://github.com/k073l/S1MelonModTemplate/blob/master/Helpers/Utils.cs).
    /// </summary>
    internal static class Il2CppListExtensions
    {
#if (IL2CPPMELON || IL2CPPBEPINEX)
        public static List<T> ToSystemList<T>(this Il2CppSystem.Collections.Generic.List<T> il2cppList)
        {
            var list = new List<T>(il2cppList.Count);
            for (int i = 0; i < il2cppList.Count; i++)
                list.Add(il2cppList[i]);
            return list;
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var item in source)
                list.Add(item);
            return list;
        }

        public static void AddRange<T>(this Il2CppSystem.Collections.Generic.List<T> target, IEnumerable<T> source)
        {
            foreach (var item in source)
                target.Add(item);
        }
#else
        public static List<T> ToSystemList<T>(this List<T> list) => list;

        public static List<T> ToIl2CppList<T>(this IEnumerable<T> source)
        {
            return new List<T>(source);
        }

        public static void AddRange<T>(this List<T> target, IEnumerable<T> source)
        {
            target.AddRange(source);
        }
#endif
    }
}


