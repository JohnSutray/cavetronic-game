using Arch.Core;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Cavetronic.Systems;

public class DroneControlSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float MoveForce = 5f;

  private readonly QueryDescription _moveLeftQuery = new QueryDescription()
    .WithAll<DroneHead, ControlSubjectInput<MoveLeft>, PhysicsBodyRef>();

  private readonly QueryDescription _moveRightQuery = new QueryDescription()
    .WithAll<DroneHead, ControlSubjectInput<MoveRight>, PhysicsBodyRef>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _moveLeftQuery, (
      ref ControlSubjectInput<MoveLeft> input,
      ref PhysicsBodyRef bodyRef
    ) => {
      if (input.Active) {
        bodyRef.Body.ApplyForce(new AetherVector2(-MoveForce, 0));
      }
    });

    GameWorld.Ecs.Query(in _moveRightQuery, (
      ref ControlSubjectInput<MoveRight> input,
      ref PhysicsBodyRef bodyRef
    ) => {
      if (input.Active) {
        bodyRef.Body.ApplyForce(new AetherVector2(MoveForce, 0));
      }
    });
  }
}
