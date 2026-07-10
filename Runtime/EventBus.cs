using System.Collections.Generic;

namespace UnityEventBus
{
    /// <summary>
    /// Static, type-safe event bus. Raising allocates nothing: bindings registered or
    /// deregistered while a raise is in flight are queued and applied when the outermost
    /// raise finishes. A binding deregistered mid-raise is not invoked for that raise;
    /// a binding registered mid-raise is first invoked on the next raise.
    /// Exceptions thrown by a handler propagate to the caller of <see cref="Raise"/>.
    /// Not thread-safe by design — use from the Unity main thread.
    /// </summary>
    public static class EventBus<T> where T : IEvent
    {
        private static readonly List<IEventBinding<T>> _bindings = new List<IEventBinding<T>>();
        private static readonly List<IEventBinding<T>> _pendingAdditions = new List<IEventBinding<T>>();
        private static readonly List<IEventBinding<T>> _pendingRemovals = new List<IEventBinding<T>>();

        private static int _raiseDepth;

        static EventBus()
        {
            EventBusRegistry.Register(typeof(T).Name, Clear);
        }

        public static int BindingCount => _bindings.Count + _pendingAdditions.Count - _pendingRemovals.Count;

        public static void Register(IEventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            if (_raiseDepth > 0)
            {
                _pendingRemovals.Remove(binding);
                if (!_bindings.Contains(binding) && !_pendingAdditions.Contains(binding))
                {
                    _pendingAdditions.Add(binding);
                }
                return;
            }

            if (!_bindings.Contains(binding))
            {
                _bindings.Add(binding);
            }
        }

        public static void Deregister(IEventBinding<T> binding)
        {
            if (binding == null)
            {
                return;
            }

            if (_raiseDepth > 0)
            {
                _pendingAdditions.Remove(binding);
                if (_bindings.Contains(binding) && !_pendingRemovals.Contains(binding))
                {
                    _pendingRemovals.Add(binding);
                }
                return;
            }

            _bindings.Remove(binding);
        }

        public static void Raise(T @event)
        {
            _raiseDepth++;
            try
            {
                for (int i = 0; i < _bindings.Count; i++)
                {
                    IEventBinding<T> binding = _bindings[i];
                    if (_pendingRemovals.Contains(binding))
                    {
                        continue;
                    }

                    binding.OnEvent.Invoke(@event);
                    binding.OnEventNoArgs.Invoke();
                }
            }
            finally
            {
                _raiseDepth--;
                if (_raiseDepth == 0)
                {
                    ApplyPendingChanges();
                }
            }
        }

        /// <summary>Removes every binding and resets in-flight raise state.</summary>
        public static void Clear()
        {
            _bindings.Clear();
            _pendingAdditions.Clear();
            _pendingRemovals.Clear();
            _raiseDepth = 0;
        }

        private static void ApplyPendingChanges()
        {
            for (int i = 0; i < _pendingRemovals.Count; i++)
            {
                _bindings.Remove(_pendingRemovals[i]);
            }
            _pendingRemovals.Clear();

            for (int i = 0; i < _pendingAdditions.Count; i++)
            {
                _bindings.Add(_pendingAdditions[i]);
            }
            _pendingAdditions.Clear();
        }
    }
}
