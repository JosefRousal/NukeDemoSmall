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
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[TeamCity(Version = "2024.03", ManuallyTriggeredTargets = [nameof(Compile)], ImportSecrets = [nameof(EsriApiKey)])]
[TeamCityToken(nameof(EsriApiKey), "25f663b7-67d1-4103-80db-48b7f7ca69ff")]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Pack);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter, Secret] readonly string EsriApiKey;

    [Solution] readonly Solution Solution;

    readonly AbsolutePath BuildOutputDirectory = RootDirectory / "build" / "output";
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    Project GetProject()
    {
        return Solution.GetProject("NukeDemoSmall.Api");
    }
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetClean(_ => _
                .SetProject(GetProject()));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(_ => _
                .SetProjectFile(GetProject()));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(_ => _
                .SetProjectFile(GetProject())
                .SetOutputDirectory(BuildOutputDirectory));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            OctopusTasks.OctopusPack(_ => _
                .SetId("api")
                .SetBasePath(BuildOutputDirectory)
                .SetOutputFolder(ArtifactsDirectory));
        });
}