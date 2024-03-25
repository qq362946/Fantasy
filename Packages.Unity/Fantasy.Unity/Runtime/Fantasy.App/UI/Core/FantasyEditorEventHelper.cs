#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Fantasy
{
    public static class FantasyEditorEventHelper
    {
        private static readonly Dictionary<int, Action> Actions = new();

        public static void AddListener(int eventId, Action trigger)
        {
            Actions.TryAdd(eventId, default);
            Actions[eventId] += trigger;
        }

        public static void RemoveListener(int eventId, Action trigger)
        {
            if (!Actions.ContainsKey(eventId))
                return;
            Actions[eventId] -= trigger;
        }

        public static void RemoveAllListener(int eventId)
        {
            if (Actions.ContainsKey(eventId))
                Actions.Remove(eventId);
        }

        public static void ClearAll()
        {
            Actions.Clear();
        }

        public static void Trigger(int eventId)
        {
            if (Actions.TryGetValue(eventId, out var action))
                action?.Invoke();
        }
    }
}
#endif