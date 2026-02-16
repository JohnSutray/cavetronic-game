using Arch.Core;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Cavetronic.Systems;

public class DroneControlSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float MoveForce = 5f;

  private readonly QueryDescription _dronesQuery = new QueryDescription()
    .WithAll<DroneHead, ControlSubject, PhysicsBodyRef>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _dronesQuery, (ref ControlSubject subject, ref PhysicsBodyRef bodyRef) => {
      var input = subject.Input;

      if ((input & (ulong)InputSignal.Left) != 0) {
        bodyRef.Body.ApplyForce(new AetherVector2(-MoveForce, 0));
      }

      if ((input & (ulong)InputSignal.Right) != 0) {
        bodyRef.Body.ApplyForce(new AetherVector2(MoveForce, 0));
      }
    });
  }
}
