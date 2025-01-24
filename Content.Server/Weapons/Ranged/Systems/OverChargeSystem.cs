using Content.Server.DeltaV.Weapons.Ranged.Components;
using Content.Server.DeltaV.Weapons.Ranged.Systems;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.DeltaV.Weapons.Ranged;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Player;
using Robust.Shared.Prototypes;


namespace Content.Server.Weapons.Ranged.Systems;


public sealed class OverChargeSystem : EntitySystem
{
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedGunSystem _sharedGun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OverChargeComponent, InteractUsingEvent>(OnInteracting);
    }

    private void OnInteracting(EntityUid uid, OverChargeComponent component, InteractUsingEvent args)
    {

        if (args.Handled)
            return;

        if (!_toolSystem.HasQuality(args.Used, "Pulsing"))
            return;

        if (!TryComp(uid, out EnergyGunComponent? energyComp))
            return;

        args.Handled = true;
        component.Hacked = !component.Hacked;
    }
}
