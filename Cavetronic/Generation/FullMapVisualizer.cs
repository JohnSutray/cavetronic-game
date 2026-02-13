using nkast.Aether.Physics2D.Common;
using Raylib_cs;

namespace Cavetronic.Generation;

public class FullMapVisualizer {
  private readonly Dictionary<(int x, int y), ChunkData> _chunks = new();
  private readonly CaveGenerationConfig _config;

  public FullMapVisualizer(CaveGenerationConfig config) {
    _config = config;
  }

  public void AddChunk(int chunkX, int chunkY, float[,] rawNoise, bool[,] boolGrid, bool[,] smoothedGrid, List<List<Vector2>> contours) {
    _chunks[(chunkX, chunkY)] = new ChunkData {
      RawNoise = rawNoise,
      BoolGrid = boolGrid,
      SmoothedGrid = smoothedGrid,
      Contours = contours
    };
  }

  public void SaveFullMap() {
    if (_chunks.Count == 0) return;

    // Находим границы
    var minX = _chunks.Keys.Min(k => k.x);
    var maxX = _chunks.Keys.Max(k => k.x);
    var minY = _chunks.Keys.Min(k => k.y);
    var maxY = _chunks.Keys.Max(k => k.y);

    var chunksX = maxX - minX + 1;
    var chunksY = maxY - minY + 1;

    var chunkPixelSize = _config.ChunkSize * 8; // 64 * 8 = 512 pixels per chunk (увеличили для лучшей видимости)
    var fullWidth = chunksX * chunkPixelSize;
    var fullHeight = chunksY * chunkPixelSize;

    // Создаем 4 больших изображения (raw, bool, smoothed, polygons) горизонтально
    var totalWidth = fullWidth * 4;
    var image = Raylib.GenImageColor(totalWidth, fullHeight, Color.Black);

    // Рисуем каждый чанк
    foreach (var kvp in _chunks) {
      var (chunkX, chunkY) = kvp.Key;
      var data = kvp.Value;

      var offsetX = (chunkX - minX) * chunkPixelSize;
      var offsetY = (chunkY - minY) * chunkPixelSize;

      var cellPixelSize = chunkPixelSize / _config.ChunkSize; // 512 / 64 = 8 pixels per cell

      // Кадр 1: Raw noise
      DrawNoiseToImage(ref image, data.RawNoise, offsetX, offsetY, cellPixelSize, _config.Threshold);

      // Кадр 2: Boolean grid
      DrawBoolGridToImage(ref image, data.BoolGrid, offsetX + fullWidth, offsetY, cellPixelSize);

      // Кадр 3: Smoothed grid
      DrawBoolGridToImage(ref image, data.SmoothedGrid, offsetX + fullWidth * 2, offsetY, cellPixelSize);

      // Кадр 4: Smoothed grid + контуры для наглядности
      DrawBoolGridToImage(ref image, data.SmoothedGrid, offsetX + fullWidth * 3, offsetY, cellPixelSize);

      // Контуры уже в мировых координатах (в физических единицах), преобразуем в локальные для отрисовки
      var localContours = data.Contours.Select(contour =>
        contour.Select(p => p - new Vector2(chunkX * _config.ChunkSize * _config.CellSize, chunkY * _config.ChunkSize * _config.CellSize)).ToList()
      ).ToList();

      DrawContoursToImage(ref image, localContours, offsetX + fullWidth * 3, offsetY, cellPixelSize);
    }

    // Рисуем границы чанков
    DrawChunkBorders(ref image, chunksX, chunksY, chunkPixelSize, fullWidth);

    // Добавляем метки
    DrawLabels(ref image, fullWidth, fullHeight);

    // Сохраняем в папку с исходниками
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    var sourcePath = System.IO.Path.Combine(baseDir, "..", "..", "..", "Images");
    var fullSourcePath = System.IO.Path.GetFullPath(sourcePath);
    Directory.CreateDirectory(fullSourcePath);
    var filename = System.IO.Path.Combine(fullSourcePath, "full_map.png");

    Raylib.ExportImage(image, filename);
    Raylib.UnloadImage(image);

    Console.WriteLine($"Saved: {filename} ({totalWidth}x{fullHeight})");
  }

