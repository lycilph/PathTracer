namespace Core.PhotonMapping.Sppm;

public sealed class SppmIterationStats
{
    public int VisiblePointsCreated;
    public int VisiblePointsSkippedNonLambertian;
    public int VisiblePointsMissed;

    public int PhotonsStored;            // from photon tracer stats
    public int PhotonDeposits;           // photon contributed to >=1 visible point
    public int PhotonMisses;             // photon hit Lambertian but found zero visible points

    public double EyePassMs;
    public double PhotonPassMs;
    public double GatherMs;


    // radius diagnostics
    public float RadiusMin;
    public float RadiusAvg;
    public float RadiusMax;
}