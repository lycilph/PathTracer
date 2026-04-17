using System.Globalization;
using Core.Materials;
using Core.Math;

namespace Core.Scene;

/// <summary>
/// Minimal OBJ loader (Milestone 5): supports positions (v) and faces (f).
/// - Faces may be triangles or polygons; polygons are triangulated as a fan.
/// - Vertex/texcoord/normal indices are accepted but only position index is used.
/// 
/// This is intentionally minimal and deterministic.
/// </summary>
public static class ObjLoader
{
    public static TriangleMesh Load(string path, IMaterial material, float scale = 1f, in Vec3? translate = null)
    {
        var verts = new List<Vec3>(1024);
        var tris = new List<Triangle>(2048);

        var inv = CultureInfo.InvariantCulture;
        Vec3 t = translate ?? Vec3.Zero;

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            if (parts[0] == "v" && parts.Length >= 4)
            {
                float x = float.Parse(parts[1], inv) * scale + t.X;
                float y = float.Parse(parts[2], inv) * scale + t.Y;
                float z = float.Parse(parts[3], inv) * scale + t.Z;
                verts.Add(new Vec3(x, y, z));
            }
            else if (parts[0] == "f" && parts.Length >= 4)
            {
                // Parse all vertex indices in the face
                Span<int> idx = stackalloc int[parts.Length - 1];
                int n = 0;
                for (int i = 1; i < parts.Length; i++)
                {
                    var token = parts[i];
                    int slash = token.IndexOf('/');
                    string vi = slash >= 0 ? token[..slash] : token;
                    if (string.IsNullOrEmpty(vi)) continue;

                    int vIndex = int.Parse(vi, inv);
                    if (vIndex < 0) vIndex = verts.Count + vIndex + 1; // negative indices
                    vIndex -= 1; // OBJ is 1-based
                    idx[n++] = vIndex;
                }

                // Triangulate fan: (0, i, i+1)
                for (int i = 1; i + 1 < n; i++)
                {
                    var v0 = verts[idx[0]];
                    var v1 = verts[idx[i]];
                    var v2 = verts[idx[i + 1]];
                    tris.Add(new Triangle(v0, v1, v2, material));
                }
            }
        }

        return new TriangleMesh(tris);
    }
}