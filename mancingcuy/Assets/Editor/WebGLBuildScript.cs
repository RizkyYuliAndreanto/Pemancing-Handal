using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuildScript
{
    public static void Build()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var outputPath = Path.Combine(projectRoot, "WebGLBuild");
        var scenes = new[] { "Assets/FishingPondGenerated/FishingPond.unity" };

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }

        Debug.Log($"WebGL build completed: {report.summary.totalSize} bytes");
    }
}
