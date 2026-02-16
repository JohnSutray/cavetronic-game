using Arch.Core;
using nkast.Aether.Physics2D.Dynamics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Cavetronic.Systems;

public class SpawnerControlSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float ProductionDuration = 5f;
  private const float DroneRadius = 0.5f;
  private const float DroneDensity = 1f;

  private readonly QueryDescription _spawnersQuery = new QueryDescription()
    .WithAll<DroneHeadSpawner, ControlSubject, Position, StableId>();

  public override void Tick(float dt) {
    var deferredCreations = new List<(float X, float Y, int DroneId)>();

    GameWorld.Ecs.Query(in _spawnersQuery, (
      ref DroneHeadSpawner spawner,
      ref ControlSubject subject,
      ref Position pos,
      ref StableId stableId
    ) => {
      var input = subject.Input;

      // Action1 (Space): выпустить дрона из production в stage
      if ((input & (ulong)InputSignal.Action1) != 0
          && spawner.ProductionReady
          && spawner.StageDroneId == 0) {
        var droneId = GameWorld.NextStableId();
        spawner.StageDroneId = droneId;
        spawner.ProductionReady = false;
        spawner.ProductionTimer = ProductionDuration;
        deferredCreations.Add((pos.X, pos.Y + 1f, droneId));
      }

      // Action2 (E): вселиться в дрона из stage
      if ((input & (ulong)InputSignal.Action2) != 0
          && spawner.StageDroneId != 0) {
        subject.TransferTargetId = spawner.StageDroneId;
        spawner.StageDroneId = 0;
      }
    });

    // Deferred: создание дронов
    foreach (var (x, y, droneId) in deferredCreations) {
      var body = GameWorld.Physics.CreateBody(new AetherVector2(x, y), 0f, BodyType.Dynamic);
      body.CreateCircle(DroneRadius, DroneDensity);
      body.LinearDamping = 0.5f;

      var entity = GameWorld.Ecs.Create(
        new StableId { Id = droneId },
        new DroneHead(),
        new Position { X = x, Y = y },
        new PhysicsBodyRef { Body = body }
      );
      GameWorld.RegisterEntity(droneId, entity);
    }
  }
}
