# Generative_Agents
A project for simulating agents using llms and unity.

## Downloading & Running the Simulation (Major Release)

If you want to run the pre-built simulation without setting up the training environment, follow these steps:

1.  **Go to the Releases Page:**
    * Click this link to access the simulation download: [Generative Agents - Major Release](https://github.com/ViniciusDugue/Generative_Agents/releases/tag/Major_Release)

2.  **Download the Simulation:**
    * On the release page, look for the file named `Version.1.0.zip` under the "Assets" section.
    * Click on `Version.1.0.zip` to download it to your computer.

3.  **Unzip the File:**
    * Once the download is complete, locate the `Version.1.0.zip` file (usually in your Downloads folder).
    * Right-click on the file and select an option like "Extract All...", "Unzip...", or use your preferred unzipping software to extract the contents into a new folder.

4.  **Run the Simulation:**
    * Open the new folder that was created after unzipping.
    * To start the application correctly, you **must double-click and run the `Launch_Sim.bat` file.**

5.  **Enjoy the Simulation!**

    *Note: Your operating system (like Windows) might show a security warning when running `.bat` files or executables downloaded from the internet. You may need to grant permission or click "More info" -> "Run anyway" to proceed.*

---

# Training Steps
1. activate your virtual environment
2. run this line in the command prompt to setup the mlagents training: `mlagents-learn --force --run id=test1`
3. Go to the agents inspector in unity and in the behavior parameters script set the Behavior Type to default
4. In behavior parameters in the inspector, set the Model to None
5. To start training, press play in the unity editor

# Changing Config file for Training
1. Go to results/id=test1/ folder and find the configuration.yaml file
2. Open configuration.yaml file and edit it
3. Link for file parameters: https://unity-technologies.github.io/ml-agents/Training-Configuration-File/

# Tracking Progress
1. Track the epoch progress in the command prompt in realtime
2. Run this in the command prompt to track loss and progress in more detail with tensorboard: `tensorboard --logdir results/id=Test1`

# Inference
1. Once the model is trained, the models weights will be in the results/id=test1/ folder and it will be an .onnx file
2. Drag that file into the unity project scope
3. Go to the agents inspector in unity and in the behavior parameters script set the Model to your .onnx file
4. Go to the agents inspector in unity and in the behavior parameters script set the Behavior Type to inference
5. To start inference, press play

# Increasing Environment Count
1. Change config file parameter num_envs

# Training Multi-agents
1. same as Training Steps but run this instead: `mlagents-learn --force --run id=test2`
