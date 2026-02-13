using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;

namespace Cavetronic.Systems;

public class TerrainSystem : EcsSystem {
  public TerrainSystem(GameWorld gameWorld) : base(gameWorld) {
  }

  public override void Init() {
    GenerateCave();
    GenerateRocks(10);
  }

  private void GenerateCave() {
    // Верхняя стена (сталактиты)
    float[] topHeights = { 2f, 2.5f, 1.8f, 3f, 2.2f, 1.5f, 2.8f, 2f };
    for (int i = 0; i < topHeights.Length; i++) {
      CreateWallSegment(i * 2f, 0, 2f, topHeights[i]);
    }

    // Нижняя стена (сталагмиты)
    float[] bottomHeights = { 2f, 1.5f, 2.5f, 1.8f, 3f, 2.2f, 1.8f, 2.5f };
    float floorY = 10f;
    for (int i = 0; i < bottomHeights.Length; i++) {
      CreateWallSegment(i * 2f, floorY, 2f, bottomHeights[i]);
    }

    // Левая и правая стены
    CreateWallSegment(-2f, 0, 2f, 12f);
    CreateWallSegment(16f, 0, 2f, 12f);

    // Сталактит по центру
    CreateTriangle(new Vector2(7f, 0f), new Vector2(8f, 0f), new Vector2(7.5f, 4f));

    // Сталагмит по центру
    CreateTriangle(new Vector2(9f, 12f), new Vector2(10f, 12f), new Vector2(9.5f, 8.5f));
  }

  private void CreateWallSegment(float x, float y, float width, float height) {
    var center = new Vector2(x + width / 2, y + height / 2);
    var body = GameWorld.Physics.CreateBody(center, 0, BodyType.Static);
    body.CreateRectangle(width, height, 1f, Vector2.Zero);
  }

  private void CreateTriangle(Vector2 a, Vector2 b, Vector2 c) {
    var center = (a + b + c) / 3f;
    var vertices = new Vertices(new[] {
      a - center,
      b - center,
      c - center
    });
    var body = GameWorld.Physics.CreateBody(center, 0, BodyType.Static);
    body.CreatePolygon(vertices, 1f);
  }

  private void GenerateRocks(int count) {
    var random = new Random(42);
    for (int i = 0; i < count; i++) {
      float x = 2f + (float)random.NextDouble() * 12f;
      float y = 3f + (float)random.NextDouble() * 2f;
      float radius = 0.15f + (float)random.NextDouble() * 0.2f;

      var rock = GameWorld.Physics.CreateBody(new Vector2(x, y), 0, BodyType.Dynamic);
      var fixture = rock.CreateCircle(radius, 1f);
      fixture.Restitution = 0.3f;
    }
  }
}