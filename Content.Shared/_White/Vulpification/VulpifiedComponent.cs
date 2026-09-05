using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._White.Vulpification;

/// <summary>
///     Marks an entity as vulpified: transformed into a vulpkanin.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VulpifiedComponent : Component
{
    /// <summary>
    ///     Chance to spread the infection on a melee hit.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float InfectionChance = 0.5f;

    /// <summary>
    ///     Movement speed multiplier applied to vulpified entities.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedMultiplier = 1.0f;

    /// <summary>
    ///     The species the entity was transformed into.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SpeciesPrototype>))]
    public string SpeciesId = "Vulpkanin";

    /// <summary>
    ///     Faction icon shown for vulpified entities.
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "VulpifiedFaction";
}