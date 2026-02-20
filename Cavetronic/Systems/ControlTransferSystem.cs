using Arch.Buffer;
using Arch.Core;

namespace Cavetronic.Systems;

public class ControlTransferSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<ControlOwner>();
  private readonly CommandBuffer _buffer = new();

  public override void Tick(float dt) {
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
      RecordInputCleanup(_buffer, playerEntity);
      RecordInputCleanup(_buffer, subjectEntity);

      var newSubjectId = subject.TransferTargetId;
      subject.TransferTargetId = 0;
      owner.SubjectId = newSubjectId;
      owner.ReassignedAtTick = GameWorld.Tick;

      // Добавить ControlSubject на новую сущность (если нет)
      if (GameWorld.TryGetEntity(newSubjectId, out var newEntity)) {
        if (!GameWorld.Ecs.Has<ControlSubject>(newEntity)) {
          _buffer.Add<ControlSubject>(in newEntity);
        }
      }
    });

    _buffer.Playback(GameWorld.Ecs);

    // Deferred: уничтожить помеченные сущности (нужен EntityIndex → ручной проход)
    foreach (var (entity, stableId) in GameWorld.PendingDestroy) {
      GameWorld.UnregisterEntity(stableId);

      if (GameWorld.Ecs.IsAlive(entity)) {
        GameWorld.Ecs.Destroy(entity);
      }
    }

    GameWorld.PendingDestroy.Clear();
  }

  private void RecordInputCleanup(CommandBuffer buffer, Entity entity) {
    RemoveIfHas<ControlSubjectInput<Action1>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<Action2>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<MoveLeft>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<MoveRight>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<CursorInput>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<CursorLeftMoveAction>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<CursorRightClickAction>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<ShiftModifier>>(buffer, entity);
    RemoveIfHas<ControlSubjectInput<InputExclusive>>(buffer, entity);
  }

  private void RemoveIfHas<T>(CommandBuffer buffer, Entity entity) where T : struct {
    if (GameWorld.Ecs.Has<T>(entity)) {
      buffer.Remove<T>(in entity);
    }
  }
}
