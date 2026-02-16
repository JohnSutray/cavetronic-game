using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class InputSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<Player, ControlOwner>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _playersQuery, (ref ControlOwner owner) => {
      owner.Input = 0;

      if (Raylib.IsKeyDown(KeyboardKey.W)) owner.Input |= (ulong)InputSignal.Up;
      if (Raylib.IsKeyDown(KeyboardKey.S)) owner.Input |= (ulong)InputSignal.Down;
      if (Raylib.IsKeyDown(KeyboardKey.A)) owner.Input |= (ulong)InputSignal.Left;
      if (Raylib.IsKeyDown(KeyboardKey.D)) owner.Input |= (ulong)InputSignal.Right;
      if (Raylib.IsKeyPressed(KeyboardKey.Space)) owner.Input |= (ulong)InputSignal.Action1;
      if (Raylib.IsKeyPressed(KeyboardKey.E)) owner.Input |= (ulong)InputSignal.Action2;
      if (Raylib.IsKeyPressed(KeyboardKey.F)) owner.Input |= (ulong)InputSignal.Action3;
    });
  }
}
