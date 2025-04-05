using Azure.AI.Projects;
using FinancialReportGenerator.Configuration;
using FinancialReportGenerator.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Agent = Azure.AI.Projects.Agent;

namespace FinancialReportGenerator.Services
{
    /// <summary>
    /// Service for initializing and managing AI agents
    /// </summary>
    public class AgentService
    {
        private readonly Kernel _kernel;
        private readonly AgentsClient _agentsClient;
        private readonly AIProjectClient _aiProjectClient;
        private readonly AgentConfiguration _config;

        public AgentService(
            Kernel kernel,
            AgentsClient agentsClient,
            AIProjectClient aiProjectClient,
            AgentConfiguration config)
        {
            _kernel = kernel;
            _agentsClient = agentsClient;
            _aiProjectClient = aiProjectClient;
            _config = config;
        }

        /// <summary>
        /// Gets the initial prompt for the agent conversation
        /// </summary>
        public string GetInitialPrompt(string companyName)
        {
            return string.Format(_config.InitialPromptTemplate, companyName);
        }

        /// <summary>
        /// Initializes all necessary agents for the financial report generation
        /// </summary>
        public async Task<AgentGroup> InitializeAgentsAsync()
        {
            // Prepare Bing Grounding tool for researcher agent
            var bingGroundingTool = await PrepareBingGroundingToolAsync();

            // Create or update the Financial Researcher agent with Bing Search plugin
            Console.WriteLine($"Setting up {_config.ResearcherName} agent...");
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AzureAIAgent researcher = await FindOrCreateAgentAsync(
                _config.ResearcherName,
                "Financial researcher with web search capabilities",
                _config.ResearcherInstructions,
                _config.DeploymentModelName_BING, // Using GPT-4o for research capabilities
                tools: [bingGroundingTool]);
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Find or create the Financial Report Writer agent
            Console.WriteLine($"Setting up {_config.WriterName} agent...");
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AzureAIAgent writer = await FindOrCreateAgentAsync(
                _config.WriterName,
                "Financial report writer for creating comprehensive analyses",
                _config.WriterInstructions,
                _config.DeploymentModelName);
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Find or create the Financial Report Editor agent
            Console.WriteLine($"Setting up {_config.EditorName} agent...");
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            AzureAIAgent editor = await FindOrCreateAgentAsync(
                _config.EditorName,
                "Financial report editor for ensuring accuracy and standards compliance",
                _config.EditorInstructions,
                _config.DeploymentModelName);
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            return new AgentGroup
            {
                researcher = researcher,
                writer = writer,
                editor = editor
            };
        }

        /// <summary>
        /// Prepares the Bing Grounding tool for the researcher agent
        /// </summary>
        private async Task<BingGroundingToolDefinition> PrepareBingGroundingToolAsync()
        {
            // Get Bing search connection
            ConnectionResponse bingConnection = await _aiProjectClient.GetConnectionsClient()
                .GetConnectionAsync(_config.BingConnectionName);

            // Create connection list
            ToolConnectionList connectionList = new()
            {
                ConnectionList = { new ToolConnection(bingConnection.Id) }
            };

            // Create Bing grounding tool
            return new BingGroundingToolDefinition(connectionList);
        }

        /// <summary>
        /// Finds an agent with the given name or creates a new one if it doesn't exist
        /// </summary>
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private async Task<AzureAIAgent> FindOrCreateAgentAsync(
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            string agentName,
            string description,
            string instructions,
            string modelId,
            ToolDefinition[]? tools = null)
        {
            Agent? agentModel = null;

            try
            {
                // Try to get the existing agent by name
                var agentsResponse = await _agentsClient.GetAgentsAsync();
                var existingAgent = agentsResponse.Value.FirstOrDefault(a => a.Name == agentName);

                if (existingAgent != null)
                {
                    Console.WriteLine($"Found existing agent: {agentName}");

                    // Update the existing agent
                    agentModel = await _agentsClient.UpdateAgentAsync(
                        existingAgent.Id,
                        modelId,
                        agentName,
                        description,
                        instructions,
                        tools: tools ?? []);

                    Console.WriteLine($"Updated agent: {agentName}");
                }
                else
                {
                    // Create a new agent
                    Console.WriteLine($"Creating new agent: {agentName}");
                    agentModel = await _agentsClient.CreateAgentAsync(
                        modelId,
                        agentName,
                        description,
                        instructions,
                        tools: tools ?? []);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding/updating agent {agentName}: {ex.Message}");

                // Fall back to creating a new agent
                Console.WriteLine($"Creating new agent: {agentName}");
                agentModel = await _agentsClient.CreateAgentAsync(
                    modelId,
                    agentName,
                    description,
                    instructions,
                    tools: tools ?? []);
            }

            // Create the AzureAI agent wrapper
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return new AzureAIAgent(agentModel, _agentsClient)
            {
                Kernel = _kernel,
                Name = agentName
            };
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        // Add this method to the AgentService class
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        public async Task<AzureAIAgent> FindOrCreateReportCleanerAgentAsync()
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            // Define the agent specifications
            string agentName = _config.FinalReportCleanerName;
            string description = "A financial report cleaner agent that improves formatting and removes drafting artifacts.";
            string instructions = _config.FinalReportCleanerInstructions;
            string modelId = _config.DeploymentModelName;

            // Create or retrieve the agent
            return await FindOrCreateAgentAsync(
                agentName,
                description,
                instructions,
                modelId);
        }
    }
}
