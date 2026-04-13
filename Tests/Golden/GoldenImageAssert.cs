using Core.Math;
using Core.Testing;

namespace Tests.Golden;

public static class GoldenImageAssert
{
    /// <summary>
    /// Asserts that the rendered image matches a golden reference within an RMSE threshold.
    ///
    /// If UPDATE_GOLDENS=1 is set, the golden image is written/updated instead of compared.
    /// </summary>
    public static void Matches(
        string goldenPath,
        int width,
        int height,
        Vec3[] actual,
        float rmseThreshold)
    {
        Assert.Equal(width * height, actual.Length);
        Assert.True(
            ImageMetrics.AllFiniteNonNegative(actual),
            "Actual image contains NaN, Inf, or negative values");

        bool update = Environment.GetEnvironmentVariable("UPDATE_GOLDENS") == "1";

        // Update or bootstrap golden
        if (update || !File.Exists(goldenPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(goldenPath) ?? ".");
            ImageIO.Save(goldenPath, width, height, actual);
            return; // Do not fail in update mode
        }

        // Load golden
        var (gw, gh, golden) = ImageIO.Load(goldenPath);
        Assert.Equal(width, gw);
        Assert.Equal(height, gh);

        float rmse = ImageMetrics.Rmse(golden, actual);
        float psnr = ImageMetrics.PsnrFromRmse(rmse);

        Assert.True(
            rmse <= rmseThreshold,
            $"Golden mismatch. RMSE={rmse} (threshold {rmseThreshold}), PSNR={psnr} dB. Golden={goldenPath}");
    }
}