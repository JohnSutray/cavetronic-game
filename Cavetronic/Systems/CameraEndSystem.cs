using Raylib_cs;

namespace Cavetronic.Systems;

public class CameraEndSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  public override void Tick(float dt) {
    Raylib.EndMode2D();
  }
}
