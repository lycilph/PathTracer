# Milestone 1 - Rays, Camera, Spheres, Image Output

## Goal
Establish the first end-to-end rendering pipeline:
- Construct rays from a pinhole camera
- Intersect rays with spheres
- Shade hits deterministically (normal visualization)
- Write an image to disk

This milestone is intentionally **non-Monte-Carlo** to keep the mathematics and debugging surface small.

## Mathematical Notes

### Ray
A ray is defined as:

**r(t) = O + tD**

where **O** is origin, **D** is direction, and **t >= 0**.

### Ray-Sphere intersection
Sphere: center **C**, radius **R**.

Solve for t:

||(O + tD) - C||^2 = R^2

Rearranged into a quadratic:

a t^2 + 2b t + c = 0

with:
- a = D·D
- b = (O-C)·D
- c = (O-C)·(O-C) - R^2

Discriminant:

Delta = b^2 - a c

If Delta < 0 => no hit.

We select the smallest root within [tMin, tMax].

## Output
We output **PPM (P6)** for zero dependencies.

## How to run

Add the CLI project to your solution:

```bash
dotnet sln PathTracer.sln add src/Tracer.Cli/Tracer.Cli.csproj
dotnet run --project src/Tracer.Cli
```

Optional args:

```bash
dotnet run --project src/Tracer.Cli -- 1280 720 out.ppm
```

## Tests
- Sphere intersection: hit and miss cases
- Camera determinism and a basic direction sanity check
