using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

// Retrieve the Azure OpenAI endpoint and deployment name from environment variables
string endpoint = GetEnvironmentConfiguration("AZURE_OPENAI_ENDPOINT");
string deploymentName = GetEnvironmentConfiguration("AZURE_OPENAI_DEPLOYMENT_NAME");
string apikey = GetEnvironmentConfiguration("AZURE_OPENAI_API_KEY");

// Initialize a kernel with chat completion service
Kernel kernel =
    Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apikey)
        .Build();

// Initialize a chat agent with basic instructions
ChatCompletionAgent agent =
    new()
    {
        Name = "SK-Assistant",
        Instructions = "You are a helpful assistant.",
        Kernel = kernel
    };

// Get a response to a user message
ChatMessageContent response = await agent.InvokeAsync("Write a haiku about Semantic Kernel.").SingleAsync();
Console.WriteLine(response.Content);

// Output:
// Words weave through the code,
// Intelligence in nature,
// Kernel of meaning.

// Helper to retreive environment configuration
static string GetEnvironmentConfiguration(string variableName) =>
    Environment.GetEnvironmentVariable(variableName) ??
    throw new InvalidOperationException($"Environment variable {variableName} is not defined.");