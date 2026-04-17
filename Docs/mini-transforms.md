# Mini Transform Wrappers (Translate + Scale)

Before introducing a full transform system (matrices, rotation, normal transforms, instancing), it is very useful to have tiny wrappers for:

- `Translate(IHittable obj, Vec3 offset)`
- `Scale(IHittable obj, float s)` (uniform scale)

These wrappers:
- keep the learning curve small
- enable positioning triangle meshes immediately
- preserve determinism and testability

## Notes
- `Scale` is uniform only. Non-uniform scale requires transforming normals with the inverse transpose.
- Order of wrappers matters: `Translate(Scale(obj, s), t)` != `Scale(Translate(obj, t), s)`.
