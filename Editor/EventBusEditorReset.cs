using UnityEditor;

namespace UnityEventBus.Editor
{
    /// <summary>
    /// Clears every touched bus when leaving play mode so stale listeners never leak into
    /// the next session — required when Domain Reload is disabled in Enter Play Mode Options,
    /// where static state survives between play sessions.
    /// </summary>
    [InitializeOnLoad]
    public static class EventBusEditorReset
    {
        static EventBusEditorReset()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EventBusRegistry.ClearAll();
            }
        }
    }
}
