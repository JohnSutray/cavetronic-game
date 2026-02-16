using Arch.Core;

namespace Cavetronic.Systems;

public class ControlTransferSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<Player, ControlOwner>();

  public override void Tick(float dt) {
    var deferredAddControlSubject = new List<Entity>();

    GameWorld.Ecs.Query(in _playersQuery, (ref ControlOwner owner) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) return;
      if (!GameWorld.Ecs.Has<ControlSubject>(subjectEntity)) return;

      ref var subject = ref GameWorld.Ecs.Get<ControlSubject>(subjectEntity);
      if (subject.TransferTargetId == 0) return;

      var newSubjectId = subject.TransferTargetId;
      subject.TransferTargetId = 0;
      owner.SubjectId = newSubjectId;

      // Добавить ControlSubject на новую сущность (если нет)
      if (GameWorld.TryGetEntity(newSubjectId, out var newEntity)) {
        if (!GameWorld.Ecs.Has<ControlSubject>(newEntity)) {
          deferredAddControlSubject.Add(newEntity);
        }
      }
    });

    // Deferred: добавить ControlSubject новым сущностям
    foreach (var entity in deferredAddControlSubject) {
      GameWorld.Ecs.Add<ControlSubject>(entity);
    }

    // Deferred: уничтожить помеченные сущности
    foreach (var (entity, stableId) in GameWorld.PendingDestroy) {
      GameWorld.UnregisterEntity(stableId);
      if (GameWorld.Ecs.IsAlive(entity)) {
        GameWorld.Ecs.Destroy(entity);
      }
    }

    GameWorld.PendingDestroy.Clear();
  }
}
