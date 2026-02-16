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

  private readonly List<Entity> _toClean = new();

  public override void Tick(float dt) {
    // Phase 1: advance — shift Active → PreviouslyActive, reset Active
    AdvanceInput<Action1>(in _subjectAction1Query);
    AdvanceInput<Action2>(in _subjectAction2Query);
    AdvanceInput<MoveLeft>(in _subjectMoveLeftQuery);
    AdvanceInput<MoveRight>(in _subjectMoveRightQuery);

    // Phase 2: sync — propagate player markers → subject entities
    GameWorld.Ecs.Query(in _playersQuery, (
      Entity playerEntity,
      ref ControlOwner owner,
      ref StableId stableId
    ) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) {
        return;
      }

      if (owner.ReassignedAtTick > 0
          && GameWorld.Tick - owner.ReassignedAtTick < SkipControlAfterReassignTicks) {
        return;
      }

      SyncInput<Action1>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<Action2>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<MoveLeft>(playerEntity, subjectEntity, stableId.Id);
      SyncInput<MoveRight>(playerEntity, subjectEntity, stableId.Id);
    });

    // Phase 3: cleanup — remove markers where (!Active && !PreviouslyActive)
    CleanupInput<Action1>(in _subjectAction1Query);
    CleanupInput<Action2>(in _subjectAction2Query);
    CleanupInput<MoveLeft>(in _subjectMoveLeftQuery);
    CleanupInput<MoveRight>(in _subjectMoveRightQuery);
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

    if (!GameWorld.Ecs.Has<ControlSubjectInput<T>>(subjectEntity)) {
      GameWorld.Ecs.Add(subjectEntity, new ControlSubjectInput<T> {
        Active = true,
        PreviouslyActive = false,
        OwnerId = ownerId
      });
    } else {
      ref var input = ref GameWorld.Ecs.Get<ControlSubjectInput<T>>(subjectEntity);

      if (!input.Active) {
        input.Active = true;
        input.OwnerId = ownerId;
      }
    }
  }

  private void CleanupInput<T>(in QueryDescription query) where T : struct {
    _toClean.Clear();

    GameWorld.Ecs.Query(in query, (Entity entity, ref ControlSubjectInput<T> input) => {
      if (!input.Active && !input.PreviouslyActive) {
        _toClean.Add(entity);
      }
    });

    foreach (var entity in _toClean) {
      GameWorld.Ecs.Remove<ControlSubjectInput<T>>(entity);
    }
  }
}
