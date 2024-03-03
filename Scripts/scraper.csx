#r "nuget: YamlDotNet, 15.1.1"
#nullable enable

using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using YamlDotNet.Serialization;


var httpClient = new HttpClient();

const int first = 1;
const int last = 4773;

await DownloadHtml(httpClient);
await DownloadImages(first, last);

async Task DownloadImages(int first, int last)
{
    for (int i = first; i <= last; i++)
    {
        var dataYaml = GetLocalFile($"../data/webscrape/profile/view/{i}/data.yaml");

        if (!dataYaml.Exists)
        {
            Console.WriteLine($"Skipping entry #{i}...");
            continue;
        }

        var deserialiser =
            new DeserializerBuilder()
                .Build();

        var doc = JsonSerializer.SerializeToDocument(deserialiser.Deserialize(File.ReadAllText(dataYaml.FullName)));
        var cartProps = doc.RootElement.GetProperty("cartProperties");
        var pcbProps = doc.RootElement.GetProperty("pcbProperties");
        var additionalSection = doc.RootElement.GetProperty("metadata").GetProperty("additionalImages");
        var additional =
            additionalSection.ValueKind == JsonValueKind.Array
                ? additionalSection.EnumerateArray()
                : Enumerable.Empty<JsonElement>();

        var imageSourcesByName = new Dictionary<string, string>()
        {
            { "cart-top", cartProps.GetProperty("topImage").GetProperty("filename").GetString()! },
            { "cart-front", cartProps.GetProperty("frontImage").GetProperty("filename").GetString()! },
            { "cart-back", cartProps.GetProperty("backImage").GetProperty("filename").GetString()! },
            { "pcb-front", pcbProps.GetProperty("frontImage").GetProperty("filename").GetString()! },
            { "pcb-back", pcbProps.GetProperty("backImage").GetProperty("filename").GetString()! },
        };

        additional
            .Select((item, i) => (item, i))
            .ToList()
            .ForEach(x => imageSourcesByName.Add($"additional-{x.i}", x.item.GetProperty("filename").GetString()!));

        Console.WriteLine($"Getting images for entry #{i}");

        await Task.WhenAll(
            DownloadAllFiles(),
            Task.Delay(TimeSpan.FromSeconds(5)));

        async Task DownloadAllFiles()
        {
            await Task.WhenAll(imageSourcesByName.Select(kvp => DownloadFile(kvp)));

            Console.WriteLine($"All images for entry #{i} downloaded.");
            Console.WriteLine();

            async Task DownloadFile(KeyValuePair<string, string> kvp)
            {
                if (kvp.Value == null) return;

                var ext = kvp.Value.Split('.').Last();
                var target = GetLocalFile($"../data/webscrape/profile/view/{i}/img/{kvp.Key}.{ext}");
                var urlPath = $"/images/700/{kvp.Value.Split('/').Last()}";

                var uri = new Uri("https://nescartdb.com" + urlPath);
                var getResponse = await httpClient.GetAsync(uri);

                Console.WriteLine($"  ... got {kvp.Key} from {urlPath}");

                target.Directory!.Create();
                var content = await getResponse.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(target.FullName, content);
            }

        }
    }
}

async Task DownloadHtml(HttpClient httpClient)
{
    const int first = 1;
    //const int first = 1177;
    const int last = 4773;

    for (int i = first; i <= last; i++)
    {
        var (url, content) = await GetContent($"https://nescartdb.com/profile/view/{i}/");
        var slug = url.Split("/").Last();

        WriteFile($"../data/webscrape/profile/view/{i}/source.txt", url);
        WriteFile($"../data/webscrape/profile/view/{i}/source.html", content);
        WriteFile($"../data/webscrape/profile/view/{i}/slug.txt", slug);
        if (slug.Length == 0)
        {
            WriteFile($"../data/webscrape/profile/view/{i}/is-invalid.txt", ":(");
        }

        Console.WriteLine(url);

        await Task.Delay(1000);
    }
}

static FileInfo GetLocalFile(string relativePath, [CallerFilePath] string? callerFilePath = null)
{
    var parent = Directory.GetParent(callerFilePath ?? "")!;
    var path = Path.Combine(parent.FullName, relativePath);
    return new FileInfo(path);
}

static void WriteFile(string relativePath, string content, [CallerFilePath] string? callerFilePath = null)
{
    var file = GetLocalFile(relativePath, callerFilePath);
    file.Directory!.Create();
    File.WriteAllText(file.FullName, content);
}

async Task<(string url, string content)> GetContent(string url)
{
    HttpStatusCode statusCode;
    HttpResponseMessage response;

    do
    {
        response = await httpClient!.GetAsync(url);
        statusCode = response.StatusCode;

        if (response.Headers.Location != null)
        {
            url = response.Headers.Location.ToString();
        }
    } while (statusCode == HttpStatusCode.Moved);

    return (url, await response.Content.ReadAsStringAsync());
}
