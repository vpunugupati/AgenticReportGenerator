using System.Text;

namespace FinancialReportGenerator.Models
{
    /// <summary>
    /// Handles the collection and formatting of Bing search references
    /// </summary>
    public class References
    {
        private readonly StringBuilder _websiteReferences = new();
        private readonly HashSet<string> _uniqueUrls = new();
        private readonly HashSet<string> _uniqueQueries = new();

        /// <summary>
        /// Adds URL citation from an annotation to the references
        /// </summary>
        public void AddUrlCitation(string title, string url)
        {
            if (string.IsNullOrEmpty(url) || !_uniqueUrls.Add(url))
                return;

            _websiteReferences.AppendLine($"- [{title}]({url})");
        }

        /// <summary>
        /// Adds a search query to the references
        /// </summary>
        public void AddSearchQuery(string query)
        {
            if (string.IsNullOrEmpty(query) || !_uniqueQueries.Add(query))
            {
                return;
            }

        }

        /// <summary>
        /// Gets the formatted references as Markdown text
        /// </summary>
        public string GetFormattedReferences()
        {
            if (_uniqueUrls.Count == 0 && _uniqueQueries.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("## References");
            sb.AppendLine();

            if (_uniqueUrls.Count > 0)
            {
                sb.AppendLine("### Referenced Websites");
                sb.Append(_websiteReferences);
                sb.AppendLine();
            }

            if (_uniqueQueries.Count > 0)
            {
                sb.AppendLine("### Bing Search Queries");
                sb.AppendLine();

                foreach (var query in _uniqueQueries)
                {
                    // Clean and properly format the query for display
                    string cleanQuery = query.Replace("\"", "").Trim();
                    string encodedQuery = Uri.EscapeDataString(cleanQuery);
                    string bingSearchUrl = $"https://www.bing.com/search?q={encodedQuery}";

                    sb.AppendLine($"- [{cleanQuery}]({bingSearchUrl})");
                }
            }

            return sb.ToString();
        }
    }
}
