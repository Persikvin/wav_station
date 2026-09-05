using System.Linq;
using Content.Server.Administration;
using Content.Server.Antag;
using Content.Server._White.GameTicking.Rules.Components;
using Content.Server._White.Vulpification;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._White.Vulpification;

/// <summary>
///     "makevulpify <username>" — turns the target player into a vulpkanin.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class MakeVulpifiedCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Command => "makevulpify";
    public string Description => Loc.GetString("make-vulpified-command-description");
    public string Help => "makevulpify <username>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (session.AttachedEntity is not { } target)
        {
            shell.WriteError(Loc.GetString("shell-target-entity-does-not-exist"));
            return;
        }

        var system = _entities.System<VulpificationSystem>();
        system.VulpifyEntity(target);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(names, Loc.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }
}

/// <summary>
///     "makevulpifyinitialinfected <username>" — makes the target player the patient zero of vulpification.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class MakeVulpifyInitialInfectedCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultVulpificationRule = "Vulpification";

    public string Command => "makevulpifyinitialinfected";
    public string Description => Loc.GetString("make-vulpify-initial-infected-command-description");
    public string Help => "makevulpifyinitialinfected <username>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!_playerManager.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        var system = _entities.System<AntagSelectionSystem>();
        system.ForceMakeAntag<VulpificationRuleComponent>(session, DefaultVulpificationRule);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var names = _playerManager.Sessions.Select(c => c.Name).ToArray();
            return CompletionResult.FromHintOptions(names, Loc.GetString("shell-argument-username-optional-hint"));
        }

        return CompletionResult.Empty;
    }
}