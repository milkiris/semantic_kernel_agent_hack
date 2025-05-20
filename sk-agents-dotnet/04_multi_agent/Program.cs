using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
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

//ChatCompletionAgent billingAgent =
//    new()
//    {
//        Name = "BillingAgent",
//        Instructions =
//            """
//            You specialize in handling customer questions related to billing issues. 
//            This includes clarifying invoice charges, payment methods, billing cycles, 
//            explaining fees, addressing discrepancies in billed amounts, updating payment details, 
//            assisting with subscription changes, and resolving payment failures. 
//            Your goal is to clearly communicate and resolve issues specifically about payments and charges.
//            """,
//        Kernel = kernel
//    };


//ChatCompletionAgent refundAgent =
//    new()
//    {
//        Name = "RefundAgent",
//        Instructions =
//            """
//            You specialize in addressing customer inquiries regarding refunds. 
//            This includes evaluating eligibility for refunds, explaining refund policies, 
//            processing refund requests, providing status updates on refunds, handling complaints related to refunds, 
//            and guiding customers through the refund claim process. 
//            Your goal is to assist users clearly and empathetically to successfully resolve their refund-related concerns.
//            """,
//        Kernel = kernel
//    };


// Retrieve the agents from Azure Foundry
PersistentAgentsClient client = new(endpoint, new AzureCliCredential());
AzureAIAgent billingAgent = new(await client.Administration.GetAgentAsync("<your agent id>"), client);
AzureAIAgent refundAgent = new(await client.Administration.GetAgentAsync("<your agent id>"), client);

// Add the agents as plugins
KernelPlugin agentPlugin =
    KernelPluginFactory.CreateFromFunctions(
        "AgentsPlugin",
        [
            AgentKernelFunctionFactory.CreateFromAgent(billingAgent),
            AgentKernelFunctionFactory.CreateFromAgent(refundAgent)
        ]);

Kernel triageAgentKernel = kernel.Clone();
triageAgentKernel.Plugins.Add(agentPlugin);
triageAgentKernel.FunctionInvocationFilters.Add(new InvocationFilter());

// Initialize a chat agent with basic instructions
ChatCompletionAgent agent =
    new()
    {
        Name = "TriageAgent",
        Instructions =
        """
        Your role is to evaluate the user's request and forward it to the appropriate agent based on the nature of 
        the query. Forward requests about charges, billing cycles, payment methods, fees, or payment issues to the 
        BillingAgent. Forward requests concerning refunds, refund eligibility, refund policies, or the status of 
        refunds to the RefundAgent. Your goal is accurate identification of the appropriate specialist to ensure the
        user receives targeted assistance.
        """,
        Kernel = triageAgentKernel,
        Arguments =
            new KernelArguments(
                new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
    };

// Get a response to a user message
Console.WriteLine("Welcome to the chat bot!\n  Type 'exit' to exit.\n  Try to get some billing or refund help.\n");

AgentThread? thread = null;
while (await Chat())
{
    // Continue the loop until the user types "EXIT"
}

// Output:
// User:> I was charged twice for my subscription last month, can I get one of those payments refunded?
//     Agent [BillingAgent] called with messages: I was charged twice for my subscription last month.
//     Agent [RefundAgent] called with messages: Can I get one of those payments refunded?
//     Response from agent RefundAgent: Of course, I'll be happy to help you with your refund inquiry. Could you please 
//         provide a bit more detail about the specific payment you are referring to? For instance, the item or service 
//         purchased, the transaction date, and the reason why you're seeking a refund? This will help me understand your 
//         situation better and provide you with accurate guidance regarding our refund policy and process.
//         Response from agent BillingAgent: I'm sorry to hear about the duplicate charge. To resolve this issue, could 
//         you please provide the following details:

// 1.The date(s) of the transaction(s).
// 2. The last four digits of the card used for the transaction or any other payment method details.
// 3. The subscription plan you are on.

// Once I have this information, I can look into the charges and help facilitate a refund for the duplicate transaction. 
// Let me know if you have any questions in the meantime!

// Agent :> To address your concern about being charged twice and seeking a refund for one of those payments, please 
//     provide the following information:

// 1. * *Duplicate Charge Details**: Please share the date(s) of the transaction(s), the last four digits of the card used 
//     or details of any other payment method, and the subscription plan you are on. This information will help us verify 
//     the duplicate charge and assist you with a refund.

// 2. **Refund Inquiry Details**: Please specify the transaction date, the item or service related to the payment you want 
//     refunded, and the reason why you're seeking a refund. This will allow us to provide accurate guidance concerning 
//     our refund policy and process.

// Once we have these details, we can proceed with resolving the duplicate charge and consider your refund request. If you 
// have any more questions, feel free to ask!

async Task<bool> Chat()
{
    Console.Write("User:> ");
    string? userInput = Console.ReadLine();

    if (string.IsNullOrEmpty(userInput) || userInput.Equals("EXIT", StringComparison.CurrentCultureIgnoreCase))
    {
        return false;
    }

    AgentResponseItem<ChatMessageContent> response = await agent.InvokeAsync(userInput, thread).SingleAsync();
    thread ??= response.Thread;

    Console.WriteLine($"Agent:> {response.Message.Content}");

    return true;
}

// Helper to retreive environment configuration
static string GetEnvironmentConfiguration(string variableName) =>
    Environment.GetEnvironmentVariable(variableName) ??
    throw new InvalidOperationException($"Environment variable {variableName} is not defined.");

// A filter that will be called for each function call in the response.
sealed class InvocationFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"    Plugin [{context.Function.Name}] called with context: {JsonSerializer.Serialize(context.Arguments)}");
        await next(context);
        ChatMessageContent[] result = context.Result.GetValue<ChatMessageContent[]>() ?? [];
        Console.WriteLine($"    Response from plugin [{context.Function.Name}]: {result.FirstOrDefault()?.Content ?? "[none]"}");
    }
}