using AngleSharp.Common;
using Microsoft.Extensions.Configuration;

namespace Generator
{
    public class ProfileYamlToMetadataModule :
#if DEBUG
        Module
#else
        ParallelModule
#endif
    {
        public ProfileYamlToMetadataModule()
        {
        }

        protected override Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
        {
            if (input.Source.Extension != ".yaml")
            {
                return input.YieldAsync();
            }

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddYamlStream(input.GetContentStream());
            var helper = configBuilder.Build();
            
            var metadata = helper.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
            metadata["metadata:moreProfiles"] = metadata.Keys.Count(k => k.StartsWith("metadata:moreProfiles:"));
            metadata["metadata:relatedProfiles"] = metadata.Keys.Count(k => k.StartsWith("metadata:relatedProfiles:"));
            metadata["metadata:additionalImages"] = metadata.Keys.Count(k => k.StartsWith("metadata:additionalImages:")) / 3;
            metadata["romDetails"] = metadata.Keys.Count(k => k.StartsWith("romDetails:")) / 6;
            metadata["detailedChipInfo"] = metadata.Keys.Count(k => k.StartsWith("detailedChipInfo:")) / 12;
            metadata["sourceDir"] = input.Source.Parent.FullPath;

            var docs = context.Inputs.OrderBy(i => i.Source).ToList();

            var currentIndex = docs.IndexOf(input);
            int? previousIndex = currentIndex - 1;
            previousIndex = previousIndex < 0 ? null : previousIndex;
            int? nextIndex = currentIndex + 1;
            nextIndex = nextIndex >= docs.Count ? null : nextIndex;

            var previous = previousIndex == null ? null : docs[previousIndex.Value];
            var next = nextIndex == null ? null : docs[nextIndex.Value];

            metadata["previous"] = previous?.Get("profileId");
            metadata["next"] = next?.Get("profileId");

            return input.Clone(metadata, string.Empty).YieldAsync();
        }
    }
}
