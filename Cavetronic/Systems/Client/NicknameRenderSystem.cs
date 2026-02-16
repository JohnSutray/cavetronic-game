using System.Numerics;
using Arch.Core;
using nkast.Aether.Physics2D.Collision.Shapes;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class NicknameRenderSystem(GameWorld gameWorld, CameraSystem cameraSystem) : EcsSystem(gameWorld) {
  private const float LabelOffsetY = 2f;
  private const int FontSize = 20;

  private readonly QueryDescription _ownersQuery = new QueryDescription().WithAll<ControlOwner, StableId>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _ownersQuery, (ref ControlOwner owner, ref StableId stableId) => {
      if (!GameWorld.Nicknames.TryGetValue(stableId.Id, out var nickname)) {
        return;
      }

      if (!GameWorld.TryGetEntity(owner.SubjectId, out var subjectEntity)) {
        return;
      }

      var worldPos = GetLabelWorldPosition(subjectEntity);
      var screenPos = Raylib.GetWorldToScreen2D(worldPos, cameraSystem.Camera);

      var textWidth = Raylib.MeasureText(nickname, FontSize);
      Raylib.DrawText(nickname, (int)screenPos.X - textWidth / 2, (int)screenPos.Y, FontSize, Color.Green);
    });
  }

  private Vector2 GetLabelWorldPosition(Entity entity) {
    if (GameWorld.Ecs.Has<PhysicsBodyRef>(entity)) {
      ref var bodyRef = ref GameWorld.Ecs.Get<PhysicsBodyRef>(entity);
      var body = bodyRef.Body;
      var topY = GetBodyTopY(body);
      return new Vector2(body.Position.X, topY - LabelOffsetY);
    }

    if (GameWorld.Ecs.Has<Position>(entity)) {
      var pos = GameWorld.Ecs.Get<Position>(entity);
      return new Vector2(pos.X, pos.Y - LabelOffsetY);
    }

    return Vector2.Zero;
  }

  private static float GetBodyTopY(nkast.Aether.Physics2D.Dynamics.Body body) {
    var topY = body.Position.Y;
    var cos = MathF.Cos(body.Rotation);
    var sin = MathF.Sin(body.Rotation);

    foreach (var fixture in body.FixtureList) {
      switch (fixture.Shape) {
        case CircleShape circle: {
          var cy = body.Position.Y + circle.Position.X * sin + circle.Position.Y * cos - circle.Radius;
          topY = Math.Min(topY, cy);
          break;
        }

        case PolygonShape polygon: {
          for (var i = 0; i < polygon.Vertices.Count; i++) {
            var v = polygon.Vertices[i];
            var wy = body.Position.Y + v.X * sin + v.Y * cos;
            topY = Math.Min(topY, wy);
          }

          break;
        }
      }
    }

    return topY;
  }
}
