namespace Cavetronic.Generation;

public class NoiseGenerator(int seed, float frequency, int octaves) {
  private readonly FastNoiseLite _noise = CreateNoise(seed, frequency, octaves);

  private static FastNoiseLite CreateNoise(int seed, float frequency, int octaves) {
    var noise = new FastNoiseLite(seed);
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFrequency(frequency);
    noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    noise.SetFractalOctaves(octaves);
    return noise;
  }

  public bool[,] GenerateGrid(int startX, int startY, int width, int height, float threshold) {
    var grid = new bool[width, height];

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        float noiseValue = _noise.GetNoise(startX + x, startY + y);
        // Noise returns [-1, 1], convert to [0, 1]
        // ИНВЕРСИЯ: solid там где шум НИЗКИЙ (пещеры = пустота там где шум высокий)
        grid[x, y] = (noiseValue + 1f) / 2f < threshold;
      }
    }

    return grid;
  }
}
