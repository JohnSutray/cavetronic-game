using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Dynamics;
using Raylib_cs;

namespace Cavetronic.Systems;

public class DebugRenderSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float LineThickness = 0.1f; // Толщина линий в метрах
  private readonly Dictionary<Body, Color> _bodyColors = new();
  private int _colorIndex;

  private static readonly Color[] Palette = [
    new(0, 255, 0, 255),
    new(0, 200, 100, 255),
    new(100, 255, 0, 255),
    new(0, 255, 150, 255),
    new(50, 200, 50, 255),
    new(0, 150, 80, 255),
    new(80, 255, 80, 255),
    new(0, 180, 0, 255),
    new(30, 230, 120, 255),
    new(120, 200, 0, 255),
  ];

  public override void Tick(float dt) {
    foreach (var body in GameWorld.Physics.BodyList) {
      var color = GetBodyColor(body);
      foreach (var fixture in body.FixtureList) {
        switch (fixture.Shape) {
          case PolygonShape polygon:
            DrawPolygon(body, polygon, color);
            break;
          case CircleShape circle:
            DrawCircle(body, circle, color);
            break;
        }
      }
    }
  }

  private Color GetBodyColor(Body body) {
    if (_bodyColors.TryGetValue(body, out var color)) return color;
    color = Palette[_colorIndex % Palette.Length];
    _colorIndex++;
    _bodyColors[body] = color;
    return color;
  }

  private void DrawPolygon(Body body, PolygonShape polygon, Color color) {
    var vertices = polygon.Vertices;
    var pos = body.Position;
    float rot = body.Rotation;
    float cos = MathF.Cos(rot);
    float sin = MathF.Sin(rot);

    for (int i = 0; i < vertices.Count; i++) {
      int next = (i + 1) % vertices.Count;

      var v1 = vertices[i];
      var v2 = vertices[next];

      // Рисуем в мировых координатах (метры) - камера сама преобразует
      float x1 = pos.X + v1.X * cos - v1.Y * sin;
      float y1 = pos.Y + v1.X * sin + v1.Y * cos;
      float x2 = pos.X + v2.X * cos - v2.Y * sin;
      float y2 = pos.Y + v2.X * sin + v2.Y * cos;

      Raylib.DrawLineEx(new System.Numerics.Vector2(x1, y1), new System.Numerics.Vector2(x2, y2), LineThickness, color);
    }
  }

  private void DrawCircle(Body body, CircleShape circle, Color color) {
    // Рисуем в мировых координатах (метры) - камера сама преобразует
    float cx = body.Position.X + circle.Position.X;
    float cy = body.Position.Y + circle.Position.Y;
    float r = circle.Radius;

    // DrawRing рисует контур кольца (innerRadius, outerRadius)
    float halfThickness = LineThickness / 2f;
    Raylib.DrawRing(new System.Numerics.Vector2(cx, cy), r - halfThickness, r + halfThickness, 0, 360, 32, color);
  }
}