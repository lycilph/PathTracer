using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;

namespace Core.Scene.Scenes;

public static class ThinLensDofShowcase
{
    public static (Scene scene, ICamera pinhole, ICamera thinLens) Create(int width, int height)
    {
        float aspect = (float)width / height;

        var white = new Lambertian(new Vec3(0.8f, 0.8f, 0.8f));
        var black = new Lambertian(new Vec3(0.05f, 0.05f, 0.05f));
        var red = new Lambertian(new Vec3(0.85f, 0.20f, 0.20f));
        var green = new Lambertian(new Vec3(0.20f, 0.85f, 0.20f));
        var blue = new Lambertian(new Vec3(0.20f, 0.20f, 0.85f));

        var lightRadiance = new Vec3(40f, 40f, 40f);
        var lightMat = new DiffuseLight(lightRadiance);

        var list = new HittableList();

        // Ground (large)
        list.Add(new XZRect(-20, 20, -30, 5, k: 0f, white));

        // Overhead area light (emissive geometry + light list)
        list.Add(new FlipFace(new XZRect(-2.5f, 2.5f, -10f, -5f, k: 8f, lightMat)));

        // --- Three spheres at different depths (along -Z) ---
        // Near (should be blurry when focusing on middle)
        list.Add(new Sphere(new Vec3(-1.2f, 1.0f, -2.5f), 1.0f, red));

        // Focus target (should be sharp)
        list.Add(new Sphere(new Vec3(0.0f, 1.0f, -6.0f), 1.0f, green));

        // Far (should be blurry)
        list.Add(new Sphere(new Vec3(1.2f, 1.0f, -12.0f), 1.0f, blue));

        // --- Checkerboard back wall at z = -18 ---
        // High-frequency detail makes blur obvious.
        float zWall = -18f;
        int tiles = 16;
        float size = 16f; // spans x,y from -8..8
        float x0 = -8f, y0 = 0f;
        float dx = size / tiles;
        float dy = size / tiles;

        for (int iy = 0; iy < tiles; iy++)
        {
            for (int ix = 0; ix < tiles; ix++)
            {
                float xa = x0 + ix * dx;
                float xb = xa + dx;
                float ya = y0 + iy * dy;
                float yb = ya + dy;

                bool isWhite = ((ix + iy) & 1) == 0;
                list.Add(new XYRect(xa, xb, ya, yb, k: zWall, isWhite ? white : black));
            }
        }

        // BVH for speed
        IHittable world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(-2.5f, 2.5f, -10f, -5f, k: 8f, normal: -Vec3.UnitY, radiance: lightRadiance)
        };

        var scene = new Scene(world, lights);

        // Camera pose
        var lookFrom = new Vec3(0f, 2.0f, 6f);
        var lookAt = new Vec3(0f, 1.0f, -6f);  // aim at the middle (green) sphere
        var vUp = Vec3.UnitY;

        // Focus distance: distance to focus target along the viewing direction.
        // With your ThinLensCamera implementation, using Euclidean distance works well for this setup.
        float focusDist = (lookAt - lookFrom).Length();

        // Make aperture BIG to clearly see DoF (dial down later if you want subtler)
        float apertureRadius = 0.25f;

        ICamera pinhole = new PinholeCamera(
            vfovDegrees: 35f,
            aspectRatio: aspect,
            lookFrom: lookFrom,
            lookAt: lookAt,
            vUp: vUp);

        ICamera thin = new ThinLensCamera(
            vfovDegrees: 35f,
            aspectRatio: aspect,
            lookFrom: lookFrom,
            lookAt: lookAt,
            vUp: vUp,
            focusDistance: focusDist,
            apertureRadius: apertureRadius);

        return (scene, pinhole, thin);
    }
}