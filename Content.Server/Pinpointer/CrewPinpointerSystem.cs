using Content.Shared.Examine;
using Content.Shared.Pinpointer;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;


namespace Content.Server.Pinpointer;

/// <summary>
///  Disable Crew Pinpointer after 60 seconds.
/// </summary>
public sealed class CrewPinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedPinpointerSystem _pinpointer = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrackedTargetProjectileComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, TrackedTargetProjectileComponent component, ComponentInit args)
    {
        component.RemainingTime = component.Timer;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var crew))
        {
            if (crew.HasTarget && crew.Component == "TrackedTargetProjectile" && crew.Target != null)
            {
                var target = crew.Target.Value;
                TickTimer(target, frameTime);
            }
        }
    }

    private void TickTimer(EntityUid uid, float frameTime, TrackedTargetProjectileComponent? target = null)
    {
        if (!Resolve(uid, ref target))
            return;

        target.RemainingTime -= frameTime;

        if (target.RemainingTime <= 0)
        {
            target.RemainingTime = 0;
            DeleteTarget(uid, target);
        }
    }

    private void DeleteTarget(EntityUid uid, TrackedTargetProjectileComponent? target = null)
    {
        if (!Resolve(uid, ref target))
            return;

        var pinpointerQuery = EntityQueryEnumerator<PinpointerComponent>();
        while (pinpointerQuery.MoveNext(out var pinUid, out var pinpointer))
        {
            if (pinpointer.Target == uid)
            {
                // Clear the target reference on the PinpointerComponent
                _pinpointer.TrySetArrowAngle(pinUid, 0, pinpointer);
                _pinpointer.SetDistance(pinUid, Distance.Unknown, pinpointer);
                _pinpointer.SetTarget(pinUid, null, pinpointer);
                _pinpointer.TogglePinpointer(pinUid, pinpointer);
            }
        }

        RemComp<TrackedTargetProjectileComponent>(uid);

    }
}
