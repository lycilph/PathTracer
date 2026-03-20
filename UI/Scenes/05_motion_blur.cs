// Motion Blur
// Demonstrates shutter-time motion blur. Spheres moving at different
// speeds produce different amounts of blur.
// The shutter is open for half a time unit (0 to 0.5).

return Scene
    .WithCamera(
        position: new Vector3(0, 2, 8),
        lookAt: new Vector3(0, 0, 0),
        fovDegrees: 40,
        shutterOpen: 0.0,
        shutterClose: 0.5)
    .WithRenderSettings(
        imageWidth: 800,
        imageHeight: 400,
        samplesPerPixel: 128)
    // Ground
    .AddSphere(
        centre: new Vector3(0, -100.5, 0),
        radius: 100,
        material: MaterialBuilder.Lambertian(new Vector3(0.5, 0.5, 0.5)),
        name: "Ground")
    // Static sphere for reference
    .AddSphere(
        centre: new Vector3(0, 0, 0),
        radius: 0.5,
        material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.8, 0.8)),
        name: "StaticSphere")
    // Fast moving sphere
    .AddMovingSphere(
        centre0: new Vector3(-3, 0, 0),
        centre1: new Vector3(-1, 0, 0),
        time0: 0.0,
        time1: 1.0,
        radius: 0.5,
        material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.3, 0.3)),
        name: "FastSphere")
    // Medium moving sphere
    .AddMovingSphere(
        centre0: new Vector3(1.5, 0, 0),
        centre1: new Vector3(2.5, 0, 0),
        time0: 0.0,
        time1: 1.0,
        radius: 0.5,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.8, 0.6, 0.2), roughness: 0.1),
        name: "MetalSphere")
    .AddAreaLight(
        corner: new Vector3(-4, 4, -2),
        edge1: new Vector3(8, 0, 0),
        edge2: new Vector3(0, 0, 4),
        emission: new Vector3(8, 8, 8),
        name: "OverheadLight")
    .Build();