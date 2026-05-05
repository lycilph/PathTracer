using CommunityToolkit.Mvvm.ComponentModel;

namespace Studio;

public partial class SppmSettingsViewModel : ObservableObject
{
    // Keep as strings to avoid converters/validation complexity right now.
    // We'll clamp/parse when used.

    [ObservableProperty] private string photonsPerPass = "1000000";
    [ObservableProperty] private string photonMaxDepth = "12";

    // Future settings (placeholders you can enable later without changing UI structure):
    [ObservableProperty] private string alpha = "0.7";
    [ObservableProperty] private string initialRadius = "30";
    [ObservableProperty] private string maxIterations = "0"; // 0 = infinite

    public int GetPhotonsPerPass()
    {
        int v = ParseInt(PhotonsPerPass, 1_000_000);
        // clamp defensively; 1M is fine for early debugging
        return Math.Clamp(v, 1_000, 50_000_000);
    }

    public int GetPhotonMaxDepth()
    {
        int v = ParseInt(PhotonMaxDepth, 12);
        return Math.Clamp(v, 1, 64);
    }

    public float GetSppmAlpha()
    {
        // placeholder for later (12.3)
        if (!float.TryParse(Alpha, out var a)) a = 0.7f;
        return Math.Clamp(a, 0.01f, 0.99f);
    }

    public float GetInitialRadius()
    {
        // placeholder for later (12.2/12.3)
        if (!float.TryParse(InitialRadius, out var r)) r = 30f;
        return Math.Max(1e-3f, r);
    }

    public int GetMaxIterations()
    {
        int v = ParseInt(MaxIterations, 0);
        return Math.Max(0, v);
    }

    private static int ParseInt(string s, int fallback)
      => int.TryParse(s, out var v) ? v : fallback;
}