
using Core.Math;

namespace Core.Rendering.SPPM;

public readonly record struct GridCell(int X, int Y, int Z);

public sealed class PhotonHashGrid
{
    private readonly Dictionary<GridCell, List<int>> _cells = [];
    private readonly float _cellSize;

    public PhotonHashGrid(float cellSize)
    {
        _cellSize = cellSize;
    }

    private GridCell ToCell(Vec3 p)
    {
        return new GridCell(
            (int)MathF.Floor(p.X / _cellSize),
            (int)MathF.Floor(p.Y / _cellSize),
            (int)MathF.Floor(p.Z / _cellSize));
    }

    public void Insert(int visiblePointIndex, Vec3 position)
    {
        var cell = ToCell(position);

        if (!_cells.TryGetValue(cell, out var list))
        {
            list = [];
            _cells[cell] = list;
        }

        list.Add(visiblePointIndex);
    }

    public IEnumerable<int> Query(Vec3 position)
    {
        var c = ToCell(position);

        for (int z = -1; z <= 1; z++)
        for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
        {
            var key = new GridCell(c.X + x, c.Y + y, c.Z + z);

            if (_cells.TryGetValue(key, out var list))
            {
                foreach (var idx in list)
                    yield return idx;
            }
        }
    }
}
