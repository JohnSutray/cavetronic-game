using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using PhysicsWorld = nkast.Aether.Physics2D.Dynamics.World;

namespace Cavetronic.Generation;

public class PhysicsBodyBuilder(PhysicsWorld physics, CaveGenerationConfig config) {
  // Создаёт физическое тело из ShapedShard (полигон гарантированно выпуклый после EnsureConvexShards)
  public Body? CreateBodyFromShard(ShapedShard shard) {
    if (shard.Polygon.Count < 3) return null;

    var body = physics.CreateBody(shard.Position, 0, BodyType.Static);

    try {
      var vertices = new Vertices(shard.Polygon);
      var fixture = body.CreatePolygon(vertices, config.Density);
      fixture.Friction = config.Friction;
      fixture.Restitution = config.Restitution;
    } catch (Exception e) {
      Console.WriteLine($"  [Physics] Failed to create shard: {e.Message}");
      physics.Remove(body);
      return null;
    }

    return body;
  }
}
