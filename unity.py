from dotenv import load_dotenv
import openai
from fastapi import FastAPI, HTTPException, Request
import uvicorn
from enum import Enum
from typing import Union
from pydantic import BaseModel, Field, ConfigDict
import os


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

# Load environment variables from .env file
load_dotenv()
api_key = os.getenv("OPEN_API_KEY")

# Create OpenAI client
client = openai.OpenAI(
    api_key=api_key,
)

# Create FastAPI app
app = FastAPI()

# Call the LLM with the JSON schema
async def NLP(input_string: str):
    context = (
        "Please answer the following prompt in JSON format with the following fields: "
        "'Agent_ID', 'Health', and 'Next_Action'. "
        "The Agent_ID should be a string representing the agent's ID number. "
        "Health should indicate the current health points of the agent. "
        "Next_Action should describe what the agent is currently doing."
        "Prompt: "
    )

    contextualized_input = context + input_string

    chat_completion = client.beta.chat.completions.parse(
        model="gpt-4o-mini",
        messages=[
            {
                "role": "system",
                "content": "You are an AI agent in a hostile, survival environment that answers in JSON.",
            },
            {
                "role": "user",
                "content": contextualized_input,
            },
        ],
        response_format=AgentResponse
    )

    return chat_completion.choices[0].message

# Define the FastAPI endpoint
@app.post("/nlp/")
async def process_input(request: Request):
    try:
        # Get the raw input data from the client
        input_data = await request.json()
        input_string = input_data.get("input_string")

        if not input_string:
            raise HTTPException(status_code=400, detail="input_string is required")

        # Call the NLP function with the input string
        message = await NLP(input_string)

        # Return the raw JSON response to the client
        # json_data = json.loads(response_json)
        if message.parsed:
            print(message.parsed.next_action)
            return (message.parsed)
        else:
            return(message.refusal)
        #return json_data

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e

# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run("unity:app", host="localhost", port=12345)