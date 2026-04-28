# Milestone 6 — Mirror + Glass Materials

## Goal
Add physically-based **delta** materials:

- **Mirror**: perfect specular reflection  
- **Glass (Dielectric)**: ideal refraction + Fresnel reflection, including **total internal reflection (TIR)**

This milestone also updates the integrator so delta materials work correctly alongside:
- BVH / meshes
- Next Event Estimation (NEE) + MIS (for non-delta)
- Russian roulette termination
- Progressive CLI progress reporting

---

## Why Mirror and Glass are special: Delta BSDFs
Mirror and ideal glass are **delta distributions**:
- they do not scatter light into a range of directions
- they produce exactly **one** outgoing direction for a given incident direction (or a discrete choice between two, reflection vs refraction)

Because of that:
- `Evaluate(wo, wi)` is **not** a regular function over solid angle → return `Vec3.Zero`
- `Pdf(wo, wi)` is **not** meaningful as a density over directions → return `0`

Instead, the correct operation is:
- `Sample(...)` produces the only physically valid direction(s)

---

## Mirror (Perfect Specular Reflection)

### Physics
For an incident direction `v` and surface normal `n`: