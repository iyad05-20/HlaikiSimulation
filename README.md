# 🕌 Jamaa El Fna Simulator

**Jamaa El Fna Simulator** is an immersive social simulation game developed in Unity 6. Set in the heart of Marrakesh’s legendary square, the game leverages cutting-edge Large Language Models (LLMs) to create a living, breathing world where every interaction matters.

---

## 🌟 Core Concepts

### 1. The Living Square (Al Halqa)
The game isn't just about walking; it's about being part of the cultural tapestry of Jamaa El Fna. Players interact with a diverse cast of characters, from storytellers (Hlaiki) to merchants, each with their own history, mood, and secrets.

### 2. Social Simulation (LLM-Driven)
Unlike traditional RPGs with fixed dialogue trees, interactions are powered by **Generative AI**.
*   **Dynamic Conversations:** NPCs respond to free-form text input from the player.
*   **Contextual Memory:** NPCs remember previous interactions, allowing for long-term relationship building.
*   **Emotional Intelligence:** Characters exhibit shifting emotions (Open, Occupied, Closed) based on how they are treated.

### 3. Cultural Reputation
The square has "eyes and ears." Your actions and words ripple through the community:
*   **Global Reputation:** A shared score that reflects how the entire square perceives you.
*   **Favorability:** A personal relationship score with individual NPCs.
*   **Keyword Influence:** Using cultural terms or being respectful increases your standing, while rudeness can shut doors.

---

## 🛠️ Key Functionalities

### 🚶 Advanced Player Controller
*   **Camera-Relative Movement:** Fluid navigation using the Unity New Input System.
*   **Dynamic Locomotion:** Support for walking, running, and jumping with physics-based gravity scaling for a responsive feel.

### 🗣️ Intelligent NPC Logic (`LLMNpcLogic`)
*   **Persona Integration:** Each character is defined by a JSON persona file containing their backstory, cultural vocabulary, and emotional triggers.
*   **The "Fused Prompt" System:** A sophisticated backend that asks the LLM to provide both a **Dialogue Response** and a **Social Score** (JSON) in a single request for maximum performance.
*   **Emotional Overlays:** NPCs change their dialogue style and (planned) physical animations based on their internal emotional state.

### 📜 Narrative Fragments
*   **Story Collection:** By gaining a character's trust, players unlock "Story Fragments"—hidden pieces of lore and local secrets.
*   **Codex System:** (In progress) A central journal to track discovered secrets and relationship progress.

### 💾 Persistence & Session Management
*   **Session Summaries:** After every conversation, the AI generates a factual summary of the encounter.
*   **Cross-Session Memory:** These summaries are saved and injected into future conversations, ensuring NPCs "remember" you.

---

## 🚀 Technical Stack

*   **Engine:** Unity 6 (6000.3.9f1)
*   **Render Pipeline:** Universal Render Pipeline (URP)
*   **AI Backend:** Groq API (Llama 3 / Mixtral)
*   **Input:** Unity Input System v1.18.0
*   **UI System:** Hybrid Screen-Space & World-Space (TextMeshPro)
*   **Architecture:** Singleton-based Manager pattern (GameManager, NPCManager, SessionManager).

---

## 📂 Project Structure

*   `Assets/Scripts/Npc/LLM/`: Core AI logic and persona data structures.
*   `Assets/Scripts/Ui/`: Dialogue panels and interaction feedback.
*   `Assets/StreamingAssets/personas/`: JSON definitions for every character in the square.
*   `Assets/Scenes/`: The master scene `JamaaLFnaSimulator.unity`.

---

*“Welcome to the Square. Listen closely, speak wisely.”*
