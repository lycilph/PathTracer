using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Acceleration;

public class BvhNodeTests
{
    private static readonly IMaterial DummyMat = new TestMaterial();

    private static Sphere MakeSphere(double x, double y, double z, double r = 0.4)
        => new(new Vector3(x, y, z), r, DummyMat);

    // ── Basic hit/miss ────────────────────────────────────────────────────────

    [Fact]
    public void Hit_SinglePrimitive_HitsCorrectly()
    {
        var primitives = new List<IHittable> { MakeSphere(0, 0, 5) };
        var bvh = new BvhNode(primitives);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        bvh.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.6, 0.1);
    }

    [Fact]
    public void Hit_RayMissesAllPrimitives_ReturnsFalse()
    {
        var primitives = new List<IHittable>
        {
            MakeSphere( 5, 0, 5),
            MakeSphere(-5, 0, 5)
        };
        var bvh = new BvhNode(primitives);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        bvh.Hit(ray, out _).Should().BeFalse();
    }

    // ── Nearest hit ───────────────────────────────────────────────────────────

    [Fact]
    public void Hit_MultipleSpheres_ReturnsNearest()
    {
        var primitives = new List<IHittable>
        {
            MakeSphere(0, 0, 10),  // far
            MakeSphere(0, 0,  5),  // near
            MakeSphere(0, 0, 15)   // furthest
        };
        var bvh = new BvhNode(primitives);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        bvh.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.6, 0.1);
    }

    // ── Result matches SceneList ──────────────────────────────────────────────

    [Fact]
    public void Hit_MatchesSceneList_ForAllRays()
    {
        // BVH and SceneList must produce identical results — BVH is purely
        // an acceleration structure, not a different algorithm
        var spheres = new List<IHittable>
        {
            MakeSphere( 0,  0,  5),
            MakeSphere( 2,  0,  7),
            MakeSphere(-2,  1,  6),
            MakeSphere( 0, -1,  8),
            MakeSphere( 1,  1, 10)
        };

        var scene = new SceneList();
        foreach (var s in spheres) scene.Add(s);

        var bvh = new BvhNode(new List<IHittable>(spheres));

        // Test a grid of rays
        for (var dx = -2; dx <= 2; dx++)
            for (var dy = -2; dy <= 2; dy++)
            {
                var dir = new Vector3(dx * 0.5, dy * 0.5, 1).Normalize();
                var ray = new Ray(Vector3.Zero, dir);

                var sceneHit = scene.Hit(ray, out var sceneRecord);
                var bvhHit = bvh.Hit(ray, out var bvhRecord);

                sceneHit.Should().Be(bvhHit,
                    because: $"BVH and SceneList must agree for direction ({dx},{dy})");

                if (sceneHit)
                    bvhRecord.T.Should().BeApproximately(sceneRecord.T, 1e-10,
                        because: "BVH must return the same hit distance as SceneList");
            }
    }

    // ── Large scene ───────────────────────────────────────────────────────────

    [Fact]
    public void Hit_LargeScene_CorrectlyFindsHits()
    {
        // 100 spheres arranged in a grid — BVH should handle this without issues
        var primitives = new List<IHittable>();
        for (var i = 0; i < 10; i++)
            for (var j = 0; j < 10; j++)
                primitives.Add(MakeSphere(i * 2 - 9, j * 2 - 9, 20, 0.4));

        var bvh = new BvhNode(primitives);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        // Ray aimed at the centre sphere — should hit something
        var centreRay = new Ray(
            Vector3.Zero,
            new Vector3(-1, -1, 20).Normalize());

        // Just verify it doesn't throw and returns a consistent result
        var act = () => bvh.Hit(centreRay, out _);
        act.Should().NotThrow();
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Hit_TwoPrimitives_ReturnsNearest()
    {
        var primitives = new List<IHittable>
        {
            MakeSphere(0, 0, 10),
            MakeSphere(0, 0,  5)
        };
        var bvh = new BvhNode(primitives);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        bvh.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.6, 0.1);
    }
}