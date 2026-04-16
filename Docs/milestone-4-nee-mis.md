# Milestone 4 - Next Event Estimation (NEE) + Multiple Importance Sampling (MIS)

## Goal
Greatly reduce variance (noise) by explicitly sampling lights at each bounce and combining that with BSDF sampling using MIS.

## Direct lighting estimator (NEE)
At a surface point x, we sample a point on a light and connect it (shadow ray):

L_dir = Le(light) * f(wo, wi) * cos(theta) / pdf_light

where pdf_light is the solid-angle PDF at x for the chosen light sampling strategy.

## MIS
We can obtain the same direction wi by:
- sampling the light (NEE)
- sampling the BSDF

MIS combines both with a heuristic weight to reduce variance:

w_light = (pdf_light^2) / (pdf_light^2 + pdf_bsdf^2)

w_bsdf  = (pdf_bsdf^2)  / (pdf_bsdf^2  + pdf_light^2)

## Implementation notes
- We store lights separately in `Scene.Lights` to support direct sampling.
- Area light sampling is uniform in area, converted to solid-angle PDF.
- We treat emissive geometry as a material (`DiffuseLight`) and also register a corresponding light object for NEE.

## Tests
- Unit tests for MIS heuristic
- Unit tests for light PDF conversion
- Determinism and sanity tests updated
- New golden image for Cornell with NEE+MIS

To update goldens intentionally:

```bash
UPDATE_GOLDENS=1 dotnet test
```
