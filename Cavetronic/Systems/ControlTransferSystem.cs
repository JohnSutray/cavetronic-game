using Arch.Core;

namespace Cavetronic.Systems;

public class ControlTransferSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<ControlOwner>();

  private readonly List<Entity> _deferredAddControlSubject = new();
  private readonly List<Entity> _deferredInputCleanup = new();

  public override void Tick(float dt) {
    _deferredAddControlSubject.Clear();
    _deferredInputCleanup.Clear();

    GameWorld.Ecs.Query(in _playersQuery, (Entity playerEntity, ref ControlOwner owner) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) {
        return;
      }

      if (!GameWorld.Ecs.Has<ControlSubject>(subjectEntity)) {
        return;
      }

      ref var subject = ref GameWorld.Ecs.Get<ControlSubject>(subjectEntity);

      if (subject.TransferTargetId == 0) {
        return;
      }

      // Сброс инпутов при смене сабжекта
      _deferredInputCleanup.Add(playerEntity);
      _deferredInputCleanup.Add(subjectEntity);

      var newSubjectId = subject.TransferTargetId;
      subject.TransferTargetId = 0;
      owner.SubjectId = newSubjectId;
      owner.ReassignedAtTick = GameWorld.Tick;

      // Добавить ControlSubject на новую сущность (если нет)
      if (GameWorld.TryGetEntity(newSubjectId, out var newEntity)) {
        if (!GameWorld.Ecs.Has<ControlSubject>(newEntity)) {
          _deferredAddControlSubject.Add(newEntity);
        }
      }
    });

    // Deferred: сброс всех инпутов при трансфере
    foreach (var entity in _deferredInputCleanup) {
      if (GameWorld.Ecs.IsAlive(entity)) {
        RemoveInput<Action1>(entity);
        RemoveInput<Action2>(entity);
        RemoveInput<MoveLeft>(entity);
        RemoveInput<MoveRight>(entity);
      }
    }

    // Deferred: добавить ControlSubject новым сущностям
    foreach (var entity in _deferredAddControlSubject) {
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

  private void RemoveInput<T>(Entity entity) where T : struct {
    if (GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
      GameWorld.Ecs.Remove<ControlSubjectInput<T>>(entity);
    }
  }
}
