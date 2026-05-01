namespace Core.PhotonMapping;

public sealed class PhotonTraceStats
{
    public int PhotonsRequested;
    public int PhotonsEmitted;
    public int PhotonsStored;      // stored on Lambertian
    public int PathsTerminatedRR;
    public int PathsTerminatedMaxDepth;

    public double AvgPathLength;   // computed after pass
}