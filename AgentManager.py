from pydantic_ai import Agent
from pydantic_ai.settings import ModelSettings
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.providers.openai import OpenAIProvider
from AgentClasses import AgentResponse
from pydantic_ai.messages import ModelMessage

class AgentManager:
    def __init__(self):
        # Holds agent instance per entity (keyed by entity name or id)
        self.agents = {}

    def register_entity(self,  agent_id: int, system_prompt: str, model: OpenAIModel) -> bool:
        """
        Creates a new Agent for an entity and registers it.
        """
        # Return False if the agent is already registered for this entity
        if agent_id in self.agents:
            return False
        
        agent = Agent(model=model, 
                      system_prompt=system_prompt,
                      result_type=AgentResponse)
        
        self.agents[agent_id] = {"agent": agent, 
                                 "message_history": []} # Store the agent and an empty message history list.
        return True

    def get_agent(self, agent_id: int) -> Agent:
        """
        Retrieves the Agent for a specific entity.
        """
        if agent_id not in self.agents:
            raise ValueError(f"Agent for entity {agent_id} is not registered.")
        return self.agents[agent_id]["agent"]
    
    def get_message_history(self, agent_id: int) -> list[ModelMessage]:
        """
        Retrieves the Agent for a specific entity.
        """
        if agent_id not in self.agents:
            raise ValueError(f"Agent for entity {agent_id} is not registered.")
        return self.agents[agent_id]["message_history"]
    
    def update_message_history(self, agent_id: int, message_history: list[ModelMessage]):
        """
        Retrieves the Agent for a specific entity.
        """
        if agent_id not in self.agents:
            raise ValueError(f"Agent for entity {agent_id} is not registered.")
        self.agents[agent_id]["message_history"] = message_history

    async def run_entity_query(self, agent_id: int, prompt: str):
        """
        Runs a query using the specified entity's Agent.
        The Agent's message history will be automatically maintained between calls.
        """
        agent = self.get_agent(agent_id)
        # Pass in the conversation state if needed using agent.run_sync's message_history parameter.
        # For a new conversation, you can simply call run_sync with the new prompt.
        result = agent.run(prompt)
        return result