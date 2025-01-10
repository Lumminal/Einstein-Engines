using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles;

/// <summary>
///     Tracks the target hit by the tracking projectile and updates it to the crew pinpointer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrackingProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> TrackedEntities = new();

    [DataField, AutoNetworkedField]
    public EntityUid? TrackedEntity;

    [DataField, AutoNetworkedField]
    public string? TrackedEntityName;
}
