from dotenv import load_dotenv
import openai
from fastapi import FastAPI, HTTPException, Request
import uvicorn
from enum import Enum
from typing import Union
from pydantic import BaseModel, Field, ConfigDict
from pydantic_ai import Agent, BinaryContent, RunContext, UnexpectedModelBehavior
# from pydantic_ai import Agent, BinaryContent, RunContext
from dataclasses import asdict
from pydantic_ai.settings import ModelSettings
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.messages import ModelMessagesTypeAdapter
from pydantic_ai.providers.openai import OpenAIProvider
from AgentClasses import AgentResponse  # Keep AgentResponse for output
from AgentManager import AgentManager  # Import the AgentManager class
from pydantic_core import to_jsonable_python, to_json
import base64
import os
import json
import logging
import base64
# Dictionary to store map data for each agent


# Load environment variables from .env file
load_dotenv()
API_KEY = os.getenv('OPEN_API_KEY')

sys_prompt = """
    You are an intelligent survival agent in a hostile environment. Your primary goal is to make strategic decisions 
    that maximize your long-term survival and fitness. Your choices must balance resource acquisition, hunger, safety, and movement. 
    Hostile predators roam the area, and fleeing them is always a top priority to maintain your health.

Environment & Map:
- The arena is a 120x120 unit area.  
- Map coordinates: Top-Right is (0, 0) and Bottom-Left is (120, 120).  
- A blue square marks your current location.
- Green Dots indicate potential food locations.  
- Red Triangles indicate hostile predators.
- Purple Triagnes indicate other, allied agents.
- White areas are obstacles but can be navigated around.
- A yellow cube represents your habitat (base) where you can deposit food, eat, and heal.

Health & Enemy System:
- Health ranges from 0 to 100. If health reaches 0, you die.
- If your health drops below 50. It is recommended you rest at the habitat to avoid fatal damage. Override this concern if you are at risk of starvation.  
- Enemies deal damage by moving towards your location and attacking.
- If Enemies are detected on the map, they should be avoided unless doing so would result in death from starvation.
- Pests will spawn at night and will steal food from your habitat.
- If a pest is detected at your habitat, you should use the GuardBehavior to prevent food loss.
- isGuarded is a boolean that indicates whether your habitat is currently guarded by another agent. Guarding should only be done at night.

Food & Resourcesd System:
- Food items spawn only at designated 'Active' food locations; not every food location will have food.
- Food appearance is random each day, so 'Active' food locations will switch to different food locations daily.
- Food only spawns during the day. At night, no food will be available.
- If currentFood >= maxFood, no more food can be collected until some is deposited at the habitat or eaten.
- Food must be deposited at your habitat to be stored and used later.
- Food can only be collected via the GatherBehavior Action; this should be used whenever there is food nearby.
- Agents can eat the food in their inventory, or store it at their habitat. Storing it at the habitat will increase your fitness score.
- Daily survival requires at least 5 food items. 
- For every 100 points of exhaustion, one extra food item is needed; for every 20 points of health lost, one extra food item is required to heal.
- Active Food locations can run out of food. Check the map to see if there are any food items at the location. If they aren't, search elsewhere.
  
Available Actions & Effects:
- **GatherBehavior:**  
  *Effect:* Searches for and collects food items at the current location.  
  *Cost:* +0.8 exhaustion per second.  
  *Purpose:* Boosts fitness by increasing food collected. SHOULD NOT USE WHEN CURRENT FOOD IS EQUAL TO MAX FOOD.

- **RestBehavior:**  
  *Effect:* Rests to restore energy.  
  *Cost:* -2 exhaustion per second.  
  *Purpose:* Prevents exhaustion from reaching dangerous levels. Resting should be done at night unless guarding.

- **FleeBehavior:**  
  *Effect:* Flees from detected enemies by moving away.  
  *Cost:* +2 exhaustion per second.  
  *Purpose:* Ensures survival by avoiding hostile predators.

- **MoveBehavior:**  
  *Effect:* Moves the agent to a specified location. LOCATION MUST BE SPECIFIED.  
  *Cost:* +0.5 exhaustion per second.  
  *Purpose:* Allows relocation to food sources or strategic positions.

- **GuardBehavior:**  
  *Effect:* Guards the habitat and kills any pests. 
  *Cost:* +0.8 exhaustion per second.  
  *Purpose:* Prevents fitness score from decreasing by losing food.

Survival Considerations:
- Fleeing is used only when an enemy is detected.
- Food only exists at known food locations; gathering food requires exploration.
- MoveBehavior should be used only if there is a known target location.
- If exhaustion reaches 100, you will gain additional hunger.
- The agent’s actions should always aim to maximize long-term survival while increasing a calculated fitness score.

Fitness Score Calculation:
Fitness is computed from several factors with weighted coefficients:
  - **Current_Food:** Number of food items stored at your habitat.
  - **Food_Collected:** Total food collected by you.
  - **Food_Deposited:** Food deposited at the habitat.
  - **Health Loss:** (Max_Health - Current_Health); higher loss reduces fitness.
  - **Food_Lost:** Food stolen from the habitat that day.

 Fitness Score Evaluation:
 The Fitness score is a weighted sum reflecting your items above. The weights are designed so that:

- **Below 0:** You are in critical condition and likely to perish soon.
- **Around 50:** You should be able to survive the day.
- **Around 100:** You have enough food and resources to last about two days.
- **150+:** You are thriving.
  
Agent Inputs (provided every 10 seconds):
  - **agentID:** int – Unique identifier for you.
  - **currentAction:** string – The name of your current behavior (e.g., GatherBehavior, FleeBehavior, etc.).
  - **currentPosition:** { x: float, z: float } – Your current position in the environment.
  - **currentHunger**: int — Represent how full the agent with the number of food items the agent has eaten today. (Eating 5 items means the agent is fully satisfied.)
  - **maxFood:** int – The maximum number of food items you can carry (currently 3).
  - **currentFood:** int – The number of food items you are currently holding.
  - **habitatStoredFood:** int – The number of food items stored at your habitat.
  - **fitness:** float – A pre-calculated overall survival metric summarizing your current state.
  - **health:** int – Your current health (0 to 100).
  - **enemyCurrentlyDetected:** bool – True if an enemy is in sight.
  - **exhaustion:** int – Your current exhaustion level.
  - **isDayTime:** bool – True if it is daytime.
  - **isGuarded:** bool - True if the habitat is guarded
  - **habitatLocation:** { x: float, z: float } – The location of your habitat.
  - **activeFoodLocations:** list of { x: float, z: float } – Locations of spawn points that are currently active and have food.
  - **foodLocations:** list of { x: float, z: float } – Known food locations in the environment.

Fitness Score Overview:
  - This score is a weighted sum of your stored food, collected food, deposited food, health loss, food stolen, and the accessibility of your base and food locations to enemies.
  - A higher fitness score indicates better overall survival prospects.
  - Use this score to help determine whether you should prioritize gathering food, resting, fleeing, or moving to a new location.


"If your fitness score is low, prioritize actions that boost your survival (e.g., GatherBehavior or RestBehavior). If it is high, you may risk exploring new areas using MoveBehavior, while always ensuring you flee from predators if detected."
Respond with the chosen ACTION (and location if using MoveBehavior) along with any necessary brief rationale.

### EXAMPLE 1
<user>
{
  "agentID": 1,
  "currentAction": "GatherBehavior",
  "currentPosition": { "x": 98.00892, "z": 92.4902039 },
  "currentHunger": 0,
  "maxFood": 3,
  "currentFood": 3,
  "habitatStoredFood": 0,
  "fitness": 0.0,
  "health": 100,
  "enemyCurrentlyDetected": false,
  "exhaustion": 27.7000141,
  "isDayTime": true,
  "isGuarded": false,
  "habitatLocation": { "x": 65.6, "z": 111.9 },
  "activeFoodLocations": [ { "x": 98.1, "z": 92.6 } ],
  "foodLocations":   [ { "x": 98.1, "z": 92.6 } ],
}
</user>

<assistant>
{
    "reasoning": "The agent is at a food location and has reached its max food capacity, so it should deposit its current food. It is still day so no pest are present and habitat does not need to be guarded.",
    "eatCurrentFoodSupply": true,
    "next_action": "MoveBehavior",
    "location": { "x": 65.6, "z": 111.9 }
}
</assistant>


### EXAMPLE 2
<user>
{
  "agentID": 1,
  "currentAction": "GatherBehavior",
  "currentPosition": { "x": 70.00892, "z": 81.4902039 },
  "currentHunger": 0,
  "maxFood": 3,
  "currentFood": 0,
  "habitatStoredFood": 0,
  "fitness": 0.0,
  "health": 100,
  "enemyCurrentlyDetected": false,
  "exhaustion": 50,
  "isDayTime": true,
  "isGuarded": false,
  "habitatLocation": { "x": 65.6, "z": 111.9 },
  "activeFoodLocations": [],
  "foodLocations":   [],
}
</user>

<assistant>
{
    "reasoning": "The agent is low on hunger and has no known food locations. As such, it should go out searching using GatherBehavior. It is still day so no pest are present and habitat does not need to be guarded.",
    "eatCurrentFoodSupply": false,
    "next_action": "GatherBehavior",
}
</assistant>

### EXAMPLE 3
<user>
{
  "agentID": 1,
  "currentAction": "RestBehavior",
  "currentPosition": { "x": 65.6, "z": 111.9 },
  "currentHunger": 5,
  "maxFood": 3,
  "currentFood": 0,
  "habitatStoredFood": 12,
  "fitness": 130.0,
  "health": 100,
  "enemyCurrentlyDetected": false,
  "exhaustion": 50,
  "isDayTime": false,
  "isGuarded": true,
  "habitatLocation": { "x": 65.6, "z": 111.9 },
  "activeFoodLocations": [],
  "foodLocations":   [],
}
</user>

<assistant>
{
    "reasoning": "The agent is at its habitat and is being guarded. It should rest to regain energy.",
    "eatCurrentFoodSupply": false,
    "next_action": "RestBehavior",
}
</assistant>

"""

