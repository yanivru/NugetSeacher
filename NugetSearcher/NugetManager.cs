using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetSearcher
{
    public class NuGetManager
    {
        public IEnumerable<SourceRepository> RemoteNuGetFeed { get; }
        public ISettings NuGetSettings { get; }

        private readonly List<Lazy<INuGetResourceProvider>> _providers = new(Repository.Provider.GetCoreV3());

        public NuGetManager(string solutionPath)
        {
            NuGetSettings = Settings.LoadDefaultSettings(solutionPath);
            RemoteNuGetFeed = NuGetSettings.GetSection("packageSources").Items
                .OfType<SourceItem>()
                .Select(x => new PackageSource(x.Value))
                .Select(x => new SourceRepository(x, _providers));

            PreviewFeatureSettings.DefaultCredentialsAfterCredentialProviders = true;
        }

        #region ListPackages/Install API

        /// <summary>
        /// Search Feed
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="includePrelease"></param>
        /// <returns></returns>
        public async Task<ILookup<string, NuGetVersion>> ListPackagesAsync(string searchTerm,
            bool includePrelease)
        {
            var results = await RemoteNuGetFeed.ToAsyncEnumerable()
                .SelectAwait(async x => await ListPackagesAsync(x, searchTerm, includePrelease))
                .ToListAsync();

            return results.SelectMany(x => x)
                .SelectMany(x => x.Value.Select(y => (x.Key, y)))
                .ToLookup(x => x.Key, x => x.y);
        }

        private async Task<Dictionary<string, List<NuGetVersion>>> ListPackagesAsync(SourceRepository sourceRepository, string searchTerm, bool includePrelease)
        {
            var retVal = new Dictionary<string, List<NuGetVersion>>();
            var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
            if (searchResource != null)
            {
                var searchFilter = new SearchFilter(true) { OrderBy = SearchOrderBy.Id, IncludeDelisted = false };
                foreach (var result in await searchResource.SearchAsync(searchTerm, searchFilter, 0, 30, NullLogger.Instance, CancellationToken.None))
                    retVal.Add(result.Identity.Id, new List<NuGetVersion>((await result.GetVersionsAsync()).Select(vi => vi.Version)));
            }
            else
            {
                var listResource = await sourceRepository.GetResourceAsync<ListResource>();
                IEnumerableAsync<IPackageSearchMetadata> allPackages = await listResource.ListAsync(searchTerm, includePrelease, true, false, NullLogger.Instance, CancellationToken.None);
                var enumerator = allPackages.GetEnumeratorAsync();
                var searchResults = new List<IPackageSearchMetadata>();
                while (true)
                {
                    var moved = await enumerator.MoveNextAsync();
                    if (!moved) break;
                    if (enumerator.Current == null) break;
                    searchResults.Add(enumerator.Current);
                }
                foreach (var result in searchResults)
                {
                    if (!retVal.ContainsKey(result.Identity.Id))
                        retVal.Add(result.Identity.Id, new List<NuGetVersion>());
                    retVal[result.Identity.Id].Add(result.Identity.Version);
                }
            }
            return retVal;
        }

        #endregion
    }
}