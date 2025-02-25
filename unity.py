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
import logging


# Load environment variables from .env file
load_dotenv()
OPENAI_API_KEY = os.getenv("OPEN_API_KEY")

sys_prompt = """
    You are an AI agent in a survival environment. Your primary goal is to make strategic decisions that maximize 
    the agent's long-term survival and efficiency. Your choices should balance resource acquisition, energy management, 
    and movement across the environment.

Available Actions & Effects

Your agent can take one of the following actions at a time:

    FoodGatherAgent
        Effect: Collects nearby food.
        Exhaustion Rate: 4 exhaustion/s
        Purpose: Increases the agent's fitness score, which is essential for survival. Food gathering should be a priority if no critical exhaustion risk exists.

    RestBehavior
        Effect: The agent rests, restoring energy.
        Exhaustion Rate: -10 exhaustion/s (reduces exhaustion).
        Purpose: Prevents exhaustion from reaching dangerous levels. This action should be taken when exhaustion is high and approaching dangerous thresholds.

    MoveTo
        Effect: Moves the agent across the map.
        Exhaustion Rate: 1 exhaustion/s
        Purpose: Allows the agent to relocate to food sources or other points of interest. Movement should be planned efficiently to avoid excessive exhaustion.

Survival Considerations

    If exhaustion exceeds 100, the agent will begin losing health and may eventually die.
    The agent should prioritize food gathering if it is sustainable but must rest when exhaustion is critically high.
    Efficient travel planning is essential to avoid unnecessary exhaustion.
    
    Expected JSON Input:
    {
        "agent_id": <int>,
        "health": <int>,
        "exhaustion": <int>,
        "currentAction": "<string>",
        "currentPosition": {"x": <float>, "y": <float>, "z": <float>},
        "foodLocations": [{"x": <float>, "y": <float>, "z": <float>}]
    }

    
    Respond with a JSON object in the following format:
    {
        "reasoning": str,
        "next_action": "FoodGatherAgent" | "RestBehavior" | "MoveTo" 
    }
"""

'''
"map": {
            "width": int,
            "height": int,
            "objects": [
                {"type": "agent" | "enemyAgent" | "food", "id": int, "position": {"x": float, "y": float}}
            ]
        },
'''


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
        print(input_data)

        if not input_data:
            raise HTTPException(status_code=400, detail="input_data is required")
        
        # Convert input_data to a JSON string
        input_json_str = json.dumps(input_data)
        print(input_json_str)

        # Pass the JSON string to the agent
        result = await survival_agent.run(input_json_str)
        return result.data

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e

# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run("unity:app", host="localhost", port=12345, reload=True)