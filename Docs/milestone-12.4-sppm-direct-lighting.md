# Milestone 12.4 — Add Direct Lighting at Visible Points (SPPM)

## Goal
Extend the SPPM (12.3) renderer by adding a direct lighting term at Lambertian visible points.
This improves brightness and makes the result closer to standard Cornell reference renders.

The final pixel estimate becomes:

L_pixel = L_direct + L_indirect_sppm

where:
- L_direct is computed by Next Event Estimation (light sampling + shadow ray) at the visible point
- L_indirect_sppm is computed from the progressive photon statistics (τ, R, Ne)

## Implementation Overview

### Direct lighting estimator
Use a shared `DirectLighting.EstimateDirect(...)` helper:
- randomly select one light
- sample the light
- cast a shadow ray for visibility
- evaluate BSDF at the visible point
- optionally apply MIS weight vs BSDF pdf

### Eye pass changes
When a Lambertian visible point is created:
- compute direct lighting at the surface point (NEE)
- multiply by camera throughput β
- accumulate in a persistent per-pixel sum `DirectSum`

### Final radiance
For each visible point after Ne eye passes:
- direct estimate: DirectSum / Ne
- indirect estimate: (β ⊙ τ) / (π R² Ne)
- L = L_direct + L_indirect

### Debug buffers
Add an optional DirectLighting debug buffer to inspect direct vs indirect contributions separately.

## Constraints
- Visible points are Lambertian-only
- Direct lighting is only evaluated for Lambertian visible points
- Specular surfaces remain unsupported until a later fallback/hybrid step

## Definition of Done
- Cornell box becomes noticeably brighter (direct from ceiling light)
- Walls show correct direct illumination gradients
- DirectLighting debug view shows light contribution concentrated on visible surfaces
- IndirectPhoton remains stable and convergent as in 12.3