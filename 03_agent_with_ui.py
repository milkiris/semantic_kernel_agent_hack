import chainlit as cl
import semantic_kernel as sk
from semantic_kernel.connectors.ai import FunctionChoiceBehavior
from semantic_kernel.connectors.ai.open_ai import (
    OpenAIChatCompletion,
    OpenAIChatPromptExecutionSettings,
)
from semantic_kernel.functions import kernel_function
from semantic_kernel.contents import ChatHistory

from semantic_kernel.agents import ChatCompletionAgent, ChatHistoryAgentThread
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion

request_settings = OpenAIChatPromptExecutionSettings(
    function_choice_behavior=FunctionChoiceBehavior.Auto(filters={"excluded_plugins": ["ChatBot"]})
)

# Example Native Plugin (Tool)
class WeatherPlugin:
    @kernel_function(name="get_weather", description="Gets the weather for a city")
    def get_weather(self, city: str) -> str:
        """Retrieves the weather for a given city."""
        if "paris" in city.lower():
            return f"The weather in {city} is 20°C and sunny."
        elif "london" in city.lower():
            return f"The weather in {city} is 15°C and cloudy."
        else:
            return f"Sorry, I don't have the weather for {city}."

@cl.on_chat_start
async def on_chat_start():
    # Setup Semantic Kernel
    kernel = sk.Kernel()

    # Add your AI service (e.g., OpenAI)
    # Make sure OPENAI_API_KEY and OPENAI_ORG_ID are set in your environment
    ai_service = OpenAIChatCompletion(service_id="default", ai_model_id="gpt-4o")
    kernel.add_service(ai_service)

    # Import the WeatherPlugin
    kernel.add_plugin(WeatherPlugin(), plugin_name="Weather")
    
    # Instantiate and add the Chainlit filter to the kernel
    # This will automatically capture function calls as Steps
    sk_filter = cl.SemanticKernelFilter(kernel=kernel)

    agent = ChatCompletionAgent(
        kernel=kernel,
        name="Host",
        instructions="You are a helpful assistant",
    )

    thread: ChatHistoryAgentThread = None
    cl.user_session.set("agent", agent)
    cl.user_session.set("thread", thread)

@cl.on_message
async def on_message(message: cl.Message):
    agent = cl.user_session.get("agent") # type: Agent
    thread = cl.user_session.get("thread") # type: ChatHistoryAgentThread 

    answer = cl.Message(content="")

    async for response in agent.invoke_stream(messages=message.content, thread=thread):

        if response.content:
            await answer.stream_token(str(response.content))

        thread = response.thread
        cl.user_session.set("thread", thread)

    # Send the final message
    await answer.send()