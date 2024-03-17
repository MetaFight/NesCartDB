using ConcurrentCollections;
using Generator;
using Statiq.Razor;
using Statiq.Yaml;
using System.Text;

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
            new SetDestination(".html"),
            new ConcatDocuments(
                new GenerateRedirects()
                    .AlwaysCreateAdditionalOutput()
                    .WithAdditionalOutput("_redirects", GenerateNetlifyRedirects)
            ),
        };
        this.OutputModules = new()
        {
            new WriteFiles(),
        };
    }

    private async Task<string> GenerateNetlifyRedirects(IDictionary<NormalizedPath, string> redirects, IExecutionContext context)
    {
        var redirectPrefix = context.Settings.GetString(WebKeys.NetlifyRedirectPrefix, "^");

        StringBuilder redirectsBuilder = new StringBuilder();

        // Make sure we keep any existing manual redirect content
        IFile existingFile = context.FileSystem.GetInputFile("_redirects");
        if (existingFile.Exists)
        {
            redirectsBuilder.Append(await existingFile.ReadAllTextAsync());
        }

        // Generate content for any prefix redirects
        ConcurrentHashSet<NormalizedPath> prefixRedirects = new ConcurrentHashSet<NormalizedPath>();
        if (context.Settings.GetBool(WebKeys.NetlifyPrefixRedirects, true))
        {
            // Gather prefixed files and folders (do in parallel and remove duplicates since
            // path manipulation can be time consuming) - need to only get documents from
            // dependencies since those are the only pipelines that are guaranteed to have
            // executed their post-process phases due to PostProcessHasDependencies
            NormalizedPath[] destinationPaths = context.Outputs
                .FromPipelines(Dependencies.ToArray())
                .Select(x => x.Destination)
                .Where(x => !x.IsNullOrEmpty)
                .Distinct()
                .ToArray();
            Parallel.ForEach(
                destinationPaths,
                path =>
                {
                    // First check if any parent folder has a prefix
                    NormalizedPath parent = path.Parent;
                    bool added = false;
                    while (!parent.IsNullOrEmpty)
                    {
                        if (parent.Name.StartsWith(redirectPrefix))
                        {
                            prefixRedirects.Add(parent);
                            added = true;
                            break;
                        }
                        parent = parent.Parent;
                    }

                    // Now check if this is a prefixed file
                    if (!added && path.Name.StartsWith(redirectPrefix))
                    {
                        prefixRedirects.Add(path);
                    }
                });

            // Generate redirect content for each prefix redirect
            if (prefixRedirects.Count > 0)
            {
                if (redirectsBuilder.Length > 0)
                {
                    redirectsBuilder.AppendLine();
                    redirectsBuilder.AppendLine();
                }
                redirectsBuilder.AppendLine("# Prefix redirects generated by Statiq");
                foreach (NormalizedPath prefixRedirect in prefixRedirects)
                {
                    redirectsBuilder.AppendLine("/"
                        + $"{context.GetLink(prefixRedirect.Parent)}/{context.GetLink(prefixRedirect.Name.Substring(redirectPrefix.Length)).TrimStart('/')} /{context.GetLink(prefixRedirect).TrimStart('/')}".TrimStart('/'));
                }
            }
        }

        // Produce the additional redirect content
        if (redirects.Count > 0)
        {
            if (redirectsBuilder.Length > 0)
            {
                if (prefixRedirects.Count == 0)
                {
                    // Only include an extra line if we generated prefix redirects since
                    // those will have already added a new line at the end due to AppendLine()
                    redirectsBuilder.AppendLine();
                }
                redirectsBuilder.AppendLine();
            }
            redirectsBuilder.AppendLine("# Automatic redirects generated by Statiq");
            foreach (KeyValuePair<NormalizedPath, string> redirect in redirects)
            {
                redirectsBuilder.AppendLine($"/{redirect.Key} {redirect.Value}");
            }
        }

        return redirectsBuilder.ToString().Trim();
    }
}