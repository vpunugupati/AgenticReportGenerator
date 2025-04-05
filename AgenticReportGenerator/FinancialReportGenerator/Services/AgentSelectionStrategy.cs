using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace FinancialReportGenerator.Services
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only
    public sealed class AgentSelectionStrategy : SelectionStrategy
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only
    {
        // Phase constants
        private const int ResearchPhase = 0;
        private const int DraftingPhase = 1;
        private const int ReviewPhase = 2;
        private const int RevisionPhase = 3;
        private const int FinalReviewPhase = 4;

        // Phase names for logging
        private static readonly string[] PhaseNames = new string[]
        {
            "Research Phase",
            "Drafting Phase",
            "Review Phase",
            "Revision Phase",
            "Final Review Phase"
        };

        // Current state
        private int _currentPhase = ResearchPhase;
        private int _consecutiveTurns = 0;
        private string _lastSpeakerName = string.Empty;
        private bool _researchComplete = false;
        private bool _draftComplete = false;
        private bool _revisionRequested = false;
        private int _maxConsecutiveTurnsAllowed = 5;

        // Agent role names (configurable)
        public string ResearcherName { get; set; } = "FinancialResearcher";
        public string WriterName { get; set; } = "FinancialReportWriter";
        public string EditorName { get; set; } = "FinancialReportEditor";

        // Logging settings
        public bool EnableLogging { get; set; } = true;

        // Track message counts by agent
        private Dictionary<string, int> _agentMessageCounts = new();

        protected override Task<Agent> SelectAgentAsync(
            IReadOnlyList<Agent> agents,
            IReadOnlyList<ChatMessageContent> history,
            CancellationToken cancellationToken = default)
        {
            // Initialize message counts if needed
            EnsureAgentCountsInitialized(agents);

            if (EnableLogging)
            {
                LogCurrentState();
            }

            // Analyze conversation context before making selection
            AnalyzeConversationContext(history);

            if (EnableLogging && history.Count > 0)
            {
                var lastMessage = history[history.Count - 1];
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{timestamp}] AGENT SELECTION: Last message from {lastMessage.AuthorName ?? "unknown"}, research complete: {_researchComplete}, draft complete: {_draftComplete}, revision requested: {_revisionRequested}");
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only
            }

            // Select next agent based on phase and context
            Agent selectedAgent = SelectNextAgent(agents, history);

            // Update state after selection
            UpdateState(selectedAgent);

            if (EnableLogging)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{timestamp}] AGENT SELECTION: Selected {selectedAgent.Name} for next turn (Phase: {PhaseNames[_currentPhase]}, consecutive turns: {_consecutiveTurns})");
            }

            return Task.FromResult(selectedAgent);
        }


        private void EnsureAgentCountsInitialized(IReadOnlyList<Agent> agents)
        {
            foreach (var agent in agents)
            {
                if (!_agentMessageCounts.ContainsKey(agent.Name))
                {
                    _agentMessageCounts[agent.Name] = 0;
                }
            }
        }

        private void AnalyzeConversationContext(IReadOnlyList<ChatMessageContent> history)
        {
            if (history.Count == 0) return;

            var lastMessage = history[history.Count - 1];

            // Check for phase transition indicators in message content
            string content = lastMessage.Content?.ToLower() ?? string.Empty;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
            // Detect completed research
            if (lastMessage.AuthorName == ResearcherName &&
                ContainsResearchCompletion(content))
            {
                _researchComplete = true;
                if (EnableLogging)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"[{timestamp}] AGENT SELECTION: Research completion detected from {ResearcherName}");
                }
            }

            // Detect draft completion
            if (lastMessage.AuthorName == WriterName &&
                ContainsDraftCompletion(content))
            {
                _draftComplete = true;
                _revisionRequested = false;
                if (EnableLogging)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"[{timestamp}] AGENT SELECTION: Draft completion detected from {WriterName}");
                }
            }

            // Detect revision requests or report approval
            if (lastMessage.AuthorName == EditorName)
            {
                bool reportApproved = content.Contains("report approved", StringComparison.OrdinalIgnoreCase);

                if (reportApproved)
                {
                    // Mark the report as approved, no more revisions needed
                    _revisionRequested = false;
                    _draftComplete = true;

                    if (EnableLogging)
                    {
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        Console.WriteLine($"[{timestamp}] AGENT SELECTION: Report approval detected from {EditorName}");
                    }
                }
                else if (ContainsRevisionRequest(content))
                {
                    _revisionRequested = true;
                    _draftComplete = false; // Reset as revision needed

                    if (EnableLogging)
                    {
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        Console.WriteLine($"[{timestamp}] AGENT SELECTION: Revision request detected from {EditorName}");
                    }
                }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only
            }
        }


        private Agent SelectNextAgent(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history)
        {
            string reason;
            Agent selectedAgent;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Check if the last message from editor contains report approval
            bool reportApproved = false;
            if (history.Count > 0)
            {
                var lastMessage = history[history.Count - 1];
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                if (lastMessage.AuthorName == EditorName)
                {
                    string content = lastMessage.Content?.ToLower() ?? string.Empty;
                    reportApproved = content.Contains("report approved", StringComparison.OrdinalIgnoreCase);
                }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }

            switch (_currentPhase)
            {
                case ResearchPhase:
                    if (!_researchComplete && _consecutiveTurns < _maxConsecutiveTurnsAllowed)
                    {
                        // Continue research if not complete and within consecutive turn limit
                        reason = "continuing research (incomplete and within turn limit)";
                        selectedAgent = agents.First(a => a.Name == ResearcherName);
                    }
                    else
                    {
                        // Move to drafting phase
                        reason = "moving to drafting phase (research complete or max turns reached)";
                        _currentPhase = DraftingPhase;
                        selectedAgent = agents.First(a => a.Name == WriterName);
                    }
                    break;

                case DraftingPhase:
                    if (_researchComplete && !_draftComplete && _consecutiveTurns < _maxConsecutiveTurnsAllowed)
                    {
                        // Writer continues drafting
                        reason = "continuing drafting (research complete and within turn limit)";
                        selectedAgent = agents.First(a => a.Name == WriterName);
                    }
                    else
                    {
                        // Move to review phase
                        reason = "moving to review phase (drafting complete)";
                        _currentPhase = ReviewPhase;
                        selectedAgent = agents.First(a => a.Name == EditorName);
                    }
                    break;

                case ReviewPhase:
                    if (_draftComplete && _consecutiveTurns < 2)
                    {
                        // Editor provides initial review
                        reason = "editor providing initial review";
                        selectedAgent = agents.First(a => a.Name == EditorName);
                    }
                    else
                    {
                        // Move to revision phase if changes requested
                        _currentPhase = _revisionRequested ? RevisionPhase : FinalReviewPhase;
                        reason = _revisionRequested
                            ? "moving to revision phase (changes requested)"
                            : "moving to final review (no changes requested)";
                        selectedAgent = _revisionRequested
                            ? agents.First(a => a.Name == WriterName)
                            : agents.First(a => a.Name == EditorName);
                    }
                    break;

                case RevisionPhase:
                    if (!_draftComplete && _revisionRequested && _consecutiveTurns < _maxConsecutiveTurnsAllowed)
                    {
                        // Writer implements requested changes
                        reason = "writer implementing requested changes";
                        selectedAgent = agents.First(a => a.Name == WriterName);
                    }
                    else if (!_draftComplete && _agentMessageCounts[ResearcherName] < 5 && NeedsAdditionalResearch(history))
                    {
                        // Get additional research if needed
                        reason = "requesting additional research";
                        selectedAgent = agents.First(a => a.Name == ResearcherName);
                    }
                    else
                    {
                        // Move to final review
                        reason = "moving to final review";
                        _currentPhase = FinalReviewPhase;
                        selectedAgent = agents.First(a => a.Name == EditorName);
                    }
                    break;

                case FinalReviewPhase:
                default:
                    if (reportApproved)
                    {
                        // If report is approved, keep the editor for final confirmation
                        reason = "report approved, editor providing final confirmation";
                        selectedAgent = agents.First(a => a.Name == EditorName);
                    }
                    else if (_revisionRequested)
                    {
                        // Check if we've exceeded the allowed iterations for the writer in the final review phase
                        int writerMessagesInFinalReview = GetAgentMessagesInCurrentPhase(WriterName, history);

                        if (writerMessagesInFinalReview >= _maxConsecutiveTurnsAllowed)
                        {
                            // If writer has had too many attempts, force a final editor decision
                            reason = "max writer revisions reached in final review, forcing editor decision";
                            _revisionRequested = false; // Reset to stop the revision loop
                            selectedAgent = agents.First(a => a.Name == EditorName);
                        }
                        else
                        {
                            // If revisions requested in final review, go back to writer
                            reason = "revisions requested in final review, writer addressing feedback";
                            selectedAgent = agents.First(a => a.Name == WriterName);
                        }
                    }
                    else if (_consecutiveTurns < _maxConsecutiveTurnsAllowed)
                    {
                        // Editor continues final review
                        reason = "editor continuing final review";
                        selectedAgent = agents.First(a => a.Name == EditorName);
                    }
                    else
                    {
                        // Max iterations reached, let the editor finish
                        reason = "max iterations reached, editor providing final assessment";
                        selectedAgent = agents.First(a => a.Name == EditorName);
                    }
                    break;
            }

            if (EnableLogging)
            {
                Console.WriteLine($"[{timestamp}] AGENT SELECTION: Selected {selectedAgent.Name} - Reason: {reason}");
            }

            return selectedAgent;
        }

        // Helper method to count how many messages an agent has sent in the current phase
        private int GetAgentMessagesInCurrentPhase(string agentName, IReadOnlyList<ChatMessageContent> history)
        {
            int count = 0;
            int phaseStartIndex = 0;

            // Find when the current phase started
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var message = history[i];
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
                if (message.Content?.Contains($"Phase: {PhaseNames[_currentPhase]}", StringComparison.OrdinalIgnoreCase) == true)
                {
                    phaseStartIndex = i;
                    break;
                }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only
            }

            // Count messages from this agent in the current phase
            for (int i = phaseStartIndex; i < history.Count; i++)
            {
                var message = history[i];
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
                if (message.AuthorName == agentName)
                {
                    count++;
                }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only
            }

            return count;
        }

        private void UpdateState(Agent selectedAgent)
        {
            // Update consecutive turn counter
            if (_lastSpeakerName == selectedAgent.Name)
            {
                _consecutiveTurns++;
            }
            else
            {
                _consecutiveTurns = 1;
                _lastSpeakerName = selectedAgent.Name;
            }

            // Increment message count for this agent
            _agentMessageCounts[selectedAgent.Name]++;
        }

        // Helper methods to analyze message content
        private bool ContainsResearchCompletion(string content)
        {
            bool result = content.Contains("research complete", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("findings complete", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("here are the financial results", StringComparison.OrdinalIgnoreCase) ||
                   _agentMessageCounts[ResearcherName] >= 3 && content.Length > 1000;

            if (EnableLogging && result)
            {
                Console.WriteLine("AGENT SELECTION: Research completion criteria matched");
            }

            return result;
        }

        private bool ContainsDraftCompletion(string content)
        {
            // First check if the message only contains the completion phrase without actual content
            if (content.Trim().Equals("draft complete for your review", StringComparison.OrdinalIgnoreCase))
            {
                if (EnableLogging)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"[{timestamp}] AGENT SELECTION: Ignoring empty draft completion message");
                }
                return false;
            }

            bool result = (content.Contains("draft complete", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("for your review", StringComparison.OrdinalIgnoreCase)) &&
                   content.Contains("executive summary", StringComparison.OrdinalIgnoreCase) &&
                    content.Contains("financial performance", StringComparison.OrdinalIgnoreCase) &&
                    content.Contains("Outlook and Guidance", StringComparison.OrdinalIgnoreCase);

            if (EnableLogging && result)
            {
                Console.WriteLine("AGENT SELECTION: Draft completion criteria matched");
            }

            return result;
        }

        private bool ContainsRevisionRequest(string content)
        {
            // First check if report is explicitly approved
            if (content.Contains("report approved", StringComparison.OrdinalIgnoreCase) &&
                !content.Contains("no \"report approved\"", StringComparison.OrdinalIgnoreCase) &&
                !content.Contains("not approved", StringComparison.OrdinalIgnoreCase))
            {
                // This is a clear approval
                if (EnableLogging)
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine($"[{timestamp}] AGENT SELECTION: Report approval detected");
                }
                return false;
            }
            else 
            {
                // Check for explicit rejection/revision phrases
                bool hasExplicitRevisionRequest =
                    content.Contains("revise", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("not approved", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("requires revision", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("needs changes", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("modify", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("change", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("fix", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("improve", StringComparison.OrdinalIgnoreCase);

                if (EnableLogging && hasExplicitRevisionRequest)
                {
                    Console.WriteLine("AGENT SELECTION: Revision request criteria matched");
                }

                return hasExplicitRevisionRequest;
            }  
        }

        private bool NeedsAdditionalResearch(IReadOnlyList<ChatMessageContent> history)
        {
            // Check if recent messages indicate need for more research
            var recentMessages = history.TakeLast(3);
            foreach (var message in recentMessages)
            {
                string content = message.Content?.ToLower() ?? string.Empty;
                if (content.Contains("need more data") ||
                    content.Contains("additional information") ||
                    content.Contains("missing details") ||
                    content.Contains("research"))
                {
                    if (EnableLogging)
                    {
                        Console.WriteLine("AGENT SELECTION: Additional research needed based on recent messages");
                    }
                    return true;
                }
            }
            return false;
        }

        private void LogCurrentState()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{timestamp}] AGENT SELECTION: Current phase: {PhaseNames[_currentPhase]}, Research complete: {_researchComplete}, Draft complete: {_draftComplete}, Revision requested: {_revisionRequested}");
            Console.WriteLine($"[{timestamp}] AGENT SELECTION: Consecutive turns: {_consecutiveTurns}, Last speaker: {_lastSpeakerName}");
            Console.WriteLine($"[{timestamp}] AGENT SELECTION: Message counts: Researcher ({_agentMessageCounts.GetValueOrDefault(ResearcherName, 0)}), Writer ({_agentMessageCounts.GetValueOrDefault(WriterName, 0)}), Editor ({_agentMessageCounts.GetValueOrDefault(EditorName, 0)})");
        }
    }
}
