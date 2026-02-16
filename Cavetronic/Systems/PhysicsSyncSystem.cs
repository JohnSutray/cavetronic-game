using Arch.Core;

namespace Cavetronic.Systems;

public class PhysicsSyncSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _query = new QueryDescription().WithAll<PhysicsBodyRef, Position>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _query, (ref PhysicsBodyRef bodyRef, ref Position pos) => {
      pos.X = bodyRef.Body.Position.X;
      pos.Y = bodyRef.Body.Position.Y;
    });
  }
}
