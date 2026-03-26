using Core;
using Core.Algebra;
using Core.Geometry;
using Engine.Materials;
using Engine.PhotonMapping;
using FluentAssertions;

namespace Engine.Tests.PhotonMapping;

public class RadianceEstimatorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    //private static HitRecord MakeHit(
    //    Vector3? point = null,
    //    Vector3? normal = null,
    //    IMaterial? material = null)
    //{
    //    var mat = material ?? new Lambertian(new Vector3(0.8, 0.8, 0.8));
    //    var p = point ?? Vector3.Zero;
    //    var n = normal ?? Vector3.UnitY;
    //    var ray = new Core.Algebra.Ray(p - n, n);
    //    return HitRecord.Create(1.0, p, ray, n, mat);
    //}

    private static HitRecord MakeHit(
    Vector3? point = null,
    Vector3? normal = null,
    IMaterial? material = null)
    {
        var mat = material ?? new Lambertian(new Vector3(0.8, 0.8, 0.8));
        var p = point ?? Vector3.Zero;
        var n = normal ?? Vector3.UnitY;

        // Ray comes from above (opposite to normal) so it hits the front face
        // and the normal is preserved correctly
        var ray = new Core.Algebra.Ray(p + n * 2, -n);
        return HitRecord.Create(1.0, p, ray, n, mat);
    }
    
    private static Photon MakePhoton(
    Vector3 position,
    Vector3? power = null,
    PhotonPathType pathType = PhotonPathType.Indirect)
    {
        // Direction pointing into the surface from above (downward)
        // This opposes UnitY normal so dot(direction, normal) < 0
        return new Photon(
            position,
            new Vector3(0, -1, 0),
            power ?? new Vector3(1, 1, 1),
            pathType);
    }

    private static PixelEstimationState MakeState(double radius = 1.0) =>
        PixelEstimationState.Initial(radius);

    // ── Empty photon map ──────────────────────────────────────────────────────

    [Fact]
    public void Estimate_EmptyPhotonMap_ReturnsZero()
    {
        var estimator = new RadianceEstimator();
        var map = new PhotonMap([]);
        var hit = MakeHit();
        var state = MakeState();

        var result = estimator.Estimate(hit, map, ref state);

        result.Should().Be(Vector3.Zero);
    }

    // ── Basic estimation ──────────────────────────────────────────────────────

    [Fact]
    public void Estimate_SinglePhotonAtHitPoint_ReturnsNonZero()
    {
        var estimator = new RadianceEstimator { KNearest = 1 };
        var map = new PhotonMap([MakePhoton(Vector3.Zero)]);
        var hit = MakeHit(Vector3.Zero);
        var state = MakeState(radius: 1.0);

        var result = estimator.Estimate(hit, map, ref state);

        result.X.Should().BeGreaterThan(0);
        result.Y.Should().BeGreaterThan(0);
        result.Z.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Estimate_PhotonOutsideRadius_ReturnsZero()
    {
        var estimator = new RadianceEstimator { KNearest = 1 };
        // Photon is 10 units away, radius is 0.5
        var map = new PhotonMap([MakePhoton(new Vector3(10, 0, 0))]);
        var hit = MakeHit(Vector3.Zero);
        var state = MakeState(radius: 0.5);

        var result = estimator.Estimate(hit, map, ref state);

        result.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Estimate_MorePhotonsNearby_ProducesHigherRadiance()
    {
        var estimator = new RadianceEstimator { KNearest = 10 };

        // Dense photon cluster near hit point
        var densePhotons = Enumerable.Range(0, 50)
            .Select(i => MakePhoton(new Vector3(i * 0.01, 0, 0)))
            .ToList();

        // Sparse photons far from hit point
        var sparsePhotons = Enumerable.Range(0, 5)
            .Select(i => MakePhoton(new Vector3(i * 0.5, 0, 0)))
            .ToList();

        var denseMap = new PhotonMap(densePhotons);
        var sparseMap = new PhotonMap(sparsePhotons);

        var denseHit = MakeHit(Vector3.Zero);
        var sparseHit = MakeHit(Vector3.Zero);

        var denseState = MakeState(radius: 1.0);
        var sparseState = MakeState(radius: 1.0);

        var denseResult = estimator.Estimate(denseHit, denseMap,
                                             ref denseState);
        var sparseResult = estimator.Estimate(sparseHit, sparseMap,
                                              ref sparseState);

        denseResult.X.Should().BeGreaterThan(sparseResult.X,
            because: "denser photon distribution should produce higher radiance");
    }

    // ── PPM radius update ─────────────────────────────────────────────────────

    [Fact]
    public void Estimate_RadiusShrinks_AfterEachPass()
    {
        var estimator = new RadianceEstimator
        {
            KNearest = 5,
            Alpha = 0.7
        };

        var photons = Enumerable.Range(0, 20)
            .Select(i => MakePhoton(new Vector3(i * 0.05, 0, 0)))
            .ToList();

        var map = new PhotonMap(photons);
        var hit = MakeHit(Vector3.Zero);
        var state = MakeState(radius: 1.0);

        var initialRadius = state.Radius;

        estimator.Estimate(hit, map, ref state);

        state.Radius.Should().BeLessThan(initialRadius,
            because: "PPM radius must shrink after each pass");
    }

    [Fact]
    public void Estimate_RadiusNeverGrowsLarger()
    {
        var estimator = new RadianceEstimator
        {
            KNearest = 5,
            Alpha = 0.7
        };

        var photons = Enumerable.Range(0, 20)
            .Select(i => MakePhoton(new Vector3(i * 0.05, 0, 0)))
            .ToList();

        var map = new PhotonMap(photons);
        var hit = MakeHit(Vector3.Zero);
        var state = MakeState(radius: 1.0);

        // Run multiple passes
        var previousRadius = state.Radius;
        for (var pass = 0; pass < 10; pass++)
        {
            estimator.Estimate(hit, map, ref state);
            state.Radius.Should().BeLessThanOrEqualTo(previousRadius,
                because: $"radius must not grow at pass {pass}");
            previousRadius = state.Radius;
        }
    }

    [Fact]
    public void Estimate_AccumulatedPhotonCount_IncreasesEachPass()
    {
        var estimator = new RadianceEstimator { KNearest = 5 };

        var photons = Enumerable.Range(0, 20)
            .Select(i => MakePhoton(new Vector3(i * 0.05, 0, 0)))
            .ToList();

        var map = new PhotonMap(photons);
        var hit = MakeHit(Vector3.Zero);
        var state = MakeState(radius: 1.0);

        estimator.Estimate(hit, map, ref state);
        var afterFirstPass = state.AccumulatedPhotonCount;

        estimator.Estimate(hit, map, ref state);
        var afterSecondPass = state.AccumulatedPhotonCount;

        afterSecondPass.Should().BeGreaterThan(afterFirstPass,
            because: "accumulated photon count must increase each pass");
    }

    // ── Normal facing ─────────────────────────────────────────────────────────

    [Fact]
    public void Estimate_PhotonFromBehindSurface_IsIgnored()
    {
        var estimator = new RadianceEstimator { KNearest = 1 };

        // Photon travelling upward — same direction as normal (UnitY)
        // This means dot(photonDir, normal) > 0 — coming from behind
        var backfacePhoton = new Photon(
            Vector3.Zero,
            Vector3.UnitY,  // same direction as normal — behind surface
            new Vector3(1, 1, 1),
            PhotonPathType.Indirect);

        var map = new PhotonMap([backfacePhoton]);

        // Ray comes from above hitting a surface with upward normal
        var ray = new Core.Algebra.Ray(new Vector3(0, 2, 0), -Vector3.UnitY);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY,
                                   new Lambertian(new Vector3(0.8, 0.8, 0.8)));
        var state = MakeState(radius: 1.0);

        var result = estimator.Estimate(hit, map, ref state);

        result.Should().Be(Vector3.Zero,
            because: "photons arriving from behind the surface must be ignored");
    }

    // ── Material response ─────────────────────────────────────────────────────

    [Fact]
    public void Estimate_HigherAlbedo_ProducesHigherRadiance()
    {
        var estimator = new RadianceEstimator { KNearest = 5 };
        var photons = Enumerable.Range(0, 10)
            .Select(i => MakePhoton(new Vector3(i * 0.05, 0, 0)))
            .ToList();
        var map = new PhotonMap(photons);

        var brightHit = MakeHit(
            material: new Lambertian(new Vector3(0.9, 0.9, 0.9)));
        var darkHit = MakeHit(
            material: new Lambertian(new Vector3(0.1, 0.1, 0.1)));

        var brightState = MakeState(radius: 1.0);
        var darkState = MakeState(radius: 1.0);

        var brightResult = estimator.Estimate(brightHit, map,
                                              ref brightState);
        var darkResult = estimator.Estimate(darkHit, map,
                                            ref darkState);

        brightResult.X.Should().BeGreaterThan(darkResult.X,
            because: "brighter material should produce higher radiance estimate");
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void PixelEstimationState_Initial_HasZeroFluxAndCount()
    {
        var state = PixelEstimationState.Initial(0.5);

        state.AccumulatedFlux.Should().Be(Vector3.Zero);
        state.AccumulatedPhotonCount.Should().Be(0);
        state.Radius.Should().Be(0.5);
    }

    [Fact]
    public void PixelEstimationState_Initial_PreservesRadius()
    {
        var state = PixelEstimationState.Initial(0.123);
        state.Radius.Should().BeApproximately(0.123, 1e-10);
    }
}