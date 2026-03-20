using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Geometry;

public class ObjLoaderTests
{
    private static readonly IMaterial DummyMat = new TestMaterial();

    /// <summary>Writes OBJ content to a temp file and returns its path.</summary>
    private static string WriteTempObj(string content)
    {
        var path = Path.Combine(Path.GetTempPath(),
                                $"test_{Guid.NewGuid():N}.obj");
        File.WriteAllText(path, content);
        return path;
    }

    // ── Basic loading ─────────────────────────────────────────────────────────

    [Fact]
    public void Load_SingleTriangle_ProducesOneTriangle()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var triangles = ObjLoader.Load(path, DummyMat);
        triangles.Should().HaveCount(1);
    }

    [Fact]
    public void Load_QuadFace_ProducesTwoTriangles()
    {
        // A quad face should be fan-triangulated into 2 triangles
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 1 1 0
            v 0 1 0
            f 1 2 3 4
            """);

        var triangles = ObjLoader.Load(path, DummyMat);
        triangles.Should().HaveCount(2);
    }

    [Fact]
    public void Load_MultipleTriangles_ProducesCorrectCount()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            v 0 0 1
            f 1 2 3
            f 1 2 4
            f 1 3 4
            """);

        var triangles = ObjLoader.Load(path, DummyMat);
        triangles.Should().HaveCount(3);
    }

    // ── Comments and metadata ─────────────────────────────────────────────────

    [Fact]
    public void Load_CommentsAndMetadata_AreIgnored()
    {
        var path = WriteTempObj("""
            # This is a comment
            o MyObject
            g default
            mtllib material.mtl
            usemtl mat1
            s off
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var act = () => ObjLoader.Load(path, DummyMat);
        act.Should().NotThrow();
        ObjLoader.Load(path, DummyMat).Should().HaveCount(1);
    }

    // ── Normal formats ────────────────────────────────────────────────────────

    [Fact]
    public void Load_FaceWithNormalIndices_LoadsWithoutError()
    {
        // v/vt/vn format
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            vn 0 0 1
            vn 0 0 1
            f 1//1 2//2 3//3
            """);

        var act = () => ObjLoader.Load(path, DummyMat, smoothNormals: true);
        act.Should().NotThrow();
    }

    [Fact]
    public void Load_SmoothNormals_TriangleIsHittable()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            vn 0 0 1
            vn 0 0 1
            f 1//1 2//2 3//3
            """);

        var triangles = ObjLoader.Load(path, DummyMat, smoothNormals: true);
        var ray = new Ray(new Vector3(0.2f, 0.2f, -5), Vector3.UnitZ);

        triangles[0].Hit(ray, out _).Should().BeTrue();
    }

    [Fact]
    public void Load_FlatNormals_IgnoresNormalData()
    {
        // Same OBJ with smoothNormals=false should still load correctly
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """);

        var triangles = ObjLoader.Load(path, DummyMat, smoothNormals: false);
        triangles.Should().HaveCount(1);
    }

    // ── Material assignment ───────────────────────────────────────────────────

    [Fact]
    public void Load_AllTrianglesGetSuppliedMaterial()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            v 0 0 1
            f 1 2 3
            f 1 2 4
            """);

        var mat = new TestMaterial();
        var triangles = ObjLoader.Load(path, mat);

        foreach (var tri in triangles)
            tri.Material.Should().BeSameAs(mat);
    }

    // ── Error handling ────────────────────────────────────────────────────────

    [Fact]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = () => ObjLoader.Load("nonexistent.obj", DummyMat);
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Load_EmptyFile_ThrowsInvalidDataException()
    {
        var path = WriteTempObj("# just a comment\n");
        var act = () => ObjLoader.Load(path, DummyMat);
        act.Should().Throw<InvalidDataException>();
    }

    // ── Intersection correctness ──────────────────────────────────────────────

    [Fact]
    public void Load_TriangleGeometry_RayIntersectsCorrectly()
    {
        var path = WriteTempObj("""
            v -1 -1 0
            v  1 -1 0
            v  0  1 0
            f 1 2 3
            """);

        var triangles = ObjLoader.Load(path, DummyMat);

        // Ray through the centroid
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        triangles[0].Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(5.0, 1e-10);
    }
}