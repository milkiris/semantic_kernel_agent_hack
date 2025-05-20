# Copyright (c) Microsoft. All rights reserved.

import asyncio
import os
from typing import Annotated

from azure.ai.projects.models import FileSearchTool, OpenAIFile, VectorStore, BingGroundingTool
from azure.identity.aio import DefaultAzureCredential

from semantic_kernel.agents import AzureAIAgent, AzureAIAgentSettings, AzureAIAgentThread
from semantic_kernel.contents import AuthorRole
from semantic_kernel.contents import AuthorRole
from semantic_kernel.agents import ChatCompletionAgent, ChatHistoryAgentThread
from semantic_kernel.connectors.ai.open_ai import AzureChatCompletion
from semantic_kernel.functions import kernel_function

"""
The following sample demonstrates how to create a simple, Azure AI agent that
uses a file search tool to answer user questions.
"""

# Simulate a conversation with the agent
USER_INPUTS = [
    "What's the latest on the semantic kernel blog?",
]

class SearchAgentPlugin:

    @kernel_function(description="Search for a topic")
    async def search(self, search_query: Annotated[str, "Search query"]) -> Annotated[str, "search results"]:
        ai_agent_settings = AzureAIAgentSettings.create()

        async with (
            DefaultAzureCredential() as creds,
            AzureAIAgent.create_client(credential=creds) as client,
        ):

            agent_definition = await client.agents.create_agent(
                model=ai_agent_settings.model_deployment_name,
            )

            agent_definition = await client.agents.get_agent("asst_I7zm5GEQHBunb1TWMlAdnI3z")

            agent = AzureAIAgent(
                client=client,
                definition=agent_definition,
            )
            thread: AzureAIAgentThread = None

            try:
                print(f"# Search Query: '{search_query}'")
                async for response in agent.invoke(messages=search_query, thread=thread):
                    if response.role != AuthorRole.TOOL:
                        print(f"# Search Result: {response.content}")
                        return response.content
                    thread = response.thread
            finally:
                # 7. Cleanup: Delete the thread and agent and other resources
                await thread.delete() if thread else None


async def main() -> None:

    ai_agent_settings = AzureAIAgentSettings.create()

    async with (
        DefaultAzureCredential() as creds,
        AzureAIAgent.create_client(credential=creds) as client,
    ):

        agent_definition = await client.agents.get_agent("asst_I7zm5GEQHBunb1TWMlAdnI3z")

        search_agent = AzureAIAgent(
            client=client,
            definition=agent_definition,
        )

        agent = ChatCompletionAgent(
            service=AzureChatCompletion(),
            name="Host",
            instructions="You are a helpful assistant. Reply like a pirate",
            plugins=[search_agent],
        )

        thread: ChatHistoryAgentThread = None

        user_input = "Which team won the 2025 NCAA March Madness?"
        print(f"# User: {user_input}")
        
        response = await agent.get_response(messages=user_input, thread=thread)
        print(f"# {response.name}: {response} ")

if __name__ == "__main__":
    asyncio.run(main())