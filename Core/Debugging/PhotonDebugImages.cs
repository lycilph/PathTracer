using Core.Math;
using Core.PhotonMapping;

namespace Core.Debugging;

public static class PhotonDebugImages
{
    public static void FillPhotonMapsXZ(
        Scene.Scene scene,
        IReadOnlyList<Photon> photons,
        DebugBufferSet dbg)
    {
        int w = dbg.Width;
        int h = dbg.Height;

        dbg.Clear(DebugBufferId.PhotonHitMapXZ);
        dbg.Clear(DebugBufferId.PhotonFluxMapXZ);

        // Use world bounding box to normalize XZ into [0,1]
        if (!scene.World.BoundingBox(out var box))
            throw new InvalidOperationException("Scene world has no bounding box.");

        float minX = box.Min.X;
        float maxX = box.Max.X;
        float minZ = box.Min.Z;
        float maxZ = box.Max.Z;

        float invX = 1f / System.Math.Max(maxX - minX, 1e-6f);
        float invZ = 1f / System.Math.Max(maxZ - minZ, 1e-6f);

        // Accumulate counts and flux magnitude
        var hit = dbg.Get(DebugBufferId.PhotonHitMapXZ);
        var flux = dbg.Get(DebugBufferId.PhotonFluxMapXZ);

        for (int i = 0; i < photons.Count; i++)
        {
            var p = photons[i].Position;

            float nx = (p.X - minX) * invX;
            float nz = (p.Z - minZ) * invZ;

            int x = (int)(nx * (w - 1));
            int y = (int)(nz * (h - 1));

            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                continue;

            int idx = y * w + x;

            // hit count in R channel
            hit[idx] += new Vec3(1f, 0f, 0f);

            // flux magnitude in R
            float m = photons[i].Flux.Length();
            flux[idx] += new Vec3(m, 0f, 0f);
        }

        // Normalize with log scale for display friendliness
        NormalizeLog(hit);
        NormalizeLog(flux);
    }

    private static void NormalizeLog(Vec3[] buf)
    {
        float max = 0f;
        for (int i = 0; i < buf.Length; i++)
            if (buf[i].X > max) max = buf[i].X;

        if (max <= 0f) return;

        float inv = 1f / max;

        for (int i = 0; i < buf.Length; i++)
        {
            float v = buf[i].X * inv;
            // log-ish curve
            v = float.Log(1f + 20f * v) / float.Log(21f);
            buf[i] = new Vec3(v, v, v);
        }
    }
}