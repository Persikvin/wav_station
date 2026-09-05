using Content.Shared._White.Vulpification;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._White.Vulpification;

/// <summary>
///     Manages the pink vision tint for vulpified players. // WD EDIT
///     NOTE: intentionally NOT gated on CCVars.NoVisionFilters — unlike other vision
///     systems (DogVision, UltraVision, Shadowkin...), which are disabled by default
///     because that CVar defaults to true. Vulps should see pink straight away.
/// </summary>
public sealed class VulpificationVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    private VulpificationVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulpifiedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VulpifiedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VulpifiedComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<VulpifiedComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnInit(EntityUid uid, VulpifiedComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, VulpifiedComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, VulpifiedComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, VulpifiedComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}