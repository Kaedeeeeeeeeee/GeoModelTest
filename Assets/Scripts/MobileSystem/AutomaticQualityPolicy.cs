using System;

/// <summary>
/// Protects a 30 FPS target by lowering quality after sustained slow gameplay.
/// Auto starts conservatively and never raises quality without an explicit player choice.
/// </summary>
public sealed class AutomaticQualityPolicy
{
    private const float WindowSeconds = 4f;
    private const float WarmupSeconds = 8f;
    private float _warmupRemaining = WarmupSeconds;
    private float _windowSeconds;
    private int _windowFrames;
    private int _slowWindows;

    public int CurrentLevel { get; private set; }
    public float AverageFps { get; private set; }

    public AutomaticQualityPolicy(int initialLevel)
    {
        CurrentLevel = Math.Max(0, initialLevel);
    }

    public void Suspend()
    {
        _warmupRemaining = WarmupSeconds;
        _windowSeconds = 0f;
        _windowFrames = 0;
        _slowWindows = 0;
    }

    public int Sample(float frameSeconds, bool eligible)
    {
        if (!eligible)
        {
            Suspend();
            return CurrentLevel;
        }

        if (float.IsNaN(frameSeconds) || float.IsInfinity(frameSeconds) || frameSeconds <= 0f)
        {
            return CurrentLevel;
        }

        // Bound isolated synchronous-load stalls without hiding sustained very low FPS.
        float elapsed = Math.Min(frameSeconds, 0.25f);
        if (_warmupRemaining > 0f)
        {
            _warmupRemaining -= elapsed;
            return CurrentLevel;
        }

        _windowSeconds += elapsed;
        _windowFrames++;
        if (_windowSeconds < WindowSeconds)
        {
            return CurrentLevel;
        }

        AverageFps = _windowFrames / _windowSeconds;
        _windowSeconds = 0f;
        _windowFrames = 0;

        _slowWindows = AverageFps < 26f ? _slowWindows + 1 : 0;
        if (CurrentLevel > 0 && (_slowWindows >= 2 || AverageFps < 18f))
        {
            CurrentLevel--;
            Suspend();
        }

        return CurrentLevel;
    }
}
