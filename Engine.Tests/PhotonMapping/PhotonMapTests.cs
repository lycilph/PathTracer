using Core.Algebra;
using Engine.PhotonMapping;
using FluentAssertions;

namespace Engine.Tests.PhotonMapping;

public class PhotonMapTests
{
    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_EmptyList_CountIsZero()
    {
        var map = new PhotonMap([]);
        map.Count.Should().Be(0);
    }

    [Fact]
    public void Constructor_SinglePhoton_CountIsOne()
    {
        var map = new PhotonMap([MakePhoton(Vector3.Zero)]);
        map.Count.Should().Be(1);
    }

    [Fact]
    public void Constructor_ManyPhotons_CountIsCorrect()
    {
        var photons = MakeGrid(5, 5, 5);
        var map = new PhotonMap(photons);
        map.Count.Should().Be(125);
    }

    // ── FindNearest — basic ───────────────────────────────────────────────────

    [Fact]
    public void FindNearest_EmptyMap_ReturnsEmpty()
    {
        var map = new PhotonMap([]);
        var results = map.FindNearest(Vector3.Zero, k: 5, maxRadius: 10);
        results.Should().BeEmpty();
    }

    [Fact]
    public void FindNearest_SinglePhoton_WithinRadius_ReturnsIt()
    {
        var photon = MakePhoton(new Vector3(0, 0, 0));
        var map = new PhotonMap([photon]);

        var results = map.FindNearest(new Vector3(0.1, 0, 0),
                                      k: 1, maxRadius: 1.0);

        results.Should().HaveCount(1);
        results[0].Photon.Should().BeSameAs(photon);
    }

    [Fact]
    public void FindNearest_SinglePhoton_OutsideRadius_ReturnsEmpty()
    {
        var map = new PhotonMap([MakePhoton(new Vector3(5, 0, 0))]);

        var results = map.FindNearest(Vector3.Zero, k: 1, maxRadius: 1.0);

        results.Should().BeEmpty();
    }

    [Fact]
    public void FindNearest_ReturnsCorrectCount()
    {
        var photons = MakeGrid(3, 3, 3); // 27 photons
        var map = new PhotonMap(photons);

        var results = map.FindNearest(new Vector3(1, 1, 1),
                                      k: 5, maxRadius: 10.0);

        results.Should().HaveCount(5);
    }

    [Fact]
    public void FindNearest_FewerThanKPhotonsInRadius_ReturnsAll()
    {
        var photons = new List<Photon>
        {
            MakePhoton(new Vector3(0, 0, 0)),
            MakePhoton(new Vector3(0.1, 0, 0)),
            MakePhoton(new Vector3(0.2, 0, 0))
        };
        var map = new PhotonMap(photons);

        // Ask for 10 but only 3 exist within radius
        var results = map.FindNearest(Vector3.Zero, k: 10, maxRadius: 1.0);

        results.Should().HaveCount(3);
    }

    // ── FindNearest — correctness ─────────────────────────────────────────────

    [Fact]
    public void FindNearest_ResultsAreSortedByDistanceAscending()
    {
        var photons = new List<Photon>
        {
            MakePhoton(new Vector3(3, 0, 0)),
            MakePhoton(new Vector3(1, 0, 0)),
            MakePhoton(new Vector3(2, 0, 0))
        };
        var map = new PhotonMap(photons);

        var results = map.FindNearest(Vector3.Zero, k: 3, maxRadius: 10.0);

        results.Should().HaveCount(3);
        results[0].DistanceSq.Should().BeLessThan(results[1].DistanceSq);
        results[1].DistanceSq.Should().BeLessThan(results[2].DistanceSq);
    }

    [Fact]
    public void FindNearest_MatchesBruteForce_ForManyPoints()
    {
        // BVH-style correctness test — kd-tree must agree with naive search
        var rng = new Random(42);
        var photons = Enumerable.Range(0, 200)
            .Select(_ => MakePhoton(new Vector3(
                rng.NextDouble() * 10,
                rng.NextDouble() * 10,
                rng.NextDouble() * 10)))
            .ToList();

        var map = new PhotonMap(photons);

        // Test several query points
        var queries = Enumerable.Range(0, 10)
            .Select(_ => new Vector3(
                rng.NextDouble() * 10,
                rng.NextDouble() * 10,
                rng.NextDouble() * 10))
            .ToList();

        foreach (var query in queries)
        {
            const int k = 10;
            const double radius = 5.0;

            // kd-tree result
            var kdResult = map.FindNearest(query, k, radius)
                .Select(r => r.DistanceSq)
                .OrderBy(d => d)
                .ToList();

            // Brute force result
            var bruteResult = photons
                .Select(p => (p, DistSq: (p.Position - query).LengthSquared))
                .Where(x => x.DistSq <= radius * radius)
                .OrderBy(x => x.DistSq)
                .Take(k)
                .Select(x => x.DistSq)
                .ToList();

            kdResult.Should().HaveSameCount(bruteResult,
                because: "kd-tree must find same number of photons as brute force");

            for (var i = 0; i < kdResult.Count; i++)
                kdResult[i].Should().BeApproximately(bruteResult[i], 1e-10,
                    because: $"kd-tree result {i} must match brute force");
        }
    }

    [Fact]
    public void FindNearest_NearestPhoton_IsActuallyNearest()
    {
        var photons = new List<Photon>
        {
            MakePhoton(new Vector3(1, 0, 0)),
            MakePhoton(new Vector3(0.5, 0, 0)),  // closest to origin
            MakePhoton(new Vector3(2, 0, 0)),
            MakePhoton(new Vector3(3, 0, 0))
        };
        var map = new PhotonMap(photons);

        var results = map.FindNearest(Vector3.Zero, k: 1, maxRadius: 10.0);

        results.Should().HaveCount(1);
        results[0].DistanceSq.Should().BeApproximately(0.25, 1e-10);
    }

    // ── GetAllPhotons ─────────────────────────────────────────────────────────

    [Fact]
    public void GetAllPhotons_ReturnsAllStoredPhotons()
    {
        var photons = MakeGrid(3, 3, 3);
        var map = new PhotonMap(photons);

        var all = map.GetAllPhotons();

        all.Should().HaveCount(27);
    }

    [Fact]
    public void GetAllPhotons_EmptyMap_ReturnsEmpty()
    {
        var map = new PhotonMap([]);
        map.GetAllPhotons().Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Photon MakePhoton(Vector3 position) =>
        new(position,
            -Vector3.UnitY,
            new Vector3(0.5, 0.5, 0.5),
            PhotonPathType.Indirect);

    /// <summary>
    /// Creates a regular grid of photons for testing.
    /// </summary>
    private static List<Photon> MakeGrid(int nx, int ny, int nz)
    {
        var photons = new List<Photon>(nx * ny * nz);
        for (var x = 0; x < nx; x++)
            for (var y = 0; y < ny; y++)
                for (var z = 0; z < nz; z++)
                    photons.Add(MakePhoton(new Vector3(x, y, z)));
        return photons;
    }
}
