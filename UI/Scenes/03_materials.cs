// Materials Showcase
// Five spheres in a row demonstrating each material type:
// Lambertian, Mirror, GGX Metal (rough), GGX Metal (smooth), Dielectric.

return Scene
    .WithCamera(
        position: new Vector3(0, 1, 5),
        lookAt: new Vector3(0, 0, 0),
        fovDegrees: 45)
    .WithRenderSettings(
        imageWidth: 800,
        imageHeight: 400,
        samplesPerPixel: 128)
    // Ground
    .AddSphere(
        centre: new Vector3(0, -101, 0),
        radius: 100,
        material: MaterialBuilder.Lambertian(new Vector3(0.5, 0.5, 0.5)),
        name: "Ground")
    // Lambertian
    .AddSphere(
        centre: new Vector3(-4, 0, 0),
        radius: 1.0,
        material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.3, 0.3)),
        name: "Lambertian")
    // Mirror
    .AddSphere(
        centre: new Vector3(-2, 0, 0),
        radius: 1.0,
        material: MaterialBuilder.Mirror(new Vector3(0.9, 0.9, 0.9)),
        name: "Mirror")
    // GGX Metal rough
    .AddSphere(
        centre: new Vector3(0, 0, 0),
        radius: 1.0,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.4),
        name: "RoughMetal")
    // GGX Metal smooth
    .AddSphere(
        centre: new Vector3(2, 0, 0),
        radius: 1.0,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.05),
        name: "SmoothMetal")
    // Dielectric
    .AddSphere(
        centre: new Vector3(4, 0, 0),
        radius: 1.0,
        material: MaterialBuilder.Dielectric(ior: 1.5),
        name: "Glass")
    // Lights
    .AddAreaLight(
        corner: new Vector3(-6, 4, -2),
        edge1: new Vector3(12, 0, 0),
        edge2: new Vector3(0, 0, 4),
        emission: new Vector3(8, 8, 8),
        name: "OverheadLight")
    .Build();