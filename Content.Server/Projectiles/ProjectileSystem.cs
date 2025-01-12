using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Damage.Systems;
using Content.Server.Effects;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Chat;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Security;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedCriminalRecordsConsoleSystem _criminal = default!;
    [Dependency] private readonly SecurityStatus _securityStatus = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<TrackingProjectileComponent, StartCollideEvent>(OnTrackCollide);
        SubscribeLocalEvent<EmbeddableProjectileComponent, DamageExamineEvent>(OnDamageExamine, after: [typeof(DamageOtherOnHitSystem)]);

    }

    private void OnStartCollide(EntityUid uid, ProjectileComponent component, ref StartCollideEvent args)
    {
        // This is so entities that shouldn't get a collision are ignored.
        if (args.OurFixtureId != ProjectileFixture || !args.OtherFixture.Hard
            || component.DamagedEntity || component is { Weapon: null, OnlyCollideWhenShot: true, })
            return;



        var target = args.OtherEntity;
        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var modifiedDamage = _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, origin: component.Shooter);
        var deleted = Deleted(target);

        if (modifiedDamage is not null && EntityManager.EntityExists(component.Shooter))
        {
            if (modifiedDamage.AnyPositive() && !deleted)
                _color.RaiseEffect(Color.Red, [ target, ], Filter.Pvs(target, entityManager: EntityManager));

            _adminLogger.Add(
                LogType.BulletHit,
                HasComp<ActorComponent>(target) ? LogImpact.Extreme : LogImpact.High,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {modifiedDamage.GetTotal():damage} damage");

        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, modifiedDamage, component.SoundHit, component.ForceSound);

            if (!args.OurBody.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, args.OurBody.LinearVelocity.Normalized());
        }

        component.DamagedEntity = true;

        if (component.DeleteOnCollide)
            QueueDel(uid);

        if (component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), Filter.Pvs(xform.Coordinates, entityMan: EntityManager));
    }

    /// <summary>
    ///  Handles interaction with Tracking Projectile and Crew Pinpointer.
    /// </summary>
    private void OnTrackCollide(EntityUid uid, TrackingProjectileComponent component, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        var targetName = Name(target);
        var bolt = args.OurEntity;

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (HasComp<TrackedTargetProjectileComponent>(target))
            return;

        if (component.TrackedEntity != target && component.TrackedEntity != null)
        {
            var query = EntityQueryEnumerator<TrackedTargetProjectileComponent>();
            while (query.MoveNext(out var prevTrackedUid, out _))
            {
                _entityManager.RemoveComponent<TrackedTargetProjectileComponent>(prevTrackedUid);
            }
        }

        _entityManager.AddComponent<TrackedTargetProjectileComponent>(target);
        component.TrackedEntity = target;

        // TODO: find a way to send an alt message if person is already wanted
        {
            _criminal.UpdateCriminalIdentity(targetName, SecurityStatus.Wanted);
            _criminal.SetCriminalIcon(targetName, SecurityStatus.Wanted, target);
        }

        if (!component.RadioMsgSent)
        {
            string msg;
            if (_securityStatus.HasFlag(SecurityStatus.None))
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

    }

    private void OnDamageExamine(EntityUid uid, EmbeddableProjectileComponent component, ref DamageExamineEvent args)
    {
        if (!component.EmbedOnThrow)
            return;

        if (!args.Message.IsEmpty)
            args.Message.PushNewline();

        var isHarmful = TryComp<EmbedPassiveDamageComponent>(uid, out var passiveDamage) && passiveDamage.Damage.AnyPositive();
        var loc = isHarmful
            ? "damage-examine-embeddable-harmful"
            : "damage-examine-embeddable";

        var staminaCostMarkup = FormattedMessage.FromMarkupOrThrow(Loc.GetString(loc));
        args.Message.AddMessage(staminaCostMarkup);
    }


}
