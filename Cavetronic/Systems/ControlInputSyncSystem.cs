using Arch.Core;

namespace Cavetronic.Systems;

public class ControlInputSyncSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _subjectsQuery = new QueryDescription().WithAll<ControlSubject>();
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<Player, ControlOwner>();

  public override void Tick(float dt) {
    // 1. Очистить все ControlSubject.Input
    GameWorld.Ecs.Query(in _subjectsQuery, (ref ControlSubject subject) => {
      subject.Input = 0;
    });

    // 2. Копировать Input из ControlOwner → ControlSubject
    GameWorld.Ecs.Query(in _playersQuery, (ref ControlOwner owner) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) return;
      if (!GameWorld.Ecs.Has<ControlSubject>(subjectEntity)) return;

      ref var subject = ref GameWorld.Ecs.Get<ControlSubject>(subjectEntity);
      subject.Input |= owner.Input;
    });
  }
}
