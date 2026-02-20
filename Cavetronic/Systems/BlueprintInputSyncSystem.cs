using Arch.Buffer;
using Arch.Core;

namespace Cavetronic.Systems;

// Аналог ControlInputSyncSystem для blueprint-специфичных инпутов.
// Синхронизирует ControlSubjectInput<T> с сущности игрока на Blueprint-сущность.
// Копирует Payload в отличие от базового ControlInputSyncSystem.
public class BlueprintInputSyncSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const int SkipControlAfterReassignTicks = 10;

  private readonly QueryDescription _playersQuery =
    new QueryDescription().WithAll<ControlOwner, StableId>();

  private readonly QueryDescription _subjectCursorQuery =
    new QueryDescription().WithAll<Blueprint, ControlSubjectInput<CursorInput>>();

  private readonly QueryDescription _subjectLeftQuery =
    new QueryDescription().WithAll<Blueprint, ControlSubjectInput<CursorLeftMoveAction>>();

  private readonly QueryDescription _subjectRightQuery =
    new QueryDescription().WithAll<Blueprint, ControlSubjectInput<CursorRightClickAction>>();

  private readonly QueryDescription _subjectShiftQuery =
    new QueryDescription().WithAll<Blueprint, ControlSubjectInput<ShiftModifier>>();

  private readonly QueryDescription _subjectExclusiveQuery =
    new QueryDescription().WithAll<Blueprint, ControlSubjectInput<InputExclusive>>();

  private readonly CommandBuffer _buffer = new();

  public override void Tick(float dt) {
    // Phase 1: advance — shift Active → PreviouslyActive, reset Active
    AdvanceInput<CursorInput>(in _subjectCursorQuery);
    AdvanceInput<CursorLeftMoveAction>(in _subjectLeftQuery);
    AdvanceInput<CursorRightClickAction>(in _subjectRightQuery);
    AdvanceInput<ShiftModifier>(in _subjectShiftQuery);
    AdvanceInput<InputExclusive>(in _subjectExclusiveQuery);

    // Phase 2: sync — propagate player inputs → blueprint subject entity
    GameWorld.Ecs.Query(in _playersQuery, (
      Entity playerEntity,
      ref ControlOwner owner,
      ref StableId stableId
    ) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) {
        return;
      }

      if (!GameWorld.Ecs.Has<Blueprint>(subjectEntity)) {
        return;
      }

      if (
        owner.ReassignedAtTick > 0
        && GameWorld.Tick - owner.ReassignedAtTick < SkipControlAfterReassignTicks
      ) {
        return;
      }

      var exclusiveOwnerId = GetExclusiveOwnerId(subjectEntity);

      // InputExclusive принимается от всех
      SyncInput<InputExclusive>(playerEntity, subjectEntity, stableId.Id);

      // Остальные инпуты — только от эксклюзивного владельца (или если эксклюзива нет)
      if (exclusiveOwnerId != 0 && exclusiveOwnerId != stableId.Id) {
        return;
      }

      SyncInput<CursorInput>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<CursorLeftMoveAction>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<CursorRightClickAction>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<ShiftModifier>(playerEntity, subjectEntity, stableId.Id);
    });

    // Phase 3: cleanup — remove where !Active && !PreviouslyActive
    CleanupInput<CursorInput>(in _subjectCursorQuery);
    CleanupInput<CursorLeftMoveAction>(in _subjectLeftQuery);
    CleanupInput<CursorRightClickAction>(in _subjectRightQuery);
    CleanupInput<ShiftModifier>(in _subjectShiftQuery);
    CleanupInput<InputExclusive>(in _subjectExclusiveQuery);
  }

  private int GetExclusiveOwnerId(Entity subjectEntity) {
    if (!GameWorld.Ecs.Has<ControlSubjectInput<InputExclusive>>(subjectEntity)) {
      return 0;
    }

    var ex = GameWorld.Ecs.Get<ControlSubjectInput<InputExclusive>>(subjectEntity);

    if (!ex.Active && !ex.PreviouslyActive) {
      return 0;
    }

    return ex.Payload.OwnerStableId;
  }

  private void AdvanceInput<T>(in QueryDescription query) where T : struct {
    GameWorld.Ecs.Query(in query, (ref ControlSubjectInput<T> input) => {
      input.PreviouslyActive = input.Active;
      input.Active = false;
    });
  }

  private void SyncInput<T>(Entity playerEntity, Entity subjectEntity, int ownerId) where T : struct {
    if (!GameWorld.Ecs.Has<ControlSubjectInput<T>>(playerEntity)) {
      return;
    }

    var playerInput = GameWorld.Ecs.Get<ControlSubjectInput<T>>(playerEntity);

    if (!GameWorld.Ecs.Has<ControlSubjectInput<T>>(subjectEntity)) {
      GameWorld.Ecs.Add(
        subjectEntity,
        new ControlSubjectInput<T> {
          Active = true,
          PreviouslyActive = false,
          OwnerId = ownerId,
          Payload = playerInput.Payload
        }
      );
    }
    else {
      ref var input = ref GameWorld.Ecs.Get<ControlSubjectInput<T>>(subjectEntity);

      if (!input.Active) {
        input.Active = true;
        input.OwnerId = ownerId;
        input.Payload = playerInput.Payload;
      }
    }
  }

  private void CleanupInput<T>(in QueryDescription query) where T : struct {
    GameWorld.Ecs.Query(in query, (Entity entity, ref ControlSubjectInput<T> input) => {
      if (!input.Active && !input.PreviouslyActive) {
        _buffer.Remove<ControlSubjectInput<T>>(in entity);
      }
    });

    _buffer.Playback(GameWorld.Ecs);
  }
}
