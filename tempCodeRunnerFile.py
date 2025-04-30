resp = openai.ChatCompletion.create(
    model="gpt-3.5-turbo",
    messages=[{"role":"system","content":"Say hello"}],
)
print(resp.choices[0].message.content)