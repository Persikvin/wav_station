using Robust.Shared.Prototypes;

namespace Content.Server._White.Vulpification;

/// <summary>
///     Used for the initial infected, who cannot be cured. Gives a
///     "succumb to the vulpification" action.
/// </summary>
[RegisterComponent]
public sealed partial class IncurableVulpificationComponent : Component
{
    [DataField]
    public EntProtoId VulpifySelfActionPrototype = "ActionTurnVulpified";

    [DataField]
    public EntityUid? Action;
}