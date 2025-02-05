from pydantic import BaseModel, Field, ConfigDict
from enum import Enum

# Define the Pydantic Class for JSON outputs
class Actions(str, Enum):
    SCAN = "Scan perimiter for threats."
    COLLECT_FOOD = "Begin collecting nearby food."
    STORE_FOOD = "Go back to base to store the collected food."
    EVADE_ENEMY = "Take evasive action against detected enemy threats."

class AgentResponse(BaseModel):
    """
    Represents an agent's status response with JSON-compatible field names
    """
    model_config = ConfigDict(
        populate_by_name=True,  # Allows initialization using both field names and JSON aliases
        json_schema_extra={
            "example": {
                "Agent_ID": "AGENT-007",
                "Health": 85,
                "Next_Action": Actions.STORE_FOOD
            }
        }
    )

    agent_id: str = Field(
        ...,
        alias="Agent_ID",
        description="Unique identifier for the agent",
    )

    health: int = Field(
        ...,
        alias="Health",
        description="Current health points (0-100)",
    )

    next_action: Actions = Field(
        ...,
        alias="Next_Action",
        description="Agent's current activity or objective",
    )
