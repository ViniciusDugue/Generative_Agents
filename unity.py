from dotenv import load_dotenv
import openai
from fastapi import FastAPI, HTTPException, Request
import uvicorn
from enum import Enum
from typing import Union
from pydantic import BaseModel, Field, ConfigDict
from pydantic_ai import Agent, RunContext
from pydantic_ai.settings import ModelSettings
from pydantic_ai.models.openai import OpenAIModel
from agent_classes import AgentResponse  # Keep AgentResponse for output
import os
import json

# Load environment variables from .env file
load_dotenv()
OPENAI_API_KEY = os.getenv("OPEN_API_KEY")

sys_prompt = """
    You are an AI agent in a survival environment. Your goal is to decide the next action for the agent based on its current state. 

    Use the map data to understand the location of:
    - Agents (friendly)
    - Enemy Agents (hostile, avoid them)
    - Food (collect to maintain health)

    You actions are defined as:
    1. FoodGatherAgent: Collect nearby food
    2. RestBehavior: Rest to reduce exhaustion
    3. AvoidEnemy: Move away from the nearest enemy agent
    4. Explore: Move randomly when no immediate needs exist
    
    Decision Rules:
    - If exhaustion exceeds 100, choose RestBehavior.
    - If health is below 70 and food is nearby, choose FoodGatherAgent.
    - If an enemy is too close, choose AvoidEnemy.
    - Otherwise, choose Explore.
    
    Expected JSON Input:
    {
        "map": {
            "width": int,
            "height": int,
            "objects": [
                {"type": "agent" | "enemyAgent" | "food", "id": int, "position": {"x": float, "y": float}}
            ]
        },
        "agent_state": {
            "agent_id": int,
            "health": int,
            "exhaustion": int,
            "status": str,
            "position": {"x": float, "y": float}
        }
    }
    
    Respond with a JSON object in the following format:
    {
        "exhaustion": int,
        "next_action": "FoodGatherAgent" | "RestBehavior" | "AvoidEnemy" | "Explore"
    }
"""

model = OpenAIModel('gpt-4o-mini', api_key=OPENAI_API_KEY)
settings = ModelSettings(temperature=0)

survival_agent = Agent(
    model=model,
    system_prompt=sys_prompt,
    result_type=AgentResponse  # Still use AgentResponse for output validation
)

# Create FastAPI app
app = FastAPI()

# Define the FastAPI endpoint
@app.post("/nlp")
async def process_input(request: Request):
    try:
        # Get the raw input data from the client
        input_data = await request.json()

        if not input_data:
            raise HTTPException(status_code=400, detail="input_data is required")
        
        # Convert input_data to a JSON string
        input_json_str = json.dumps(input_data)
        
        # Pass the JSON string to the agent
        result = await survial_agent.run(input_json_str)
        return result.data

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e

# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run("unity:app", host="localhost", port=12345, reload=True)