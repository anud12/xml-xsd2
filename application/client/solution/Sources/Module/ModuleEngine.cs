using System.Text;
using Jint;
using Jint.Native;
using NewGameProject.Runtime;

namespace NewGameProject.Module;

public static class ModuleEngine
{
    internal static HashSet<string> ArchiveFileSet = new();

    public static Runtime.Panel[] ExecuteModule(string moduleJs)
        => ExecuteModule(moduleJs, clearCollector: true);

    /// <c>clearCollector</c> stays false for sub-modules so a multi-module
    /// archive keeps every module's panels (ProcessArchive runs each index.js
    /// through its own engine).
    public static Runtime.Panel[] ExecuteModule(string moduleJs, bool clearCollector)
    {
        if (clearCollector)
            PanelCollector.Clear();
        var engine = new Engine();

        engine.SetValue("__host_registerPanel", new Action<string>(json =>
        {
            PanelCollector.Register(json);
        }));

        engine.SetValue("__host_fileExists", new Func<string, bool>(path =>
            ArchiveFileSet.Contains(path)));

        // Actions are executed by the native runtime: the C# side only records
        // the declarations. A no-op registerAction keeps the shim happy.
        engine.SetValue("__host_registerAction", new Action<string, JsValue>((name, apply) => { }));

        // Effects are stored C#-side: prepare/apply run in the main engine
        // (so closures from module scope are available) and re-run on each
        // RunIteration while the effect's reoccurAfterMs interval keeps it alive.
        // The prepare return value is passed to apply as its output argument.
        var runtimeValRef = new JsValue[1];
        Action<string, JsValue, JsValue, JsValue> registerEffect =
            (name, reoccurAfterMs, prepare, apply) =>
            {
                try
                {
                    var safeName = name.Replace(".", "_").Replace(":", "_");
                    engine.SetValue($"__effect_reoccur_{safeName}", reoccurAfterMs ?? JsValue.Null);
                    engine.SetValue($"__effect_prepare_{safeName}", prepare ?? JsValue.Null);
                    engine.SetValue($"__effect_apply_{safeName}", apply ?? JsValue.Null);
                    var hasReoccur = reoccurAfterMs != null && !reoccurAfterMs.IsUndefined() && !reoccurAfterMs.IsNull();
                    var hasPrepare = prepare != null && !prepare.IsUndefined() && !prepare.IsNull();
                    var hasApply = apply != null && !apply.IsUndefined() && !apply.IsNull();
                    EffectStore.RegisterEffect(name,
                        count =>
                        {
                            JsValue output = JsValue.Null;
                            try
                            {
                                if (hasPrepare)
                                    output = engine.Invoke($"__effect_prepare_{safeName}", runtimeValRef[0], count, runtimeValRef[0], runtimeValRef[0]);
                                if (hasApply)
                                {
                                    engine.SetValue("__inApply", true);
                                    try
                                    {
                                        engine.Invoke($"__effect_apply_{safeName}", runtimeValRef[0], output);
                                    }
                                    finally
                                    {
                                        engine.SetValue("__inApply", false);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                RuntimeInterop.Log($"[ModuleEngine] effect '{name}' count={count} error: {ex.Message}");
                            }
                        },
                        () =>
                        {
                            if (!hasReoccur)
                                return 0;
                            try
                            {
                                var res = engine.Invoke($"__effect_reoccur_{safeName}", runtimeValRef[0], 0, runtimeValRef[0], runtimeValRef[0]);
                                if (res.IsNumber())
                                    return (long)res.AsNumber();
                                if (res.IsObject())
                                {
                                    engine.SetValue("__tmp_reoccur_res", res);
                                    var objJson = engine.Evaluate("JSON.stringify(globalThis.__tmp_reoccur_res)").ToString();
                                    if (objJson.Contains("__maybe"))
                                        return -1;
                                }
                                if (res.IsUndefined() || res.IsNull())
                                    return -1;
                            }
                            catch (Exception ex)
                            {
                                RuntimeInterop.Log($"[ModuleEngine] effect '{name}' reoccur error: {ex.Message}");
                            }
                            return 0;
                        });
                }
                catch (Exception ex)
                {
                    RuntimeInterop.Log($"[ModuleEngine] registerEffect error: {ex.Message}");
                }
            };

        engine.SetValue("__host_registerEffect", registerEffect);

        engine.SetValue("__host_attachBehavior", new Action<string, string>((entityId, behaviorName) =>
        {
            try
            {
                var stepsJson = engine.Evaluate(
                    $"JSON.stringify(globalThis.__behaviorDefinitions && "
                    + $"globalThis.__behaviorDefinitions[{System.Text.Json.JsonSerializer.Serialize(behaviorName)}] && "
                    + $"(__behavior_steps({System.Text.Json.JsonSerializer.Serialize(behaviorName)}) || null))").ToString();
                if (!string.IsNullOrEmpty(stepsJson) && stepsJson != "null")
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(stepsJson);
                    var steps = new List<BehaviorStore.JsStep>();
                    foreach (var st in doc.RootElement.EnumerateArray())
                    {
                        if (st.TryGetProperty("action", out var a) && a.ValueKind == System.Text.Json.JsonValueKind.String)
                            steps.Add(new BehaviorStore.JsStep(BehaviorStore.StepKind.Action, a.GetString(), 0));
                        else if (st.TryGetProperty("wait", out var w))
                        {
                            long units = w.ValueKind == System.Text.Json.JsonValueKind.Number ? w.GetInt64() : 0;
                            steps.Add(new BehaviorStore.JsStep(BehaviorStore.StepKind.Wait, null, units));
                        }
                    }
                    BehaviorStore.Start(steps);
                    RuntimeInterop.Log($"behavior attached: {entityId} -> {behaviorName}");
                }
                else
                {
                    RuntimeInterop.Log($"[ModuleEngine] attachBehavior: no steps for '{behaviorName}'");
                }
            }
            catch (Exception ex)
            {
                RuntimeInterop.Log($"[ModuleEngine] attachBehavior error: {ex.Message}");
            }
        }));

        engine.SetValue("__host_log", new Action<string>(message =>
        {
            RuntimeInterop.Log(message);
        }));

        var _emitting = new HashSet<string>();
        JsValue EmitEffect(string n, JsValue d)
        {
            var safe = n.Replace(".", "_").Replace(":", "_");
            if (EffectStore.IsProcessing)
            {
                var childPrepare = engine.GetValue($"__effect_prepare_{safe}");
                var childApply = engine.GetValue($"__effect_apply_{safe}");
                if (childPrepare == null || childPrepare.IsUndefined() || childPrepare.IsNull())
                    return JsValue.Null;
                if (!_emitting.Add(safe))
                    return JsValue.Null;
                try
                {
                    var output = engine.Invoke(childPrepare, runtimeValRef[0], 0, d ?? JsValue.Null, JsValue.Null);
                    if (childApply != null && !childApply.IsUndefined() && !childApply.IsNull())
                    {
                        engine.SetValue("__inApply", true);
                        try
                        {
                            engine.Invoke(childApply, runtimeValRef[0], output);
                        }
                        finally
                        {
                            engine.SetValue("__inApply", false);
                        }
                    }
                    return output;
                }
                catch (Exception ex)
                {
                    RuntimeInterop.Log($"[ModuleEngine] emitEffect '{n}' error: {ex.Message}");
                    return JsValue.Null;
                }
                finally
                {
                    _emitting.Remove(safe);
                }
            }
            else
            {
                EffectStore.Emit(n, d);
                return JsValue.Null;
            }
        }
        engine.SetValue("__host_emitEffect", new Action<string, JsValue>((n, d) => EmitEffect(n, d)));
        engine.SetValue("__host_emitEffectResult", new Func<string, JsValue, JsValue>(EmitEffect));

        engine.SetValue("__host_setEntityText", new Action<string, string, string>(
            (entityId, key, value) =>
            {
                try { RuntimeInterop.SetEntityTextMapValue(entityId, key, value); }
                catch (Exception ex) { RuntimeInterop.Log($"[ModuleEngine] setEntityText error: {ex.Message}"); }
            }));

        engine.SetValue("__host_setEntityNumber", new Action<string, string, double>(
            (entityId, key, value) =>
            {
                try { RuntimeInterop.SetEntityNumberMapValue(entityId, key, value); }
                catch (Exception ex) { RuntimeInterop.Log($"[ModuleEngine] setEntityNumber error: {ex.Message}"); }
            }));

        engine.Execute(HostApiSetup.Script);
        runtimeValRef[0] = engine.Evaluate("hostApi.runtime");

        var wrappedJs = WrapModule(moduleJs);
        try
        {
            engine.Execute(wrappedJs);
        }
        catch (Exception ex)
        {
            RuntimeInterop.Log($"[ModuleEngine] execute error: {ex.Message}");
        }

        return PanelCollector.ToPanels();
    }

    static string WrapModule(string moduleJs)
    {
        var sb = new StringBuilder();
        var js = moduleJs.Replace("export default ", "");

        sb.AppendLine("(function() {");
        sb.AppendLine("  var __mod = " + js + ";");
        sb.AppendLine("  if (typeof __mod === 'function') {");
        sb.AppendLine("    try {");
        sb.AppendLine("      __mod(hostApi);");
        sb.AppendLine("    } catch (e) {");
        sb.AppendLine("      __host_log('[module] entrypoint error: ' + (e && e.message ? e.message : e));");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("})();");

        return sb.ToString();
    }
}
