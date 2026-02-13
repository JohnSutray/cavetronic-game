using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Common.Decomposition;
using nkast.Aether.Physics2D.Dynamics;
using PhysicsWorld = nkast.Aether.Physics2D.Dynamics.World;

namespace Cavetronic.Generation;

public class PhysicsBodyBuilder(PhysicsWorld physics, CaveGenerationConfig config) {
  public Body? CreateBodyFromRegion(List<Vector2> contour) {
    if (contour.Count < 3) return null;

    // Вычисляем центр региона
    var center = CalculateCenter(contour);

    // Создаем статическое тело
    var body = physics.CreateBody(center, 0, BodyType.Static);

    try {
      // Конвертируем в локальные координаты
      var localVertices = new Vertices(contour.Select(v => v - center).ToArray());

      // Разбиваем на выпуклые части
      var convexParts = Triangulate.ConvexPartition(
        localVertices,
        TriangulationAlgorithm.Bayazit
      );

      int fixtureCount = 0;
      // Создаем fixture для каждой выпуклой части
      foreach (var part in convexParts) {
        if (part.Count >= 3) {
          var fixture = body.CreatePolygon(part, config.Density);
          fixture.Friction = config.Friction;
          fixture.Restitution = config.Restitution;
          fixtureCount++;
        }
      }

      if (fixtureCount > 5) {
        Console.WriteLine($"  [Physics] Created body with {fixtureCount} fixtures from {contour.Count} vertices");
      }
    } catch (Exception e) {
      // Если разбиение не удалось, удаляем тело
      Console.WriteLine($"  [Physics] Failed to create body: {e.Message}");
      physics.Remove(body);
      return null;
    }

    return body;
  }

  private static Vector2 CalculateCenter(List<Vector2> vertices) {
    var sum = Vector2.Zero;
    foreach (var v in vertices) {
      sum += v;
    }
    return sum / vertices.Count;
  }
}
