// Cornell Box — Classic
// The canonical Cornell Box scene with a glass sphere and a silver
// metallic sphere. Demonstrates global illumination, soft shadows,
// refraction and specular reflection.

return Scene
    .WithCamera(
        position: new Vector3(0, 0, 3.5),
        lookAt: Vector3.Zero,
        fovDegrees: 40)
    .WithRenderSettings(
        imageWidth: 512,
        imageHeight: 512,
        samplesPerPixel: 256)
    .AddQuad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
        new Vector3(0, 0, 2),
        MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
        name: "Floor")
    .AddQuad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
        new Vector3(0, 0, 2),
        MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
        name: "Ceiling")
    .AddQuad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
        new Vector3(0, 2, 0),
        MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
        name: "BackWall")
    .AddQuad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
        new Vector3(0, 0, 2),
        MaterialBuilder.Lambertian(new Vector3(0.65, 0.05, 0.05)),
        name: "LeftWall")
    .AddQuad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
        new Vector3(0, 0, 2),
        MaterialBuilder.Lambertian(new Vector3(0.12, 0.45, 0.15)),
        name: "RightWall")
    .AddSphere(
        centre: new Vector3(0.35, -0.55, 0.2),
        radius: 0.45,
        material: MaterialBuilder.Dielectric(ior: 1.5),
        name: "GlassBall")
    .AddSphere(
        centre: new Vector3(-0.35, -0.55, -0.2),
        radius: 0.45,
        material: MaterialBuilder.GgxMetal(
            new Vector3(0.95, 0.93, 0.88), roughness: 0.05),
        name: "SilverBall")
    .AddAreaLight(
        corner: new Vector3(-0.25, 0.999, -0.25),
        edge1: new Vector3(0.5, 0, 0),
        edge2: new Vector3(0, 0, 0.5),
        emission: new Vector3(15, 15, 15),
        name: "CeilingLight")
    .Build();