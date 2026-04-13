# Milestone 0 — Foundation

## What we built
- Immutable `Vec3` math type
- Immutable `Ray` with `Time` for future motion blur
- Deterministic RNG using PCG32
- Seed hashing utilities for per-pixel/per-sample determinism
- xUnit tests covering math and RNG behavior

## Why immutable structs
- Prevent accidental mutations
- Easier reasoning in multithreaded rendering
- Better safety when passing values around

## Why PCG32
- High quality for Monte Carlo
- Fast and simple
- Deterministic and stream-friendly

## Testing approach
- Analytical tests for math correctness
- Determinism tests for RNG and seed hashing
- (Later milestones) statistical and golden image tests