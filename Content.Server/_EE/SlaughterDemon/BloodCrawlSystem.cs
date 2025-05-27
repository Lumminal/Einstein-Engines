using Content.Server.Fluids.EntitySystems;
using Content.Server.Stealth;
using Content.Shared._EE.SlaughterDemon;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids.Components;
using Content.Shared.Stealth.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;


namespace Content.Server._EE.SlaughterDemon;


/// <summary>
/// AHHHH BLOOD BLOOD ME LOVE BLOOD. LOVELY RED YES
/// </summary>
public sealed class BloodCrawlSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BloodCrawlComponent, BloodCrawlEvent>(OnBloodCrawl);
    }

    private void OnBloodCrawl(EntityUid uid, BloodCrawlComponent component, BloodCrawlEvent args)
    {
        if (!IsStandingOnBlood(uid, component))
            Logger.Info("YOU ARE NOT THE BLOOD GOD");


        component.IsCrawling = !component.IsCrawling;

        if (component.IsCrawling)
        {
            var stealth = EnsureComp<StealthComponent>(uid);
            // make them unable to do anything in this form

            _stealth.SetVisibility(uid, -1, stealth);
        }
        else
        {
            RemCompDeferred<StealthComponent>(uid);
        }
    }

    private bool IsStandingOnBlood(EntityUid demon, BloodCrawlComponent component)
    {

        var xform = EntityManager.GetComponent<TransformComponent>(demon);
        var gridUid = _transformSystem.GetGrid(xform.Coordinates);
        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid.Value, out var grid))
            return false;

        var tile = _map.GetTileRef(gridUid.Value, grid, xform.Coordinates);

        if (!_puddleSystem.TryGetPuddle(tile, out var puddleUid))
            return false;

        if (!TryComp<SolutionContainerManagerComponent>(puddleUid, out var solContainter))
            return false;

        var puddleComp = EntityManager.GetComponent<PuddleComponent>(puddleUid);
        if (!_solutionContainerSystem.ResolveSolution((puddleUid, solContainter), puddleComp.SolutionName, ref puddleComp.Solution, out var solution))
            return false;

        if (!solution.ContainsReagent("Blood", null))
            return false;

        return true;
    }
}
