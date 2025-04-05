using Azure.AI.Projects;
using Azure.Identity;
using FinancialReportGenerator.Configuration;
using FinancialReportGenerator.Models;
using FinancialReportGenerator.Services;
using FinancialReportGenerator.Utils;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FinancialReportGenerator
{
    public partial class FinancialReportAgents
    {
        private readonly AgentService _agentService;
        private readonly AgentChatService _agentChatService;
        private readonly ReferenceCollectorService _bingReferenceService;
        private readonly Kernel _kernel;

        public FinancialReportAgents()
        {
            // Load configuration
            var config = new AgentConfiguration();

            // Setup Semantic Kernel
            _kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(config.DeploymentModelName, config.DeploymentEndpoint, config.OpenAiKey)
                .Build();

            // Setup Azure AI Project & Agent client authentication
            Console.WriteLine("Authenticating to Azure...");
            Console.WriteLine("A browser window will open for you to sign in...");

            //var credential = new ClientSecretCredential(config.AzureTenantId, config.AzureClientId, config.AzureClientSecret);

            // Force interactive browser authentication
            var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
            {
                TenantId = config.AzureTenantId,
                DisableAutomaticAuthentication = false
            });

            // Create clients with the authenticated credential
            var agentsClient = new AgentsClient(config.ConnectionString, credential);
            var aiProjectClient = new AIProjectClient(config.ConnectionString, credential);

            // Initialize services
            _agentService = new AgentService(_kernel, agentsClient, aiProjectClient, config);
            _agentChatService = new AgentChatService(_kernel);
            _bingReferenceService = new ReferenceCollectorService(aiProjectClient);
        }


        public async Task RunFinancialReportAgentsAsync(string companyName)
        {
            // Initialize the agents for the chat
            Console.WriteLine("Initializing financial report agents...");
            var agents = await _agentService.InitializeAgentsAsync();
            Console.WriteLine("Agents(Researcher, Writer, Editor) initialized successfully.");

            // Configure and run the agent group chat
            var chat = _agentChatService.ConfigureAgentGroupChat(
                agents.researcher, 
                agents.writer, 
                agents.editor);

            // Start the conversation with a financial report request
            await RunAgentConversationAsync(companyName, chat);
        }

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private async Task RunAgentConversationAsync(string companyName, AgentGroupChat agentGroupchat)
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            Console.WriteLine("\nStarting collaboration between Financial Researcher, Report Writer, and Report Editor...");
           
            // Create a collector for Bing search references
            var bingReferences = new References();

            // Set the initial prompt with company name
            string userPrompt = _agentService.GetInitialPrompt(companyName);

            Console.WriteLine($"\n[User]: {userPrompt}\n");
            agentGroupchat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userPrompt));

            // Process agent responses
            try
            {
                await _agentChatService.ProcessAgentConversationAsync(
                    agentGroupchat,
                    bingReferences,
                    _bingReferenceService,
                    message => Console.WriteLine($"[{message.agentName}]: {message.content}\n"));

                // Show conversation summary
                Console.WriteLine("\nFinancial Report Collaboration Complete!");
                Console.WriteLine($"Chat finished: {agentGroupchat.IsComplete}");
                Console.WriteLine($"Total messages: {await agentGroupchat.GetChatMessagesAsync().CountAsync()}");

                // Generate final report with references
                if (agentGroupchat.IsComplete)
                {
                    Console.WriteLine("\nGenerating final report files...");

                    // Get all messages
                    var messages = await agentGroupchat.GetChatMessagesAsync().ToListAsync();

                    // Find the editor's final approval message
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                    var editorApprovalMessage = messages
                        .Where(m => m.AuthorName == "FinancialReportEditor")
                        .LastOrDefault(m => m.Content?.Contains("REPORT APPROVED", StringComparison.OrdinalIgnoreCase) == true);
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                    string reportContent = null;
                    if (editorApprovalMessage != null)
                    {
                        // Extract the approved report content by finding the most recent writer message with "Executive Summary"
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        var approvedContent = messages
                            .Where(m => (m.AuthorName == "FinancialReportWriter" || m.AuthorName == "FinancialReportEditor") &&
                                   m.Content?.Contains("Executive Summary", StringComparison.OrdinalIgnoreCase) == true &&
                                   m.Metadata != null && m.Metadata.ContainsKey("CreatedAt"))
                            .OrderByDescending(m => DateTimeUtils.GetDateTimeFromMetadata(m.Metadata["CreatedAt"]))
                            .FirstOrDefault();
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                        if (approvedContent != null && !string.IsNullOrEmpty(approvedContent.Content))
                        {
                            reportContent = approvedContent.Content;
                        }
                    }
                    else
                    {
                        // Use the last message from the writer that contains "Executive Summary"
                        var lastWriterMessage = messages
                            .Where(m => m.Content?.Contains("Executive Summary", StringComparison.OrdinalIgnoreCase) == true &&
                                   m.Metadata != null && m.Metadata.ContainsKey("CreatedAt"))
                            .OrderByDescending(m => DateTimeUtils.GetDateTimeFromMetadata(m.Metadata["CreatedAt"]))
                            .FirstOrDefault();

                        if (lastWriterMessage != null && !string.IsNullOrEmpty(lastWriterMessage.Content))
                        {
                            reportContent = lastWriterMessage.Content;
                        }
                    }

                    if (!string.IsNullOrEmpty(reportContent))
                    {
                        // Clean the report content before generating files
                        string cleanedContent = await CleanReportContentAsync(reportContent, companyName);

                        Console.WriteLine("Report content cleaned successfully.");
                        Console.WriteLine(cleanedContent);

                        if (cleanedContent.StartsWith("```markdown") && cleanedContent.EndsWith("```"))
                        {
                            //Remove that two lines from start and end
                            // Find the first line break after the opening fence
                            int startIndex = cleanedContent.IndexOf('\n');
                            if (startIndex != -1)
                            {
                                // Find the position of the last line break before the closing fence
                                int endIndex = cleanedContent.LastIndexOf('\n');

                                if (endIndex > startIndex)
                                {
                                    // Extract the content between the fences (after first line break and before last line break)
                                    cleanedContent = cleanedContent.Substring(startIndex + 1, endIndex - startIndex - 1);
                                    Console.WriteLine("Markdown code fences removed from report content.");
                                }
                            }

                        }

                        // Generate the report files with the cleaned content
                        await ReportService.GenerateReportFilesAsync(cleanedContent, companyName, bingReferences);
                    }
                    else
                    {
                        Console.WriteLine("No suitable content found for report generation.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during agent conversation: {ex.Message}");
            }
        }

        private async Task<string> CleanReportContentAsync(string content, string companyName)
        {
            Console.WriteLine("Cleaning report content before generation...");

            try
            {
                // Find or create the report cleaner agent
                var reportCleaner = await _agentService.FindOrCreateReportCleanerAgentAsync();

                // Create a chat to interact with the cleaner agent
                var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

                // Define the cleaning instructions with specific mention of removing "REPORT APPROVED"
                string cleaningPrompt = $@"
                You are cleaning a financial report for {companyName}. Apply these specific improvements:

                1. CLEAN TASK: Remove any drafting artifacts like 'DRAFT COMPLETE FOR YOUR REVIEW', 'REPORT APPROVED', or placeholder text
                2. FORMAT TASK: Ensure all tables are properly aligned with consistent spacing
                3. STRUCTURE TASK: Confirm all sections (Executive Summary, Financial Performance, Outlook) are present and properly formatted
                4. CONSISTENCY TASK: Standardize numerical formatting (e.g., $XXM for millions consistently)
                5. COMPLETENESS TASK: Make sure there are no incomplete sentences or sections
                6. MARKDOWN TASK: Verify all markdown formatting is correct and consistent
                7. APPROVAL REMOVAL TASK: Remove any standalone lines containing only 'REPORT APPROVED' or similar approval markers

                IMPORTANT: 
                - Do not change any financial data, facts, or analysis. Your job is only to improve formatting.
                - Be particularly careful to remove any instances of 'REPORT APPROVED' text that might appear at the end of the document.
                - The final report should not contain any approval or draft status markers.

                Here's the report to clean:

                {content}

                Return only the cleaned report without any additional commentary or approval markers.
                ";

                // Process the content with the cleaner agent
                var history = new ChatHistory();
                history.AddUserMessage(cleaningPrompt);

                var result = await chatCompletion.GetChatMessageContentAsync(history);

                if (!string.IsNullOrEmpty(result?.Content))
                {
                    Console.WriteLine("Report content successfully cleaned.");

                    // Additional manual check for "REPORT APPROVED" text, just to be safe
                    string cleanedContent = result.Content;
                    if (cleanedContent.Contains("REPORT APPROVED", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove any lines that contain only "REPORT APPROVED" (with possible whitespace)
                        string[] lines = cleanedContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                        List<string> filteredLines = new List<string>();

                        foreach (string line in lines)
                        {
                            if (!line.Trim().Equals("REPORT APPROVED", StringComparison.OrdinalIgnoreCase))
                            {
                                filteredLines.Add(line);
                            }
                            else
                            {
                                Console.WriteLine("Removed 'REPORT APPROVED' line during post-processing.");
                            }
                        }

                        return string.Join(Environment.NewLine, filteredLines);
                    }

                    return cleanedContent;
                }
                else
                {
                    Console.WriteLine("Warning: Report cleaning returned empty response. Using original content.");
                    return content;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning report content: {ex.Message}");
                Console.WriteLine("Using original content instead.");
                return content;
            }
        }

    }
}
