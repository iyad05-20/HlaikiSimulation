# 🔍 État Actuel du Projet : Jamaa El Fna Simulator

Ce document résume l'état technique, fonctionnel et architectural actuel du projet Unity **HlaikiPolished** au moment présent, afin de servir de référence pour l'équipe de développement.

---

## 🏗️ 1. Environnement & Stack Technique

*   **Moteur** : Unity 3D (2022.3+ / 2023.x).
*   **Pipeline de Rendu** : **URP (Universal Render Pipeline)** v17.3 (activé dans `Packages/manifest.json`).
*   **Contrôles & Entrées** : Nouveau système Unity **Input System** v1.18.0 (configuré via `GameInputManager.cs`).
*   **Cinématique & Caméra** : Cinemachine v2.10 (contrôle des mouvements fluides de caméra orbitale via `PlayerLogic.cs` et `Camera/`).
*   **Système d'UI** : Unity UI classique (Canvas, Images, Boutons) couplé à **TextMeshPro** pour des polices haute définition.

---

## 🎮 2. Systèmes Fonctionnels Actifs dans le Code (`Assets/Scripts/`)

### 🚶 A. Déplacement & Interaction du Joueur (`PlayerLogic.cs`)
*   Le joueur peut se déplacer librement dans la place 3D.
*   Le script détecte la proximité physique avec les NPCs via des Triggers ou des calculs de distance, affichant une bulle de dialogue UI ou un bouton d'interaction.

### 🗣️ B. Système d'IA & Dialogue LLM (`Assets/Scripts/Npc/LLM/`)
*   **Réseau centralisé (`GroqApiClient.cs`)** : Un Singleton propre configuré pour envoyer des requêtes HTTP POST asynchrones (coroutines) à l'API de Groq (modèle Llama ou Mixtral). La clé API est déportée dans l'inspecteur pour éviter toute fuite de secret.
*   **Comportement NPC (`LLMNpcLogic.cs`)** :
    *   Hérite de `NpcLogic.cs` (base commune d'interaction).
    *   Charge les fichiers de configuration de personnages (*Personas*) au format JSON stockés dans `Assets/StreamingAssets/personas/` (`driss.json`, `zahra.json`).
    *   Gère l'historique local de discussion de l'entité.
    *   Effectue actuellement un double appel LLM séquentiel (Scoring des relations ➡️ Génération de la réplique).

### 🖥️ C. Interface Utilisateur (`Assets/Scripts/Ui/`)
*   **`DialoguePanel.cs`** : Gère l'affichage en écran partagé du portrait du NPC, de son nom/rôle, et applique un **effet de machine à écrire (Typewriter)** pour faire défiler le texte de dialogue.
*   **`InputHandler.cs`** : Gère la saisie de texte du joueur par clavier pour soumettre ses questions au LLM et désactive les mouvements du joueur en cours de discussion.

---

## 🐙 3. État Git & Collaboration

*   **Branche Active** : **`NPCllm`** (synchronisée avec le dépôt distant `origin/NPCllm` sur GitHub).
*   **Protection des Secrets** : Historique Git nettoyé en profondeur et validé par *GitHub Push Protection*. Aucune clé API n'est présente dans les commits.
*   **Documentation Embarquée** (comprise dans le dernier commit de la branche) :
    *   [CodeBaseDiff.md](file:///c:/Users/lenovo/HlaikiPolished/CodeBaseDiff.md) : Analyse comparative des deux architectures (Locale vs Centralisée du coéquipier).
    *   [TeammateSimplifiedIntegrationGuide.md](file:///c:/Users/lenovo/HlaikiPolished/TeammateSimplifiedIntegrationGuide.md) : Guide pas-à-pas pour aider le coéquipier à câbler sa logique propre sans dégrader le projet.

---

## ⚠️ 4. Le Grand Défi Architectural Actuel (Le "Gap")

Le projet se trouve actuellement à la frontière de deux architectures :
1.  **L'existante locale (dans `Assets/Scripts/`)** : Le script `LLMNpcLogic.cs` de chaque NPC effectue ses propres appels réseau et stocke ses variables de favorabilité localement.
2.  **La nouvelle logique centrale (dans `HlaikiSim_data/`)** : Ton coéquipier a rédigé d'excellents modules (des Singletons comme `DialogueManager.cs`, `NPCManager.cs`, `GameState.cs` et `EventTracker.cs`) fonctionnant de manière globale, avec un **Fused Prompt** (1 seul appel API au lieu de 2 pour scoring + réponse). 
    *   *Statut* : Cette logique est prête en local dans son dossier d'organisation, mais **non encore intégrée** dans les dossiers Unity officiels ni reliée aux scènes.
