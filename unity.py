from dotenv import load_dotenv
import openai
from fastapi import FastAPI, HTTPException, Request
import uvicorn
from enum import Enum
from typing import Union
from pydantic import BaseModel, Field, ConfigDict
from pydantic_ai import Agent, BinaryContent, RunContext
# from pydantic_ai import Agent, BinaryContent, RunContext
from pydantic_ai.settings import ModelSettings
from pydantic_ai.models.openai import OpenAIModel
from agent_classes import AgentResponse  # Keep AgentResponse for output
import base64
import os
import json
import logging
import base64
# Dictionary to store map data for each agent


# Load environment variables from .env file
load_dotenv()
OPENAI_API_KEY = os.getenv("OPEN_API_KEY")

sys_prompt = """
    You are an intelligent agent in a survival environment. Your primary goal is to make strategic decisions that maximize 
    your long-term survival and efficiency. Your choices should balance resource acquisition, energy management, 
    and movement across the environment. If exhaustion reaches 100, you will begin losing health and will not be able to move until you rest.
    A map of the environment may optionally be provided to you as an image. You will be queried every 20 seconds with your current status and available actions. 
    You will respond with the action you wish to take.

Map Data:
The map data will be provided as a png image. The Top-Right corner of the map is (0, 0) and the Bottom-Left corner is (120, 120). 
The map is 120x120 units. A Blue dot represents your current location. Green sqaures represent food locations. White areas are considered
as obstacles, but can be traversed around.


Available Actions & Effects
You can take one of the following ACTIONS at a time:

* FoodGathererAgent
    Effect: Searches for and collects food (if available), from the current location.
    Cost: 0.8 exhaustion per second (increases exhaustion).
    Purpose: Increases the agent's fitness score, which is essential for survival. Food gathering should be a priority if no critical exhaustion risk exists.

* RestBehavior
    Effect: The agent rests, restoring energy.
    Cost: -2 exhaustion per second (reduces exhaustion).
    Purpose: Prevents exhaustion from reaching dangerous levels. This action should be taken when exhaustion is high and approaching dangerous thresholds.

* MoveBehaivor
    Effect: Moves the agent to a specified location. LOCATION MUST BE SPECIFIED.
    Cost: 0.5 exhaustion per second (increases exhaustion).
    Purpose: Allows the agent to relocate to food sources or other points of interest. Movement should be planned efficiently to avoid excessive exhaustion.

Survival Considerations
    - Food only spawns at specific food locations in the enviornment.
    - Food locations can be discovered through exploration.
    - MoveBehavior should only be used if there is a known location to travel to.
    - If exhaustion exceeds 100, the agent will begin losing health and may eventually die.
    - The agent should prioritize food gathering if it is sustainable but must rest when exhaustion is critically high.
    - Resting wastes time, which can lead to a reduced fitness. It should only be used when necessary.

Input Parameters:
<input>
    agentId: int, # Unique identifier for the agent
    health: int, # Current health of the agent (0 to 100)
    exhaustion: int, # Current exhaustion level of the agent (0 [completely rested] to 100 [complete exhaustion])
    currentAction: str, # Current action the agent is performing
    currentPosition: {"x": float, "y": float, "z": float}, # Current position of the agent in the environment
    foodLocations: list[{"x": float, "y": float, "z": float}], # Locations of food sources in the environment
    mapData: str, # Base64 encoded image of the map data (optional)
</input>
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

# Dictionary to store map data for each agent
agent_map_data = {}

# Define the FastAPI endpoint
@app.post("/nlp")
async def process_input(request: Request):
    try:
        input_data = await request.json()
        if not input_data:
            raise HTTPException(status_code=400, detail="input_data is required")
        input_json_str = json.dumps(input_data)
        map_data = None
        result = None
            
        for key, value in input_data.items():
            if key != "mapData" and value is not None:
                print(f"{key}: {value}")
            else:
                print(f"{key}: None")
        
        if "mapData" in input_data and input_data["mapData"] is not None:
            map_data = base64.b64decode(input_data.pop("mapData"))

        # Pass Map Data if it exists, otherwise run normally
        if map_data:
            result = await survival_agent.run(
                [
                    input_json_str,
                    BinaryContent(data=map_data, media_type='image/png'),  
                ],
                model_settings=settings
            )
        elif map_data is None:
            result = await survival_agent.run(
                [
                    input_json_str,
                ],
                model_settings=settings
            )
        
        print(result.data)
        return result.data
    except Exception as e:
        logging.error("Error processing /nlp request", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e)) from e
    
@app.post("/map")
async def process_map_with_llm(request: Request):
    try:
        input_data = await request.json()
        for key, value in input_data.items():
            if key != "mapData":
                print(f"{key}: {value}")
        
        if "map_base64" not in input_data or "agent_id" not in input_data:
            raise HTTPException(status_code=400, detail="Map and agent ID are required")
        return {"message": "Received map data", "agent_id": input_data["agent_id"]}
    
    except Exception as e:
        print("Error processing /map:", e)
        raise HTTPException(status_code=500, detail=str(e))







# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run("unity:app", host="127.0.0.1", port=12345, reload=True)