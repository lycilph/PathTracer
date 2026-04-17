# Milestone 5 - Meshes + BVH Acceleration

## Goal
Add support for triangle meshes and an acceleration structure (BVH) to render complex geometry efficiently.

This milestone adds:
- Axis-aligned bounding boxes (AABB)
- Bounding boxes for all primitives
- BVH construction and traversal
- Triangle primitive (Moller-Trumbore)
- Minimal OBJ loader (v + f)

## Why BVH?
Naively testing every triangle for every ray is O(N) per ray.
BVH reduces average intersection cost dramatically by pruning large groups of triangles via bounding boxes.

## Determinism
BVH build is deterministic:
- axis = largest centroid extent
- stable sort by centroid along axis

This keeps golden tests stable.

## Testing
- AABB hit/miss tests
- BVH vs brute-force consistency test
- OBJ loader triangle count test using an embedded OBJ string
- Golden scene can be updated after enabling BVH (output should remain stable)
