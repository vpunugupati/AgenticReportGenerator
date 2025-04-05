using DotNetEnv;

namespace FinancialReportGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Load environment variables from the .env file
            Env.Load();

            Console.WriteLine("Financial Report Generator");
            Console.WriteLine("==========================");

            // Ask the user for the company name with a default suggestion
            Console.Write("Enter the company name for financial report preparation (default: Microsoft): ");
            string? input = Console.ReadLine();

            // Use Microsoft as the default if input is empty or null
            string companyName = string.IsNullOrWhiteSpace(input) ? "Microsoft" : input.Trim();

            Console.WriteLine($"Generating financial report for: {companyName}");

            try
            {
                var financialReportAgents = new FinancialReportAgents();
                await financialReportAgents.RunFinancialReportAgentsAsync(companyName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
