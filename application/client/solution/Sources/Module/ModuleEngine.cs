using System.Text;
using Jint;

namespace NewGameProject.Module;

public static class ModuleEngine
{
    public static Runtime.Panel[] ExecuteModule(string moduleJs)
    {
        PanelCollector.Clear();
        var engine = new Engine();

        engine.SetValue("__host_registerPanel", new Action<string>(json =>
        {
            PanelCollector.Register(json);
        }));

        engine.Execute(HostApiSetup.Script);

        var wrappedJs = WrapModule(moduleJs);
        try
        {
            engine.Execute(wrappedJs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModuleEngine] {ex.Message}");
        }

        return PanelCollector.ToPanels();
    }

    static string WrapModule(string moduleJs)
    {
        var sb = new StringBuilder();
        var js = moduleJs.Replace("export default ", "").Replace("export default\n", "");

        sb.AppendLine("(function() {");
        sb.AppendLine("  var __mod = " + js + ";");
        sb.AppendLine("  if (typeof __mod === 'function') {");
        sb.AppendLine("    __mod(hostApi);");
        sb.AppendLine("  }");
        sb.AppendLine("})();");

        return sb.ToString();
    }
}
