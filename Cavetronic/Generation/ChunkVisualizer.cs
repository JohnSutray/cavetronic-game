using nkast.Aether.Physics2D.Common;
using Raylib_cs;

namespace Cavetronic.Generation;

public static class ChunkVisualizer {
  public static void SaveChunkVisualization(
    int chunkX,
    int chunkY,
    float[,] rawNoise,
    bool[,] boolGrid,
    bool[,] smoothedGrid,
    List<List<Vector2>> contours,
    CaveGenerationConfig config
  ) {
    var gridSize = rawNoise.GetLength(0);
    var cellPixelSize = 4; // 64 * 4 = 256 pixels per frame
    var frameWidth = gridSize * cellPixelSize;
    var frameHeight = gridSize * cellPixelSize;
    var totalHeight = frameHeight * 3; // 3 кадра вертикально

    // Создаем Image напрямую
    var image = Raylib.GenImageColor(frameWidth, totalHeight, Color.Black);

    // Кадр 1: Raw noise (grayscale)
    DrawNoiseToImage(ref image, rawNoise, 0, 0, cellPixelSize, config.Threshold);

    // Кадр 2: Boolean grid
    DrawBoolGridToImage(ref image, boolGrid, 0, frameHeight, cellPixelSize);

    // Кадр 3: Smoothed grid + contours
    DrawBoolGridToImage(ref image, smoothedGrid, 0, frameHeight * 2, cellPixelSize);
    DrawContoursToImage(ref image, contours, 0, frameHeight * 2, cellPixelSize);

    // Добавляем текстовые метки
    AddLabels(ref image, frameWidth, frameHeight);

    // Сохраняем
    Directory.CreateDirectory("Images");
    var filename = $"Images/chunk_{chunkX}_{chunkY}.png";
    Raylib.ExportImage(image, filename);
    Raylib.UnloadImage(image);

    var solidCount = CountSolid(smoothedGrid);
    var total = smoothedGrid.GetLength(0) * smoothedGrid.GetLength(1);
    Console.WriteLine($"Chunk ({chunkX},{chunkY}): {contours.Count} contours, {solidCount}/{total} solid ({100f * solidCount / total:F1}%) -> {filename}");
  }

  private static void DrawNoiseToImage(ref Image image, float[,] noise, int offsetX, int offsetY, int cellSize, float threshold) {
    var width = noise.GetLength(0);
    var height = noise.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var value = noise[x, y]; // [-1, 1]
        var normalized = (value + 1f) / 2f; // [0, 1]
        var gray = (byte)(normalized * 255);

        Color color;
        // ИНВЕРСИЯ: solid где шум НИЗКИЙ (< threshold)
        if (normalized < threshold) {
          // Solid - зеленоватый
          color = new Color(gray, (byte)255, gray, (byte)255);
        } else {
          // Empty - серый
          color = new Color(gray, gray, gray, (byte)255);
        }

        // Рисуем клетку
        for (int px = 0; px < cellSize; px++) {
          for (int py = 0; py < cellSize; py++) {
            Raylib.ImageDrawPixel(ref image, offsetX + x * cellSize + px, offsetY + y * cellSize + py, color);
          }
        }
      }
    }
  }

  private static void DrawBoolGridToImage(ref Image image, bool[,] grid, int offsetX, int offsetY, int cellSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var color = grid[x, y] ? Color.White : new Color(20, 20, 20, 255);

        // Рисуем клетку
        for (int px = 0; px < cellSize; px++) {
          for (int py = 0; py < cellSize; py++) {
            Raylib.ImageDrawPixel(ref image, offsetX + x * cellSize + px, offsetY + y * cellSize + py, color);
          }
        }
      }
    }
  }

  private static void DrawContoursToImage(ref Image image, List<List<Vector2>> contours, int offsetX, int offsetY, int cellSize) {
    foreach (var contour in contours) {
      if (contour.Count < 2) continue;

      // Рисуем линии между точками контура
      for (int i = 0; i < contour.Count; i++) {
        var start = contour[i];
        var end = contour[(i + 1) % contour.Count];

        var x1 = (int)(start.X * cellSize) + offsetX;
        var y1 = (int)(start.Y * cellSize) + offsetY;
        var x2 = (int)(end.X * cellSize) + offsetX;
        var y2 = (int)(end.Y * cellSize) + offsetY;

        // Простая линия через Bresenham (встроенная в Raylib)
        DrawLineOnImage(ref image, x1, y1, x2, y2, Color.Green);
      }
    }
  }

  private static void DrawLineOnImage(ref Image image, int x1, int y1, int x2, int y2, Color color) {
    // Простой алгоритм Bresenham
    var dx = Math.Abs(x2 - x1);
    var dy = Math.Abs(y2 - y1);
    var sx = x1 < x2 ? 1 : -1;
    var sy = y1 < y2 ? 1 : -1;
    var err = dx - dy;

    while (true) {
      if (x1 >= 0 && x1 < image.Width && y1 >= 0 && y1 < image.Height) {
        Raylib.ImageDrawPixel(ref image, x1, y1, color);
      }

      if (x1 == x2 && y1 == y2) break;

      var e2 = 2 * err;
      if (e2 > -dy) {
        err -= dy;
        x1 += sx;
      }
      if (e2 < dx) {
        err += dx;
        y1 += sy;
      }
    }
  }

  private static void AddLabels(ref Image image, int frameWidth, int frameHeight) {
    // Raylib.ImageDrawText не очень хорошо работает, поэтому просто рисуем рамки
    var borderColor = Color.Yellow;

    // Рамка вокруг каждого кадра
    for (int i = 0; i < 3; i++) {
      var y = i * frameHeight;
      // Верхняя линия
      for (int x = 0; x < frameWidth; x++) {
        Raylib.ImageDrawPixel(ref image, x, y, borderColor);
        Raylib.ImageDrawPixel(ref image, x, y + 1, borderColor);
      }
    }
  }

  private static int CountSolid(bool[,] grid) {
    var count = 0;
    for (int x = 0; x < grid.GetLength(0); x++) {
      for (int y = 0; y < grid.GetLength(1); y++) {
        if (grid[x, y]) count++;
      }
    }
    return count;
  }

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
