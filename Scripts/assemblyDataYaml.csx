#r "nuget: YamlDotNet, 15.1.1"
#r "nuget: Humanizer.Core, 2.14.1"
#r "nuget: HtmlAgilityPack, 1.11.59"
#nullable enable

using HtmlAgilityPack;
using Humanizer;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


const int first = 1;
const int last = 4773;
// const int first = 1534;
// const int last = 1534;

AssembleDataYaml(first, last);
void AssembleDataYaml(int first, int last)
{
    for (int i = first; i <= last; i++)
    {
        var isInvalidMarkerFile = GetLocalFile($"../data/webscrape/profile/view/{i}/is-invalid.txt");

        if (isInvalidMarkerFile.Exists)
        {
            Console.WriteLine($"Skipping invalid #{i}");
            continue;
        }

        var dir = isInvalidMarkerFile.Directory!;
        var source = File.ReadAllText(dir.FullName + "/source.html");

        try
        {
            ExtractSections(source, out var metadata, out var releaseInfo, out var cartProperties, out var romDetails, out var pcbProperties, out var detailedChipInfo);
            var slug = File.ReadAllText(GetLocalFile($"../data/webscrape/profile/view/{i}/slug.txt").FullName);
            //Console.WriteLine(slug);

            var data = new
            {
                Metadata = metadata,
                Slug = slug,
                ReleaseInfo = releaseInfo,
                CartProperties = cartProperties,
                RomDetails = romDetails,
                PcbProperties = pcbProperties,
                DetailedChipInfo = detailedChipInfo,
            };

            var serializer =
                new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .DisableAliases()
                    .Build();
            var yaml = serializer.Serialize(data);
            WriteFile($"../data/webscrape/profile/view/{i}/data.yaml", yaml);

            //break;
        }
        catch (Exception ex)
        {
        }
    }

    static void ExtractSections(
        string source,
        out Metadata metadata,
        out Dictionary<string, string?> releaseInfo,
        out Dictionary<string, object?> cartProperties,
        out List<RomDetailEntry> romDetails,
        out Dictionary<string, object?> pcbProperties,
        out List<DetailedChipInfoEntry> detailedChipInfo)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(source);
        var root = doc.DocumentNode;

        var hasTopCartImage = GetAttr(root, "/html/body/div[3]/table[1]/tbody/tr[1]/td[1]", "rowspan") == "2";
        var hasSubTitle = root.SelectSingleNode("/html/body/div[3]/table[1]/tr/td[@class='headingsubtitle']") != null;

        var nameColOffset = hasTopCartImage ? 1 : 0;
        var submittedRowOffset = hasSubTitle ? 1 : 0;

        metadata = new(
            Name: GetText(root, "/html/body/div[3]/table[1]/tbody/tr/td[@class='headingmain']"),
            AltName: GetText(root, "/html/body/div[3]/table[1]/tbody/tr/td[@class='headingsubtitle']"),
            //Name: GetText(root, $"/html/body/div[3]/table[1]/tbody/tr[1]/td[{1 + nameColOffset}]"),
            Variant: GetText(root, $"/html/body/div[3]/table[1]/tbody/tr[1]/td[{1 + nameColOffset}]/span"),
            SubmittedBy: GetText(root, $"/html/body/div[3]/table[1]/tbody/tr[{2 + submittedRowOffset}]/td/a"),
            SubmittedOn: GetText(root, $"/html/body/div[3]/table[1]/tbody/tr[{2 + submittedRowOffset}]/td/span"),
            MoreProfiles: GetAttrs(root, $"/html/body/div[3]/table[1]/tbody/tr[1]/td[{3 + nameColOffset}]/table/tbody/tr[2]/td/table/tbody/tr/td[2]/a", "href")
                                .Select(item => int.Parse(item.Split("/").Last())).ToList(),
            RelatedProfiles: GetAttrs(root, $"/html/body/div[3]/table[1]/tbody/tr[1]/td[{4 + nameColOffset}]/table/tbody/tr[2]/td/table/tbody/tr/td/a", "href")
                                .Select(item => int.Parse(item.Split("/").Skip(3).First())).ToList(),
            AdditionalImages:
                root.SelectNodes("/html/body/div[3]/table[5]/tr/td/table/tr/td/a")?
                    .Select(item => new ImageEntry(
                        ToolTipJS: GetAttr(item, ".", "onmouseover"),
                        Filename: GetAttr(item, "./img", "src")
                    ))
                    .ToList()
        );
        releaseInfo = root.SelectNodes("/html/body/div[3]/table[2]/tr/td[3]/table/tr/th")
                .Select(n => n.InnerText)
                .ToDictionary(
                    header => header,
                    header => header switch
                    {
                        "Publisher" or "Developer"
                            => GetText(root, $"/html/body/div[3]/table[2]/tr/td[3]/table/tr/th[text()='{header}']/../td/a"),
                        _ => GetText(root, $"/html/body/div[3]/table[2]/tr/td[3]/table/tr/th[text()='{header}']/../td"),
                    });
        releaseInfo["Country"] = GetAttr(root, "/html/body/div[3]/table[2]/tbody/tr/td[3]/table/tbody/tr[2]/td/img", "title");

        cartProperties = root.SelectNodes("/html/body/div[3]/table[2]/tr/td[4]/table/tr/th")
                .Select(n => n.InnerText)
                .ToDictionary(
                    header => header,
                    header => (object?)(header switch
                    {
                        "Cart Producer" => GetAttr(root, $"/html/body/div[3]/table[2]/tr/td[4]/table/tr/th[text()='{header}']/../td/img", "title"),
                        _ => GetText(root, $"/html/body/div[3]/table[2]/tr/td[4]/table/tr/th[text()='{header}']/../td"),
                    }));

        cartProperties["Top Image"] = new ImageEntry(
            ToolTipJS: GetAttr(root, "/html/body/div[3]/table[1]/tr[1]/td[1]/table/tr/td/a", "onmouseover"),
            Filename: GetAttr(root, "/html/body/div[3]/table[1]/tr[1]/td[1]/table/tr/td/a/img", "src")
            );

        cartProperties["Front Image"] = new ImageEntry(
            ToolTipJS: GetAttr(root, "//td[@id='cartfront']/table/tr/td/a", "onmouseover"),
            Filename: GetAttr(root, "//td[@id='cartfront']/table/tr/td/a/img", "src")
            );

        cartProperties["Back Image"] = new ImageEntry(
            ToolTipJS: GetAttr(root, "//td[@id='cartback']/table/tr/td/a", "onmouseover"),
            Filename: GetAttr(root, "//td[@id='cartback']/table/tr/td/a/img", "src")
            );

        romDetails = root.SelectNodes("/html/body/div[3]/table[3]/tr/td[1]/table/tr")
                .Select(n => n.SelectNodes("./td"))
                .Where(row => row?.Count >= 4)
                .Select(row =>
                    row?.Count switch
                    {
                        5 => new RomDetailEntry(
                            Type: GetText(row[0], "."),
                            Label: GetText(row[1], "."),
                            Size: GetText(row[2], "."),
                            Crc32: GetText(row[3], "."),
                            X: GetText(row[4], ".")
                        ),
                        4 => new RomDetailEntry(
                            Type: "N/A",
                            Label: "ROMs Combined",
                            Size: GetText(row[1], "."),
                            Crc32: GetText(row[2], "."),
                            X: GetText(row[3], ".")
                        ),
                        _ => throw new NotImplementedException(),
                    })
                .ToList();

        var localPcbProperties = new Dictionary<string, object?>()
        {
            { "Name", GetText(root, "/html/body/div[3]/table[3]/tbody/tr/td[3]/table/tbody/tr[1]/td/a") },
            {
                "Front Image",
                new ImageEntry(
                    GetAttr(root, "/html/body/div[3]/table[3]/tr/td[1]/table/tr[8]/td/table/tr/td[1]/table/tr/td/a", "onmouseover"),
                    GetAttr(root, "/html/body/div[3]/table[3]/tr/td[1]/table/tr[8]/td/table/tr/td[1]/table/tr/td/a/img", "src"))
            },
            {
                "Back Image",
                new ImageEntry(
                    GetAttr(root, "/html/body/div[3]/table[3]/tr/td[1]/table/tr[8]/td/table/tr/td[2]/table/tr/td/a", "onmouseover"),
                    GetAttr(root, "/html/body/div[3]/table[3]/tr/td[1]/table/tr[8]/td/table/tr/td[2]/table/tr/td/a/img", "src"))
            },
        };
        root.SelectNodes("/html/body/div[3]/table[3]/tr/td[3]/table/tr/th")
            .Select(n => n.InnerText)
            .ToList()
            .ForEach(header =>
            {
                var tdPath = $"/html/body/div[3]/table[3]/tr/td[3]/table/tr/th[text()='{header}']/../td";
                var value = header switch
                {
                    "PCB Producer" => GetAttr(root, tdPath + "/img", "title"),
                    "PCB Class" or "iNES Mapper"
                        => GetText(root, tdPath + "/a"),
                    _ => GetText(root, tdPath),
                };
                localPcbProperties[header] = value;
            });
        pcbProperties = localPcbProperties;

        detailedChipInfo = (root.SelectNodes("/html/body/div[3]/table[4]/tr/td/table/tr") ?? Enumerable.Empty<HtmlNode>())
                .Reverse().Skip(1).Reverse()
                .Select(n => n.SelectNodes("./td"))
                .Where(row => row?.Count == 9)
                .Select(row => new DetailedChipInfoEntry(
                    Designation: GetText(row[0], "."),
                    Maker: GetText(row[1], "."),
                    PartNumber: GetText(row[2], "."),
                    PartNumberSubDetail: GetText(row[2], "./span"),
                    Type: GetText(row[3], "."),
                    TypeSubDetail: GetText(row[3], "./span"),
                    Package: GetText(row[4], "."),
                    DateCodePrefix: GetText(row[5], "./span"),
                    DateCodeSuffix: GetText(row[5], "."),
                    Std: GetText(row[7], "."),
                    Misc: GetText(row[8], ".")
                ))
                .ToList();


        releaseInfo = releaseInfo.ToDictionary(kvp => kvp.Key.ApplyCase(), kvp => kvp.Value);
        cartProperties = cartProperties.ToDictionary(kvp => kvp.Key.ApplyCase(), kvp => kvp.Value);
        pcbProperties = pcbProperties.ToDictionary(kvp => kvp.Key.ApplyCase(), kvp => kvp.Value);
    }

    static string? GetAttr(HtmlNode root, string xpath, string attribute)
    {
        xpath = xpath.Replace("/tbody", "");
        return root.SelectSingleNode(xpath)?.Attributes[attribute]?.DeEntitizeValue;
    }

    static IEnumerable<string> GetAttrs(HtmlNode root, string xpath, string attribute)
    {
        xpath = xpath.Replace("tbody", "");
        return root.SelectNodes(xpath)?.Select(n => n.Attributes[attribute].DeEntitizeValue) ?? Enumerable.Empty<string>();
    }

    static string? GetText(HtmlNode root, string xpath)
    {
        xpath = xpath.Replace("/tbody", "");
        xpath += "/text()";
        var result = HtmlEntity.DeEntitize(root.SelectSingleNode(xpath)?.InnerText)?.Trim();

        if (result == null)
        {
        }

        return result;
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

public static string ApplyCase(this string source) => source.Transform(To.LowerCase, To.TitleCase).Camelize();

record DetailedChipInfoEntry(string? Designation, string? Maker, string? PartNumber, string? PartNumberSubDetail, string? Type, string? TypeSubDetail, string? Package, string? DateCodePrefix, string? DateCodeSuffix, string? Std, string? Misc);
record ImageEntry(string? ToolTipJS, string? Filename);
record Metadata(string? Name, string? AltName, string? Variant, string? SubmittedBy, string? SubmittedOn, List<int> MoreProfiles, List<int> RelatedProfiles, List<ImageEntry>? AdditionalImages);
record RomDetailEntry(string? Type, string? Label, string? Size, string? Crc32, string? X);
