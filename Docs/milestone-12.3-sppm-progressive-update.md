# Milestone 12.3 — SPPM Progressive Update (R, N, τ) and Final Radiance

## Goal
Complete the Stochastic Progressive Photon Mapping (SPPM) estimator by adding:

- Persistent per-visible-point statistics
- Progressive radius shrink
- Progressive photon accumulation
- Correct final radiance computation
- Meaningful indirect photon debug images

This milestone removes the remaining bias seen in Milestone 12.2 and produces a convergent indirect lighting solution.

---

## Background

SPPM alternates:
1. An eye pass that generates visible points
2. A photon pass that gathers photons into those points

Unlike classic PPM, SPPM re-generates visible points each iteration while sharing accumulated statistics. This allows support for distributed ray tracing effects while still converging to the correct solution.

---

## Per-Visible-Point State

Each visible point stores:

- **R** — search radius
- **N** — accumulated photon count
- **τ** — accumulated photon flux

Iteration-local values:

- **M** — photons found this iteration
- **Φ** — photon flux this iteration

---

## Progressive Update Equations

Given parameter α ∈ (0,1):