# Milestone 9 — Motion Blur

## Goal
Add physically correct **motion blur** by sampling time within a camera shutter interval and evaluating moving geometry at that time.

This milestone introduces:
- Camera shutter interval sampling (`shutterOpen..shutterClose`)
- Time-aware geometry (first: `MovingSphere`)
- BVH-safe bounding boxes that enclose object motion
- Unit tests + a tiny golden test to keep everything deterministic and regression-safe

---

## Background: What motion blur is in a path tracer
In real cameras, the shutter is open for a finite time interval. Light arriving during that interval is integrated, so moving objects appear blurred.

In a Monte Carlo renderer, we reproduce this by adding **time** as another sampled dimension:
- Each camera ray receives a time `ray.Time`
- Geometry is evaluated at that time
- The rendered pixel becomes an integral over lens + time + BSDF sampling (and lights)

---

## Design Overview

### Ray time
The renderer already supports `Ray.Time`. In this milestone:
- The **camera** decides the time for each ray
- Objects that can move compute their pose based on `ray.Time`

### Shutter interval
We add shutter parameters to cameras:
- `shutterOpen`
- `shutterClose`

For each ray:
- if `shutterClose <= shutterOpen`: use constant time = `shutterOpen` (no motion blur)
- otherwise sample uniformly:
  - `time = lerp(shutterOpen, shutterClose, sampler.Next1D())`

Determinism is preserved because the camera uses the **per-pixel sampler**.

---

## Implementation Details

### 1) Camera changes
Both cameras implement time sampling:

- `PinholeCamera`:
  - same geometry, but adds `Ray.Time` sampling

- `ThinLensCamera`:
  - continues sampling lens position for DoF
  - additionally samples `Ray.Time` inside the shutter interval

Important note:
- Thin lens already consumes random numbers (lens sampling).
- Motion blur adds an additional random number for time.
- This changes RNG consumption and therefore images; regenerate goldens after enabling motion blur.

### 2) Moving geometry
We add a first moving primitive:

#### `MovingSphere`
A sphere moving linearly from:
- `center0` at `time0`
to
- `center1` at `time1`

Center interpolation:
- `center(time) = lerp(center0, center1, normalizedTime)`

Intersection:
- identical to sphere intersection, but uses `center(ray.Time)`.

Bounding boxes:
- BVH requires a bounding box per object.
- For moving objects, the bounding box must enclose the entire motion interval.
- For `MovingSphere`, we compute bounds at both endpoints and take the surrounding AABB.

---

## BVH Considerations
BVH correctness depends on bounding boxes being conservative:
- Each node’s AABB must fully contain its children for the entire shutter interval.

In this milestone, we keep it simple:
- `MovingSphere.BoundingBox` encloses motion between `time0` and `time1`
- Static objects behave the same as before

This ensures BVH traversal remains correct and fast.

---

## Testing Strategy

### Unit tests
1. **Center interpolation**
   - `Center(time0) == center0`
   - `Center(time1) == center1`
   - midpoint behaves as expected

2. **Time-dependent intersection**
   - same ray origin/direction but different `ray.Time` should hit at different world positions when the sphere moves

3. **Bounding box correctness**
   - AABB must enclose both endpoint spheres
   - ensures BVH safety

4. **Camera shutter time sampling**
   - deterministic test using a fixed RNG value verifies time = lerp(open, close, u)

### Golden test
A small scene designed to clearly show blur:
- a moving sphere under an area light
- shutter interval `[0, 1]`
- fixed seed + SPP + resolution
- golden image stored in `.ptgi` format

To generate/update goldens intentionally:
```bash
UPDATE_GOLDENS=1 dotnet test