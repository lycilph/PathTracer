using Core.Math;

namespace Core.PhotonMapping.Sppm;

/// <summary>
/// Spatial hash grid for visible points.
/// Cell size should be near the current radius (fixed in 12.2).
/// </summary>
public sealed class VisiblePointGrid
{
    private readonly float _cellSize;
    private readonly float _invCellSize;

    private readonly Dictionary<long, List<VisiblePoint>> _cells = new();

    public VisiblePointGrid(float cellSize)
    {
        _cellSize = System.Math.Max(cellSize, 1e-6f);
        _invCellSize = 1f / _cellSize;
    }

    public void Clear() => _cells.Clear();

    public void Insert(VisiblePoint vp)
    {
        var (ix, iy, iz) = CellCoord(vp.Position);
        long key = Hash(ix, iy, iz);

        if (!_cells.TryGetValue(key, out var list))
        {
            list = new List<VisiblePoint>(8);
            _cells[key] = list;
        }

        list.Add(vp);
    }

    public IEnumerable<VisiblePoint> Query(Vec3 p)
    {
        var (ix, iy, iz) = CellCoord(p);

        // Check 27 neighboring cells
        for (int dz = -1; dz <= 1; dz++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    long key = Hash(ix + dx, iy + dy, iz + dz);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        for (int i = 0; i < list.Count; i++)
                            yield return list[i];
                    }
                }
    }

    private (int x, int y, int z) CellCoord(in Vec3 p)
    {
        int ix = (int)float.Floor(p.X * _invCellSize);
        int iy = (int)float.Floor(p.Y * _invCellSize);
        int iz = (int)float.Floor(p.Z * _invCellSize);
        return (ix, iy, iz);
    }

    private static long Hash(int x, int y, int z)
    {
        // 3D integer hash -> 64-bit key
        // Uses large primes; deterministic and fast.
        unchecked
        {
            long hx = (long)x * 73856093;
            long hy = (long)y * 19349663;
            long hz = (long)z * 83492791;
            return hx ^ hy ^ hz;
        }
    }
}