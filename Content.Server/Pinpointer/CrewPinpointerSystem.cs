using Content.Shared.Pinpointer;
using Content.Shared.Verbs;
using Content.Shared.Database;
using Content.Shared.Projectiles;

namespace Content.Server.Pinpointer;


public sealed class CrewPinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedPinpointerSystem _sharedPinpointerSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinpointerComponent, GetVerbsEvent<Verb>>(CrewPinpointerStop);

    }

    // <summary>
    // Stops tracking current target with verb action.
    // </summary>
    private void CrewPinpointerStop(EntityUid uid, PinpointerComponent component, GetVerbsEvent<Verb> args)
    {
        // TODO: fix question mark going out of screen

        var disable = true;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (component.Component != "TrackedTargetProjectile")
            return;

        if (component.HasTarget && !component.IsActive)
            disable = false;

        var v = new Verb
        {
            Priority = 1,
            Disabled = disable,
            Text = Loc.GetString("verb-common-stop-tracking"), // don't forget to remove crew-pinpointer-stop-tracking from the other .ftl
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () =>
            {
                _sharedPinpointerSystem.SetDistance(uid, Distance.Unknown, component);
                _sharedPinpointerSystem.TrySetArrowAngle(uid, 0f, component);
                if (component.Target != null)
                    RemComp<TrackedTargetProjectileComponent>(component.Target.Value);
                disable = true;

            }
        };

        args.Verbs.Add(v);
    }

}
