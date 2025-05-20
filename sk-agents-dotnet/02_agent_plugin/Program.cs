using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.ComponentModel;
using System.Text.Json;

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
kernel.ImportPluginFromType<MenuPlugin>();
kernel.FunctionInvocationFilters.Add(new InvocationFilter());

// Initialize a chat agent with basic instructions
ChatCompletionAgent agent =
    new()
    {
        Name = "SK-Assistant",
        Instructions = "You are a helpful assistant. Answer questions about the menu.",
        Kernel = kernel,
        Arguments =
            new KernelArguments(
                new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };

// Get a response to a user message
ChatMessageContent response = await agent.InvokeAsync("What is the soup special and its price?").SingleAsync();
Console.WriteLine(response.Content);

// Output:
// The price of the Clam Chowder, which is the soup special, is $9.99.

// Helper to retreive environment configuration
static string GetEnvironmentConfiguration(string variableName) =>
    Environment.GetEnvironmentVariable(variableName) ??
    throw new InvalidOperationException($"Environment variable {variableName} is not defined.");

// The plugin to provide menu information
sealed class MenuPlugin
{
    [KernelFunction, Description("Provides a list of specials from the menu.")]
    public string GetSpecials() =>
        """
        Special Soup: Clam Chowder
        Special Salad: Cobb Salad
        Special Drink: Chai Tea
        """;

    [KernelFunction, Description("Provides the price of the requested menu item.")]
    public string GetItemPrice(
        [Description("The name of the menu item.")]
        string menuItem) => "$9.99";
}

// A filter that will be called for each function call in the response.
sealed class InvocationFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"    Plugin [{context.Function.Name}] called with context: {JsonSerializer.Serialize(context.Arguments)}");
        await next(context);
        Console.WriteLine($"    Response from plugin [{context.Function.Name}]: {context.Result.GetValue<string>()}");
    }
}