using Content.Server.Chat.Managers;
using Content.Server.Damage.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Mind.Commands;
using Content.Shared.Clumsy;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Weapons.Melee;
using Content.Shared._White.Vulpification;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;

namespace Content.Server._White.Vulpification;

/// <summary>
///     Handles the actual transformation of an entity into a vulpkanin.
/// </summary>
public sealed partial class VulpificationSystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    /// <summary>
    ///     The species that vulpified humanoids are transformed into.
    /// </summary>
    public const string TargetSpecies = "Vulpkanin";

    /// <summary>
    ///     The body type of the target species.
    /// </summary>
    public const string TargetBodyType = "VulpkaninNormal";

    /// <summary>
    ///     The general-purpose function to turn an entity into a vulpkanin.
    ///     Handles humanoid species conversion and the "vulpified" traits.
    /// </summary>
    public void VulpifyEntity(EntityUid target, MobStateComponent? mobState = null)
    {
        // Don't re-vulpify the already foxy.
        if (HasComp<VulpifiedComponent>(target) || HasComp<VulpificationImmuneComponent>(target))
            return;

        if (!Resolve(target, ref mobState, logMissing: false))
            return;

        var vulpified = AddComp<VulpifiedComponent>(target);

        // Vulps are as clumsy as clowns: give them the Clumsy component.
        AddComp<ClumsyComponent>(target);

        // No longer waiting to become a vulpkanin.
        // Requires deferral because this may be the event that called VulpifyEntity in the first place.
        RemCompDeferred<PendingVulpificationComponent>(target);

        // Actual species conversion for humanoids.
        if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp))
        {
            _humanoidAppearance.SetSpecies(target, TargetSpecies, sync: false, humanoid: huApComp);
            _humanoidAppearance.SetBodyType(target, TargetBodyType, sync: false, humanoid: huApComp);
            _humanoidAppearance.SetSkinColor(target, Color.FromHex("#FF00FF"), sync: false, verify: false, humanoid: huApComp);
            huApComp.EyeColor = Color.FromHex("#F5A623");

            // WD EDIT: apply the species' default markings (the fluffy vulpkanin tail / ears)
            // so freshly-turned vulps aren't left tailless. Marking colors come from the skin.
            huApComp.MarkingSet.EnsureDefault(huApComp.SkinColor, huApComp.EyeColor);

            Dirty(target, huApComp);
        }

        // WD EDIT: vulps fight with their fangs instead of their fists.
        // The humanoid species-base melee ("fist") component is turned into a bite attack.
        if (TryComp<MeleeWeaponComponent>(target, out var melee))
        {
            melee.Damage = new DamageSpecifier { DamageDict = { ["Slash"] = FixedPoint2.New(8), ["Piercing"] = FixedPoint2.New(2) } };
            melee.Animation = "WeaponArcBite";
            melee.WideAnimation = "WeaponArcBite";
            melee.Angle = Angle.Zero;
            melee.SoundHit = new SoundPathSpecifier("/Audio/Effects/bite.ogg");
            Dirty(target, melee);
        }

        // Heals the entity from all the damage it took while human.
        if (TryComp<DamageableComponent>(target, out var damageablecomp))
            _damageable.SetAllDamage(target, damageablecomp, 0);
        _mobState.ChangeMobState(target, MobState.Alive);

        // Gives it the funny "Vulpikanin ___"-style name.
        _nameMod.RefreshNameModifiers(target);

        // popup
        _popup.PopupEntity(Loc.GetString("vulpification-transform", ("target", target)), target, PopupType.LargeCaution);

        // Make non-player mobs sentient.
        MakeSentientCommand.MakeSentient(target, EntityManager);

        // He's gotta have a mind
        var hasMind = _mind.TryGetMind(target, out var mindId, out _);
        if (hasMind && _mind.TryGetSession(mindId, out var session))
        {
            // Vulpified role for player manifest.
            _roles.MindAddRole(mindId, "MindRoleVulpified", mind: null, silent: true);

            // Greeting message for new bebe vulpkanins.
            _chatMan.DispatchServerMessage(session, Loc.GetString("vulpification-role-greeting"));

            // Notify the player about their new role assignment.
            _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg"), session);
        }
        else
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(target);
            EnsureComp<GhostTakeoverAvailableComponent>(target);
            ghostRole.RoleName = Loc.GetString("vulpification-transformed-entity");
            ghostRole.RoleDescription = Loc.GetString("vulpification-role-desc");
            ghostRole.RoleRules = Loc.GetString("vulpification-role-rules");
        }

        // Vulpification game mode tracking.
        var ev = new EntityVulpifiedEvent(target);
        RaiseLocalEvent(target, ref ev, true);
    }
}
