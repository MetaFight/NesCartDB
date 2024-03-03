using Statiq.Testing;

namespace Generator
{
    public class ProfileDataToDocumentModule : ParallelModule
    {
        public ProfileDataToDocumentModule()
        {
        }

        protected override Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
        {
            var profileId = input.Source.Parent.FullPath.Split("/").Last();

            var yaml = input.ContentProvider.GetStream().ReadToEnd();
            var newContent = $"""
                ---
                sourceDir: {input.Source.Parent.FullPath}
                profileId: {profileId}
                {yaml}
                ---
                """;

            return Task.FromResult(new[] { input.Clone(newContent) }.AsEnumerable());
        }
    }
}
