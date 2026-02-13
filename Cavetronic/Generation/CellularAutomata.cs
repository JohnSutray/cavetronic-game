namespace Cavetronic.Generation;

public static class CellularAutomata {
  // Сглаживание + опционально заполнение маленьких пустот
  public static bool[,] Smooth(bool[,] grid, int iterations, int solidThreshold, int maxVoidSize = 0) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var result = (bool[,])grid.Clone();

    // Итерации сглаживания
    for (int iter = 0; iter < iterations; iter++) {
      var temp = new bool[width, height];

      for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
          var neighbors = CountSolidNeighbors(result, x, y);
          temp[x, y] = neighbors >= solidThreshold;
        }
      }

      result = temp;
    }

    // "Задушить" маленькие пустоты после сглаживания
    if (maxVoidSize > 0) {
      result = FillSmallVoids(result, maxVoidSize);
    }

    return result;
  }

  // "Задушить" маленькие пустоты - все empty острова меньше заданного размера
  private static bool[,] FillSmallVoids(bool[,] grid, int maxVoidSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var result = (bool[,])grid.Clone();
    var visited = new bool[width, height];

    // Находим все empty острова
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        if (!result[x, y] && !visited[x, y]) {
          // Нашли новый empty остров
          var island = FloodFillEmptyIsland(result, visited, x, y);

          // Если остров слишком маленький - заполняем его
          if (island.Count <= maxVoidSize) {
            foreach (var (ix, iy) in island) {
              result[ix, iy] = true;
            }
          }
        }
      }
    }

    return result;
  }

  private static List<(int x, int y)> FloodFillEmptyIsland(bool[,] grid, bool[,] visited, int startX, int startY) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var cells = new List<(int x, int y)>();
    var queue = new Queue<(int x, int y)>();

    queue.Enqueue((startX, startY));
    visited[startX, startY] = true;

    while (queue.Count > 0) {
      var (x, y) = queue.Dequeue();
      cells.Add((x, y));

      var neighbors = new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) };
      foreach (var (nx, ny) in neighbors) {
        if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
            !grid[nx, ny] && !visited[nx, ny]) {
          visited[nx, ny] = true;
          queue.Enqueue((nx, ny));
        }
      }
    }

    return cells;
  }

  private static int CountSolidNeighbors(bool[,] grid, int x, int y) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var count = 0;

    for (int dx = -1; dx <= 1; dx++) {
      for (int dy = -1; dy <= 1; dy++) {
        if (dx == 0 && dy == 0) continue; // Skip center cell

        var nx = x + dx;
        var ny = y + dy;

        // Treat out-of-bounds as solid (walls at chunk edges)
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) {
          count++;
        } else if (grid[nx, ny]) {
          count++;
        }
      }
    }

    return count;
  }
}
