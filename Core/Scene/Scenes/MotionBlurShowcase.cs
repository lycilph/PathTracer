using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;

namespace Core.Scene.Scenes;

public static class MotionBlurShowcase
{
    public static (Scene scene, ICamera pinhole, ICamera thinLens) Create(int width, int height)
    {
        float aspect = (float)width / height;

        // Motion blur showcase: moving sphere under area light
        var white = new Lambertian(new Vec3(0.75f, 0.75f, 0.75f));
        var red = new Lambertian(new Vec3(0.85f, 0.25f, 0.25f));
        var lightMat = new DiffuseLight(new Vec3(35f, 35f, 35f));

        var list = new HittableList();

        // Ground
        list.Add(new XZRect(-20, 20, -30, 10, k: 0f, white));

        // Light
        list.Add(new FlipFace(new XZRect(-2.5f, 2.5f, -10f, -5f, k: 10f, lightMat)));

        // Moving sphere: sweeps in X during shutter interval [0,1]
        list.Add(new MovingSphere(
            center0: new Vec3(-3f, 1.0f, -8f),
            center1: new Vec3(3f, 1.0f, -8f),
            time0: 0f,
            time1: 1f,
            radius: 1.0f,
            material: red));

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(-2.5f, 2.5f, -10f, -5f, k: 10f, normal: -Vec3.UnitY, radiance: new Vec3(35f,35f,35f))
        };
        var scene = new Scene(world, lights);

        // Camera: choose pinhole or thin lens, but with shutter interval
        var lookFrom = new Vec3(0f, 3f, 6f);
        var lookAt = new Vec3(0f, 1f, -8f);

        ICamera pinhole = new PinholeCamera(
            vfovDegrees: 35f,
            aspectRatio: aspect,
            lookFrom: lookFrom,
            lookAt: lookAt,
            vUp: Vec3.UnitY,
            shutterOpen: 0f,
            shutterClose: 0.1f);

        // Or, thin lens + motion blur together:
        float focusDist = (lookAt - lookFrom).Length();
        ICamera thin = new ThinLensCamera(35f, aspect, lookFrom, lookAt, Vec3.UnitY, focusDist, 0.2f, 0f, 1f);

        return (scene, pinhole, thin);
    }
}
