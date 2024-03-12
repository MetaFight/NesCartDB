using Generator;
using Statiq.Razor;
using Statiq.Yaml;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Security.Policy;

public class ProfilePipeline : Pipeline
{
    public ProfilePipeline()
    {
        this.InputModules = new()
        {
            new ReadFiles("webscrape/profile/view/*/data.yaml"),
        };
        this.ProcessModules = new()
        {
            new ProfilePrepModule(),
            new ProfileYamlToMetadataModule(),
            new ProfileSlugModule(),
            new ExtractFrontMatter(new ParseYaml()),
            new ProfileNavigationModule(),
            new RenderRazor(),
            new ConcatDocuments(
                new GroupDocuments("firstLetter"),
                new ExecuteConfig(Config.FromDocument((input, context) =>
                {
                    var letter = input.Get("GroupKey").ToString();

                    var metadata = new MetadataItems
                    {
                        { "isLetterPage", true },
                        { "letterPage", letter },
                    };

                    return input.Clone(destination: $"profiles/by-name/{letter}.html", items: metadata);
                })),
                new ExecuteConfig(Config.FromDocument((input, context) =>
                {
                    var orderedLetterPageUrls =
                        context.Inputs
                            .Where(x => (bool)x.Get("isLetterPage"))
                            .Select(d => (letter: d.Get("letterPage").ToString(), url: d.Destination))
                            .OrderBy(x => x.letter)
                            .ToList();

                    var metadata = new MetadataItems
                    {
                        { "orderedLetterPageUrls", orderedLetterPageUrls },
                    };

                    return input.Clone(destination: input.Destination, items: metadata);
                })),
                new RenderRazor().WithLayout("_ByNameLetter.cshtml")
            ),
            new ConcatDocuments(
                new GroupDocuments("isLetterPage"),
                new RenderRazor().WithLayout("_ByNameIndex.cshtml"),
                new SetMetadata("RedirectFrom", new string[] { "index.html" }),
                new SetDestination("profiles/by-name.html")
            ),
            new ConcatDocuments(new GenerateRedirects()),
            new SetDestination(".html"),
        };
        this.OutputModules = new()
        {
            new WriteFiles(),
        };
    }
}