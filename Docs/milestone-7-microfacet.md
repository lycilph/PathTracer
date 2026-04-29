# Milestone 7 — Microfacet Materials (GGX)

## Goal
Add physically-based microfacet materials:
- Rough metal (conductor-like) using GGX microfacet BRDF

This milestone introduces:
- GGX normal distribution function (NDF)
- Smith masking-shadowing (G)
- Schlick Fresnel with RGB F0 (for metals)
- Importance sampling of the GGX NDF (half-vector sampling)

## BRDF (Cook–Torrance reflection)
For outgoing direction wo and incoming direction wi, with half-vector h = normalize(wi + wo):

f(wo, wi) = (D(h) * G(wo, wi) * F(wi·h)) / (4 * (n·wo) * (n·wi))

Where:
- D(h) is GGX (Trowbridge–Reitz) NDF
- G is Smith geometry term using Schlick-GGX approximation
- F is Schlick Fresnel with RGB F0 (metal reflectance at normal incidence)

## Sampling
We sample the half-vector h from the GGX NDF:
pdf_h = D(h) * (n·h)

Then reflect wo about h to obtain wi (pure reflection lobe).

We convert pdf_h to pdf_wi using:
pdf_wi = pdf_h / (4 * dot(wo, h))

## Integrator notes
- Microfacet metal is NOT delta; NEE + MIS applies as usual.
- The path tracer already uses Evaluate/Pdf/Sample; no integrator changes beyond adding the new material.

## Tests
- Unit tests for sampling validity and PDF consistency
- Golden image test: microfacet sphere showcase under a rectangular area light

To generate/update goldens intentionally:

```bash
UPDATE_GOLDENS=1 dotnet test