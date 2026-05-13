using Core.Math;

namespace Core.Rendering.Sppm;

/// <summary>
/// Compact spatial hash grid built over the photons emitted each SPPM iteration.
/// Construction is O(M).  A radius query checks all grid cells overlapping the
/// query sphere and returns the subset whose photons fall within the exact radius.
///
/// Cell size = 2 × maxRadius so that a sphere of radius maxRadius overlaps at
/// most a 3×3×3 = 27-cell neighbourhood.
/// </summary>
public sealed class PhotonHashGrid
{
    private StoredPhoton[] _photons = [];

    // Flat hash table implemented as a singly-linked list per bucket.
    // _head[h]  → index of first photon in bucket h  (-1 = empty)
    // _next[i]  → index of next photon in same bucket (-1 = end)
    private int[] _head = [];
    private int[] _next = [];

    private float _cellSize;
    private float _invCellSize;
    private int   _tableSize;

    public int PhotonCount => _photons.Length;

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates the grid from <paramref name="photons"/>.
    /// <paramref name="maxRadius"/> is the largest search radius any hit point will use.
    /// </summary>
    public void Build(StoredPhoton[] photons, float maxRadius)
    {
        _photons = photons;

        if (photons.Length == 0)
        {
            _tableSize = 1;
            _head = [-1];
            _next = [];
            return;
        }

        // Cell edge length chosen so each query ball touches ≤ 27 cells.
        _cellSize    = 2f * maxRadius;
        _invCellSize = 1f / _cellSize;

        // Prime-ish table size slightly larger than 2× photon count for ~50 % load factor.
        _tableSize = NextPrime(photons.Length * 2 + 3);
        _head = new int[_tableSize];
        _next = new int[photons.Length];
        Array.Fill(_head, -1);

        for (int i = 0; i < photons.Length; i++)
        {
            int h = BucketIndex(photons[i].Position);
            _next[i] = _head[h];
            _head[h] = i;
        }
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends the indices of all photons within <paramref name="radius"/> of
    /// <paramref name="center"/> into <paramref name="result"/> (which is cleared first).
    /// </summary>
    public void GatherIndices(in Vec3 center, float radius, List<int> result)
    {
        result.Clear();
        if (_photons.Length == 0) return;

        float r2 = radius * radius;

        int ix0 = CellCoord(center.X - radius);
        int iy0 = CellCoord(center.Y - radius);
        int iz0 = CellCoord(center.Z - radius);
        int ix1 = CellCoord(center.X + radius);
        int iy1 = CellCoord(center.Y + radius);
        int iz1 = CellCoord(center.Z + radius);

        for (int iz = iz0; iz <= iz1; iz++)
        for (int iy = iy0; iy <= iy1; iy++)
        for (int ix = ix0; ix <= ix1; ix++)
        {
            int h   = BucketIndexCoords(ix, iy, iz);
            int idx = _head[h];
            while (idx >= 0)
            {
                ref readonly var p = ref _photons[idx];
                float dx = p.Position.X - center.X;
                float dy = p.Position.Y - center.Y;
                float dz = p.Position.Z - center.Z;
                if (dx * dx + dy * dy + dz * dz <= r2)
                    result.Add(idx);
                idx = _next[idx];
            }
        }
    }

    /// <summary>Returns the stored photon at <paramref name="index"/>.</summary>
    public ref readonly StoredPhoton GetPhoton(int index) => ref _photons[index];

    // ── Internals ─────────────────────────────────────────────────────────────

    private int CellCoord(float v) => (int)System.Math.Floor(v * _invCellSize);

    private int BucketIndex(in Vec3 pos)
        => BucketIndexCoords(CellCoord(pos.X), CellCoord(pos.Y), CellCoord(pos.Z));

    private int BucketIndexCoords(int ix, int iy, int iz)
    {
        unchecked
        {
            uint h = (uint)(ix * 73856093 ^ iy * 19349663 ^ iz * 83492791);
            return (int)(h % (uint)_tableSize);
        }
    }

    private static int NextPrime(int n)
    {
        if (n < 2) return 2;
        for (int c = n; ; c++)
            if (IsPrime(c)) return c;
    }

    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n == 2) return true;
        if ((n & 1) == 0) return false;
        for (int i = 3; (long)i * i <= n; i += 2)
            if (n % i == 0) return false;
        return true;
    }
}