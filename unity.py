from dotenv import load_dotenv
import openai
from fastapi import FastAPI, HTTPException, Request
import uvicorn
from enum import Enum
from typing import Union
from pydantic import BaseModel, Field, ConfigDict
from agent_classes import AgentResponse
from agent_classes import Actions
import json
import os


# Load environment variables from .env file
load_dotenv()
api_key = os.getenv("OPENAI_API_KEY")

# Create OpenAI client
client = openai.OpenAI(api_key=api_key)


# Create FastAPI app
app = FastAPI()

class AgentData(BaseModel):
    agent_id: int
    health: int
    status: str
    position: dict

# Call the LLM with the JSON schema
async def NLP(agent_data: AgentData):
    
    valid_actions = ["explore", "gather_food", "rest", "return_to_base"]
    
    context = (
        f"Analyze the agent's state and determine the next best action. "
        f"The agent is at position {agent_data.position}, with health {agent_data.health}. "
        f"Possible actions: {', '.join(valid_actions)}. "
        f"ALWAYS return JSON with a 'next_action' field. The action MUST be one of {valid_actions}."
    )

    try:
        response = client.chat.completions.create(  # Corrected API call
            model="gpt-4o-mini",
            messages=[
                {"role": "system", "content": "You are an AI guiding agents in a survival simulation. Always return JSON output."},
                {"role": "user", "content": context},
            ],
            response_format={"type": "json_object"},
        )

        # Debugging print to check OpenAI response
        print(f"🔍 OpenAI raw response: {response}")

        # Extract response
        if response.choices:
            action_data = json.loads(response.choices[0].message.content.strip())
            next_action = action_data.get("next_action", "idle")
            
            if next_action not in valid_actions:
                print(f"Invalid action received: {next_action}. Defaulting to 'explore'.")
                next_action = "explore"
        else:
            next_action = "explore"

    except Exception as e:
        print(f"❌ Error in OpenAI API call: {e}")
        return {"agent_id": agent_data.agent_id, "next_action": "explore"}

    return {"agent_id": agent_data.agent_id, "next_action": next_action}

# Define the FastAPI endpoint
@app.post("/nlp")
async def process_input(agent_data: AgentData):
    try:
        message = await NLP(agent_data)
        return message

    except Exception as e:
        print(f"❌ FastAPI Error: {e}")  # Debugging print
        raise HTTPException(status_code=500, detail=str(e)) from e


# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run(app, host="localhost", port=12345)