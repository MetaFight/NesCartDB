using AngleSharp.Common;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

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
            metadata["firstLetter"] = metadata["metadata:name"]?.ToString()?.Substring(0, 1).ToUpperInvariant();

            return input.Clone(items: metadata).YieldAsync();
        }
    }
}
