using System;
using System.Collections.Generic;

namespace UnityEventBus
{
    /// <summary>
    /// Tracks every <see cref="EventBus{T}"/> that has actually been used. Buses self-register
    /// from their static constructor on first touch, so clearing all buses needs no reflection,
    /// no assembly scanning, and works with any assembly definition layout.
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> _clearActions = new List<Action>();
        private static readonly List<string> _busNames = new List<string>();

        /// <summary>Names of the event types whose buses have been touched, in first-use order.</summary>
        public static IReadOnlyList<string> ActiveBusNames => _busNames;

        internal static void Register(string busName, Action clearAction)
        {
            _busNames.Add(busName);
            _clearActions.Add(clearAction);
        }

        /// <summary>Removes every binding from every bus that has been used so far.</summary>
        public static void ClearAll()
        {
            for (int i = 0; i < _clearActions.Count; i++)
            {
                _clearActions[i].Invoke();
            }
        }
    }
}
