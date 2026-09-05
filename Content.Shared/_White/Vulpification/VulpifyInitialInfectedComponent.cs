using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._White.Vulpification;

/// <summary>
///     Added to the initial infected (patient zero) of the vulpification rule.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VulpifyInitialInfectedComponent : Component
{
    /// <summary>
    ///     Faction icon shown to antags when this entity is the initial infected.
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "VulpifyInitialInfectedFaction";
}