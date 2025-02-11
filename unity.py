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
client = openai.OpenAI(
    api_key=api_key,
)

# Create FastAPI app
app = FastAPI()

class AgentData(BaseModel):
    agent_id: int
    health: int
    status: str
    position: dict

# Call the LLM with the JSON schema
async def NLP(agent_data: AgentData):
    context = (
        "Analyze the agent's state and determine the next best action."
        f" The agent is at position {agent_data.position}, with health {agent_data.health}."
        " Please return a JSON with the 'next_action' field (e.g., 'explore', 'repair', 'return_to_base')."
    )

    response = client.beta.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {
                "role": "system",
                "content": "You are an AI guiding agents in a hostile, survival environment that answers in JSON.",
            },
            {
                "role": "user",
                "content": context,
            },
        ],
        response_format=AgentResponse
    )

    
    if response.choices:
        try:
            action_data = json.loads(response.choices[0].message.content.strip())  # Parse JSON response
            next_action = action_data.get("next_action", "idle")
        except json.JSONDecodeError:
            next_action = "idle"
    else:
        next_action = "idle"

    return {"agent_id": agent_data.agent_id, "next_action": action}

# Define the FastAPI endpoint
@app.post("/nlp")
async def process_input(agent_data: AgentData):
    try:
        message = await NLP(agent_data)
        return message

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e


# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run(app, host="localhost", port=12345)