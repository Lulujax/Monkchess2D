using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AutoBuild
{
    [MenuItem("Tools/Build Monkchess2D")]
    public static void Build()
    {
        string folder = @"C:\Users\Lulujax\Desktop\Monkchess Build";
        Directory.CreateDirectory(folder);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new string[] { "Assets/Scenes/start.unity", "Assets/Scenes/Game.unity" },
            locationPathName = Path.Combine(folder, "Monkchess2D.exe"),
            target = BuildTarget.StandaloneWindows64
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("BUILD: " + report.summary.result + " -> " + report.summary.outputPath);
    }
}
