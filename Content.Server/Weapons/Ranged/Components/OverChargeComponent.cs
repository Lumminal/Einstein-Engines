using Content.Server.StationEvents.Events;


namespace Content.Server.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class OverChargeComponent : Component
{
    [DataField]
    public bool Hacked = false;
}
