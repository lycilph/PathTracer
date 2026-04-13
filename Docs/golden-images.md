# Golden Image Infrastructure

Golden images are small reference renders used to detect unintended changes in the renderer.

## Format
We store goldens in a custom binary format **PTGI** (Path Tracer Golden Image):

- Linear RGB floats (no gamma/tone mapping)
- Deterministic by design

This avoids differences caused by output conversions.

## Updating goldens intentionally
If a change is expected (e.g., algorithm improvement), update the goldens by setting:

- `UPDATE_GOLDENS=1`

Examples:

### PowerShell
```powershell
$env:UPDATE_GOLDENS = "1"
dotnet test
```

### Bash
```bash
UPDATE_GOLDENS=1 dotnet test
```

This will rewrite golden files to match the current renderer output.

## Adding a new golden
1. Add a new test that calls `GoldenImageAssert.Matches(...)`.
2. Run tests once with `UPDATE_GOLDENS=1`.
3. Commit the generated `.ptgi` file under `Tracer.Tests/Golden` or a shared `Golden/` folder.

## Choosing thresholds
Because we enforce deterministic sampling and our binary format stores float radiance, most tests can use very tight thresholds.

Recommended:
- Deterministic pipeline: `rmseThreshold = 1e-7`
- If later you add nondeterminism or platform variance: loosen to `1e-4` and report PSNR.
