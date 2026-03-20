// Depth of Field
// Demonstrates thin-lens depth of field. The middle sphere is in focus,
// the near and far spheres are progressively blurred.
// Adjust aperture and focusDistance to change the effect.

return Scene
    .WithCamera(
        position: new Vector3(0, 1, 6),
        lookAt: new Vector3(0, 0, 0),
        fovDegrees: 35,
        aperture: 0.3,
        focusDistance: 6.0)
    .WithRenderSettings(
        imageWidth: 800,
        imageHeight: 450,
        samplesPerPixel: 256)
    // Ground
    .AddSphere(
        centre: new Vector3(0, -100.5, 0),
        radius: 100,
        material: MaterialBuilder.Lambertian(new Vector3(0.5, 0.7, 0.5)),
        name: "Ground")
    // Near sphere (out of focus)
    .AddSphere(
        centre: new Vector3(-1.5, 0, 3),
        radius: 0.5,
        material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.3, 0.3)),
        name: "NearSphere")
    // Middle sphere (in focus)
    .AddSphere(
        centre: new Vector3(0, 0, 0),
        radius: 0.5,
        material: MaterialBuilder.Lambertian(new Vector3(0.3, 0.8, 0.3)),
        name: "FocusSphere")
    // Far sphere (out of focus)
    .AddSphere(
        centre: new Vector3(1.5, 0, -3),
        radius: 0.5,
        material: MaterialBuilder.Lambertian(new Vector3(0.3, 0.3, 0.8)),
        name: "FarSphere")
    // Glass sphere in focus plane
    .AddSphere(
        centre: new Vector3(-1, 0, 0),
        radius: 0.5,
        material: MaterialBuilder.Dielectric(ior: 1.5),
        name: "GlassSphere")
    .AddAreaLight(
        corner: new Vector3(-3, 4, -3),
        edge1: new Vector3(6, 0, 0),
        edge2: new Vector3(0, 0, 6),
        emission: new Vector3(8, 8, 8),
        name: "OverheadLight")
    .Build();