using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
    public class ProfileModule : ParallelModule
    {
        public ProfileModule()
        {
        }

        protected override async Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
        {
            var slug = input.Get("slug");
            Console.WriteLine($"Slug: {slug}");

            var profileId = input.Get("profileId");
            var result = input.Clone($"profiles/{profileId}/index.html", input.ContentProvider.GetStream());

            return new[] { result };
        }
    }
}
