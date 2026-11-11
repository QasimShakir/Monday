using System;

// Session-wide oral hygiene state. Needs no scene object and no wiring.
// Plaque runs from 0 (clean) to 100 (fully covered) and lasts until the game closes.
// Future scripts (time-of-day decay, eating) should call AddPlaque().
public static class OralHygiene
{
    public const float StartingPlaque = 85f;

    private static float plaque = StartingPlaque;

    public static float Plaque => plaque;
    public static float Cleanliness => 100f - plaque;

    // Fires with the new plaque value whenever it changes.
    public static event Action<float> OnPlaqueChanged;

    public static void SetPlaque(float value)
    {
        value = Math.Max(0f, Math.Min(100f, value));
        if (value == plaque) return;

        plaque = value;
        OnPlaqueChanged?.Invoke(plaque);
    }

    public static void AddPlaque(float amount)
    {
        SetPlaque(plaque + amount);
    }
}
