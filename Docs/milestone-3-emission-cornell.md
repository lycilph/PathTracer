# Milestone 3 - Emissive Materials and Cornell Box

## Goal
Introduce emissive materials (area lights) and add a Cornell Box test scene.

This milestone adds:
- Per-object materials
- Emission via a `DiffuseLight` material
- Axis-aligned rectangles and boxes (for Cornell geometry)
- A path tracing integrator that accumulates emitted radiance

## Rendering equation update
We now include emitted radiance:

Lo(x, wo) = Le(x, wo) + ∫ fr(x, wi, wo) Li(x, wi) cosθ dwi

In code, when a ray hits a surface:

1. Add `Le` from the material (if emissive)
2. If the material scatters, continue tracing and multiply by throughput

## Diffuse simplification
For Lambertian materials:
- fr = albedo / pi
- we sample with cosine-weighted hemisphere pdf = cosθ / pi

Therefore:
  (fr * cosθ) / pdf = albedo

So the throughput update becomes:

  L = Le + albedo ⊙ Li(next)

where ⊙ is component-wise (Hadamard) multiplication.

## Known limitations (intentional)
- No explicit light sampling (next-event estimation)
- No MIS
- No box rotation / transforms yet (boxes are axis-aligned)

These are addressed in Milestones 4+.

## Tests
- Unit tests for emission behavior
- Unit tests for rectangle intersection
- Golden image test for a tiny Cornell render

## Running

```bash
# example
 dotnet run --project src/Tracer.Cli -- 400 400 100 cornell.ppm
```

To generate/update goldens:

```bash
UPDATE_GOLDENS=1 dotnet test
```
