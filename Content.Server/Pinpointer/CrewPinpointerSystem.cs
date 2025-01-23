using Content.Shared.Pinpointer;
using Content.Shared.Verbs;
using Content.Shared.Database;
using Content.Shared.Projectiles;

namespace Content.Server.Pinpointer;


public sealed class CrewPinpointerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinpointerComponent, GetVerbsEvent<Verb>>(CrewPinpointerStop);

    }

    private void CrewPinpointerStop(EntityUid uid, PinpointerComponent component, GetVerbsEvent<Verb> args)
    {
        // TODO FIX THIS and move

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.Component != "TrackedTargetProjectile")
            return;

        var v = new Verb
        {
            Priority = 1,
            Category = VerbCategory.Interaction,
            Text = Loc.GetString("crew-pinpointer-stop-tracking"),
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () =>
            {
                var query = EntityQueryEnumerator<TrackedTargetProjectileComponent>();
                while (query.MoveNext(out var prevTrackedUid, out _))
                    _entityManager.RemoveComponent<TrackedTargetProjectileComponent>(prevTrackedUid);
            }
        };

        args.Verbs.Add(v);
    }

}
