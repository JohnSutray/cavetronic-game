using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using PhysicsWorld = nkast.Aether.Physics2D.Dynamics.World;

namespace Cavetronic.Generation;

public class PhysicsBodyBuilder(PhysicsWorld physics, CaveGenerationConfig config) {
  // Создаёт физическое тело из ShapedShard через fan triangulation от центроида
  public Body? CreateBodyFromShard(ShapedShard shard) {
    if (shard.Polygon.Count < 3) return null;

    var body = physics.CreateBody(shard.Position, 0, BodyType.Static);

    try {
      // Fan triangulation от центроида: безопасно для любых полигонов, нет рекурсии
      var centroid = Vector2.Zero;
      foreach (var v in shard.Polygon) {
        centroid += v;
      }
      centroid /= shard.Polygon.Count;

      var fixtureCount = 0;

      for (var i = 0; i < shard.Polygon.Count; i++) {
        var v1 = shard.Polygon[i];
        var v2 = shard.Polygon[(i + 1) % shard.Polygon.Count];
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
}
