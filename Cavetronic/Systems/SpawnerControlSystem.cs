using Arch.Core;
using nkast.Aether.Physics2D.Dynamics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace Cavetronic.Systems;

public class SpawnerControlSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float ProductionDuration = 5f;
  private const float DroneRadius = 0.5f;
  private const float DroneDensity = 1f;

  private readonly QueryDescription _spawnAction1Query = new QueryDescription()
    .WithAll<DroneHeadSpawner, ControlSubject, Position, StableId, ControlSubjectInput<Action1>>();

  private readonly QueryDescription _spawnAction2Query = new QueryDescription()
    .WithAll<DroneHeadSpawner, ControlSubject, ControlSubjectInput<Action2>>();

  public override void Tick(float dt) {
    var deferredCreations = new List<(float X, float Y, int DroneId)>();

    // Action1 (Space): выпустить дрона из production в stage
    GameWorld.Ecs.Query(in _spawnAction1Query, (
      ref DroneHeadSpawner spawner,
      ref ControlSubject subject,
      ref Position pos,
      ref StableId stableId,
      ref ControlSubjectInput<Action1> input
    ) => {
      if (input.Active && !input.PreviouslyActive
          && spawner.ProductionReady
          && spawner.StageDroneId == 0) {
        var droneId = GameWorld.NextStableId();
        spawner.StageDroneId = droneId;
        spawner.ProductionReady = false;
        spawner.ProductionTimer = ProductionDuration;
        deferredCreations.Add((pos.X, pos.Y + 1f, droneId));
      }
    });

    // Action2 (E): вселиться в дрона из stage
    GameWorld.Ecs.Query(in _spawnAction2Query, (
      ref DroneHeadSpawner spawner,
      ref ControlSubject subject,
      ref ControlSubjectInput<Action2> input
    ) => {
      if (input.Active && !input.PreviouslyActive
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
        new PhysicsBodyRef { Body = body },
        new ControlSubjectInputDescriptor<MoveLeft>(),
        new ControlSubjectInputDescriptor<MoveRight>()
      );
      GameWorld.RegisterEntity(droneId, entity);
    }
  }
}
