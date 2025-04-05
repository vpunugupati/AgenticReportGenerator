using Microsoft.SemanticKernel;
using FinancialReportGenerator.Models;
using System.Text;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace FinancialReportGenerator.Services
{
    /// <summary>
    /// Service for generating financial report files
    /// </summary>
    public static class ReportService
    {
        /// <summary>
        /// Generates report files (markdown, PDF) from the content
        /// </summary>
        public static async Task GenerateReportFilesAsync(
            string reportContent, 
            string companyName, 
            References references)
        {
            try
            {
                Console.WriteLine("Generating report files...");
                
                // Create output directory if it doesn't exist
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                Directory.CreateDirectory(outputDir);
                
                // Generate file paths
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseFileName = $"{companyName}_FinancialReport_{timestamp}";
                string markdownPath = Path.Combine(outputDir, $"{baseFileName}.md");
                string pdfPath = Path.Combine(outputDir, $"{baseFileName}.pdf");

                // Clean up the report content
                reportContent = reportContent.Trim();

                // Remove entire line containing "DRAFT COMPLETE FOR YOUR REVIEW"
                //string[] lines = reportContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                //reportContent = string.Join(Environment.NewLine,
                //    lines.Where(line => !line.Contains("DRAFT COMPLETE FOR YOUR REVIEW", StringComparison.OrdinalIgnoreCase) &&
                //        !line.Contains("REPORT APPROVED", StringComparison.OrdinalIgnoreCase)));

                // Add references to the report content if available
                string finalContent = reportContent;
                string formattedReferences = references.GetFormattedReferences();

                if (!string.IsNullOrEmpty(formattedReferences))
                {
                    finalContent += "\n\n" + formattedReferences;
                }

                // Add company name header and generation timestamp to the markdown content
                string enhancedMarkdown = $"# {companyName} Financial Report Summary\n\n";
                enhancedMarkdown += $"*Generated on {DateTime.Now:MMMM d, yyyy} at {DateTime.Now:h:mm tt}*\n";                
                enhancedMarkdown += finalContent;

                // Save markdown file
                await File.WriteAllTextAsync(markdownPath, enhancedMarkdown, Encoding.UTF8);
                Console.WriteLine($"Markdown report saved to: {markdownPath}");
                
                // Generate PDF file
                var pdfGenerator = new Markdown2PdfGenerator();
                byte[] pdfBytes = await pdfGenerator.ConvertToPdfAsync(
                    enhancedMarkdown, 
                    $"{companyName} Financial Report");
                    
                await File.WriteAllBytesAsync(pdfPath, pdfBytes);
                Console.WriteLine($"PDF report saved to: {pdfPath}");
                
                Console.WriteLine("Report generation complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating report files: {ex.Message}");
                throw;
            }
        }
    }
}
