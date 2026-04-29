using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;

namespace Core.Scene.Scenes;

/// <summary>
/// Cornell box variant with glass + smooth/rough metal objects.
/// Useful for testing multithreading and overall integrator correctness.
/// </summary>
public static class CornellMaterialsShowcase
{
    public static (Scene scene, PinholeCamera camera) Create(
        int imageWidth,
        int imageHeight,
        bool tintedGlass = true)
    {
        float aspect = (float)imageWidth / imageHeight;

        // --- Materials ---
        var red = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var white = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));

        // Area light radiance (linear RGB)
        var lightRadiance = new Vec3(15f, 15f, 15f);
        var lightMat = new DiffuseLight(lightRadiance);

        // Metals (GGX microfacet). F0 is “base reflectance” for metals.
        // Silver-ish F0 is high and fairly neutral.
        var metalF0 = new Vec3(0.95f, 0.93f, 0.88f);

        var smoothMetal = new MicrofacetMetal(metalF0, roughness: 0.06f);
        var roughMetal = new MicrofacetMetal(metalF0, roughness: 0.55f);

        // Glass: clear or lightly tinted (“bottle glass” style)
        Dielectric glass = tintedGlass
            ? new Dielectric(
                ior: 1.5f,
                tint: new Vec3(0.60f, 0.90f, 0.60f),       // light green transmittance @ ref distance
                absorptionStrength: 0.01f)                 // Cornell scale tuning
            : new Dielectric(ior: 1.5f);

        // --- Cornell geometry (0..555) ---
        var list = new HittableList();

        // Walls
        list.Add(new YZRect(0, 555, 0, 555, 555, green)); // right wall
        list.Add(new YZRect(0, 555, 0, 555, 0, red));     // left wall

        // Floor / ceiling / back wall
        list.Add(new XZRect(0, 555, 0, 555, 0, white));     // floor
        list.Add(new XZRect(0, 555, 0, 555, 555, white));   // ceiling
        list.Add(new XYRect(0, 555, 0, 555, 555, white));   // back wall

        // Ceiling light (emissive geometry). Flip so it emits downward into the box.
        // Geometry at y=554, light normal should be -Y.
        list.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));

        // --- Objects ---
        // Use 3 spheres, radius 80, placed to avoid overlap and keep distance from walls.
        const float r = 80f;

        // Glass sphere (front-left)
        list.Add(new Sphere(new Vec3(160f, r, 180f), r, glass));

        // Smooth metal sphere (front-right)
        list.Add(new Sphere(new Vec3(395f, r, 180f), r, smoothMetal));

        // Rough metal sphere (back-center)
        list.Add(new Sphere(new Vec3(278f, r, 380f), r, roughMetal));

        // Optional: add a small diffuse box for reference cues (comment out if you want only spheres)
        //list.Add(new Box(new Vec3(240, 0, 240), new Vec3(315, 120, 315), white));
        list.Add(new Box(
            new Vec3(50f, 0f, 360f),   // min corner (x,y,z)
            new Vec3(180f, 250f, 520f), // max corner (x,y,z)  -- taller & deeper than before
            white));


        // BVH over the whole scene for speed
        IHittable world = new BvhNode(list.Objects);

        // --- Lights list for NEE ---
        var lights = new List<ILight>
        {
            new RectAreaLightXZ(
                x0: 213, x1: 343,
                z0: 227, z1: 332,
                k: 554,
                normal: -Vec3.UnitY,
                radiance: lightRadiance)
        };

        var scene = new Scene(world, lights);

        // --- Camera (classic Cornell view) ---
        var camera = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: aspect,
            lookFrom: new Vec3(278f, 278f, -800f),
            lookAt: new Vec3(278f, 278f, 0f),
            vUp: Vec3.UnitY);

        return (scene, camera);
    }
}