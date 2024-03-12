using Microsoft.Extensions.Configuration;

namespace Generator
{
    public class ProfileSlugModule :
#if DEBUG
        Module
#else
        ParallelModule
#endif
    {
        public ProfileSlugModule()
        {
        }

        protected override Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
        {
            var simpleParts = input.Destination.ToString().Split('/');
            var simplePrefix = string.Join('/', simpleParts[..(simpleParts.Length - 1)]);

            var simpleUrl = input.Destination.ToString();
            var slugUrl = $"{simplePrefix}/{input.Get("slug")}";

            //Console.WriteLine(slugUrl);

            var content =
$"""
RedirectFrom:
- {simpleUrl}

---
""";

            return input.Clone(destination: slugUrl, content: content).YieldAsync();
        }
    }
}
