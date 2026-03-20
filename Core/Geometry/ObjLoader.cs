using Core.Algebra;

namespace Core.Geometry;

/// <summary>
/// Loads Wavefront OBJ files and produces a list of Triangle primitives (§6.3).
/// Supports vertex positions, vertex normals, and triangulated faces.
/// Quads are triangulated by fan from the first vertex.
/// Material (.mtl) files are ignored — the caller supplies a single material.
/// </summary>
public static class ObjLoader
{
    /// <summary>
    /// Loads an OBJ file and returns a list of triangles.
    /// </summary>
    /// <param name="path">Path to the .obj file.</param>
    /// <param name="material">Material applied to all triangles in the mesh.</param>
    /// <param name="smoothNormals">
    /// If true and the OBJ contains vertex normals, they are used for smooth
    /// shading via barycentric interpolation. If false, flat normals are used.
    /// </param>
    /// <returns>A list of Triangle primitives ready for BVH construction.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the OBJ file does not exist at <paramref name="path"/>.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Thrown if the OBJ file contains malformed geometry data.
    /// </exception>
    public static List<Triangle> Load(
        string path,
        IMaterial material,
        bool smoothNormals = false)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"OBJ file not found: {path}");

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<Triangle>();

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var parts = line.Split(' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            switch (parts[0])
            {
                case "v":
                    positions.Add(ParseVector3(parts, line));
                    break;

                case "vn":
                    normals.Add(ParseVector3(parts, line).Normalize());
                    break;

                case "vt":
                    // Texture coordinates — parsed but not used yet (M6)
                    break;

                case "mtllib":
                case "usemtl":
                case "o":
                case "g":
                case "s":
                    // Object names, groups, smoothing groups, material refs — ignored
                    break;

                case "f":
                    ParseFace(parts, positions, normals,
                              material, smoothNormals, triangles);
                    break;
            }
        }

        if (triangles.Count == 0)
            throw new InvalidDataException(
                $"OBJ file produced no triangles: {path}");

        return triangles;
    }

    /// <summary>
    /// Parses a face line and appends one or more triangles.
    /// Handles v, v/vt, v//vn, and v/vt/vn index formats.
    /// Quads and larger polygons are fan-triangulated.
    /// </summary>
    private static void ParseFace(
        string[] parts,
        List<Vector3> positions,
        List<Vector3> normals,
        IMaterial material,
        bool smoothNormals,
        List<Triangle> triangles)
    {
        // Parse each vertex reference in the face
        var faceVerts = new List<(int posIdx, int normIdx)>();

        for (var i = 1; i < parts.Length; i++)
        {
            var (posIdx, normIdx) = ParseFaceVertex(parts[i]);
            faceVerts.Add((posIdx, normIdx));
        }

        if (faceVerts.Count < 3)
            return; // degenerate face

        // Fan triangulation: (0,1,2), (0,2,3), (0,3,4) ...
        for (var i = 1; i < faceVerts.Count - 1; i++)
        {
            var (p0, n0) = faceVerts[0];
            var (p1, n1) = faceVerts[i];
            var (p2, n2) = faceVerts[i + 1];

            var v0 = ResolveIndex(positions, p0, "position");
            var v1 = ResolveIndex(positions, p1, "position");
            var v2 = ResolveIndex(positions, p2, "position");

            Vector3? sn0 = null, sn1 = null, sn2 = null;
            if (smoothNormals && normals.Count > 0 &&
                n0 >= 0 && n1 >= 0 && n2 >= 0)
            {
                sn0 = ResolveIndex(normals, n0, "normal");
                sn1 = ResolveIndex(normals, n1, "normal");
                sn2 = ResolveIndex(normals, n2, "normal");
            }

            triangles.Add(new Triangle(v0, v1, v2, material, sn0, sn1, sn2));
        }
    }

    /// <summary>
    /// Parses a single face vertex token which may be in one of these formats:
    /// "v", "v/vt", "v//vn", "v/vt/vn".
    /// Returns 0-based indices (-1 if absent).
    /// </summary>
    private static (int posIdx, int normIdx) ParseFaceVertex(string token)
    {
        var slashParts = token.Split('/');

        var posIdx = int.Parse(slashParts[0]) - 1; // OBJ is 1-based
        var normIdx = -1;

        if (slashParts.Length == 3 && slashParts[2].Length > 0)
            normIdx = int.Parse(slashParts[2]) - 1;

        return (posIdx, normIdx);
    }

    /// <summary>
    /// Resolves a 0-based index into a list, supporting negative indices
    /// (OBJ relative references from the end of the list).
    /// </summary>
    private static T ResolveIndex<T>(List<T> list, int index, string kind)
    {
        // OBJ supports negative indices as relative references
        var resolved = index < 0 ? list.Count + index + 1 : index;

        if (resolved < 0 || resolved >= list.Count)
            throw new InvalidDataException(
                $"OBJ {kind} index {index + 1} is out of range " +
                $"(have {list.Count} {kind}s).");

        return list[resolved];
    }

    /// <summary>Parses a "v x y z" or "vn x y z" line into a Vector3.</summary>
    private static Vector3 ParseVector3(string[] parts, string line)
    {
        if (parts.Length < 4)
            throw new InvalidDataException(
                $"Expected 3 components in OBJ line: '{line}'");

        return new Vector3(
            double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture));
    }
}