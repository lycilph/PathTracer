// Roughness Comparison
// Seven GGX metal spheres with roughness values from 0 (perfect mirror)
// to 1 (fully rough), showing how roughness affects the specular lobe.

return Scene
    .WithCamera(
        position: new Vector3(0, 1.5, 6),
        lookAt: new Vector3(0, 0, 0),
        fovDegrees: 45)
    .WithRenderSettings(
        imageWidth: 900,
        imageHeight: 400,
        samplesPerPixel: 128)
    // Ground
    .AddSphere(
        centre: new Vector3(0, -101, 0),
        radius: 100,
        material: MaterialBuilder.Lambertian(new Vector3(0.2, 0.2, 0.2)),
        name: "Ground")
    // Roughness 0.0
    .AddSphere(centre: new Vector3(-3, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.0),
        name: "Roughness0")
    // Roughness 0.1
    .AddSphere(centre: new Vector3(-2, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.1),
        name: "Roughness1")
    // Roughness 0.2
    .AddSphere(centre: new Vector3(-1, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.2),
        name: "Roughness2")
    // Roughness 0.4
    .AddSphere(centre: new Vector3(0, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.4),
        name: "Roughness4")
    // Roughness 0.6
    .AddSphere(centre: new Vector3(1, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.6),
        name: "Roughness6")
    // Roughness 0.8
    .AddSphere(centre: new Vector3(2, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.8),
        name: "Roughness8")
    // Roughness 1.0
    .AddSphere(centre: new Vector3(3, 0, 0), radius: 0.8,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 1.0),
        name: "Roughness10")
    .AddAreaLight(
        corner: new Vector3(-4, 4, -2),
        edge1: new Vector3(8, 0, 0),
        edge2: new Vector3(0, 0, 4),
        emission: new Vector3(8, 8, 8),
        name: "OverheadLight")
    .Build();