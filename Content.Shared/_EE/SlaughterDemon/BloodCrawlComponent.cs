using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._EE.SlaughterDemon;


/// <summary>
/// THIS IS USED TO SHOW YOU THAT BLOOD IS MY ONLY REDEMPTION. SO READ CLOSELY, HUMAN!!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCrawlComponent : Component
{
    [DataField]
    public bool IsCrawling;
}
