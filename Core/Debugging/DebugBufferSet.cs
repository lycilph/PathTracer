using Core.Math;

namespace Core.Debugging;

public sealed class DebugBufferSet
{
    public int Width { get; }
    public int Height { get; }

    private readonly Dictionary<DebugBufferId, Vec3[]> _buffers = new();

    public DebugBufferSet(int width, int height)
    {
        Width = width;
        Height = height;

        // Allocate the common ones now
        Allocate(DebugBufferId.Depth);
        Allocate(DebugBufferId.DirectLighting);
        Allocate(DebugBufferId.Normal);
        Allocate(DebugBufferId.Albedo);
        Allocate(DebugBufferId.VisiblePointMask);
        Allocate(DebugBufferId.Throughput);

        // For photon mapping
        Allocate(DebugBufferId.PhotonHitMapXZ);
        Allocate(DebugBufferId.PhotonFluxMapXZ);
        Allocate(DebugBufferId.Radius);
        Allocate(DebugBufferId.PhotonCountN);
        Allocate(DebugBufferId.PhotonCountM);
        Allocate(DebugBufferId.IndirectPhoton);
    }

    public void Allocate(DebugBufferId id)
    {
        if (_buffers.ContainsKey(id))
            return;

        _buffers[id] = new Vec3[Width * Height];
    }

    public Vec3[] Get(DebugBufferId id) => _buffers[id];

    public void ClearAll()
    {
        foreach (var kv in _buffers)
            Array.Clear(kv.Value);
    }

    public void Clear(DebugBufferId id)
    {
        if (_buffers.TryGetValue(id, out var b))
            Array.Clear(b);
    }

    public void SetPixel(DebugBufferId id, int x, int y, in Vec3 v)
    {
        _buffers[id][y * Width + x] = v;
    }

    public Vec3 GetPixel(DebugBufferId id, int x, int y)
        => _buffers[id][y * Width + x];
}