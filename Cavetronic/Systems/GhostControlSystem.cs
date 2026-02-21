using Arch.Core;

namespace Cavetronic.Systems;

public class GhostControlSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _ghostsQuery = new QueryDescription()
    .WithAll<Ghost, ControlSubject, StableId, ControlSubjectInput<Action1>>();

  private readonly List<(Entity Entity, int StableId)> _toDestroy = new();

  public override void Tick(float dt) {
    _toDestroy.Clear();

    GameWorld.Ecs.Query(in _ghostsQuery, (
      Entity entity,
      ref ControlSubject subject,
      ref StableId stableId,
      ref ControlSubjectInput<Action1> input
    ) => {
      if (input is { Active: true, PreviouslyActive: false }) {
        subject.TransferTargetId = StableId.DefaultSpawnerId;
        _toDestroy.Add((entity, stableId.Id));
      }
    });

    // Deferred: пометить ghost для уничтожения после обработки трансфера
    foreach (var item in _toDestroy) {
      GameWorld.PendingDestroy.Add(item);
    }
  }
}
