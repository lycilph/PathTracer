using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;

namespace Core.Scene.Scenes;

/// <summary>
/// SPPM showcase scene: a Cornell-box variant with a large glass sphere that
/// casts a prominent caustic ring on the diffuse floor.
///
/// Why this showcases SPPM:
///   Path tracing must find the path  Eye → Floor → (through glass) → Light,
///   which requires the bounce direction to thread the glass exactly — extremely
///   rare with standard sampling.  SPPM instead shoots photons FROM the light
///   THROUGH the glass onto the floor and gathers them at every eye-ray floor hit,
///   converging to the correct caustic in tens of iterations vs thousands of SPP.
///
/// Scene layout (Cornell scale, 0–555 in all axes):
///   • Red left wall, green right wall, white floor / ceiling / back wall.
///   • Small, bright ceiling light (80 × 80) for sharp caustics.
///   • Large glass sphere (r = 100) centred at (278, 115, 278), resting on the floor.
///   • Two diffuse boxes in the corners for spatial reference.
/// </summary>
public static class SppmCausticScene
{
    public static (Scene scene, PinholeCamera camera) Create(int imageWidth, int imageHeight)
    {
        // ── Materials ─────────────────────────────────────────────────────────
        var white  = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));
        var red    = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green  = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var glass  = new Dielectric(1.5f);

        // Bright emission for the small ceiling panel (high radiance → sharp caustic)
        float lightRadiance = 30f;
        var   lightMat      = new DiffuseLight(new Vec3(lightRadiance, lightRadiance * 0.95f, lightRadiance * 0.85f));

        // ── Geometry ──────────────────────────────────────────────────────────
        var world = new HittableList();
        // Floor
        world.Add(new XZRect(0, 555, 0, 555, 0, white));
        // Ceiling
        world.Add(new XZRect(0, 555, 0, 555, 555, white));
        // Back wall
        world.Add(new XYRect(0, 555, 0, 555, 555, white));
        // Left wall (red)
        world.Add(new YZRect(0, 555, 0, 555, 0,   red));
        // Right wall (green)
        world.Add(new YZRect(0, 555, 0, 555, 555, green));

        // Small ceiling light (centred, 80 × 80)
        world.Add(new FlipFace(new XZRect(238, 318, 238, 318, 554, lightMat)));

        // Glass sphere resting on the floor: centre y = radius = 115
        world.Add(new Sphere(new Vec3(278f, 115f, 278f), 115f, glass));

        // Short white box – front left, for spatial reference
        world.Add(new Box(new Vec3(60, 0, 60), new Vec3(180, 120, 180), white));

        // Taller white box – back right corner
        world.Add(new Box(new Vec3(375, 0, 340), new Vec3(475, 230, 440), white));

        // ── Light list (for NEE in camera pass) ───────────────────────────────
        var lights = new List<ILight>
        {
            new RectAreaLightXZ(
                x0:      238f, x1: 318f,
                z0:      238f, z1: 318f,
                k:       554f,
                normal:  -Vec3.UnitY,   // emits downward into the box
                radiance: new Vec3(lightRadiance, lightRadiance * 0.95f, lightRadiance * 0.85f))
        };

        var scene = new Scene(new BvhNode(world.Objects), lights);

        // ── Camera ────────────────────────────────────────────────────────────
        var lookFrom = new Vec3(278f, 278f, -800f);
        var lookAt   = new Vec3(278f, 278f,    0f);
        var vup      = Vec3.UnitY;
        float vfov   = 40f;
        float aspect = (float)imageWidth / imageHeight;

        //var camera = new PinholeCamera(lookFrom, lookAt, vup, vfov, aspect, 0f, 10f);
        var camera = new PinholeCamera(
            vfovDegrees: vfov,
            aspectRatio: aspect,
            lookFrom: lookFrom,
            lookAt: lookAt,
            vUp: vup);

        return (scene, camera);
    }
}
