
namespace Core.Rendering.SPPM;

public static class RadiusUpdater
{
    public static void Update(
        SppmPixel[] pixels,
        SppmConfig config)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            ref var p = ref pixels[i];

            float n = p.PhotonCount;
            float m = p.IterationPhotonCount;

            if (m <= 0f)
                continue;

            float nNew = n + config.Alpha * m;

            p.Radius *= MathF.Sqrt(nNew / (n + m));

            p.PhotonCount = nNew;
            p.IterationPhotonCount = 0f;
        }
    }
}
