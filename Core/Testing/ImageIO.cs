using Core.Math;

namespace Core.Testing;

public static class ImageIO
{
    private const uint Magic = 0x49544750; // 'PTGI'

    public static void Save(string path, int width, int height, ReadOnlySpan<Vec3> pixels)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        bw.Write(Magic);
        bw.Write(width);
        bw.Write(height);

        foreach (var p in pixels)
        {
            bw.Write(p.X);
            bw.Write(p.Y);
            bw.Write(p.Z);
        }
    }

    public static (int width, int height, Vec3[] pixels) Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (br.ReadUInt32() != Magic)
            throw new InvalidDataException("Invalid PTGI file");

        int width = br.ReadInt32();
        int height = br.ReadInt32();

        var pixels = new Vec3[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Vec3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        return (width, height, pixels);
    }
}