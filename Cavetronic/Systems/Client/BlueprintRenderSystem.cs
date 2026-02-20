using System.Numerics;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

// Рендерит меш редактора: рёбра, вершины, превью нового треугольника, курсор.
// Запускается внутри BeginMode2D/EndMode2D.
public class BlueprintRenderSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float VertexRadius = 0.2f;
  private const float LineThickness = 0.08f;
  private const float OriginRadius = 0.15f;
  private const float CursorRadius = 0.12f;

  private static readonly Color ColorEdge = new(0, 200, 255, 255);
  private static readonly Color ColorVertex = new(80, 140, 255, 255);
  private static readonly Color ColorVertexHover = new(255, 220, 80, 255);
  private static readonly Color ColorVertexSelected = new(255, 255, 255, 255);
  private static readonly Color ColorPreviewValid = new(0, 255, 120, 200);
  private static readonly Color ColorPreviewInvalid = new(255, 60, 60, 200);
  private static readonly Color ColorOrigin = new(255, 60, 60, 255);
  private static readonly Color ColorCursor = new(200, 200, 200, 200);

  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<Blueprint, BlueprintMesh>();

  private readonly QueryDescription _cursorQuery =
    new QueryDescription().WithAll<Blueprint, ControlSubjectInput<CursorInput>>();

  private readonly QueryDescription _verticesQuery =
    new QueryDescription().WithAll<StableId, BlueprintVertex>();

  public override void Tick(float dt) {
    // Красная точка в центре мира (0,0)
    Raylib.DrawCircleV(Vector2.Zero, OriginRadius, ColorOrigin);

    GameWorld.Ecs.Query(in _blueprintQuery, (ref BlueprintMesh mesh) => {
      DrawMesh(ref mesh);
    });

    // Курсор
    GameWorld.Ecs.Query(in _cursorQuery, (ref ControlSubjectInput<CursorInput> cursor) => {
      if (!cursor.Active && !cursor.PreviouslyActive) return;
      Raylib.DrawCircleV(
        new Vector2(cursor.Payload.WorldX, cursor.Payload.WorldY),
        CursorRadius,
        ColorCursor
      );
    });
  }

  private void DrawMesh(ref BlueprintMesh mesh) {
    // Копируем данные меша до вложенных query (ref параметр нельзя захватить в лямбду)
    var triangles = mesh.Triangles;
    var selectedId1 = mesh.SelectedId1;
    var selectedId2 = mesh.SelectedId2;
    var hoveredId = mesh.HoveredVertexId;

    // Рёбра
    foreach (var (a, b) in BlueprintGeometry.GetEdges(triangles)) {
      var (ax, ay) = BlueprintGeometry.GetVertexPos(a, GameWorld);
      var (bx, by) = BlueprintGeometry.GetVertexPos(b, GameWorld);
      Raylib.DrawLineEx(new Vector2(ax, ay), new Vector2(bx, by), LineThickness, ColorEdge);
    }

    // Вершины
    GameWorld.Ecs.Query(in _verticesQuery, (ref StableId stableId, ref BlueprintVertex vertex) => {
      var color = GetVertexColor(stableId.Id, selectedId1, selectedId2, hoveredId);
      Raylib.DrawCircleV(new Vector2(vertex.X, vertex.Y), VertexRadius, color);
    });

    // Превью нового треугольника (если 2 вершины выделены)
    DrawTrianglePreview(selectedId1, selectedId2, triangles);
  }

  private static Color GetVertexColor(int stableId, int selectedId1, int selectedId2, int hoveredId) {
    if (stableId == selectedId1 || stableId == selectedId2) {
      return ColorVertexSelected;
    }

    if (stableId == hoveredId) {
      return ColorVertexHover;
    }

    return ColorVertex;
  }

  private void DrawTrianglePreview(int selectedId1, int selectedId2, int[] triangles) {
    if (selectedId1 == 0 || selectedId2 == 0) {
      return;
    }

    var cursorX = 0f;
    var cursorY = 0f;
    var hasCursor = false;

    GameWorld.Ecs.Query(in _cursorQuery, (ref ControlSubjectInput<CursorInput> cursor) => {
      if (!cursor.Active && !cursor.PreviouslyActive) {
        return;
      }

      cursorX = cursor.Payload.WorldX;
      cursorY = cursor.Payload.WorldY;
      hasCursor = true;
    });

    if (!hasCursor) {
      return;
    }

    var (v1x, v1y) = BlueprintGeometry.GetVertexPos(selectedId1, GameWorld);
    var (v2x, v2y) = BlueprintGeometry.GetVertexPos(selectedId2, GameWorld);

    var isValid = BlueprintGeometry.IsValidNewVertex(
      cursorX, cursorY,
      selectedId1, selectedId2,
      triangles, GameWorld
    );

    var previewColor = isValid ? ColorPreviewValid : ColorPreviewInvalid;

    Raylib.DrawLineEx(new Vector2(cursorX, cursorY), new Vector2(v1x, v1y), LineThickness, previewColor);
    Raylib.DrawLineEx(new Vector2(cursorX, cursorY), new Vector2(v2x, v2y), LineThickness, previewColor);
    Raylib.DrawLineEx(new Vector2(v1x, v1y), new Vector2(v2x, v2y), LineThickness, previewColor);
  }
}
