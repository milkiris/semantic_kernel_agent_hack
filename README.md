# AGENT HACK with Semantic Kernel


## Prerequisites

- Python 3.8 or higher
- Azure OpenAI service subscription
- Semantic Kernel Python SDK

## Installation

1. Install the required packages:

```bash
pip install semantic-kernel
```

2. Create a `.env` file in your project directory with the following content:

```
AZURE_OPENAI_DEPLOYMENT_NAME=your-deployment-name
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
```

## Usage

Run the scripts:

```bash
python 01_simple_agent.py
```

For chainlit samples, you will also need to install
```bash
pip install chainlit
```

and run with, e.g.

```bash
chainlit run 03_agent_with_ui.py
```


## Resources

- [Semantic Kernel Documentation](https://github.com/microsoft/semantic-kernel/tree/main/python)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Chainlit with Semantic Kernel](https://docs.chainlit.io/integrations/semantic-kernel)