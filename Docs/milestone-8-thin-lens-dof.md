# Milestone 8 — Thin-Lens Depth of Field (DoF)

## Goal
Add a **thin-lens camera model** to support depth of field:

- Objects at the **focus distance** appear sharp
- Objects closer/farther become blurred (“bokeh”)
- Rendering remains deterministic and testable (unit + golden tests)

This milestone intentionally focuses on the **camera model**, not new BRDFs or integrator algorithms.

---

## Background: Pinhole vs Thin-Lens

### Pinhole camera (infinite depth of field)
A pinhole camera emits rays from a single point (the camera origin). Every pixel samples a single direction, so everything is in focus (ignoring diffraction).

### Thin-lens camera (finite aperture)
A thin lens has a **finite aperture radius**, meaning rays originate from different points on the lens. Pixels correspond to points on an “image plane”, but rays are refracted by the lens so they converge on a **focus plane**.

Practical result:
- If a surface is at the **focus distance**, rays from different lens points meet at the same surface point → sharp
- Otherwise, rays land on different points → blur

---

## Model and Parameters

### Inputs
- `lookFrom`, `lookAt`, `vUp` (camera pose)
- `vfovDegrees`, `aspectRatio` (projection)
- `focusDistance` (distance from camera to focus plane along forward direction)
- `apertureRadius` (lens radius)

### Coordinate conventions
We reuse the existing camera basis:
- `w` = normalize(lookFrom - lookAt) (backward)
- `u` = normalize(cross(vUp, w)) (right)
- `v` = cross(w, u) (up)

---

## Ray Construction (Algorithm)

Given pixel coordinates `uScreen`, `vScreen` in [0,1]:

1. Compute the pinhole ray direction to the image plane point:
   - `dir = lowerLeft + uScreen*horizontal + vScreen*vertical - origin`

2. Intersect this ray with the **focus plane**:
   - focus plane is perpendicular to camera forward direction (`forward = -w`)
   - focus plane point: `P0 = origin + forward * focusDistance`
   - solve for `tFocus` where the ray hits the plane:
     - `tFocus = focusDistance / dot(dir, forward)`
   - focus point:
     - `Pfocus = origin + dir * tFocus`

3. Sample a point on the lens disk:
   - sample `(dx, dy)` uniformly on unit disk
   - `lensOffset = (u*dx + v*dy) * apertureRadius`
   - `Plens = origin + lensOffset`

