using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Scene;
using Core.Scene.Scenes;

namespace Scripting;

public sealed class SceneApi
{
    private readonly int _width;
    private readonly int _height;

    public SceneApi(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public SceneDefinition CornellDefault(bool tintedGlass = true)
    {
        var (scene, camera) = CornellMaterialsShowcase.Create(_width, _height, tintedGlass);
        return new SceneDefinition(scene, camera);
    }

    public SceneDefinition DOFDefault(bool thinLens = false)
    {
        var (scene, pinhole, thinlens) = ThinLensDofShowcase.Create(_width, _height);
        var camera = thinLens ? thinlens : pinhole;
        return new SceneDefinition(scene, camera);
    }

    public SceneDefinition MotionBlurDefault(bool thinLens = false)
    {
        var (scene, pinhole, thinlens) = MotionBlurShowcase.Create(_width, _height);
        var camera = thinLens ? thinlens : pinhole;
        return new SceneDefinition(scene, camera);
    }

    public SceneDefinition CornellSimple(bool thinLens = false)
    {
        float aspect = (float)_width / _height;

        var red = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var white = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));
        var lightRadiance = new Vec3(15f, 15f, 15f);
        var lightMat = new DiffuseLight(lightRadiance);

        var list = new HittableList();
        list.Add(new YZRect(0, 555, 0, 555, 555, green));
        list.Add(new YZRect(0, 555, 0, 555, 0, red));
        list.Add(new XZRect(0, 555, 0, 555, 0, white));
        list.Add(new XZRect(0, 555, 0, 555, 555, white));
        list.Add(new XYRect(0, 555, 0, 555, 555, white));
        list.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));

        var metal = new MicrofacetMetal(new Vec3(0.95f, 0.93f, 0.88f), roughness: 0.25f);
        var glass = new Dielectric(ior: 1.5f);

        list.Add(new Sphere(new Vec3(190f, 90f, 190f), 90f, metal));
        list.Add(new Sphere(new Vec3(370f, 90f, 370f), 90f, glass));

        IHittable world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(213, 343, 227, 332, 554, normal: -Vec3.UnitY, radiance: lightRadiance)
        };

        var scene = new Scene(world, lights);

        var lookFrom = new Vec3(278f, 278f, -800f);
        var lookAt = new Vec3(278f, 278f, 0f);

        ICamera camera;
        if (thinLens)
        {
            float focusDist = (lookAt - lookFrom).Length();
            camera = new ThinLensCamera(40f, aspect, lookFrom, lookAt, Vec3.UnitY, focusDist, apertureRadius: 0.2f);
        }
        else
        {
            camera = new PinholeCamera(40f, aspect, lookFrom, lookAt, Vec3.UnitY);
        }

        return new SceneDefinition(scene, camera);
    }
}