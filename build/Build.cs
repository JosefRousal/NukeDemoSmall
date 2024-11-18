using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Octopus;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[TeamCity(
    Version = "2024.03", 
    ManuallyTriggeredTargets = [nameof(Run), nameof(Dev)], 
    ImportSecrets = [nameof(OctopusApiKey)],
    CleanCheckoutDirectory = false)]
[TeamCityToken(nameof(OctopusApiKey), "f77ace47-33af-4b49-aa92-87c6b6a67696")]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Run);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Parameter, Secret] readonly string OctopusApiKey;

    [Solution] readonly Solution Solution;

    readonly AbsolutePath BuildOutputDirectory = RootDirectory / "build" / "output";
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    Project GetProject()
    {
        return Solution.GetProject("NukeDemoSmall.Api");
    }

    Target Dev => _ => _
        .Executes(() =>
        {
            Log.Information("Some dev info");
        });

    Target Run => _ => _
        .Executes(() =>
        {
            var project = GetProject();
            
            DotNetTasks.DotNetClean(_ => _
                .SetProject(project));

            DotNetTasks.DotNetRestore(_ => _
                .SetProjectFile(project));

            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(project)
                .SetOutputDirectory(BuildOutputDirectory));

            OctopusTasks.OctopusPack(_ => _
                .SetId("api")
                .SetBasePath(BuildOutputDirectory)
                .SetOutputFolder(ArtifactsDirectory));

            OctopusTasks.OctopusPush(_ => _
                .SetPackage(ArtifactsDirectory / "*.nupkg")
                .SetServer("")
                .SetApiKey(OctopusApiKey));
        });
}