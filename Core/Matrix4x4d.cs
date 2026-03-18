namespace Core;

/// <summary>
/// A double-precision 4×4 matrix used for geometric transforms.
/// Row-major storage: M[row, col].
/// </summary>
public readonly struct Matrix4x4d
{
    private readonly double[,] _m;

    private Matrix4x4d(double[,] m) => _m = m;

    public double this[int row, int col] => _m[row, col];

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>Returns the 4×4 identity matrix.</summary>
    public static Matrix4x4d Identity => new(new double[,]
    {
        { 1, 0, 0, 0 },
        { 0, 1, 0, 0 },
        { 0, 0, 1, 0 },
        { 0, 0, 0, 1 }
    });

    /// <summary>Creates a translation matrix.</summary>
    public static Matrix4x4d Translation(double tx, double ty, double tz) =>
        new(new double[,]
        {
            { 1, 0, 0, tx },
            { 0, 1, 0, ty },
            { 0, 0, 1, tz },
            { 0, 0, 0,  1 }
        });

    /// <summary>Creates a uniform scale matrix.</summary>
    public static Matrix4x4d Scale(double s) => Scale(s, s, s);

    /// <summary>Creates a non-uniform scale matrix.</summary>
    public static Matrix4x4d Scale(double sx, double sy, double sz) =>
        new(new double[,]
        {
            { sx,  0,  0, 0 },
            {  0, sy,  0, 0 },
            {  0,  0, sz, 0 },
            {  0,  0,  0, 1 }
        });

    /// <summary>Creates a rotation matrix around the Y axis.</summary>
    /// <param name="degrees">Rotation angle in degrees.</param>
    public static Matrix4x4d RotationY(double degrees)
    {
        var r = degrees * Math.PI / 180.0;
        var c = Math.Cos(r);
        var s = Math.Sin(r);
        return new(new double[,]
        {
            {  c, 0, s, 0 },
            {  0, 1, 0, 0 },
            { -s, 0, c, 0 },
            {  0, 0, 0, 1 }
        });
    }

    /// <summary>Creates a rotation matrix around the X axis.</summary>
    /// <param name="degrees">Rotation angle in degrees.</param>
    public static Matrix4x4d RotationX(double degrees)
    {
        var r = degrees * Math.PI / 180.0;
        var c = Math.Cos(r);
        var s = Math.Sin(r);
        return new(new double[,]
        {
            { 1,  0,  0, 0 },
            { 0,  c, -s, 0 },
            { 0,  s,  c, 0 },
            { 0,  0,  0, 1 }
        });
    }

    /// <summary>Creates a rotation matrix around the Z axis.</summary>
    /// <param name="degrees">Rotation angle in degrees.</param>
    public static Matrix4x4d RotationZ(double degrees)
    {
        var r = degrees * Math.PI / 180.0;
        var c = Math.Cos(r);
        var s = Math.Sin(r);
        return new(new double[,]
        {
            {  c, -s, 0, 0 },
            {  s,  c, 0, 0 },
            {  0,  0, 1, 0 },
            {  0,  0, 0, 1 }
        });
    }

    // ── Operations ────────────────────────────────────────────────────────────

    /// <summary>Multiplies two matrices — applies transforms right to left.</summary>
    public static Matrix4x4d operator *(Matrix4x4d a, Matrix4x4d b)
    {
        var r = new double[4, 4];
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
            {
                r[i, j] = 0;
                for (var k = 0; k < 4; k++)
                    r[i, j] += a._m[i, k] * b._m[k, j];
            }
        return new Matrix4x4d(r);
    }

    /// <summary>
    /// Transforms a point (w=1) — applies translation.
    /// </summary>
    public Vector3 TransformPoint(Vector3 p)
    {
        var x = _m[0, 0] * p.X + _m[0, 1] * p.Y + _m[0, 2] * p.Z + _m[0, 3];
        var y = _m[1, 0] * p.X + _m[1, 1] * p.Y + _m[1, 2] * p.Z + _m[1, 3];
        var z = _m[2, 0] * p.X + _m[2, 1] * p.Y + _m[2, 2] * p.Z + _m[2, 3];
        var w = _m[3, 0] * p.X + _m[3, 1] * p.Y + _m[3, 2] * p.Z + _m[3, 3];
        return w == 1.0 ? new Vector3(x, y, z) : new Vector3(x / w, y / w, z / w);
    }

    /// <summary>
    /// Transforms a direction (w=0) — ignores translation.
    /// </summary>
    public Vector3 TransformDirection(Vector3 d)
    {
        var x = _m[0, 0] * d.X + _m[0, 1] * d.Y + _m[0, 2] * d.Z;
        var y = _m[1, 0] * d.X + _m[1, 1] * d.Y + _m[1, 2] * d.Z;
        var z = _m[2, 0] * d.X + _m[2, 1] * d.Y + _m[2, 2] * d.Z;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Transforms a normal using the inverse transpose of the upper 3×3.
    /// Ensures normals remain perpendicular to surfaces under non-uniform scale.
    /// </summary>
    public Vector3 TransformNormal(Vector3 n)
    {
        // For normals: use transpose of this matrix's upper 3×3
        // (caller should pass the inverse matrix for correct results)
        var x = _m[0, 0] * n.X + _m[1, 0] * n.Y + _m[2, 0] * n.Z;
        var y = _m[0, 1] * n.X + _m[1, 1] * n.Y + _m[2, 1] * n.Z;
        var z = _m[0, 2] * n.X + _m[1, 2] * n.Y + _m[2, 2] * n.Z;
        return new Vector3(x, y, z).Normalize();
    }

    /// <summary>
    /// Computes the inverse of this matrix using Gaussian elimination.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the matrix is singular and cannot be inverted.
    /// </exception>
    public Matrix4x4d Inverse()
    {
        // Augmented matrix [M | I]
        var a = new double[4, 8];
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
                a[i, j] = _m[i, j];
            a[i, i + 4] = 1.0;
        }

        // Forward elimination with partial pivoting
        for (var col = 0; col < 4; col++)
        {
            // Find pivot
            var pivot = col;
            for (var row = col + 1; row < 4; row++)
                if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col]))
                    pivot = row;

            // Swap rows
            for (var j = 0; j < 8; j++)
                (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);

            if (Math.Abs(a[col, col]) < 1e-12)
                throw new InvalidOperationException("Matrix is singular.");

            var scale = a[col, col];
            for (var j = 0; j < 8; j++)
                a[col, j] /= scale;

            for (var row = 0; row < 4; row++)
            {
                if (row == col) continue;
                var factor = a[row, col];
                for (var j = 0; j < 8; j++)
                    a[row, j] -= factor * a[col, j];
            }
        }

        // Extract inverse
        var inv = new double[4, 4];
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
                inv[i, j] = a[i, j + 4];

        return new Matrix4x4d(inv);
    }
}