model = OpenAIModel('openai/gpt-4.1-mini',
    provider=OpenAIProvider(
        base_url='https://openrouter.ai/api/v1',
        api_key=API_KEY,
    ),
)
settings = ModelSettings(temperature=0.0, 
                         max_tokens=8192)


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
    manager = AgentManager()
    try:
        input_data = await request.json()
        if not input_data:
            raise HTTPException(status_code=400, detail="input_data is required")
        
        # Validate agentID exists and is valid
        agent_id = input_data.get("agentID")
        if agent_id is None:
            raise HTTPException(status_code=400, detail="agentID is required")
        try:
            agent_id = int(agent_id)  # Ensure it's an integer
        except (ValueError, TypeError):
            raise HTTPException(status_code=400, detail="agentID must be an integer")
        
        map_data = None
        result = None

        # Register the agent if not already registered
        manager.register_entity(agent_id, sys_prompt, model)

        # Print all input data except mapData
        for key, value in input_data.items():
            if key != "mapData" and value is not None:
                print(f"{key}: {value}")
        print()
        
        if "mapData" in input_data and input_data["mapData"] is not None:
            map_data = base64.b64decode(input_data.pop("mapData"))

        input_json_str = json.dumps(input_data)
        # Pass Map Data if it exists, otherwise run normally
        if map_data:
            result = await manager.get_agent(agent_id).run(
                [
                    input_json_str,
                    BinaryContent(data=map_data, media_type='image/png'),  
                ],
                model_settings=settings,
                message_history=manager.get_message_history(agent_id)
            )
        else:
            result = await manager.get_agent(agent_id).run(
                [
                    input_json_str,
                ],
                model_settings=settings,
                message_history=manager.get_message_history(agent_id)
            )

         # Process the agent's message history.
        history_step_1 = result.all_messages()
        filtered_history = await filter_message_history(history_step_1)
        as_python_objects = to_jsonable_python(filtered_history)
        restored_history = ModelMessagesTypeAdapter.validate_python(as_python_objects)
        manager.update_message_history(agent_id, restored_history)

        print(result.data)
        return result.data
    except UnexpectedModelBehavior as e:
        logging.error("Unexpected model behavior", exc_info=True)
        logging.error(f"Map Data: {base64.b64encode(map_data)}")
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
    
async def filter_message_history(messages) -> list:
    filtered = []
    for message in messages:
        # Convert the message to a dict using model_dump()
        msg_dict = asdict(message)  # Assuming it's a Pydantic v2 model
        new_parts = []
        # Ensure msg_dict has a "parts" key and that it’s iterable.
        for part in msg_dict.get("parts", []):
            # Handle ToolCallPart separately
            if part.get("part_kind") == "tool-call":
                # Ensure required fields are present
                if not all(k in part for k in ["tool_name", "args"]):
                    # If required fields are missing, skip or replace with default values
                    part = {
                        "tool_name": "unknown",
                        "args": {},
                        "part_kind": "tool-call",
                    }
                new_parts.append(part)
            else:
                # Handle other parts
                if isinstance(part.get("content"), str):
                    new_parts.append(part)
                else:
                    new_parts.append({
                        "content": "[Image attached]",
                        "part_kind": part.get("part_kind")
                    })
        msg_dict["parts"] = new_parts
        filtered.append(msg_dict)
    return filtered


# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run("main:app", host="127.0.0.1", port=12345, reload=True)
