namespace Core.PhotonMapping.Sppm;

public static class SppmUpdater
{
    /// <summary>
    /// Apply the SPPM progressive update to one visible point.
    /// </summary>
    public static void Update(VisiblePoint vp, float alpha)
    {
        if (vp.M == 0)
            return;

        float Nold = vp.N;
        float Mold = vp.M;

        float Nnew = Nold + alpha * Mold;

        float ratio = Nnew / (Nold + Mold);
        float Rnew = vp.Radius * float.Sqrt(ratio);

        float scale = (Rnew * Rnew) / (vp.Radius * vp.Radius);
        vp.Tau += vp.Phi * scale;

        vp.N = Nnew;
        vp.Radius = Rnew;
    }
}
