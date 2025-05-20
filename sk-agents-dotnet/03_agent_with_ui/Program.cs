using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ComponentModel;

// Retrieve the Azure OpenAI endpoint and deployment name from environment variables
string endpoint = GetEnvironmentConfiguration("AZURE_OPENAI_ENDPOINT");
string deploymentName = GetEnvironmentConfiguration("AZURE_OPENAI_DEPLOYMENT_NAME");
string apikey = GetEnvironmentConfiguration("AZURE_OPENAI_API_KEY");

// Initialize a kernel with chat completion service
Kernel kernel =
    Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apikey)
        .Build();

// Add the plugin
kernel.ImportPluginFromType<WeatherPlugin>();

// Initialize a chat agent with basic instructions
ChatCompletionAgent agent =
    new()
    {
        Name = "Host",
        Instructions = "You are a helpful assistant..",
        Kernel = kernel,
        Arguments =
            new KernelArguments(
                new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };

// Get a streamed response to a user message
var response = agent.InvokeStreamingAsync("Is it going to rain in london?  If not, what are good places to visit?");
await foreach (StreamingChatMessageContent streamingContent in response)
{
    Console.Write(streamingContent.Content);
}
Console.WriteLine();

// Helper to retreive environment configuration
static string GetEnvironmentConfiguration(string variableName) =>
    Environment.GetEnvironmentVariable(variableName) ??
    throw new InvalidOperationException($"Environment variable {variableName} is not defined.");

// The plugin to provide menu information
sealed class WeatherPlugin
{
    [KernelFunction, Description("Gets the weather for a city.")]
    public static string GetWeather(string city)
    {
        if (city.Equals("paris", StringComparison.CurrentCultureIgnoreCase))
        {
            return $"The weather in {city} is 20°C and sunny.";

        }
        else if (city.Equals("london", StringComparison.CurrentCultureIgnoreCase))
        {
            return $"The weather in {city} is 15°C and cloudy.";
        }
        else
        {
            return $"Sorry, I don't have the weather for {city}.";
        }
    }
}