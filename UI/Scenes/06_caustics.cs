// Caustics
// A glass sphere focusing light onto a diffuse floor, creating
// a bright caustic pattern. Best appreciated at higher sample counts.
// This scene demonstrates why path tracing converges slowly for caustics.

return Scene
    .WithCamera(
        position: new Vector3(0, 3, 5),
        lookAt: new Vector3(0, 0, 0),
        fovDegrees: 35)
    .WithRenderSettings(
        imageWidth: 512,
        imageHeight: 512,
        samplesPerPixel: 512)
    // Floor
    .AddQuad(
        corner: new Vector3(-3, -1, -3),
        edge1: new Vector3(6, 0, 0),
        edge2: new Vector3(0, 0, 6),
        material: MaterialBuilder.Lambertian(new Vector3(0.9, 0.9, 0.9)),
        name: "Floor")
    // Back wall
    .AddQuad(
        corner: new Vector3(-3, -1, -3),
        edge1: new Vector3(6, 0, 0),
        edge2: new Vector3(0, 4, 0),
        material: MaterialBuilder.Lambertian(new Vector3(0.9, 0.9, 0.9)),
        name: "BackWall")
    // Glass sphere
    .AddSphere(
        centre: new Vector3(0, 0, 0),
        radius: 1.0,
        material: MaterialBuilder.Dielectric(ior: 1.5),
        name: "GlassSphere")
    // Bright overhead light to create strong caustics
    .AddAreaLight(
        corner: new Vector3(-0.5, 3, -0.5),
        edge1: new Vector3(1, 0, 0),
        edge2: new Vector3(0, 0, 1),
        emission: new Vector3(40, 40, 40),
        name: "SpotLight")
    .Build();