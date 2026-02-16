using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class CameraStartSystem(GameWorld gameWorld, CameraSystem cameraSystem) : EcsSystem(gameWorld) {
  public override void Tick(float dt) {
    Raylib.BeginMode2D(cameraSystem.Camera);
  }
}