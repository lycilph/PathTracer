# CLI snippet: placing a mesh using Scale + Translate

Example:

```csharp
var cube = TriangleMesh.CreateUnitCube(white);
IHittable cubePlaced = new Translate(
    new Scale(cube, 165f),
    new Vec3(190f, 82.5f, 150f));

worldList.Add(cubePlaced);
```

This places a 165x165x165 cube with its center at (190, 82.5, 150).
