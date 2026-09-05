using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared._White.Vulpification;
using Robust.Shared.Prototypes;

namespace Content.Client._White.Vulpification;

public sealed class VulpificationSystem : SharedVulpificationSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulpifiedComponent, GetStatusIconsEvent>(OnGetVulpifiedIcon);
        SubscribeLocalEvent<VulpifyInitialInfectedComponent, GetStatusIconsEvent>(OnGetInitialInfectedIcon);
    }

    private void OnGetVulpifiedIcon(Entity<VulpifiedComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void OnGetInitialInfectedIcon(Entity<VulpifyInitialInfectedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<VulpifiedComponent>(ent))
            return;

        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}