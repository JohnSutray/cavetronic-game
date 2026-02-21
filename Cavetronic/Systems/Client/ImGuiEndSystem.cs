using rlImGui_cs;

namespace Cavetronic.Systems.Client;

public class ImGuiEndSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  public override void Tick(float dt) {
    rlImGui.End();
  }
}
