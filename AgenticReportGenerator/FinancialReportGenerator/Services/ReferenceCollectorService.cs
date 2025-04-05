using Azure.AI.Projects;
using FinancialReportGenerator.Models;

namespace FinancialReportGenerator.Services
{
    /// <summary>
    /// Service for collecting Bing search references from agent conversations
    /// </summary>
    public class ReferenceCollectorService
    {
        private readonly AIProjectClient _aiProjectClient;

        public ReferenceCollectorService(AIProjectClient aiProjectClient)
        {
            _aiProjectClient = aiProjectClient;
        }

        /// <summary>
        /// Collects Bing search references (URL citations and search queries) from an agent's run
        /// </summary>
        public async Task CollectBingReferencesAsync(string threadId, string runId, References references)
        {
            try
            {
                await CollectUrlCitationsAsync(threadId, runId, references);
                await CollectSearchQueriesAsync(threadId, runId, references);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting Bing references: {ex.Message}");
            }
        }

        /// <summary>
        /// Collects URL citations from agent messages
        /// </summary>
        private async Task CollectUrlCitationsAsync(string threadId, string runId, References references)
        {
            // Get messages to extract URL citations from annotations
            PageableList<ThreadMessage> messagesResponses =
                await _aiProjectClient.GetAgentsClient().GetMessagesAsync(
                    threadId,
                    runId,
                    order: ListSortOrder.Descending);

            if (messagesResponses != null)
            {
                foreach (var message in messagesResponses)
                {
                    foreach (var contentItem in message.ContentItems)
                    {
                        if (contentItem is MessageTextContent textContent && textContent.Annotations != null)
                        {
                            foreach (var annotation in textContent.Annotations)
                            {
                                if (annotation is MessageTextUrlCitationAnnotation urlAnnotation)
                                {
                                    references.AddUrlCitation(
                                        urlAnnotation.UrlCitation.Title ?? "Referenced Website",
                                        urlAnnotation.UrlCitation.Url);

                                    Console.WriteLine($"Found URL citation: {urlAnnotation.UrlCitation.Title} - {urlAnnotation.UrlCitation.Url}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Collects search queries from run steps
        /// </summary>
        private async Task CollectSearchQueriesAsync(string threadId, string runId, References references)
        {
            // Get run steps to extract search queries
            var runStepsResponse = await _aiProjectClient.GetAgentsClient().GetRunStepsAsync(threadId, runId);

            if (runStepsResponse?.Value?.Data != null)
            {
                foreach (var runStep in runStepsResponse.Value.Data)
                {
                    if (runStep.StepDetails is RunStepToolCallDetails toolCallDetails)
                    {
                        var toolCalls = toolCallDetails.ToolCalls;
                        foreach (var toolCall in toolCalls)
                        {
                            if (toolCall is RunStepBingGroundingToolCall bingToolCall)
                            {
                                var bingCall = bingToolCall.BingGrounding;

                                foreach (var item in bingCall)
                                {
                                    string query = item.Value.ToString();
                                    query = ProcessBingQuery(query);

                                    references.AddSearchQuery(query);
                                    Console.WriteLine($"Found search query: {query}");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process Bing query to extract the actual search terms
        /// </summary>
        private string ProcessBingQuery(string query)
        {
            // Handle API URL format if present
            if (query.Contains("api.bing.microsoft.com"))
            {
                // Extract the actual query parameter from the API URL
                int queryParamIndex = query.IndexOf("?q=");
                if (queryParamIndex >= 0)
                {
                    query = query.Substring(queryParamIndex + 3);
                    // Remove any trailing parameters
                    int ampIndex = query.IndexOf('&');
                    if (ampIndex >= 0)
                    {
                        query = query.Substring(0, ampIndex);
                    }
                    query = Uri.UnescapeDataString(query);
                }
            }

            return query;
        }
    }
}
