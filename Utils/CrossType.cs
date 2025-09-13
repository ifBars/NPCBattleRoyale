#if (MONO)
using System;
#elif (IL2CPP)
using Il2CppSystem;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Object = Il2CppSystem.Object;
using Type = Il2CppSystem.Type;
#endif

namespace NPCBattleRoyale.Utils
{
    /// <summary>
    /// Cross Type helpers inspired by S1API.Internal.Utils.CrossType,
    /// kept internal to this mod to avoid external dependency.
    /// </summary>
    internal static class CrossType
    {
        internal static Type Of<T>()
        {
#if MONO
            return typeof(T);
#elif IL2CPP
            return Il2CppType.Of<T>();
#endif
        }

        internal static bool Is<T>(object obj, out T result)
#if IL2CPP
            where T : Il2CppObjectBase
#elif MONO
            where T : class
#endif
        {
#if IL2CPP
            if (obj is Object il2cppObj)
            {
                var t = Il2CppType.Of<T>();
                if (t.IsAssignableFrom(il2cppObj.GetIl2CppType()))
                {
                    result = il2cppObj.TryCast<T>();
                    return true;
                }
            }
#else
            if (obj is T cast)
            {
                result = cast;
                return true;
            }
#endif
            result = null!;
            return false;
        }

        internal static T As<T>(object obj)
#if IL2CPP
            where T : Il2CppObjectBase
#elif MONO
            where T : class
#endif
        {
#if IL2CPP
            return obj is Object il2cppObj ? il2cppObj.Cast<T>() : null!;
#else
            return (T)obj;
#endif
        }
    }
}


