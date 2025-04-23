from pydantic import BaseModel, Field, ConfigDict
from enum import Enum
from typing import Literal, Optional

class Location(BaseModel):
    """
    Represents the location with x, y, and z coordinates.
    """
    x: float = Field(
        ...,
        description="The x coordinate of the location"
    )
    z: float = Field(
        ...,
        description="The z coordinate of the location"
    )

class AgentResponse(BaseModel):
    """
    Represents an agent's action response with JSON-compatible field names.
    """
    reasoning: str = Field(
        ...,
        description="Reasoning behind the agent's next action selection",
    )

    next_action: Literal["FoodGathererAgent", "RestBehavior", "MoveBehavior"] = Field(
        ...,
        description="The next action to take for the agent",
    )

    location: Optional[Location] = Field(
        default=None,
        description="The location to move to if the next action is MoveTo",
    )

    # Optional: Add a Config class to customize JSON schema
    class Config:
        json_schema_extra = {
            "example": {
                "reasoning": "The agent is hungry, is located on a food tile and needs to gather food to maintain energy levels.",
                "next_action": "FoodGathererAgent",
                "location": {"x": 1.0, "z": 3.0}
            }
        }