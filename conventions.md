# Conventions

## Coordinate system
- Right-handed
- +Y is up
- +X is right
- -Z is forward (camera looks towards -Z by default)

## Units
- Scene units are treated as meters-like but technically unitless.

## Color
- Rendering math is performed in linear RGB.
- Initial output (early milestones) converts to sRGB with gamma (later we add HDR + tone mapping).

## Determinism policy (important!)
- Randomness is derived from (pixelX, pixelY, sampleIndex, baseSeed).
- This guarantees reproducible renders independent of thread scheduling and tile order.