namespace Core.Random;


/// <summary>
/// Deterministic random source used by samplers.
/// </summary>
public interface IRng
{
    uint NextUInt();

    /// <summary>
    /// Returns a float in [0,1).
    /// </summary>
    float NextFloat01();
}
