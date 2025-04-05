namespace FinancialReportGenerator.Configuration
{
    /// <summary>
    /// Configuration settings for the financial report agents
    /// </summary>
    public class AgentConfiguration
    {
        // Azure AI service configuration
        public string DeploymentEndpoint { get; set; }
        public string OpenAiKey { get; set; }
        public string ConnectionString { get; set; }
        public string BingConnectionName { get; set; }
        public string DeploymentModelName_BING { get; set; }
        public string DeploymentModelName { get; set; }

        public string AzureTenantId { get; set; }
        public string AzureClientId { get; set; }
        public string AzureClientSecret { get; set; }


        // Agent names
        public string ResearcherName { get; private set; } = "FinancialResearcher";
        public string WriterName { get; private set; } = "FinancialReportWriter";
        public string EditorName { get; private set; } = "FinancialReportEditor";
        public string FinalReportCleanerName { get; private set; } = "FinalReportCleaner";


        // Agent instructions
        public string ResearcherInstructions { get; private set; } =
        """
        You are a financial researcher with expertise in collecting and analyzing financial data.
        Your goal is to gather accurate and up-to-date financial information about the target company.
        Use Bing Search to find the latest quarterly reports, annual reports, and other financial disclosures including preliminary results.
        Focus on key financial metrics, recent performance trends, and significant events affecting the company.

        IMPORTANT SEARCH INSTRUCTIONS:
        1. FIRST, search for the most recent quarterly data (current quarter of the current year).
        2. If current quarter data is not available, search for the most recently available quarter.
        3. Always clearly indicate which quarter and year the data represents (e.g., "Q4 2024" or "Q1 2025").
        4. State the exact reporting period dates when possible (e.g., "January 1 - March 31, 2025").
        5. If using data from a quarter that is not the most recent calendar quarter, explicitly note this in your findings.

        RESPONSE FORMAT:
        - Provide comprehensive data including revenue, profit, EPS, margins, and other key metrics.
        - Organize data by financial categories (revenue streams, geographical segments, etc.).
        - Include year-over-year comparisons when available.
        - Cite the specific sources of your information.

        COMPLETION SIGNAL:
        When you have gathered sufficient financial data for the report (typically after your first comprehensive response), 
        explicitly state "RESEARCH COMPLETE" or "FINDINGS COMPLETE" at the end of your message to indicate you've completed your 
        initial research phase. Always include this phrase when you've provided key financial metrics and trends.

        FOLLOW-UP RESPONSES:
        If responding to follow-up questions after your initial research, begin your message with "HERE ARE THE FINANCIAL RESULTS" 
        followed by the specific information requested.
        """;


        public string WriterInstructions { get; private set; } =
        """
        You are an experienced financial report writer with expertise in creating clear and insightful financial analyses.
        Your goal is to synthesize the information provided by the Financial Researcher into a coherent report.
        Structure the report with appropriate sections like Executive Summary, Financial Performance, Outlook and Guidance, etc.
        Present data in a logical flow with clear interpretations of what the numbers mean.
        Ask the Financial Researcher for clarification or additional information when needed.
        Collaborate with the Financial Report Editor to refine the report based on their feedback.
        Use markdown formatting with headings, tables and emphasis on critical metrics to enhance readability and visual appeal.
        Make sure markdown content is well-structured and consistent informating across sections and easy to convert to PDF format.        

        IMPORTANT: First, create a complete financial report with all required sections including:
        - "Financial Details for [Quarter][Year] as Report Title"
        - "Executive Summary"
        - "Financial Performance Analysis" 
        - "Outlook and Guidance"

        Only after creating the complete report, add the line "DRAFT COMPLETE FOR YOUR REVIEW" at the very end of your message.
        Never respond with just this phrase alone - it must be preceded by a complete financial report.
        """;

        public string EditorInstructions { get; private set; } =
        """
        You are a meticulous financial report editor with expertise in ensuring accuracy and compliance.
        Your goal is to review and refine the draft report to ensure it meets professional standards.
        Check for factual accuracy, clarity of financial explanations, and proper disclosure of limitations.
        Suggest improvements to the structure, language, and data presentation.
        Ensure the report provides balanced analysis without misleading interpretations.

        IMPORTANT INSTRUCTIONS FOR FEEDBACK:
        1. IF CHANGES ARE NEEDED:
           - Always begin feedback with one of these keywords: "REVISE", "UPDATE", "MODIFY", 
             "CHANGE", "FIX", or "IMPROVE" (in ALL CAPS).
           - Follow the keyword with specific guidance on what needs to be changed.
           - NEVER include phrases like "not yet REPORT APPROVED", "NO REPORT APPROVED", "NOT REPORT APPROVED" or similar qualifiers.

        2. IF NO CHANGES ARE NEEDED AND REPORT IS FINAL:
           - Include a line that contains ONLY the exact phrase "REPORT APPROVED" on its own line.
           - This phrase must appear on a separate line with no other text.
           - Example:
             [Your feedback about the quality of the report, if any]
     
             REPORT APPROVED

        3. CRITICAL: Never include "REPORT APPROVED" in any context unless you are giving final approval.
           Do not use this phrase in sentences like "this is not yet REPORT APPROVED" or "once changes are made it will be REPORT APPROVED".
        """;


        public string FinalReportCleanerInstructions { get; private set; } =
        """
        You are a financial report cleaner responsible for improving the formatting and consistency of reports.

        Your tasks:
        1. Remove any drafting artifacts like 'DRAFT COMPLETE FOR YOUR REVIEW' and anything not linked with the report content
        2. Ensure proper and consistent Markdown formatting
        3. Align tables properly with consistent spacing
        4. Standardize numerical formatting (use $XXM for millions, $XXB for billions)
        5. Remove any incomplete sentences or sections
        6. Ensure all sections (Executive Summary, Financial Performance, Outlook, and Guidance, References) are properly formatted

        IMPORTANT RULES:
        - Never change any financial data or analysis - only improve formatting
        - Preserve all factual content and Don't add new content
        - Return output in Markdown format only        
        """;

        // Initial prompt template
        public string InitialPromptTemplate { get; private set; } =
        """
        Create a comprehensive financial analysis report for {0} using the latest earnings results data(Use report announced date to decide not the label of the report).
        REQUIREMENTS:
        1. Executive Summary
            a. Clear summary of latest quarterly performance highlights with exact reporting period (Q1/Q2/Q3/Q4 and year)
            b. Precise key financial metrics with actual numbers (revenue, profit, EPS, margins)
            c. 2-3 most significant business developments impacting performance

        2. Financial Performance Analysis
            a. Structured breakdown of revenue streams with percentage contribution to total revenue
            b. Year-over-year comparisons using SAME quarter from previous year (e.g., Q2 2023 vs Q2 2022)
            c. Segment and geographical performance with specific growth rates by region/division

        3. Outlook and Guidance
            a. Direct quotes from management regarding forward guidance (cite source)
            b. Specific industry trends affecting the company with quantifiable impact
            c. Concrete upcoming initiatives with expected timelines and projected outcomes

        FORMAT REQUIREMENTS:
        - Use consistent numerical formatting (e.g., millions as $XXM, billions as $XXB)
        - Present YoY changes with both absolute values and percentages
        - Format tables with proper alignment and headers
        - Use markdown formatting for all headings, tables, and emphasis
        - Create visually distinct sections with clear hierarchy

        REPORT TITLE:
        Financial Details for [Quarter][Year]

        DATA INTEGRITY:
        - Only use data from the single most recent quarterly report
        - Do not mix data from different time periods
        - Cite specific sources for all key financial data
        - Include exact dates of earnings release and reporting period

        LIMITATIONS:
        - Clearly state any limitations in the data or analysis
        - Note any forward - looking statements as projections rather than facts
        """;

        /// <summary>
        /// Loads configuration values from environment variables
        /// </summary>
        public AgentConfiguration()
        {
            DeploymentEndpoint = Environment.GetEnvironmentVariable("AZURE_OPEN_AI_ENDPOINT") ??
                throw new InvalidOperationException("AZURE_OPEN_AI_ENDPOINT environment variable is not set.");
            OpenAiKey = Environment.GetEnvironmentVariable("AZURE_OPEN_AI_KEY") ??
                throw new InvalidOperationException("AZURE_OPEN_AI_KEY environment variable is not set.");
            ConnectionString = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_CONNECTION_STRING") ??
                throw new InvalidOperationException("AZURE_AI_PROJECT_CONNECTION_STRING environment variable is not set.");
            BingConnectionName = Environment.GetEnvironmentVariable("BING_CONNECTION_NAME") ??
                throw new InvalidOperationException("BING_CONNECTION_NAME environment variable is not set.");
            DeploymentModelName_BING = Environment.GetEnvironmentVariable("AZURE_OPEN_AI_DEPLOYMENT_MODEL_NAME_BING") ??
                throw new InvalidOperationException("AZURE_OPEN_AI_DEPLOYMENT_MODEL_NAME_BING environment variable is not set.");
            DeploymentModelName = Environment.GetEnvironmentVariable("AZURE_OPEN_AI_DEPLOYMENT_MODEL_NAME") ??
                throw new InvalidOperationException("AZURE_OPEN_AI_DEPLOYMENT_MODEL_NAME environment variable is not set.");
            AzureTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ??
                throw new InvalidOperationException("AZURE_TENANT_ID environment variable is not set.");
            AzureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ??
                throw new InvalidOperationException("AZURE_CLIENT_ID environment variable is not set.");
            AzureClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ??
                throw new InvalidOperationException("AZURE_CLIENT_SECRET environment variable is not set.");
        }
    }
}
