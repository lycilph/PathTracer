# Add-on: Russian Roulette Termination + CLI Progress

## Russian roulette (RR)
RR terminates low-throughput paths early while keeping the estimator unbiased.

At bounce >= rrStart:
- Compute continuation probability p from path throughput (max RGB component)
- With probability (1-p) terminate the path
- If continuing, divide throughput by p

This preserves expectation while reducing average path length.

We clamp p to [0.05, 0.95] to avoid extreme variance.

## CLI progress reporting
The CLI now prints row-level progress as:
- percent complete
- rows processed
- elapsed time

This is implemented via an optional callback from the renderer.
