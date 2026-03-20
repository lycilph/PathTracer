using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Geometry;

public class MeshTests
{
    private static readonly IMaterial DummyMat = new TestMaterial();

    private static string WriteTempObj(string content)
    {
        var path = Path.Combine(Path.GetTempPath(),
                                $"mesh_{Guid.NewGuid():N}.obj");
        File.WriteAllText(path, content);
        return path;
    }

    // A simple tetrahedron — 4 triangular faces
    private static string TetrahedronObj() => """
        v  0  1  0
        v  1 -1  1
        v -1 -1  1
        v  0 -1 -1
        f 1 2 3
        f 1 2 4
        f 1 3 4
        f 2 3 4
        """;

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Load_Tetrahedron_HasCorrectTriangleCount()
    {
        var path = WriteTempObj(TetrahedronObj());
        var mesh = Mesh.Load(path, DummyMat);
        mesh.TriangleCount.Should().Be(4);
    }

    [Fact]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = () => Mesh.Load("nonexistent.obj", DummyMat);
        act.Should().Throw<FileNotFoundException>();
    }

    // ── Intersection ──────────────────────────────────────────────────────────

    [Fact]
    public void Hit_RayThroughMesh_ReturnsHit()
    {
        // A flat quad mesh in the XY plane — ray from -Z should hit
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  1  1 0
            v -1  1 0
            f 1 2 3
            f 1 3 4
            """);

        var mesh = Mesh.Load(path, DummyMat);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);

        mesh.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void Hit_RayMissesMesh_ReturnsFalse()
    {
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  1  1 0
            v -1  1 0
            f 1 2 3
            f 1 3 4
            """);

        var mesh = Mesh.Load(path, DummyMat);
        var ray = new Ray(new Vector3(5, 5, -5), Vector3.UnitZ);

        mesh.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_ReturnsNearestTriangle()
    {
        // Two triangles at different depths — should return the nearer one
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  0  1 0
            v -1 -1 3
            v  1 -1 3
            v  0  1 3
            f 1 2 3
            f 4 5 6
            """);

        var mesh = Mesh.Load(path, DummyMat);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);

        mesh.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(5.0, 1e-10);
    }

    // ── Bounds ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetBounds_ContainsAllVertices()
    {
        var path = WriteTempObj(TetrahedronObj());
        var mesh = Mesh.Load(path, DummyMat);
        var bounds = mesh.GetBounds();

        // Tetrahedron vertices range from -1 to +1 on X/Z, -1 to +1 on Y
        bounds.Min.X.Should().BeLessThanOrEqualTo(-1);
        bounds.Min.Y.Should().BeLessThanOrEqualTo(-1);
        bounds.Max.X.Should().BeGreaterThanOrEqualTo(1);
        bounds.Max.Y.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetBounds_RayHittingMeshAlsoHitsBounds()
    {
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  1  1 0
            v -1  1 0
            f 1 2 3
            f 1 3 4
            """);

        var mesh = Mesh.Load(path, DummyMat);
        var bounds = mesh.GetBounds();
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);

        mesh.Hit(ray, out _).Should().BeTrue();
        bounds.Hit(ray).Should().BeTrue();
    }

    // ── BVH correctness ───────────────────────────────────────────────────────

    [Fact]
    public void Hit_MatchesNaiveSearch_ForManyRays()
    {
        // Mesh BVH must produce identical results to brute force triangle search
        var path = WriteTempObj(TetrahedronObj());
        var triangles = ObjLoader.Load(path, DummyMat);
        var mesh = Mesh.Load(path, DummyMat);

        var naive = new SceneList();
        foreach (var t in triangles) naive.Add(t);

        // Test a grid of rays
        for (var dx = -2; dx <= 2; dx++)
            for (var dy = -2; dy <= 2; dy++)
            {
                var dir = new Vector3(dx * 0.3, dy * 0.3, 1).Normalize();
                var ray = new Ray(new Vector3(0, 0, -3), dir);
                var naiveHit = naive.Hit(ray, out var naiveRecord);
                var meshHit = mesh.Hit(ray, out var meshRecord);

                naiveHit.Should().Be(meshHit,
                    because: $"Mesh and SceneList must agree for dir ({dx},{dy})");

                if (naiveHit)
                    meshRecord.T.Should().BeApproximately(naiveRecord.T, 1e-10,
                        because: "Mesh BVH must return same hit distance as naive search");
            }
    }

    // ── Integration with scene ────────────────────────────────────────────────

    [Fact]
    public void Mesh_InSceneList_IsHittable()
    {
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  1  1 0
            v -1  1 0
            f 1 2 3
            f 1 3 4
            """);

        var scene = new SceneList();
        scene.Add(Mesh.Load(path, DummyMat));

        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        scene.Hit(ray, out _).Should().BeTrue();
    }

    [Fact]
    public void Mesh_WithSmoothNormals_IsHittable()
    {
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  0  1 0
            vn 0 0 1
            vn 0 0 1
            vn 0 0 1
            f 1//1 2//2 3//3
            """);

        var mesh = Mesh.Load(path, DummyMat, smoothNormals: true);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);

        mesh.Hit(ray, out _).Should().BeTrue();
    }
}