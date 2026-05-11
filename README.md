<h1 align="center">Generative Agents</h1>

<p align="center">
  <em>Multi-agent survival simulation in Unity where every agent runs its own LLM.</em>
</p>

<p align="center">
  <img alt="Award" src="https://img.shields.io/badge/%E2%98%85_Best_Use_of_AI-CSULB_Senior_Expo-e0af68?style=flat-square&labelColor=11141b">
  <img alt="Engine" src="https://img.shields.io/badge/Unity-C%23-7aa2f7?style=flat-square&labelColor=11141b">
  <img alt="ML" src="https://img.shields.io/badge/ML--Agents-ONNX-f7768e?style=flat-square&labelColor=11141b">
</p>

<p align="center">
  <a href="https://viniciusdugue.dev/projects/generative-agents/index.html">
    <img src="https://viniciusdugue.dev/projects/generative-agents/poster.jpg" alt="Generative Agents poster" width="720">
  </a>
</p>

<p align="center">
  <a href="https://viniciusdugue.dev/projects/generative-agents/index.html"><b>→ Project page on viniciusdugue.dev</b></a>
</p>

---

## About

**Generative Agents** (a.k.a. Dynamic Digital Agents) is a multi-agent survival simulation built in Unity. Every agent observes the environment through image snapshots and contextual data, then queries its own large language model to make informed long-term decisions about how to survive.

Agents balance hunger, threats, and exploration in a stylized open world — but instead of hand-tuned heuristics, their behavior emerges from prompting an LLM with what they "see." On top of that, ML-Agents handles a reinforcement-learning pass so agents can learn faster by combining language-model priors with reward signals.

Built as a CSULB senior capstone with **Carla Zuccarini** and **Nathaniel Fedida**. The project was awarded **Best Use of AI** at the CSULB Senior Project Expo.

## How it works

- **Vision-grounded prompting.** Each agent renders a snapshot of its surroundings each tick, packs it into a structured prompt (alongside hunger, position, recent actions), and asks its LLM what to do next.
- **Behavior priors from the LLM.** Outputs are parsed into discrete actions (move, eat, build, attack, flee). The LLM's commonsense gives the agent a head start on long-horizon plans no scripted policy would invent.
- **RL fine-tuning via ML-Agents.** A reinforcement loop trains an ONNX policy in parallel with the LLM, so the system can fall back to a fast inference model when latency matters.
- **Configurable environments.** Number of agents, food density, hostile creatures, day/night cycle — all driven from `configuration.yaml` and TensorBoard-monitored during training.

## Stack

`Unity` · `C#` · `ML-Agents` · `Python` · `ONNX`

---

## Run the simulation

Pre-built Windows release — no setup required.

1. **Download** [`Version.1.0.zip`](https://github.com/ViniciusDugue/Generative_Agents/releases/tag/Major_Release) from the Releases page.
2. **Unzip** it anywhere.
3. **Run `Launch_Sim.bat`** — Windows may show a security prompt; click *More info → Run anyway*.

## Training your own model

```bash
# 1. Activate your virtual environment, then start ML-Agents:
mlagents-learn --force --run-id=test1
```

Then in the Unity editor:

1. Select an agent → **Behavior Parameters** script.
2. Set **Behavior Type** to `Default` and **Model** to `None`.
3. Press Play to start training.

**Editing the config:** open `results/id=test1/configuration.yaml`. Reference: [ML-Agents training-configuration docs](https://unity-technologies.github.io/ml-agents/Training-Configuration-File/).

**Monitoring progress:**
```bash
tensorboard --logdir results/id=test1
```

**More agents per env:** set `num_envs` in the config file.

**Multi-agent training:** use a different run-id (e.g. `mlagents-learn --force --run-id=test2`).

## Inference

Once a model is trained, the weights live in `results/id=test1/` as a `.onnx` file.

1. Drag the `.onnx` file into the Unity project.
2. On an agent's **Behavior Parameters**, set **Model** to that `.onnx` and **Behavior Type** to `Inference Only`.
3. Press Play.

---

## Team

- [Vinicius Dugue](https://github.com/ViniciusDugue)
- Carla Zuccarini
- Nathaniel Fedida

## Recognition

★ **Best Use of AI** — CSULB Senior Project Expo

---

<p align="center">
  <sub>
    More work at <a href="https://viniciusdugue.dev">viniciusdugue.dev</a>
    · <a href="https://www.linkedin.com/in/viniciusdugue/">LinkedIn</a>
  </sub>
</p>
