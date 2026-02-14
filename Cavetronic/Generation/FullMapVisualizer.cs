using nkast.Aether.Physics2D.Common;
using Raylib_cs;

namespace Cavetronic.Generation;

public class FullMapVisualizer(CaveGenerationConfig config) {
  private readonly Dictionary<(int x, int y), ChunkData> _chunks = new();

  public void AddChunk(int chunkX, int chunkY, Chunk chunk, ChunkDebugData debugData) {
    var shards = chunk.Islands
      .SelectMany(i => i.Shards)
      .Select(s => s.Polygon.Select(p => p + s.Position).ToList())
      .ToList();

    _chunks[(chunkX, chunkY)] = new ChunkData {
      SmoothedGrid = debugData.SmoothedGrid,
      Shards = shards
    };
  }

  public void SaveFullMap() {
    if (_chunks.Count == 0) return;

    var minX = _chunks.Keys.Min(k => k.x);
    var maxX = _chunks.Keys.Max(k => k.x);
    var minY = _chunks.Keys.Min(k => k.y);
    var maxY = _chunks.Keys.Max(k => k.y);

    var chunksX = maxX - minX + 1;
    var chunksY = maxY - minY + 1;

    var chunkPixelSize = config.ChunkSize * 8;
    var fullWidth = chunksX * chunkPixelSize;
    var fullHeight = chunksY * chunkPixelSize;

    var image = Raylib.GenImageColor(fullWidth, fullHeight, Color.Black);

    foreach (var kvp in _chunks) {
      var (chunkX, chunkY) = kvp.Key;
      var data = kvp.Value;

      var offsetX = (chunkX - minX) * chunkPixelSize;
      var offsetY = (chunkY - minY) * chunkPixelSize;
      var cellPixelSize = chunkPixelSize / config.ChunkSize;

      DrawBoolGridToImage(ref image, data.SmoothedGrid, offsetX, offsetY, cellPixelSize);

      var localShards = data.Shards.Select(shard =>
        shard.Select(p => p - new Vector2(chunkX * config.ChunkSize, chunkY * config.ChunkSize)).ToList()
      ).ToList();

      DrawShardsToImage(ref image, localShards, offsetX, offsetY, cellPixelSize);
    }

    DrawChunkBorders(ref image, chunksX, chunksY, chunkPixelSize);

    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    var sourcePath = System.IO.Path.Combine(baseDir, "..", "..", "..", "Images");
    var fullSourcePath = System.IO.Path.GetFullPath(sourcePath);
    Directory.CreateDirectory(fullSourcePath);
    var filename = System.IO.Path.Combine(fullSourcePath, "full_map.png");

    Raylib.ExportImage(image, filename);
    Raylib.UnloadImage(image);

    Console.WriteLine($"Saved: {filename} ({fullWidth}x{fullHeight})");
  }

  private void DrawBoolGridToImage(ref Image image, bool[,] grid, int offsetX, int offsetY, int cellSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var color = grid[x, y] ? Color.Black : Color.White;

        for (int px = 0; px < cellSize; px++) {
          for (int py = 0; py < cellSize; py++) {
            Raylib.ImageDrawPixel(ref image, offsetX + x * cellSize + px, offsetY + y * cellSize + py, color);
          }
        }
      }
    }
  }

  private void DrawShardsToImage(ref Image image, List<List<Vector2>> shards, int offsetX, int offsetY, int cellSize) {
    foreach (var shard in shards) {
      if (shard.Count < 3) continue;

      for (int i = 0; i < shard.Count; i++) {
        var start = shard[i];
        var end = shard[(i + 1) % shard.Count];

        var x1 = (int)(start.X * cellSize) + offsetX;
        var y1 = (int)(start.Y * cellSize) + offsetY;
        var x2 = (int)(end.X * cellSize) + offsetX;
        var y2 = (int)(end.Y * cellSize) + offsetY;

        var lineColor = new Color(0, 255, 0, 255);
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

  private void DrawChunkBorders(ref Image image, int chunksX, int chunksY, int chunkPixelSize) {
    var borderColor = Color.Red;

    for (int cx = 0; cx <= chunksX; cx++) {
      var x = cx * chunkPixelSize;
      for (int y = 0; y < chunksY * chunkPixelSize; y++) {
        if (x >= 0 && x < image.Width && y >= 0 && y < image.Height) {
          Raylib.ImageDrawPixel(ref image, x, y, borderColor);
          if (x + 1 < image.Width) Raylib.ImageDrawPixel(ref image, x + 1, y, borderColor);
        }
      }
    }

    for (int cy = 0; cy <= chunksY; cy++) {
      var y = cy * chunkPixelSize;
      for (int x = 0; x < chunksX * chunkPixelSize; x++) {
        if (x >= 0 && x < image.Width && y >= 0 && y < image.Height) {
          Raylib.ImageDrawPixel(ref image, x, y, borderColor);
          if (y + 1 < image.Height) Raylib.ImageDrawPixel(ref image, x, y + 1, borderColor);
        }
      }
    }
  }

  private class ChunkData {
    public bool[,] SmoothedGrid = null!;
    public List<List<Vector2>> Shards = null!;
  }
}
