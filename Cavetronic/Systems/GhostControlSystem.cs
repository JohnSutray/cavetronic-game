using Arch.Core;

namespace Cavetronic.Systems;

public class GhostControlSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _ghostsQuery = new QueryDescription()
    .WithAll<Ghost, ControlSubject, StableId, ControlSubjectInput<Action1>>();

  public override void Tick(float dt) {
    var toDestroy = new List<(Entity Entity, int StableId)>();

    GameWorld.Ecs.Query(in _ghostsQuery, (
      Entity entity,
      ref ControlSubject subject,
      ref StableId stableId,
      ref ControlSubjectInput<Action1> input
    ) => {
      if (input.Active && !input.PreviouslyActive) {
        subject.TransferTargetId = StableId.DefaultSpawnerId;
        toDestroy.Add((entity, stableId.Id));
      }
    });

    // Deferred: пометить ghost для уничтожения после обработки трансфера
    foreach (var item in toDestroy) {
      GameWorld.PendingDestroy.Add(item);
    }
  }
}
