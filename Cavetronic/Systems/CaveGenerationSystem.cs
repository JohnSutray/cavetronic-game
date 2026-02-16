using Cavetronic.Generation;

namespace Cavetronic.Systems;

public class CaveGenerationSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly CaveGenerationConfig _config = new();

  public override void Init() {
    var noiseGenerator = new NoiseGenerator(_config.Seed, _config.Frequency, _config.Octaves);
    var chunkGenerator = new ChunkGenerator(_config, noiseGenerator);
    var bodyBuilder = new PhysicsBodyBuilder(GameWorld.Physics, _config);
    var visualizer = new FullMapVisualizer(_config);

    for (int chunkX = -1; chunkX <= 1; chunkX++) {
      for (int chunkY = -1; chunkY <= 1; chunkY++) {
        var (chunk, debugData) = chunkGenerator.GenerateChunk(chunkX, chunkY);

        // Создаём физические тела из shaped-шардов
        foreach (var island in chunk.Islands) {
          foreach (var shard in island.Shards) {
            bodyBuilder.CreateBodyFromShard(shard);
          }
        }

        // Визуализация
        visualizer.AddChunk(chunkX, chunkY, chunk, debugData);
      }
    }

    visualizer.SaveFullMap();
  }
}
