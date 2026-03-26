// Photon Mapping — Caustics
// A glass sphere focusing light onto a diffuse floor, creating
// a bright caustic pattern. Uses Progressive Photon Mapping (PPM)
// which converges much faster than path tracing for caustics.
//
// Compare with 06_caustics.cs which uses path tracing for the same scene —
// notice how PPM resolves the caustic pattern in far fewer passes.
//
// The render runs indefinitely — press Abort when satisfied with the result.

return Scene
    .WithCamera(
        position: new Vector3(0, 3, 8),
        lookAt: new Vector3(0, 0, 0),
        fovDegrees: 35)
    .WithRenderSettings(
        imageWidth: 512,
        imageHeight: 512,
        samplesPerPixel: 1)
    .WithIntegrator(IntegratorSettings.PhotonMapping(
        photonsPerPass: 100_000,
        maxPasses: 0,
        initialRadius: 0.15,
        kNearest: 50,
        alpha: 0.7))
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
    // Left wall
    .AddQuad(
        corner: new Vector3(-3, -1, -3),
        edge1: new Vector3(0, 4, 0),
        edge2: new Vector3(0, 0, 6),
        material: MaterialBuilder.Lambertian(new Vector3(0.65, 0.05, 0.05)),
        name: "LeftWall")
    // Right wall
    .AddQuad(
        corner: new Vector3(3, -1, -3),
        edge1: new Vector3(0, 4, 0),
        edge2: new Vector3(0, 0, 6),
        material: MaterialBuilder.Lambertian(new Vector3(0.12, 0.45, 0.15)),
        name: "RightWall")
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