namespace NugetSearcher
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Searching: " + string.Join(",", args));

            var nuGetManager = new NuGetManager();
            var listPackagesAsync = await nuGetManager.ListPackagesAsync(args[0], false);

            foreach (var keyValuePair in listPackagesAsync)
            {
                Console.WriteLine(keyValuePair.Key);
            }
        }
    }
}