namespace Cavetronic;

public struct CursorInput {
  public float WorldX;
  public float WorldY;
}

public struct CursorLeftMoveAction {
  public float StartX;
  public float StartY;
  public float EndX;
  public float EndY;
}

// Эмитируется только при RMB-клике (без drag). Пан камеры — клиентская сторона, вне ECS-инпута.
public struct CursorRightClickAction {
  public float WorldX;
  public float WorldY;
}

public struct ShiftModifier { }

public struct InputExclusive {
  public int OwnerStableId;
}
