using Microsoft.Extensions.Configuration;

namespace Generator
{
    public class ProfileNavigationModule :
#if DEBUG
        Module
#else
        ParallelModule
#endif
    {
        public ProfileNavigationModule()
        {
        }

        protected override Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
        {
            var metadata = new Dictionary<string, object>();
            var docs = context.Inputs.OrderBy(i => i.Get("metadata:name")).ToList();

            var currentIndex = docs.IndexOf(input);
            int? previousIndex = currentIndex - 1;
            previousIndex = previousIndex < 0 ? null : previousIndex;
            int? nextIndex = currentIndex + 1;
            nextIndex = nextIndex >= docs.Count ? null : nextIndex;

            var previous = previousIndex == null ? null : docs[previousIndex.Value];
            var next = nextIndex == null ? null : docs[nextIndex.Value];

            metadata["previousUrl"] = previous?.Destination;
            metadata["previousName"] = previous?.Get("metadata:name");
            metadata["nextUrl"] = next?.Destination;
            metadata["nextName"] = next?.Get("metadata:name");

            return input.Clone(items: metadata).YieldAsync();
        }
    }
}
