using MemoryPack;

namespace Cavetronic;

[MemoryPackable]
public partial struct NetworkId {
  public int Id;
}

[MemoryPackable]
public partial struct Position {
  public float X;
  public float Y;
}

[MemoryPackable]
public partial struct Rotation {
  public float Angle;
}

[MemoryPackable]
public partial struct Collider {
  public float[] Vertices;
}

[MemoryPackable]
public partial struct CameraTarget {
  // Маркер для entity, за которым следит камера
}