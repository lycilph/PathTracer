# Milestone 6.1 — Tinted Glass (Beer–Lambert Absorption)

## Goal
Extend the dielectric (glass) material to support tint via absorption inside the medium.

## Model
We use Beer–Lambert attenuation:

T(d) = exp(-sigmaA * d)

where sigmaA is per-channel absorption (RGB) and d is traveled distance inside the medium.

## Parameterization
We provide a user-friendly constructor:

Dielectric(ior, tint, absorptionStrength)

sigmaA = -ln(tint) * absorptionStrength

- tint is the desired transmittance color at distance 1 (before scaling)
- absorptionStrength scales absorption to match scene units

Example (Cornell scale):
tint = (0.6, 0.9, 0.6)
absorptionStrength = 0.01

## Integrator support
The path tracer tracks a current medium absorption coefficient along the path.
For each ray segment:
- compute distance to the next hit: t
- multiply radiance by T(t)
On dielectric refraction:
- entering: set medium sigmaA to the dielectric’s sigmaA
- exiting: clear medium sigmaA

## Tests
- Unit test verifies transmittance math (0.5 at distance 2 becomes 0.25)
- Golden test renders Cornell with tinted glass sphere