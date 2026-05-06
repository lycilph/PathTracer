# Milestone 12.2 — SPPM Visible Points + Photon Gathering (Lambertian only)

## Goal
Connect the two halves of SPPM by implementing:
- Eye pass: generate VisiblePoints on Lambertian surfaces only
- Spatial hash grid over visible points
- Photon gathering: deposit photons into nearby visible points (within radius)

This milestone focuses on correctness and debuggability, not performance.

## Inputs (shared SPPM settings)
- Photons/pass (default 1,000,000)
- Photon max depth (default 12)
- Initial radius R0 (default 30 in Cornell scale)

## Outputs / Debug buffers
- VisiblePointMask
- PhotonCountM (photons found per pixel in current iteration)
- IndirectPhoton (provisional photon-only contribution)

## Constraints
- Visible points and photon gathering are Lambertian-only.
- Delta materials are traversed (photons and eye paths pass through them), but no storage/gathering occurs on delta surfaces.

## Definition of Done
- VisiblePointMask shows Lambertian hit points
- PhotonCountM is non-zero in expected areas
- IndirectPhoton shows plausible energy distribution
- Stats show visible point counts, photon deposits, and miss rates