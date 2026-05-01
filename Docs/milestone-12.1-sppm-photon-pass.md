# Milestone 12.1 — SPPM Photon Pass + Debug Images

## Goal
Implement the photon tracing half of SPPM and provide debug images and stats so we can verify:
- photons are emitted correctly from lights
- photon paths traverse the scene (including delta materials)
- photons are stored on Lambertian surfaces (initial restriction)
- the spatial distribution of photon hits is sensible

This milestone does not implement gathering into visible points yet.

## Key additions
- IPhotonEmitter interface for light emission sampling
- RectAreaLightXZ implements IPhotonEmitter (Lambertian emission)
- PhotonTracer traces photons through the scene and stores hits on Lambertian surfaces
- Debug images:
  - PhotonHitMapXZ (top-down heatmap of stored photon hits)
  - PhotonFluxMapXZ (top-down heatmap weighted by photon flux magnitude)
- UI integration: view selector can display photon debug maps
- Stats: emitted/stored photons, average path length, termination reasons

## Constraints / choices
- Photons are stored only when hitting Lambertian materials.
- Delta materials are still traversed to allow photons to reach diffuse surfaces through specular interactions.

## Definition of Done
- Photon pass runs at 1280x720 project settings (debug images rendered at same resolution).
- PhotonHitMapXZ shows photon concentration in expected scene regions.
- Stats update correctly for each photon pass.
- Switching debug views after a pass updates immediately (full-frame refresh).