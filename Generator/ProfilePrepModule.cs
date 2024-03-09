using Lunr;
using Statiq.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
    public class ProfilePrepModule :
#if DEBUG
        Module
#else
        ParallelModule
#endif
    {
        public ProfilePrepModule()
        {
        }

        protected override Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
        {
            var pathTokens = input.Source.GetRelativeInputPath().ToString().Split("/");
            var profileId = pathTokens[3];

            var metadata = new Dictionary<string, object>
            {
                { "profileid", profileId },
            };

            switch (input.Source.Extension)
            {
                case ".yaml":
                    return input.Clone(
                        $"profiles/{profileId}/index.html", 
                        metadata, 
                        input.GetContentStream())
                            .YieldAsync();
                case ".jpg":
                    return input.Clone(
                        $"profiles/{profileId}/img/{input.Source.FileName}",
                        metadata,
                        input.GetContentStream(), 
                        MediaTypes.Jpg)
                            .YieldAsync();
                default:
                    return Task.FromResult(Enumerable.Empty<IDocument>());
            }
        }
    }
}
