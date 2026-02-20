namespace Cavetronic;

public struct Blueprint { }

public struct BlueprintVertex {
  public float X;
  public float Y;
}

// ИСКЛЮЧЕНИЯ ДЛЯ ССЫЛОЧНЫХ ТИПОВ ВНУТРИ КОМПОНЕНТОВ:
// int[] Triangles — топология меша требует динамического размера; value-type здесь неприменим.
public struct BlueprintMesh {
  public int[] Triangles;      // плоский массив, тройки StableId вершин: [v0,v1,v2, v0,v1,v2, ...]
  public int SelectedId1;      // StableId первой выбранной вершины (0 = нет)
  public int SelectedId2;      // StableId второй выбранной вершины (0 = нет), добавлена позже
  public int HoveredVertexId;  // StableId вершины под курсором (0 = нет)
}
