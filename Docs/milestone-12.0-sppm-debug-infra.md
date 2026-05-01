# Milestone 12.0 — SPPM Debug Infrastructure (UI + AOV Buffers)

## Goal
Prepare the system for Stochastic Progressive Photon Mapping (SPPM) by adding:
- AOV/debug buffers in the core
- UI support to display any buffer
- Eye-pass debug generation (Depth/Normal/Albedo/VisiblePointMask/Throughput)

This milestone intentionally does not implement photon tracing yet.

## Why
SPPM alternates an eye pass (distributed ray tracing) with a photon pass and updates shared statistics progressively.
Debug buffers make it possible to validate each stage visually (visible points, photon density, radius, etc.)
before we implement the full algorithm.

## Buffers
- Beauty (existing progressive path tracer)
- Depth (t)
- Normal (encoded to [0,1])
- Albedo (Lambertian only; others placeholder)
- VisiblePointMask (Lambertian first non-delta hit)
- Throughput (debug proxy)
- Placeholder buffers for later milestones:
  - Radius
  - PhotonCountN
  - PhotonCountM
  - IndirectPhoton

## UI
- Dropdown selector chooses which buffer is displayed
- The image presenter can render from either:
  - the accumulation buffer (Beauty), or
  - a selected debug buffer (AOV)

## Constraints / choices
- Visible points are Lambertian only in the initial implementation.
- Target resolution for development is 1280x720.

## Definition of Done
- You can switch the viewport between Beauty and AOVs.
- The AOVs update during rendering.
- VisiblePointMask correctly marks Lambertian first non-delta hits.
