using FinancialReportGenerator.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;

namespace FinancialReportGenerator.Services
{
    /// <summary>
    /// Service for configuring and running agent conversations
    /// </summary>
    public class AgentChatService
    {
        private readonly Kernel _kernel;

        public AgentChatService(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Configures a group chat between the financial report agents
        /// </summary>
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public AgentGroupChat ConfigureAgentGroupChat(
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AzureAIAgent researcher,
            AzureAIAgent writer,
            AzureAIAgent editor
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            )
        {
            // Create a collaborative selection strategy
            var selectionStrategy = new AgentSelectionStrategy();

            // Create an approval-based termination strategy
            var terminationStrategy = new AgentTerminationStrategy
            {
                // Only the editor can approve the final report
                AuthorizedAgent = editor,
                // Limit total number of turns
                MaximumIterations = 20
            };

            // Create and configure the agent group chat
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return new AgentGroupChat(researcher, writer, editor)
            {
                ExecutionSettings = new()
                {
                    SelectionStrategy = selectionStrategy,
                    TerminationStrategy = terminationStrategy
                }
            };
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        /// <summary>
        /// Processes the agent conversation and collects Bing references
        /// </summary>
        public async Task ProcessAgentConversationAsync(
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AgentGroupChat agentGroupChat,
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            References bingReferences,
            ReferenceCollectorService bingReferenceService,
            Action<AgentMessage> messageHandler)
        {
            await foreach (var chatMessageContent in agentGroupChat.InvokeAsync())
            {
                // Get agent name
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                string agentName = chatMessageContent.AuthorName ?? chatMessageContent.Role.ToString();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // Process message
                messageHandler(new AgentMessage(agentName, chatMessageContent.Content));

                // Collect Bing search references from researcher messages
                if (agentName == "FinancialResearcher" && chatMessageContent.Metadata != null)
                {
                    if (chatMessageContent.Metadata.TryGetValue("ThreadId", out var threadIdObj) &&
                        chatMessageContent.Metadata.TryGetValue("RunId", out var runIdObj))
                    {
                        string threadId = threadIdObj.ToString();
                        string runId = runIdObj.ToString();

                        await bingReferenceService.CollectBingReferencesAsync(threadId, runId, bingReferences);
                    }
                }
            }
        }
    }
}
