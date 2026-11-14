using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("Rage Clay Demo")]
[assembly: AssemblyProduct("Rage Clay Demo")]
[assembly: AssemblyVersion("0.2.0.0")]

internal static class DemoLauncher
{
    private static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char character in value)
        {
            if (character == '\\') { slashes++; continue; }
            if (character == '"') result.Append('\\',slashes * 2 + 1).Append('"');
            else result.Append('\\',slashes).Append(character);
            slashes = 0;
        }
        return result.Append('\\',slashes * 2).Append('"').ToString();
    }
    [STAThread]
    private static int Main(string[] args)
    {
        string directory = AppDomain.CurrentDomain.BaseDirectory;
        string[] required = { "GameJam3_Rage.exe", "UnityPlayer.dll", "MonoBleedingEdge\\EmbedRuntime\\mono-2.0-bdwgc.dll",
            "GameJam3_Rage_Data\\Managed\\Assembly-CSharp.dll", "GameJam3_Rage_Data\\globalgamemanagers", "GameJam3_Rage_Data\\level2",
            "GameJam3_Rage_Data\\ScriptingAssemblies.json", "GameJam3_Rage_Data\\RuntimeInitializeOnLoads.json",
            "GameJam3_Rage_Data\\Managed\\RageRefinement.dll", "GameJam3_Rage_Data\\Refinement\\Assets\\clay-ui.png",
            "GameJam3_Rage_Data\\Refinement\\Assets\\kiln-king.png" };
        foreach (string file in required)
            if (!File.Exists(Path.Combine(directory,file)))
            {
                if (args.Length == 0 || args[0] != "--verify-files") MessageBox.Show("Extract the entire demo ZIP first. Missing file: " + file,"Rage Clay Demo");
                return 2;
            }
        if (args.Length > 0 && args[0] == "--verify-files") return 0;
        var arguments = new StringBuilder("-force-d3d11");
        foreach (string argument in args) arguments.Append(' ').Append(Quote(argument));
        try
        {
            Process.Start(new ProcessStartInfo(Path.Combine(directory,"GameJam3_Rage.exe"),arguments.ToString())
                { WorkingDirectory = directory, UseShellExecute = false });
            return 0;
        }
        catch (Exception error) { MessageBox.Show(error.Message,"Rage Clay Demo"); return 1; }
    }
}
