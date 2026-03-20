// Simple Scene
// A single diffuse sphere lit by an area light.
// Good starting point to verify the renderer is working.

return Scene
    .WithCamera(
        position: new Vector3(0, 0, 4),
        lookAt: Vector3.Zero,
        fovDegrees: 40)
    .WithRenderSettings(
        imageWidth: 512,
        imageHeight: 512,
        samplesPerPixel: 64)
    .AddSphere(
        centre: Vector3.Zero,
        radius: 1.0,
        material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.5, 0.3)),
        name: "Sphere")
    .AddSphere(
        centre: new Vector3(0, -101, 0),
        radius: 100,
        material: MaterialBuilder.Lambertian(new Vector3(0.5, 0.5, 0.5)),
        name: "Ground")
    .AddAreaLight(
        corner: new Vector3(-1, 3, -1),
        edge1: new Vector3(2, 0, 0),
        edge2: new Vector3(0, 0, 2),
        emission: new Vector3(10, 10, 10),
        name: "Light")
    .Build();