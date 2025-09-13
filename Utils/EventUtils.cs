using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace NPCBattleRoyale.Utils
{
    /// <summary>
    /// Cross-branch (Mono / Il2Cpp) safe helpers for UnityEvent subscription.
    /// Mirrors the approach used in S1API to avoid delegate mismatches on Il2Cpp.
    /// </summary>
    internal static class EventUtils
    {
        // Track non-generic event subscriptions so we can safely remove them later.
        private static readonly Dictionary<Action, Delegate> NonGenericMap = new Dictionary<Action, Delegate>();

        // Track generic event subscriptions so we can safely remove them later.
        private static readonly Dictionary<Delegate, Delegate> GenericMap = new Dictionary<Delegate, Delegate>();

        public static void AddListener(Action listener, UnityEvent unityEvent)
        {
            if (listener == null || unityEvent == null) return;
            if (NonGenericMap.ContainsKey(listener)) return;

#if IL2CPP
            // On Il2Cpp, UnityEvent.AddListener has an implicit cast from System.Action
            System.Action wrapped = new System.Action(listener);
            unityEvent.AddListener(wrapped);
#else
            UnityAction wrapped = new UnityAction(listener);
            unityEvent.AddListener(wrapped);
#endif
            NonGenericMap[listener] = wrapped;
        }

        public static void RemoveListener(Action listener, UnityEvent unityEvent)
        {
            if (listener == null || unityEvent == null) return;
            if (!NonGenericMap.TryGetValue(listener, out var wrapped)) return;

#if IL2CPP
            if (wrapped is System.Action sa) unityEvent.RemoveListener(sa);
#else
            if (wrapped is UnityAction ua) unityEvent.RemoveListener(ua);
#endif
            NonGenericMap.Remove(listener);
        }

        public static void AddListener<T>(Action<T> listener, UnityEvent<T> unityEvent)
        {
            if (listener == null || unityEvent == null) return;
            if (GenericMap.ContainsKey(listener)) return;

#if IL2CPP
            System.Action<T> wrapped = new System.Action<T>(listener);
            unityEvent.AddListener(wrapped);
#else
            UnityAction<T> wrapped = new UnityAction<T>(listener);
            unityEvent.AddListener(wrapped);
#endif
            GenericMap[listener] = wrapped;
        }

        public static void RemoveListener<T>(Action<T> listener, UnityEvent<T> unityEvent)
        {
            if (listener == null || unityEvent == null) return;
            if (!GenericMap.TryGetValue(listener, out var wrapped)) return;

#if IL2CPP
            if (wrapped is System.Action<T> sa) unityEvent.RemoveListener(sa);
#else
            if (wrapped is UnityAction<T> ua) unityEvent.RemoveListener(ua);
#endif
            GenericMap.Remove(listener);
        }
    }
}


