using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Scene;

namespace Tests.Scene;

public class BvhConsistencyTests
{
    [Fact]
    public void Bvh_Matches_BruteForce_ForRandomRays()
    {
        var mat = new Lambertian(new Vec3(1,1,1));

        var list = new HittableList();
        // deterministic set of spheres
        for (int i = 0; i < 20; i++)
        {
            float x = -2f + i * 0.2f;
            list.Add(new Sphere(new Vec3(x, 0, -3), 0.1f, mat));
        }

        var bvh = new BvhNode(list.Objects);

        var rng = new Pcg32(123);
        for (int i = 0; i < 200; i++)
        {
            float ox = (rng.NextFloat01() * 4f) - 2f;
            float oy = (rng.NextFloat01() * 2f) - 1f;
            var origin = new Vec3(ox, oy, 0);
            var dir = new Vec3((rng.NextFloat01() - 0.5f), (rng.NextFloat01() - 0.5f), -1f).Normalized();
            var ray = new Ray(origin, dir);

            bool hitList = list.Hit(ray, 0.001f, 1000f, out var h1);
            bool hitBvh = bvh.Hit(ray, 0.001f, 1000f, out var h2);

            Assert.Equal(hitList, hitBvh);
            if (hitList && hitBvh)
                Assert.InRange(h2.T - h1.T, -1e-4f, 1e-4f);
        }
    }
}
