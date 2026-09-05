using Content.Server._White.GameTicking.Rules;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._White.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(VulpificationRuleSystem))]
public sealed partial class VulpificationRuleComponent : Component
{
    /// <summary>
    ///     When the round will next check for round end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextRoundEndCheck;

    /// <summary>
    ///     The amount of time between each check for the end of the round.
    /// </summary>
    [DataField]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     After this amount of the crew become vulpified, the shuttle will be automatically called.
    /// </summary>
    [DataField]
    public float VulpifiedShuttleCallPercentage = 0.7f;
}