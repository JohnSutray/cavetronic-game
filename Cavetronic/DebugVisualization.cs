using Cavetronic.Generation;
using Raylib_cs;

namespace Cavetronic;

public class DebugVisualization {
  public static void Visualize() {
    var config = new CaveGenerationConfig();

    const int windowWidth = 1200;
    const int windowHeight = 600;
    const int gridSize = 64;
    const int cellPixelSize = 8; // 64 * 8 = 512 pixels

    Raylib.InitWindow(windowWidth, windowHeight, "Cave Generation Debug");
    Raylib.SetTargetFPS(60);

    int currentStep = 0;
    float[,]? noiseValues = null;
    bool[,]? boolGrid = null;
    bool[,]? smoothedGrid = null;

    while (!Raylib.WindowShouldClose()) {
      // Переключение шагов
      if (Raylib.IsKeyPressed(KeyboardKey.Space)) {
        currentStep = (currentStep + 1) % 3;
      }

      // Генерация данных для текущего шага
      if (currentStep == 0 && noiseValues == null) {
        // Шаг 1: Сырой шум (градации)
        noiseValues = GenerateNoiseValues(config, gridSize);
      } else if (currentStep == 1 && boolGrid == null) {
        // Шаг 2: Boolean grid (threshold применен)
        var noiseGen = new NoiseGenerator(config.Seed, config.Frequency, config.Octaves);
        boolGrid = noiseGen.GenerateGrid(0, 0, gridSize, gridSize, config.Threshold);
      } else if (currentStep == 2 && smoothedGrid == null) {
        // Шаг 3: После Cellular Automata
        if (boolGrid == null) {
          var noiseGen = new NoiseGenerator(config.Seed, config.Frequency, config.Octaves);
          boolGrid = noiseGen.GenerateGrid(0, 0, gridSize, gridSize, config.Threshold);
        }
        smoothedGrid = CellularAutomata.Smooth(boolGrid, config.SmoothIterations, config.SolidNeighborThreshold);
      }

      Raylib.BeginDrawing();
      Raylib.ClearBackground(Color.Black);

      // Рисуем в зависимости от шага
      if (currentStep == 0 && noiseValues != null) {
        DrawNoiseGrayscale(noiseValues, cellPixelSize, config.Threshold);
        Raylib.DrawText("STEP 1: Raw Noise (Grayscale)", 10, 10, 24, Color.Green);
        Raylib.DrawText($"Seed: {config.Seed}, Frequency: {config.Frequency}, Octaves: {config.Octaves}", 10, 40, 16, Color.White);
        Raylib.DrawText($"Red line = Threshold ({config.Threshold})", 10, 60, 16, Color.Red);
      } else if (currentStep == 1 && boolGrid != null) {
        DrawBooleanGrid(boolGrid, cellPixelSize);
        Raylib.DrawText("STEP 2: Boolean Grid (After Threshold)", 10, 10, 24, Color.Green);
        Raylib.DrawText($"White = Solid (value > {config.Threshold}), Black = Empty", 10, 40, 16, Color.White);
        var solidCount = CountSolid(boolGrid);
        var total = gridSize * gridSize;
        Raylib.DrawText($"Solid cells: {solidCount}/{total} ({100f * solidCount / total:F1}%)", 10, 60, 16, Color.White);
      } else if (currentStep == 2 && smoothedGrid != null) {
        DrawBooleanGrid(smoothedGrid, cellPixelSize);
        Raylib.DrawText("STEP 3: After Cellular Automata", 10, 10, 24, Color.Green);
        Raylib.DrawText($"Iterations: {config.SmoothIterations}, Neighbor threshold: {config.SolidNeighborThreshold}", 10, 40, 16, Color.White);
        var solidCount = CountSolid(smoothedGrid);
        var total = gridSize * gridSize;
        Raylib.DrawText($"Solid cells: {solidCount}/{total} ({100f * solidCount / total:F1}%)", 10, 60, 16, Color.White);
      }

      Raylib.DrawText("Press SPACE for next step", 10, windowHeight - 30, 20, Color.Yellow);
      Raylib.DrawFPS(windowWidth - 100, 10);

      Raylib.EndDrawing();
    }

    Raylib.CloseWindow();
  }

  private static float[,] GenerateNoiseValues(CaveGenerationConfig config, int size) {
    var noise = new FastNoiseLite(config.Seed);
    noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
    noise.SetFrequency(config.Frequency);
    noise.SetFractalType(FastNoiseLite.FractalType.FBm);
    noise.SetFractalOctaves(config.Octaves);

    var values = new float[size, size];
    for (int x = 0; x < size; x++) {
      for (int y = 0; y < size; y++) {
        values[x, y] = noise.GetNoise(x, y);
      }
    }
    return values;
  }

  private static void DrawNoiseGrayscale(float[,] noise, int cellSize, float threshold) {
    var width = noise.GetLength(0);
    var height = noise.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var value = noise[x, y]; // [-1, 1]
        var normalized = (value + 1f) / 2f; // [0, 1]
        var gray = (byte)(normalized * 255);

        var color = new Color(gray, gray, gray, (byte)255);

        // Если выше threshold - подсвечиваем зеленым
        if (normalized > threshold) {
          color = new Color(gray, (byte)255, gray, (byte)255);
        }

        Raylib.DrawRectangle(x * cellSize, y * cellSize, cellSize, cellSize, color);
      }
    }
  }

  private static void DrawBooleanGrid(bool[,] grid, int cellSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var color = grid[x, y] ? Color.White : new Color(20, 20, 20, 255);
        Raylib.DrawRectangle(x * cellSize, y * cellSize, cellSize, cellSize, color);
      }
    }
  }

  private static int CountSolid(bool[,] grid) {
    var count = 0;
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        if (grid[x, y]) count++;
      }
    }
    return count;
  }
}
