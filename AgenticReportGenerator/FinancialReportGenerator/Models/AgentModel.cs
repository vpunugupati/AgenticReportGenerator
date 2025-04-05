using Microsoft.SemanticKernel.Agents.AzureAI;

namespace FinancialReportGenerator.Models
{
    /// <summary>
    /// Represents the collection of agents used in the financial report generation process
    /// </summary>
    public class AgentGroup
    {
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public required AzureAIAgent researcher { get; set; }
        public required AzureAIAgent writer { get; set; }
        public required AzureAIAgent editor { get; set; }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Represents a message in the agent conversation
    /// </summary>
    public record AgentMessage(string agentName, string content);
}
