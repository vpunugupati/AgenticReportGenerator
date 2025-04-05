using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Agents.Chat;

namespace FinancialReportGenerator.Services
{
    /// <summary>
    /// Termination strategy that ends when the Financial Report Editor approves the report
    /// </summary>
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public sealed class AgentTerminationStrategy : TerminationStrategy
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    {
        public new int MaximumIterations { get; set; } = 20;

        // The agent authorized to approve the final report (Financial Report Editor)
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public required AzureAIAgent AuthorizedAgent { get; set; }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        private int _iterations = 0;

        protected override Task<bool> ShouldAgentTerminateAsync(Microsoft.SemanticKernel.Agents.Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        {
            _iterations++;

            // Terminate if we've reached the maximum iterations
            if (_iterations >= MaximumIterations)
            {
                Console.WriteLine("Maximum iterations reached. Terminating conversation.");
                return Task.FromResult(true);
            }

            // If history is empty, don't terminate
            if (history.Count == 0)
            {
                return Task.FromResult(false);
            }

            // Get the last message
            var lastMessage = history[history.Count - 1];

            // Check if the last message is from the authorized agent (Financial Report Editor)
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            if (AuthorizedAgent != null && lastMessage.AuthorName == AuthorizedAgent.Name)
            {
                // Ensure the message actually indicates approval by checking for specific approval phrases
                // and not containing rejection indicators
                if (lastMessage.Content != null &&
                    lastMessage.Content.Contains("REPORT APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    // Check for common rejection phrases that might appear alongside "REPORT APPROVED"
                    bool containsRejectionPhrases =
                        lastMessage.Content.Contains("cannot", StringComparison.OrdinalIgnoreCase) ||
                        lastMessage.Content.Contains("unable to", StringComparison.OrdinalIgnoreCase) ||
                        lastMessage.Content.Contains("not approve", StringComparison.OrdinalIgnoreCase) ||
                        lastMessage.Content.Contains("cannot issue", StringComparison.OrdinalIgnoreCase) ||
                        lastMessage.Content.Contains("cannot provide", StringComparison.OrdinalIgnoreCase);

                    if (!containsRejectionPhrases)
                    {
                        Console.WriteLine("Financial report has been approved. Terminating conversation.");
                        return Task.FromResult(true);
                    }
                    else
                    {
                        Console.WriteLine("Message contains 'REPORT APPROVED' but appears to be a rejection. Continuing conversation.");
                    }
                }
            }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            return Task.FromResult(false);
        }
    }
}
