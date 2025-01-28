using Content.Server.Radio.EntitySystems;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Pinpointer;
using Content.Shared.Projectiles;
using Content.Shared.Security;
using Content.Shared.Security.Components;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Events;

namespace Content.Server.Projectiles;

public sealed class TrackProjectileSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedCriminalRecordsConsoleSystem _criminal = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrackingProjectileComponent, StartCollideEvent>(OnTrackCollide);

    }

    /// <summary>
    ///  Handles collision with target. Updates criminal identity. Tracks one target at a time. Sends announcement message over security radio when tracking.
    /// </summary>
    private void OnTrackCollide(EntityUid uid, TrackingProjectileComponent component, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        var targetName = Name(target);
        var bolt = args.OurEntity;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (HasComp<TrackedTargetProjectileComponent>(target))
            return;

        if (component.TrackedEntity != target && component.TrackedEntity != null)
        {
            var query = EntityQueryEnumerator<TrackedTargetProjectileComponent>();
            while (query.MoveNext(out var prevTrackedUid, out _))
                _entityManager.RemoveComponent<TrackedTargetProjectileComponent>(prevTrackedUid);
        }

        _entityManager.AddComponent<TrackedTargetProjectileComponent>(target);
        component.TrackedEntity = target;

        if (!component.RadioMsgSent)
        {
            string msg;
            if (!HasComp<CriminalRecordComponent>(target))
                msg = "dl88-track-message";
            else
                msg = "dl88-track-warrant-already";

            var track = new (string, Object)[] { ("name", target), ("bolt", bolt) };
            _radio.SendRadioMessage(
                bolt,
                Loc.GetString(msg, track),
                "Security",
                target);
            component.RadioMsgSent = true;
        }
        _criminal.UpdateCriminalIdentity(targetName, SecurityStatus.Wanted);
        _criminal.SetCriminalIcon(targetName, SecurityStatus.Wanted, target);
    }

}
