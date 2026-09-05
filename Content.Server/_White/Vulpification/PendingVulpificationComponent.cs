using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._White.Vulpification;

/// <summary>
///     An infected individual that will turn into a vulpkanin after a fixed delay.
///     The wait ends with a short "conversion finale": the victim is stunned (and
///     shakes), smoke billows out on and around their tile, then they transform.
///     Initial infected are converted instantly instead.
/// </summary>
[RegisterComponent]
public sealed partial class PendingVulpificationComponent : Component
{
    /// <summary>
    ///     How long after infection the conversion finale begins.
    /// </summary>
    [DataField("transformDelay")]
    public TimeSpan TransformDelay = TimeSpan.FromMinutes(7);

    /// <summary>
    ///     Duration of the conversion finale (shaking, smoke, no movement).
    /// </summary>
    [DataField("conversionTime")]
    public TimeSpan ConversionTime = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The moment the conversion finale starts.
    /// </summary>
    [DataField("transformAt", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TransformAt;

    /// <summary>
    ///     The moment the victim actually becomes a vulpkanin.
    /// </summary>
    [DataField("convertAt", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ConvertAt;

    /// <summary>
    ///     Whether the conversion finale has already started.
    /// </summary>
    [DataField("conversionStarted")]
    public bool ConversionStarted;

    /// <summary>
    ///     The chance each second that a warning will be shown during the wait.
    /// </summary>
    [DataField("infectionWarningChance")]
    public float InfectionWarningChance = 0.0166f;

    /// <summary>
    ///     Infection warnings shown as popups.
    /// </summary>
    [DataField("infectionWarnings")]
    public List<string> InfectionWarnings = new()
    {
        "vulpification-infection-warning",
        "vulpification-infection-underway"
    };

    /// <summary>
    ///     Throttles how often the warnings are rolled.
    /// </summary>
    [DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;
}