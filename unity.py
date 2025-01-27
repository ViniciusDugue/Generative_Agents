import os
from dotenv import load_dotenv
import json
import openai
from pydantic import BaseModel, Field
import socket

# Load environment variables from .env file
load_dotenv()

# Access the environment variables
base_url = os.getenv("BASE_URL")
api_key = os.getenv("API_KEY")

# Create client
client = openai.OpenAI(
    base_url=base_url,
    api_key=api_key,
)

# Call the LLM with the JSON schema
def NLP(input_string: str):
    
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
    
def start_server(host='localhost', port=12345):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((host, port))
    server_socket.listen(1)
    
    print(f"Server listening on {host}:{port}")
    
    while True:
        print("Waiting for a connection...")
        client_socket, client_address = server_socket.accept()
        print(f"Connected to {client_address}")
        
        try:
            while True:
                # Wait for a request from the client
                data = client_socket.recv(1024)
                if not data:
                    print("No data received from client, closing connection")
                    break

                received = data.decode('utf-8')
                print(f"Received: {received}")

                if received.lower() == 'close':
                    print("Client requested to close the connection")
                    break

                # If the client sends a request (any message other than 'close'),
                # respond with the created_user data
                generated_msg = NLP(received)
                client_socket.send(generated_msg.encode('utf-8'))
                print(f"Sent: {generated_msg}")
                
        except Exception as e:
            print(f"Error: {e}")
        finally:
            client_socket.close()
            print(f"Connection with {client_address} closed")

 
if __name__ == "__main__":
    start_server()
 