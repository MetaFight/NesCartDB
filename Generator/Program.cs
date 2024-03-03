using Microsoft.Extensions.DependencyInjection;
using Statiq.Web.Pipelines;

var pwd = new DirectoryInfo(Environment.CurrentDirectory);
Environment.CurrentDirectory = pwd.Parent!.FullName;

// TODO: Add this to config
bool isProduction = false;

var inputPath =
    isProduction
        ? "data"
        : "localDev-data";

return await Bootstrapper
    .Factory
    .CreateWeb(args)
    //.CreateDefault(args)
    .AddInputPath("docs")
    .AddInputPath(inputPath)
    .SetOutputPath("_site/")
    .ConfigureServices(services =>
    {
        services.AddTransient<Templates>();
    })
    .AddPipeline<DirectoryMetadata>()
    .AddPipeline<Inputs>()
    .AddPipeline<ProfilePipeline>()
    .AddHostingCommands()
    .RunAsync();