using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using PhysicsWorld = nkast.Aether.Physics2D.Dynamics.World;

namespace Cavetronic.Generation;

public class PhysicsBodyBuilder(PhysicsWorld physics, CaveGenerationConfig config) {
  // Создаёт физическое тело из осколка через fan triangulation от центроида
  public Body? CreateBodyFromShard(List<Vector2> shard) {
    if (shard.Count < 3) {
      return null;
    }

    var center = CalculateCenter(shard);
    var body = physics.CreateBody(center, 0, BodyType.Static);

    try {
      var local = shard.Select(v => v - center).ToList();

      // Fan triangulation от центроида: безопасно для любых полигонов, нет рекурсии
      var centroid = Vector2.Zero;
      foreach (var v in local) centroid += v;
      centroid /= local.Count;

      var fixtureCount = 0;
      for (var i = 0; i < local.Count; i++) {
        var v1 = local[i];
        var v2 = local[(i + 1) % local.Count];
        var tri = new Vertices { centroid, v1, v2 };

        // Проверяем площадь треугольника (пропускаем вырожденные)
        var cross = (v1.X - centroid.X) * (v2.Y - centroid.Y) - (v1.Y - centroid.Y) * (v2.X - centroid.X);
        if (MathF.Abs(cross) < 0.01f) continue;

        try {
          var fixture = body.CreatePolygon(tri, config.Density);
          fixture.Friction = config.Friction;
          fixture.Restitution = config.Restitution;
          fixtureCount++;
        } catch {
          // Пропускаем вырожденный треугольник
        }
      }

      if (fixtureCount == 0) {
        physics.Remove(body);
        return null;
      }
    } catch (Exception e) {
      Console.WriteLine($"  [Physics] Failed to create shard: {e.Message}");
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
