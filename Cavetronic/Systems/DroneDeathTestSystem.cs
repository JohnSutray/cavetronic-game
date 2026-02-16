using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems;

public class DroneDeathTestSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _dronesQuery = new QueryDescription()
    .WithAll<DroneHead, ControlSubject, Position, StableId, PhysicsBodyRef>();

  public override void Tick(float dt) {
    if (!Raylib.IsKeyPressed(KeyboardKey.K)) return;

    var dronesToKill = new List<(float X, float Y, int DroneStableId, Entity DroneEntity, nkast.Aether.Physics2D.Dynamics.Body Body)>();

    GameWorld.Ecs.Query(in _dronesQuery, (
      Entity entity,
      ref Position pos,
      ref StableId stableId,
      ref PhysicsBodyRef bodyRef
    ) => {
      dronesToKill.Add((pos.X, pos.Y, stableId.Id, entity, bodyRef.Body));
    });

    foreach (var (x, y, droneStableId, droneEntity, body) in dronesToKill) {
      // Удалить физическое тело дрона
      GameWorld.Physics.Remove(body);

      // Создать ghost entity
      var ghostId = GameWorld.NextStableId();
      var ghostEntity = GameWorld.Ecs.Create(
        new StableId { Id = ghostId },
        new Ghost(),
        new Position { X = x, Y = y }
      );
      GameWorld.RegisterEntity(ghostId, ghostEntity);

      // Установить TransferTargetId на дроне → ghost
      if (GameWorld.Ecs.IsAlive(droneEntity) && GameWorld.Ecs.Has<ControlSubject>(droneEntity)) {
        ref var subject = ref GameWorld.Ecs.Get<ControlSubject>(droneEntity);
        subject.TransferTargetId = ghostId;
      }

      // Пометить дрон для уничтожения (после обработки трансфера)
      GameWorld.PendingDestroy.Add((droneEntity, droneStableId));
    }
  }
}
