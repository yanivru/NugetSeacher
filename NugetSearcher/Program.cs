namespace NugetSearcher
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Folder: " + args[0]);
            Console.WriteLine("Searching: " + args[1]);

            var nuGetManager = new NuGetManager(args[0]);
            var listPackagesAsync = await nuGetManager.ListPackagesAsync(args[1], false);

            foreach (var keyValuePair in listPackagesAsync)
            {
                Console.WriteLine($"{keyValuePair.Key} - {keyValuePair.Max()}");
            }
        }
    }
}