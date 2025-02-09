namespace Content.Shared.Projectiles;

/// <summary>
/// Indicates that an entity has been hit by a tracking projectile and it being targeted by the crew pinpointer
/// </summary>
[RegisterComponent]
public sealed partial class TrackedTargetProjectileComponent : Component
{
    /// <summary>
    ///     Cooldown that removes target once it hits the specific time. Used for crew pinpointer.
    /// </summary>
    [DataField("timer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Timer = 60;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RemainingTime;
}
