using Content.Shared.Actions;

namespace Content.Shared._White.Vulpification;

/// <summary>
///     Broadcast whenever an entity is vulpified.
///     Used by the vulpification game rule to track infections and by future UIs.
/// </summary>
[ByRefEvent]
public readonly struct EntityVulpifiedEvent
{
    /// <summary>
    ///     The entity that was vulpified.
    /// </summary>
    public readonly EntityUid Target;

    public EntityVulpifiedEvent(EntityUid target)
    {
        Target = target;
    }
}

/// <summary>
///     Raised when a player intentionally triggers their own vulpification ("turn" action).
/// </summary>
public sealed partial class VulpifySelfActionEvent : InstantActionEvent { }