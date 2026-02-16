using Arch.Core;

namespace Cavetronic.Systems;

public class SpawnerProductionSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _spawnersQuery = new QueryDescription().WithAll<DroneHeadSpawner>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _spawnersQuery, (ref DroneHeadSpawner spawner) => {
      if (!spawner.ProductionReady) {
        spawner.ProductionTimer -= dt;
        if (spawner.ProductionTimer <= 0f) {
          spawner.ProductionReady = true;
          spawner.ProductionTimer = 0f;
        }
      }
    });
  }
}
