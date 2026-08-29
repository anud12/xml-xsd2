namespace NewGameProject.Module;

public static class EffectStore
{
    sealed class Effect
    {
        public int Count;
        public long NextAt;
        public readonly Action<long> Run;
        public readonly Func<long> NextInterval;
        public Effect(Action<long> run, Func<long> nextInterval)
        {
            Run = run;
            NextInterval = nextInterval;
            NextAt = long.MinValue;
        }
    }

    static readonly Dictionary<string, Effect> _effects = new();
    static readonly List<Effect> _running = new();
    static bool _processing;

    public static bool IsProcessing => _processing;

    public static void Clear()
    {
        _effects.Clear();
        _running.Clear();
    }

    public static void RegisterEffect(string name, Action<long> run, Func<long> nextInterval)
        => _effects[name] = new Effect(run, nextInterval);

    public static void Emit(string name, object? data)
    {
        if (!_effects.TryGetValue(name, out var effect))
            return;
        if (_running.Contains(effect))
            return;
        _running.Add(effect);
        effect.NextAt = long.MinValue;
    }

    public static void Process(long elapsed)
    {
        _processing = true;
        try
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var e = _running[i];
                while (e.NextAt == long.MinValue || elapsed >= e.NextAt)
                {
                    var prev = e.NextAt == long.MinValue ? 0 : e.NextAt;
                    e.Run(e.Count);
                    e.Count++;
                    var interval = e.NextInterval();
                    if (interval <= 0)
                    {
                        _running.RemoveAt(i);
                        break;
                    }
                    e.NextAt = prev + interval;
                }
            }
        }
        finally
        {
            _processing = false;
        }
    }
}
