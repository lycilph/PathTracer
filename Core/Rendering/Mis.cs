namespace Core.Rendering;

public static class Mis
{
    /// <summary>
    /// Power heuristic with beta=2.
    /// </summary>
    public static float PowerHeuristic(float pdfA, float pdfB)
    {
        if (pdfA <= 0f && pdfB <= 0f) return 0f;
        float a2 = pdfA * pdfA;
        float b2 = pdfB * pdfB;
        return a2 / (a2 + b2);
    }
}