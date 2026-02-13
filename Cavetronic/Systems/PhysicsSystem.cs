namespace Cavetronic.Systems;

public class PhysicsSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  public override void Tick(float dt) {
    GameWorld.Physics.Step(dt);
  }
}