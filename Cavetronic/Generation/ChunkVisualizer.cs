namespace Cavetronic.Generation;

public static class ChunkVisualizer {
  public static float[,] GenerateRawNoise(CaveGenerationConfig config, int startX, int startY, int size) {
    var noise = new FastNoiseLite(config.Seed);
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFrequency(config.Frequency);
    noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    noise.SetFractalOctaves(config.Octaves);

    var values = new float[size, size];
    for (int x = 0; x < size; x++) {
      for (int y = 0; y < size; y++) {
        values[x, y] = noise.GetNoise(startX + x, startY + y);
      }
    }
    return values;
  }
}
