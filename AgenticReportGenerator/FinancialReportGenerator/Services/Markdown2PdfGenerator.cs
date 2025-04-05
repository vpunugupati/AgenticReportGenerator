using Markdig;
using PuppeteerSharp;
using System.Text;
using PuppeteerSharp.Media;

namespace FinancialReportGenerator.Services
{
    public class Markdown2PdfGenerator
    {
        private readonly MarkdownPipeline _pipeline;

        public Markdown2PdfGenerator()
        {
            // Configure Markdig pipeline with all extensions for full Markdown support
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseBootstrap()
                .UseGridTables()
                .UsePipeTables()
                .UseListExtras()
                .UseEmphasisExtras()
                .UseGenericAttributes()
                .UseAutoLinks()
                .UseTaskLists()
                .UseMediaLinks()
                .UseFigures()
                .UseFootnotes()
                .Build();
        }

        public async Task<byte[]> ConvertToPdfAsync(string markdownContent, string title = null)
        {
            // Pre-process tables in the Markdown content
            string processedMarkdown = PreProcessTables(markdownContent);

            // Convert Markdown to HTML
            string html = Markdown.ToHtml(processedMarkdown, _pipeline);

            // Apply CSS for better rendering
            string styledHtml = ApplyStyle(html, title);

            // Convert HTML to PDF using PuppeteerSharp
            await new BrowserFetcher().DownloadAsync();
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();
            await page.SetContentAsync(styledHtml);
            var pdfStream = await page.PdfStreamAsync(new PdfOptions { Format = PaperFormat.A4 });

            using var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Pre-processes Markdown tables to ensure proper conversion to HTML
        /// </summary>
        private string PreProcessTables(string markdownContent)
        {
            StringBuilder result = new StringBuilder();
            bool inTable = false;
            StringBuilder tableContent = new StringBuilder();

            using (StringReader reader = new StringReader(markdownContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Detect table start
                    if (line.Trim().StartsWith("|") && line.Trim().EndsWith("|"))
                    {
                        // Start of a table or continuation
                        if (!inTable)
                        {
                            inTable = true;
                            tableContent.Clear();
                        }

                        tableContent.AppendLine(line);
                    }
                    else if (inTable)
                    {
                        // End of the table
                        inTable = false;

                        // Convert the table to HTML
                        result.AppendLine(ConvertMarkdownTableToHtml(tableContent.ToString()));

                        // Add the current non-table line
                        result.AppendLine(line);
                    }
                    else
                    {
                        // Regular line, not in a table
                        result.AppendLine(line);
                    }
                }

                // Handle case where file ends with a table
                if (inTable)
                {
                    result.AppendLine(ConvertMarkdownTableToHtml(tableContent.ToString()));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts a Markdown table to HTML
        /// </summary>
        private string ConvertMarkdownTableToHtml(string markdownTable)
        {
            string[] lines = markdownTable.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                return markdownTable; // Not enough lines for a proper table

            StringBuilder html = new StringBuilder("<table>\n");

            // Process header
            string[] headerCells = ParseTableRow(lines[0]);
            html.AppendLine("  <thead>");
            html.Append("    <tr>");
            foreach (string cell in headerCells)
            {
                // Process Markdown formatting within the cell
                string formattedCell = Markdown.ToHtml(cell, _pipeline).Trim();
                // Remove surrounding <p> tags if present
                formattedCell = RemoveSurroundingParagraphTags(formattedCell);
                html.Append($"<th>{formattedCell}</th>");
            }
            html.AppendLine("</tr>");
            html.AppendLine("  </thead>");

            // Skip the separator line (index 1)

            // Process body
            html.AppendLine("  <tbody>");
            for (int i = 2; i < lines.Length; i++)
            {
                string[] cells = ParseTableRow(lines[i]);
                html.Append("    <tr>");
                foreach (string cell in cells)
                {
                    // Process Markdown formatting within the cell
                    string formattedCell = Markdown.ToHtml(cell, _pipeline).Trim();
                    // Remove surrounding <p> tags if present
                    formattedCell = RemoveSurroundingParagraphTags(formattedCell);
                    html.Append($"<td>{formattedCell}</td>");
                }
                html.AppendLine("</tr>");
            }
            html.AppendLine("  </tbody>");

            html.AppendLine("</table>");
            return html.ToString();
        }

        /// <summary>
        /// Removes surrounding paragraph tags if present
        /// </summary>
        private string RemoveSurroundingParagraphTags(string html)
        {
            if (html.StartsWith("<p>") && html.EndsWith("</p>"))
            {
                return html.Substring(3, html.Length - 7).Trim();
            }
            return html;
        }


        /// <summary>
        /// Parses a Markdown table row into individual cells
        /// </summary>
        private string[] ParseTableRow(string rowLine)
        {
            // Remove the first and last pipe characters
            string trimmed = rowLine.Trim();
            if (trimmed.StartsWith("|"))
                trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|"))
                trimmed = trimmed.Substring(0, trimmed.Length - 1);

            // Split by pipe character
            return trimmed.Split('|').Select(cell => cell.Trim()).ToArray();
        }


        /// <summary>
        /// Converts Markdown content to PDF and saves it to a file
        /// </summary>
        /// <param name="markdownContent">The Markdown content as string</param>
        /// <param name="outputPath">Path where the PDF should be saved</param>
        /// <param name="title">Optional title for the PDF</param>
        /// <returns>True if conversion was successful</returns>
        public async Task<bool> ConvertToPdfFileAsync(string markdownContent, string outputPath, string title = null)
        {
            try
            {
                byte[] pdfBytes = await ConvertToPdfAsync(markdownContent, title);
                await File.WriteAllBytesAsync(outputPath, pdfBytes);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Markdown to PDF: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converts a Markdown file to PDF
        /// </summary>
        /// <param name="markdownFilePath">Path to the Markdown file</param>
        /// <param name="outputPath">Path where the PDF should be saved</param>
        /// <returns>True if conversion was successful</returns>
        public async Task<bool> ConvertFileToPdfAsync(string markdownFilePath, string outputPath)
        {
            try
            {
                string markdownContent = await File.ReadAllTextAsync(markdownFilePath);
                string title = Path.GetFileNameWithoutExtension(markdownFilePath);
                return await ConvertToPdfFileAsync(markdownContent, outputPath, title);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Markdown file to PDF: {ex.Message}");
                return false;
            }
        }

        private string ApplyStyle(string html, string title)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");

            if (!string.IsNullOrEmpty(title))
            {
                sb.AppendLine($"<title>{title}</title>");
            }

            // Add CSS styling for better rendering of Markdown elements
            sb.AppendLine("<style>");
            sb.AppendLine(@"
        body { 
            font-family: Arial, sans-serif; 
            line-height: 1.6; 
            margin: 2em; 
        }
        h1, h2, h3, h4, h5, h6 { 
            margin-top: 1.2em; 
            margin-bottom: 0.6em; 
            color: #333; 
        }
        h1 { font-size: 2.2em; border-bottom: 1px solid #eee; }
        h2 { font-size: 1.8em; border-bottom: 1px solid #eee; }
        h3 { font-size: 1.6em; }
        h4 { font-size: 1.4em; }
        h5 { font-size: 1.2em; }
        h6 { font-size: 1em; }
        
        p { margin: 1em 0; }
        
        a { color: #0366d6; text-decoration: none; }
        a:hover { text-decoration: underline; }
        
        pre { 
            background-color: #f6f8fa; 
            border-radius: 3px; 
            padding: 16px; 
            overflow: auto; 
        }
        
        code {
            background-color: #f6f8fa;
            padding: 0.2em 0.4em;
            border-radius: 3px;
            font-family: monospace;
        }
        
        blockquote {
            padding: 0 1em;
            border-left: 4px solid #dfe2e5;
            color: #6a737d;
            margin: 0;
        }
        
        table {
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 1em;
        }
        
        table th, table td {
            border: 1px solid #dfe2e5;
            padding: 6px 13px;
            text-align: left;
        }
        
        table th {
            background-color: #f6f8fa;
            font-weight: 600;
        }
        
        img {
            max-width: 100%;
        }
        
        ul, ol {
            padding-left: 2em;
        }
        
        input[type='checkbox'] {
            margin-right: 0.5em;
        }
        
        hr {
            height: 1px;
            border: none;
            background-color: #dfe2e5;
            margin: 1.5em 0;
        }
    ");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine(html);
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

    }
}
