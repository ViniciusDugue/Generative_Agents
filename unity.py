import os
from dotenv import load_dotenv
import json
import openai
from fastapi import FastAPI, HTTPException, Request
import uvicorn

# Load environment variables from .env file
load_dotenv()

# Access the environment variables
base_url = os.getenv("BASE_URL")
api_key = os.getenv("API_KEY")

# Create OpenAI client
client = openai.OpenAI(
    base_url=base_url,
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

    chat_completion = client.chat.completions.create(
        model="mistralai/Mistral-7B-Instruct-v0.1",
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
    )

    return chat_completion.choices[0].message.content

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
        response_json = await NLP(input_string)

        # Return the raw JSON response to the client
        return json.loads(response_json)

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# Run the FastAPI app
if __name__ == "__main__":
    uvicorn.run(app, host="localhost", port=12345)