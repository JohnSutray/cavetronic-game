using Arch.Buffer;
using Arch.Core;

namespace Cavetronic.Systems;

public class ControlInputSyncSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const int SkipControlAfterReassignTicks = 10;

  private readonly QueryDescription _playersQuery =
    new QueryDescription().WithAll<ControlOwner, StableId>();

  private readonly QueryDescription _subjectAction1Query =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<Action1>>();

  private readonly QueryDescription _subjectAction2Query =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<Action2>>();

  private readonly QueryDescription _subjectMoveLeftQuery =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<MoveLeft>>();

  private readonly QueryDescription _subjectMoveRightQuery =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<MoveRight>>();

  private readonly QueryDescription _subjectCursorInputQuery =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<CursorInput>>();

  private readonly QueryDescription _subjectCursorLeftQuery =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<CursorLeftMoveAction>>();

  private readonly QueryDescription _subjectCursorRightQuery =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<CursorRightClickAction>>();

  private readonly QueryDescription _subjectShiftQuery =
    new QueryDescription().WithAll<ControlSubject, ControlSubjectInput<ShiftModifier>>();

  private readonly CommandBuffer _buffer = new();

  public override void Tick(float dt) {
    // Phase 1: advance — shift Active → PreviouslyActive, reset Active
    AdvanceInput<Action1>(in _subjectAction1Query);
    AdvanceInput<Action2>(in _subjectAction2Query);
    AdvanceInput<MoveLeft>(in _subjectMoveLeftQuery);
    AdvanceInput<MoveRight>(in _subjectMoveRightQuery);
    AdvanceInput<CursorInput>(in _subjectCursorInputQuery);
    AdvanceInput<CursorLeftMoveAction>(in _subjectCursorLeftQuery);
    AdvanceInput<CursorRightClickAction>(in _subjectCursorRightQuery);
    AdvanceInput<ShiftModifier>(in _subjectShiftQuery);

    // Phase 2: sync — propagate player markers → subject entities
    GameWorld.Ecs.Query(in _playersQuery, (
      Entity playerEntity,
      ref ControlOwner owner,
      ref StableId stableId
    ) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) {
        return;
      }

      if (
        owner.ReassignedAtTick > 0
        && GameWorld.Tick - owner.ReassignedAtTick < SkipControlAfterReassignTicks
      ) {
        return;
      }

      SyncInput<Action1>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<Action2>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<MoveLeft>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<MoveRight>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<CursorInput>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<CursorLeftMoveAction>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<CursorRightClickAction>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<ShiftModifier>(playerEntity, subjectEntity, stableId.Id);
    });

    // Phase 3: cleanup — remove markers where (!Active && !PreviouslyActive)
    CleanupInput<Action1>(in _subjectAction1Query);
    CleanupInput<Action2>(in _subjectAction2Query);
    CleanupInput<MoveLeft>(in _subjectMoveLeftQuery);
    CleanupInput<MoveRight>(in _subjectMoveRightQuery);
    CleanupInput<CursorInput>(in _subjectCursorInputQuery);
    CleanupInput<CursorLeftMoveAction>(in _subjectCursorLeftQuery);
    CleanupInput<CursorRightClickAction>(in _subjectCursorRightQuery);
    CleanupInput<ShiftModifier>(in _subjectShiftQuery);
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

    if (!GameWorld.Ecs.Has<ControlSubjectInputDescriptor<T>>(subjectEntity)) {
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