  private void DrawNoiseToImage(ref Image image, float[,] noise, int offsetX, int offsetY, int cellSize, float threshold) {
    var width = noise.GetLength(0);
    var height = noise.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var value = noise[x, y];
        var normalized = (value + 1f) / 2f;
        var gray = (byte)(normalized * 255);

        Color color;
        if (normalized < threshold) {
          color = new Color(gray, (byte)255, gray, (byte)255); // Solid - зеленый
        } else {
          color = new Color(gray, gray, gray, (byte)255); // Empty - серый
        }

        for (int px = 0; px < cellSize; px++) {
          for (int py = 0; py < cellSize; py++) {
            Raylib.ImageDrawPixel(ref image, offsetX + x * cellSize + px, offsetY + y * cellSize + py, color);
          }
        }
      }
    }
  }

  private void DrawBoolGridToImage(ref Image image, bool[,] grid, int offsetX, int offsetY, int cellSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var color = grid[x, y] ? Color.White : new Color(20, 20, 20, 255);

        for (int px = 0; px < cellSize; px++) {
          for (int py = 0; py < cellSize; py++) {
            Raylib.ImageDrawPixel(ref image, offsetX + x * cellSize + px, offsetY + y * cellSize + py, color);
          }
        }
      }
    }
  }

  private void DrawContoursToImage(ref Image image, List<List<Vector2>> contours, int offsetX, int offsetY, int cellSize) {
    foreach (var contour in contours) {
      if (contour.Count < 2) continue;

      for (int i = 0; i < contour.Count; i++) {
        var start = contour[i]; // Локальные координаты в физических единицах (уже умножены на cellSize)
        var end = contour[(i + 1) % contour.Count];

        // Конвертируем физические единицы в пиксели (cellSize пикселей на физическую единицу)
        var x1 = (int)(start.X / _config.CellSize * cellSize) + offsetX;
        var y1 = (int)(start.Y / _config.CellSize * cellSize) + offsetY;
        var x2 = (int)(end.X / _config.CellSize * cellSize) + offsetX;
        var y2 = (int)(end.Y / _config.CellSize * cellSize) + offsetY;

        // Рисуем толстую яркую линию
        var lineColor = new Color(0, 255, 0, 255); // Яркий зеленый
        for (int offset = -2; offset <= 2; offset++) {
          DrawLineOnImage(ref image, x1 + offset, y1, x2 + offset, y2, lineColor);
          DrawLineOnImage(ref image, x1, y1 + offset, x2, y2 + offset, lineColor);
        }
      }
    }
  }

  private void DrawLineOnImage(ref Image image, int x1, int y1, int x2, int y2, Color color) {
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

  private void DrawChunkBorders(ref Image image, int chunksX, int chunksY, int chunkPixelSize, int fullWidth) {
    var borderColor = Color.Red;

    // Вертикальные линии границ чанков
    for (int section = 0; section < 4; section++) {
      var sectionOffsetX = section * fullWidth;

      for (int cx = 0; cx <= chunksX; cx++) {
        var x = sectionOffsetX + cx * chunkPixelSize;
        for (int y = 0; y < chunksY * chunkPixelSize; y++) {
          if (x >= 0 && x < image.Width && y >= 0 && y < image.Height) {
            Raylib.ImageDrawPixel(ref image, x, y, borderColor);
            if (x + 1 < image.Width) Raylib.ImageDrawPixel(ref image, x + 1, y, borderColor);
          }
        }
      }

      // Горизонтальные линии границ чанков
      for (int cy = 0; cy <= chunksY; cy++) {
        var y = cy * chunkPixelSize;
        for (int x = sectionOffsetX; x < sectionOffsetX + fullWidth; x++) {
          if (x >= 0 && x < image.Width && y >= 0 && y < image.Height) {
            Raylib.ImageDrawPixel(ref image, x, y, borderColor);
            if (y + 1 < image.Height) Raylib.ImageDrawPixel(ref image, x, y + 1, borderColor);
          }
        }
      }
    }
  }

  private void DrawPolygonsToImage(ref Image image, bool[,] grid, int offsetX, int offsetY, int cellSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        // Рисуем SOLID клетки (стены) зелёным цветом
        if (grid[x, y]) {
          var color = Color.Green;
          for (int px = 0; px < cellSize; px++) {
            for (int py = 0; py < cellSize; py++) {
              Raylib.ImageDrawPixel(ref image, offsetX + x * cellSize + px, offsetY + y * cellSize + py, color);
            }
          }
        }
      }
    }
  }

  private void DrawLabels(ref Image image, int frameWidth, int frameHeight) {
    var labelColor = Color.Yellow;

    // Разделители между секциями
    for (int i = 1; i < 4; i++) {
      var x = i * frameWidth;
      for (int y = 0; y < frameHeight; y++) {
        for (int offset = 0; offset < 3; offset++) {
          if (x + offset < image.Width) {
            Raylib.ImageDrawPixel(ref image, x + offset, y, labelColor);
          }
        }
      }
    }
  }

  private class ChunkData {
    public float[,] RawNoise = null!;
    public bool[,] BoolGrid = null!;
    public bool[,] SmoothedGrid = null!;
    public List<List<Vector2>> Contours = null!;
  }
}
