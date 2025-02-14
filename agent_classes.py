from pydantic import BaseModel, Field, ConfigDict
from enum import Enum
from typing import Literal

class AgentResponse(BaseModel):
    """
    Represents an agent's action response with JSON-compatible field names.
    """
    reasoning: str = Field(
        ...,
        alias="Reasoning",
        description="Reasoning behind the agent's action selection",
    )

    next_action: Literal["FoodGatherAgent", "RestBehavior"] = Field(
        ...,
        alias="Next_Action",
        description="Agent's current activity or objective",
    )

    # Optional: Add a Config class to customize JSON schema
    class Config:
        json_schema_extra = {
            "example": {
                "reasoning": "The agent is hungry and needs to gather food to maintain energy levels.",
                "next_action": "FoodGatherAgent"
            }
        }