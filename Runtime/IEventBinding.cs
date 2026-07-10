using System;

namespace UnityEventBus
{
    /// <summary>
    /// A subscription handle for <see cref="EventBus{T}"/>. Exposes the composed handlers
    /// read-only so no consumer can wipe another consumer's listeners through the interface.
    /// </summary>
    public interface IEventBinding<T> where T : IEvent
    {
        Action<T> OnEvent { get; }

        Action OnEventNoArgs { get; }
    }
}
