using nkast.Aether.Physics2D.Common;
using EcsWorld = Arch.Core.World;
using PhysicsWorld = nkast.Aether.Physics2D.Dynamics.World;

namespace Cavetronic;

public class GameWorld {
  public EcsWorld Ecs { get; }
  public PhysicsWorld Physics { get; }

  public GameWorld() {
    Ecs = EcsWorld.Create();
    Physics = new PhysicsWorld(new Vector2(0, 9.8f));
  }
}