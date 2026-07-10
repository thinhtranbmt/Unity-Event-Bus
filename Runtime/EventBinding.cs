using System;

namespace UnityEventBus
{
    /// <summary>
    /// Default <see cref="IEventBinding{T}"/> implementation. Owns the handler delegates;
    /// callers mutate them only through <see cref="Add(Action{T})"/> / <see cref="Remove(Action{T})"/>.
    /// </summary>
    public sealed class EventBinding<T> : IEventBinding<T> where T : IEvent
    {
        private static readonly Action<T> EmptyTyped = static _ => { };
        private static readonly Action EmptyNoArgs = static () => { };

        private Action<T> _onEvent = EmptyTyped;
        private Action _onEventNoArgs = EmptyNoArgs;

        public EventBinding(Action<T> onEvent)
        {
            _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));
        }

        public EventBinding(Action onEventNoArgs)
        {
            _onEventNoArgs = onEventNoArgs ?? throw new ArgumentNullException(nameof(onEventNoArgs));
        }

        Action<T> IEventBinding<T>.OnEvent => _onEvent;

        Action IEventBinding<T>.OnEventNoArgs => _onEventNoArgs;

        public void Add(Action<T> handler) => _onEvent += handler;

        public void Remove(Action<T> handler) => _onEvent -= handler;

        public void Add(Action handler) => _onEventNoArgs += handler;

        public void Remove(Action handler) => _onEventNoArgs -= handler;
    }
}
