namespace Cavetronic.Generation;

public class CaveGenerationConfig {
  // Noise parameters
  public int Seed = 12345;
  public float Frequency = 0.02f;
  public int Octaves = 4;
  public float Threshold = 0.45f; // Cave threshold (inverted: solid where noise < threshold)

  // Cellular Automata
  public int SmoothIterations = 2; // Проверка: вызывает stack overflow
  public int SolidNeighborThreshold = 5;

  // Chunk parameters
  public int ChunkSize = 64; // meters
  public float CellSize = 2f; // meters per cell (увеличено для меньшей детализации)

  // Physics
  public float Friction = 0.7f;
  public float Restitution = 0.1f;
  public float Density = 1f;
}
