using Generator;
using Microsoft.Extensions.DependencyInjection;
using Statiq.Sass;

var pwd = new DirectoryInfo(Environment.CurrentDirectory);
Environment.CurrentDirectory = pwd.Parent!.FullName;

var isProduction = Environment.GetEnvironmentVariable("DEPLOYMENT_ENVIRONMENT") == "PROD";

var inputPath =
    isProduction
        ? "data"
        : "localDev-data";

return await Bootstrapper
    .Factory
    //.CreateWeb(args)
    .CreateDefault(args)
    .AddSetting(WebKeys.NetlifyRedirects, true)
    .AddInputPath("docs")
    .AddInputPath(inputPath)
    .SetOutputPath("_site/")
    .ConfigureServices(services =>
    {
        services.AddTransient<Templates>();
    })
    //.AddPipeline<DirectoryMetadata>()
    //.AddPipeline<Inputs>()
    .BuildPipeline("Prep Profile Assets", builder =>
        builder
            .WithInputReadFiles("webscrape/profile/view/*/img/*.jpg")
            .WithProcessModules(new ProfilePrepModule())
            .WithOutputModules(new WriteFiles())
    )
    .AddPipeline<ProfilePipeline>()
    .BuildPipeline("Prep Web Assets", builder =>
        builder
            .WithInputReadFiles("**/*.scss")
            .WithProcessModules(new CompileSass())
            .WithOutputModules(new WriteFiles())
    )
    .AddHostingCommands()
    .RunAsync();