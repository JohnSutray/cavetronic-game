using System.Numerics;
using Arch.Core;

namespace Cavetronic.Systems.Client;

public class CameraFollowSystem(GameWorld gameWorld, CameraSystem cameraSystem) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<Player, ControlOwner>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _playersQuery, (ref ControlOwner owner) => {
      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) {
        return;
      }
      
      if (!GameWorld.Ecs.Has<Position>(subjectEntity)) {
        return;
      }

      var pos = GameWorld.Ecs.Get<Position>(subjectEntity);
      cameraSystem.Camera.Target = new Vector2(pos.X, pos.Y);
    });
  }
}
