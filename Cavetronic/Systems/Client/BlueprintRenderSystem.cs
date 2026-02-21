using System.Numerics;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

// Рендерит меш редактора: рёбра, вершины, превью нового треугольника, курсор.
// Запускается внутри BeginMode2D/EndMode2D.
public class BlueprintRenderSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float VertexRadius = 0.2f;
  private const float LineThickness = 0.08f;
  private const float LineThicknessHover = 0.18f;
  private const float OriginRadius = 0.15f;
  private const float CursorRadius = 0.12f;

  private static readonly Color ColorEdge = new(0, 200, 255, 255);
  private static readonly Color ColorEdgeHover = new(255, 200, 60, 255);
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

  private readonly List<(int A, int B)> _edges = new();
  private readonly HashSet<(int, int)> _edgeSeen = new();

  // Промежуточное состояние для DrawMesh/DrawTrianglePreview — вынесено из lambda-тел в поля.
  private int[] _triangles = [];
  private int _selectedId1;
  private int _selectedId2;
  private int _hoveredVertexId;
  private int _hoveredEdgeA;
  private int _hoveredEdgeB;
  private float _cursorX;
  private float _cursorY;
  private bool _hasCursor;

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
    // Копируем данные меша в поля до вложенных query (ref параметр нельзя захватить в лямбду).
    // Поля вместо локальных переменных — лямбды ниже захватывают только `this`, без DisplayClass.
    _triangles      = mesh.Triangles;
    _selectedId1    = mesh.SelectedId1;
    _selectedId2    = mesh.SelectedId2;
    _hoveredVertexId = mesh.HoveredVertexId;
    _hoveredEdgeA   = mesh.HoveredEdgeA;
    _hoveredEdgeB   = mesh.HoveredEdgeB;

    // Рёбра
    BlueprintGeometry.PopulateEdges(_triangles, _edges, _edgeSeen);

    foreach (var (a, b) in _edges) {
      var (ax, ay) = BlueprintGeometry.GetVertexPos(a, GameWorld);
      var (bx, by) = BlueprintGeometry.GetVertexPos(b, GameWorld);

      var isHovered = (a == _hoveredEdgeA && b == _hoveredEdgeB)
        || (a == _hoveredEdgeB && b == _hoveredEdgeA);

      var color = isHovered ? ColorEdgeHover : ColorEdge;
      var thickness = isHovered ? LineThicknessHover : LineThickness;

      Raylib.DrawLineEx(new Vector2(ax, ay), new Vector2(bx, by), thickness, color);
    }

    // Вершины
    GameWorld.Ecs.Query(in _verticesQuery, (ref StableId stableId, ref BlueprintVertex vertex) => {
      var color = GetVertexColor(stableId.Id, _selectedId1, _selectedId2, _hoveredVertexId);
      Raylib.DrawCircleV(new Vector2(vertex.X, vertex.Y), VertexRadius, color);
    });

    // Превью нового треугольника (если 2 вершины выделены)
    DrawTrianglePreview();
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

  private void DrawTrianglePreview() {
    if (_selectedId1 == 0 || _selectedId2 == 0) {
      return;
    }

    _cursorX   = 0f;
    _cursorY   = 0f;
    _hasCursor = false;

    GameWorld.Ecs.Query(in _cursorQuery, (ref ControlSubjectInput<CursorInput> cursor) => {
      if (!cursor.Active && !cursor.PreviouslyActive) {
        return;
      }

      _cursorX   = cursor.Payload.WorldX;
      _cursorY   = cursor.Payload.WorldY;
      _hasCursor = true;
    });

    if (!_hasCursor) {
      return;
    }

    var (v1x, v1y) = BlueprintGeometry.GetVertexPos(_selectedId1, GameWorld);
    var (v2x, v2y) = BlueprintGeometry.GetVertexPos(_selectedId2, GameWorld);

    var isValid = BlueprintGeometry.IsValidNewVertex(
      _cursorX, _cursorY,
      _selectedId1, _selectedId2,
      _triangles, GameWorld
    );

    var previewColor = isValid ? ColorPreviewValid : ColorPreviewInvalid;

    Raylib.DrawLineEx(new Vector2(_cursorX, _cursorY), new Vector2(v1x, v1y), LineThickness, previewColor);
    Raylib.DrawLineEx(new Vector2(_cursorX, _cursorY), new Vector2(v2x, v2y), LineThickness, previewColor);
    Raylib.DrawLineEx(new Vector2(v1x, v1y), new Vector2(v2x, v2y), LineThickness, previewColor);
  }
}
