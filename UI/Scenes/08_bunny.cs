// Stanford Bunny
// The Stanford bunny mesh loaded from an OBJ file, placed inside a
// Cornell Box. Demonstrates mesh loading and BVH acceleration.
// The bunny.obj file must be in the same directory as the executable.
//
// Note: this scene requires bunny.obj to be present next to the executable.
// It can be found in the Render project output directory.

var bunnyPath = System.IO.Path.Combine(
    System.AppContext.BaseDirectory, "bunny.obj");

const double scale = 6.2;
const double translateY = -1.0 - (0.0332 * scale);

return Scene
    .WithCamera(
        position: new Vector3(0, 0, 3.5),
        lookAt: Vector3.Zero,
        fovDegrees: 40)
    .WithRenderSettings(
        imageWidth: 512,
        imageHeight: 512,
        samplesPerPixel: 128)
    // Cornell Box walls
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
    // Stanford Bunny
    .AddMesh(
        path: bunnyPath,
        material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.7, 0.6)),
        transform: Matrix4x4d.Translation(0.1, translateY, -0.2)
                 * Matrix4x4d.RotationY(25)
                 * Matrix4x4d.Scale(scale),
        smoothNormals: false,
        name: "Bunny")
    // Area light
    .AddAreaLight(
        corner: new Vector3(-0.25, 0.999, -0.25),
        edge1: new Vector3(0.5, 0, 0),
        edge2: new Vector3(0, 0, 0.5),
        emission: new Vector3(15, 15, 15),
        name: "CeilingLight")
    .Build();