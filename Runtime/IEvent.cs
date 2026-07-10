namespace UnityEventBus
{
    /// <summary>
    /// Marker interface for every event raised through <see cref="EventBus{T}"/>.
    /// Prefer readonly structs for event types to avoid boxing and accidental mutation.
    /// </summary>
    public interface IEvent
    {
    }
}
