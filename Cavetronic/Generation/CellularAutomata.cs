namespace Cavetronic.Generation;

public static class CellularAutomata {
  public static bool[,] Smooth(bool[,] grid, int iterations, int solidThreshold) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var result = (bool[,])grid.Clone();

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

    return result;
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
