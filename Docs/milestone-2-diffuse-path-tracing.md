
# Milestone 2 - Diffuse Path Tracing

## Goal
Introduce Monte Carlo integration of the rendering equation using a diffuse (Lambertian) BSDF.

## Estimator
For a surface hit x:

Lo = ∫ fr(x, wi, wo) Li(x, wi) cosθ dwi

Monte Carlo estimator:

Lo ≈ (fr * Li * cosθ) / pdf

## Sampling choice
We use cosine-weighted hemisphere sampling:

pdf(wi) = cosθ / π

This cancels the cosine term in the estimator and reduces variance.

## Properties
- Unbiased
- High variance without direct light sampling (fixed in Milestone 4)
- Deterministic per pixel/sample

## Next
- Emissive surfaces
- Explicit light sampling
- MIS
