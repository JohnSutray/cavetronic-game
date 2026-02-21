using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

// Оверлей с диагностикой памяти и ECS. Запускается между ImGuiBeginSystem и ImGuiEndSystem.
public class MemoryStatsOverlaySystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private static readonly ImGuiWindowFlags OverlayFlags =
    ImGuiWindowFlags.NoDecoration |
    ImGuiWindowFlags.NoMove |
    ImGuiWindowFlags.NoSavedSettings |
    ImGuiWindowFlags.NoFocusOnAppearing |
    ImGuiWindowFlags.NoNav |
    ImGuiWindowFlags.AlwaysAutoResize;

  private string _line1 = "";
  private string _line2 = "";
  private string _line3 = "";
  private string _line4 = "";
  private float _timer;
  private const float Interval = 1f;

  public override void Tick(float dt) {
    _timer += dt;

    if (_timer >= Interval) {
      RefreshStats();
    }

    ImGui.SetNextWindowPos(new Vector2(Raylib.GetScreenWidth() - 200, 10), ImGuiCond.Always);
    ImGui.SetNextWindowBgAlpha(0.65f);

    if (ImGui.Begin("##mem", OverlayFlags)) {
      ImGui.Text(_line1);
      ImGui.Text(_line2);
      ImGui.Separator();
      ImGui.Text(_line3);
      ImGui.Text(_line4);
      ImGui.Separator();

      if (ImGui.Button("GC Gen2")) {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        RefreshStats();
      }
    }

    ImGui.End();
  }

  private void RefreshStats() {
    _timer = 0f;

    var heap      = GC.GetTotalMemory(false);
    var alloc     = GC.GetTotalAllocatedBytes(false);
    var gen0      = GC.CollectionCount(0);
    var gen1      = GC.CollectionCount(1);
    var gen2      = GC.CollectionCount(2);
    var entities  = GameWorld.Ecs.Size;
    var archetypes = GameWorld.Ecs.Archetypes.Count;

    _line1 = $"Heap:  {heap / 1024f / 1024f:F2} MB";
    _line2 = $"Alloc: {alloc / 1024f / 1024f:F1} MB total";
    _line3 = $"GC g0:{gen0} g1:{gen1} g2:{gen2}";
    _line4 = $"ECS  e:{entities}  arch:{archetypes}";
  }
}
