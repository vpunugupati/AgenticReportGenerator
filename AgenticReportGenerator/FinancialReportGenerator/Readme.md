# Financial Report Generator

## Description
Financial Report Generator is a .NET 8 application designed to create comprehensive financial analysis reports for companies based on their most recent quarterly earnings data. The application uses Azure AI Foundry, Azure OpenAI and Semantic Kernel to facilitate an AI-driven multi-agent workflow that researches, writes, edits, and formats financial reports.

## Features
- **Multi-Agent Collaboration**: Utilizes a team of specialized AI agents:
  - Financial Researcher: Gathers up-to-date financial data using Bing Search
  - Financial Report Writer: Synthesizes research into structured reports
  - Financial Report Editor: Reviews and refines reports for accuracy and quality
  - Final Report Cleaner: Standardizes formatting and presentation
- **Comprehensive Reporting**: Generates reports including:
  - Executive Summary
  - Financial Performance Analysis
  - Outlook and Guidance
  - References from research sources
- **Multiple Output Formats**: Produces both Markdown and PDF versions of reports
- **Azure AI Foundry Integration**: Leverages Azure AI Foundry's unified development experience for AI solutions
- **Bing Search Integration**: Accesses real-time financial data from the web

## Installation
1. Clone the repository:
   
2. Set up environment variables:
   Create a `.env` file in the root directory and add the following variables:
   
3. Build the project:
   
## Usage
1. Run the application:
   
2. Enter the company name when prompted:
   
3. The application will generate a financial report for the specified company and save it in the `Reports` directory.

## Dependencies
- .NET 8
- Azure AI Foundry
- Azure OpenAI service
- Bing Search API
- Markdig for Markdown processing
- PuppeteerSharp for HTML to PDF conversion

## Configuration
The application requires the following environment variables to be set:
- `AZURE_OPEN_AI_ENDPOINT`: The endpoint for Azure OpenAI service.
- `AZURE_OPEN_AI_KEY`: The API key for Azure OpenAI service.
- `AZURE_AI_PROJECT_CONNECTION_STRING`: The connection string for Azure AI Foundry Project.
- `BING_CONNECTION_NAME`: The name of the Bing connection.
- `AZURE_OPEN_AI_DEPLOYMENT_MODEL_NAME_BING`: The deployment model name for Bing search integration.
- `AZURE_OPEN_AI_DEPLOYMENT_MODEL_NAME`: The deployment model name for general OpenAI operations.
- `AZURE_TENANT_ID`: Your Azure tenant ID.
- `AZURE_CLIENT_ID`: The client ID for Azure authentication.
- `AZURE_CLIENT_SECRET`: The client secret for Azure authentication.

## Azure AI Foundry
This application is built using [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/what-is-azure-ai-foundry), which provides:
- A unified development experience for AI solutions
- Enterprise-ready AI capabilities
- Simplified access to a range of Azure AI services and models
- Tools for building, testing, and deploying AI applications

## Contributing
Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License
This project is licensed under the MIT License.
