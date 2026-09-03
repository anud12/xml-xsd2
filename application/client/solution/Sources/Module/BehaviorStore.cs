using NewGameProject.Runtime;

namespace NewGameProject.Module;

public static class BehaviorStore
{
    sealed class Running
    {
        public readonly List<JsStep> Steps;
        public int Index;
        public long WaitUntil;
        public bool WaitArmed;
        public Running(List<JsStep> steps)
        {
            Steps = steps;
        }
    }

    public enum StepKind { Action, Wait }

    public sealed class JsStep
    {
        public readonly StepKind Kind;
        public readonly string? ActionName;
        public readonly long WaitUnits;
        public JsStep(StepKind kind, string? actionName, long waitUnits)
        {
            Kind = kind;
            ActionName = actionName;
            WaitUnits = waitUnits;
        }
    }

    static readonly List<Running> _running = new();

    public static void Clear() => _running.Clear();

    public static void Start(List<JsStep> steps) => _running.Add(new Running(steps));

    public static void Process(long elapsed)
    {
        for (int i = _running.Count - 1; i >= 0; i--)
        {
            var r = _running[i];
            while (r.Index < r.Steps.Count)
            {
                var step = r.Steps[r.Index];
                if (step.Kind == StepKind.Wait)
                {
                    if (!r.WaitArmed)
                    {
                        r.WaitUntil = elapsed + step.WaitUnits;
                        r.WaitArmed = true;
                    }
                    if (elapsed < r.WaitUntil)
                        break;
                    r.WaitArmed = false;
                    r.Index++;
                    continue;
                }
                try { RuntimeInterop.emitAction(step.ActionName!); }
                catch (Exception ex)
                {
                    RuntimeInterop.Log($"[BehaviorStore] action error: {ex.Message}");
                }
                r.Index++;
            }
            if (r.Index >= r.Steps.Count)
                _running.RemoveAt(i);
        }
    }
}
