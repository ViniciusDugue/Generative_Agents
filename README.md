# Generative_Agents
A project for simulating agents using llms, reinforcement learning, and unity.


# Training Steps

1. activate your virtual environment
2. run this line in the command prompt to setup the mlagents training: mlagents-learn --force --run id=test1
3. Go to the agents inspector in unity and in the behavior parameters script set the Behavior Type to default 
4. In behavior parameters in the inspector, set the Model to None
5. To start training, press play in the unity editor

# Tracking Progress
1. Track the epoch progress in the command prompt in realtime
2. Run this in the command prompt to track loss and progress in more detail with tensorboard: tensorboard --logdir results/id=Test1

# Inference
1. Once the model is trained, the models weights will be in the results/test1/ folder and it will be an .onnx file
2. Drag that file into the unity project scope
3. Go to the agents inspector in unity and in the behavior parameters script set the Model to your .onnx file
4. Go to the agents inspector in unity and in the behavior parameters script set the Behavior Type to inference
5. To start inference, press play