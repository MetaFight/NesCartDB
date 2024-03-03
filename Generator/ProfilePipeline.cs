using Generator;
using Statiq.Markdown;
using Statiq.Razor;
using Statiq.Yaml;

public class ProfilePipeline : Pipeline
{
    public ProfilePipeline()
    {
        this.InputModules = new()
        {
            new ReadFiles("webscrape/profile/view/**/data.yaml"),
        };
        this.ProcessModules = new()
        {
            new ProfileDataToDocumentModule(),
            new ExtractFrontMatter(new ParseYaml("data")),
            new ProfileModule(),
            //new RenderMarkdown(),
            new RenderRazor(),
            //new GenerateExcerpt(),
            new SetDestination(".html"),
        };
        this.OutputModules = new()
        {
            new WriteFiles(),
        };
    }
}