using Raylib_cs;

namespace Cavetronic.Systems;

public class ScreenshotSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private int _frameCount = 0;
  private bool _screenshotTaken = false;
  private readonly int _frameDelay = 60; // Делаем скриншот через 60 кадров (~0.5 сек после запуска)

  public override void Tick(float dt) {
    if (_screenshotTaken) return;

    _frameCount++;

    if (_frameCount >= _frameDelay) {
      TakeScreenshot();
      _screenshotTaken = true;
    }
  }

  private void TakeScreenshot() {
    Directory.CreateDirectory("../../../Images");
    var filename = "../../../Images/game_screenshot.png";
    Raylib.TakeScreenshot(filename);
    Console.WriteLine($"Screenshot saved: {filename}");
  }
}
