namespace Cavetronic.Generation;

public class CaveGenerationConfig {
  // Noise parameters
  public int Seed = 12345;
  public float Frequency = 0.02f;
  public int Octaves = 4;
  public float Threshold = 0.45f; // Cave threshold (inverted: solid where noise < threshold)

  // Cellular Automata
  public int SmoothIterations = 3; // Проверка: вызывает stack overflow
  public int SolidNeighborThreshold = 5;

  // Chunk parameters
  public int ChunkSize = 128; // meters

  // Shard filtering
  public float MinShardArea = 4f;
  public float ShardEnclosedThreshold = 0.2f; // доля площади меньшего шарда внутри большего для удаления

  // Physics
  public float Friction = 0.7f;
  public float Restitution = 0.1f;
  public float Density = 1f;
}
