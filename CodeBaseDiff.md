# 🔍 Analyse des Différences de Code : Structure Actuelle vs Logique du Coéquipier

Ce document détaille les différences fondamentales entre notre structure de code existante (actuellement dans `Assets/Scripts/`) et la nouvelle logique implémentée par notre coéquipier (dans `HlaikiSim_data/logic organisation/`).

---

## 🗺️ Comparaison Architecturale Globale

### 1. Structure Actuelle (`Assets/Scripts/`) — Approche Décentralisée & Locale
Dans notre architecture actuelle, chaque NPC gère de manière autonome sa propre interaction avec l'IA et son propre état relationnel :
*   **Comportement Localisé** : Le script `LLMNpcLogic.cs` (attaché à chaque NPC) contient à la fois la gestion des conversations, la construction des prompts, l'appel réseau à `GroqApiClient`, la gestion des conditions locales, et l'affichage direct dans l'UI.
*   **Dual-Call LLM (Double Appel)** : Pour chaque message du joueur, nous effectuons **deux appels réseau séquentiels** :
    1.  Un premier appel pour évaluer l'entrée du joueur (Scoring JSON).
    2.  Un second appel pour générer la réponse textuelle de l'NPC en utilisant le résultat du scoring.
*   **Sauvegardes simples** : `SessionManager.cs` charge et sauvegarde directement les données de session locales sans synchronisation globale.

### 2. Logique du Coéquipier (`HlaikiSim_data/logic organisation/`) — Approche Centralisée & Globale
La logique du coéquipier réorganise le système sous forme de **Singletons Globaux** et de modules à responsabilité unique (Single Responsibility Principle) :
*   **Centralisation du LLM** : Un unique `DialogueManager.cs` global gère l'ensemble des discussions pour tous les NPCs. Il centralise l'historique de chat, la construction des prompts et les appels Groq.
*   **Fused Prompt (Appel Unique Fusionné)** : Au lieu de deux appels réseau, il fusionne le dialogue et le scoring dans **un seul appel Groq**. Il force l'IA à renvoyer un format strict :
    ```text
    RESPONSE: [Texte du NPC]
    SCORING: {"favorability_delta": X, "emotion_shift": "...", ...}
    ```
    Cela divise par deux la latence réseau et économise les tokens d'API.
*   **Découplage de l'État (State Pattern)** : Les données dynamiques de relation sont extraites de la logique comportementale et encapsulées dans des objets purs `NPCState.cs`, gérés de manière centralisée par un `NPCManager.cs`.
*   **Système Événementiel et Contexte** : `EventTracker.cs` permet d'associer des actions physiques 3D (ex: s'asseoir en silence) à des modifications d'état relationnel sans surcharger la boucle de discussion principale.

---

## 📊 Tableau Comparatif Détaillé des Fichiers

| Fichier / Module | Structure Actuelle (`Assets/Scripts/`) | Implémentation du Coéquipier (`HlaikiSim_data/`) | Impact & Changement Majeur |
| :--- | :--- | :--- | :--- |
| **Gestion du LLM & Dialogues** | **Décentralisée** (`LLMNpcLogic.cs` sur chaque NPC)<br>- Gère le cycle de vie local.<br>- Appelle `GroqApiClient` en local.<br>- Fait 2 appels (Scoring puis Dialogue). | **Centralisée** (`DialogueManager.cs` global)<br>- Un seul Singleton centralise les appels.<br>- Fait 1 seul appel fusionné (*Fused Prompt*).<br>- Gère les résumés de fin de session par LLM. | **Majeur** : Élimination de la redondance. Réduction de 50% du temps de chargement des réponses et de la consommation de jetons (tokens). |
| **État du NPC (Runtime)** | **Intégré localement** (`LLMNpcLogic.cs`) :<br>- Variables locales `favorability` et `currentEmotion` dans le script MonoBehaviour de l'entité. | **Séparé et Centralisé** (`NPCState.cs` + `NPCManager.cs`) :<br>- `NPCState` est une classe C# pure représentant l'état.<br>- `NPCManager` stocke et gère les états de tous les NPCs. | **Majeur** : Permet à d'autres scripts d'interroger la favorabilité d'un NPC sans que celui-ci soit chargé ou présent dans la scène. |
| **Sauvegarde & Sessions** | **`SessionManager.cs` classique** :<br>- Sauvegarde les données de base de l'NPC de manière isolée. | **`SessionManager.cs` Scoped** :<br>- Intégré au namespace `JemaaGame.NPC`.<br>- Gère la sérialisation des résumés d'interactions dynamiques et les états de curiosité. | **Mineur** : Amélioration de la persistance avec le namespace propre et la gestion des résumés générés par LLM en fin d'échange. |
| **Gestion de la Réputation** | **Partiellement Globale** (`GameManager.GlobalReputation`) :<br>- Notre GameManager suit la réputation globale mais sans lien dynamique direct avec les états individuels des NPCs. | **Globale & Centralisée** (`GameState.cs`) :<br>- Calcule automatiquement la réputation globale en sommant les réputations de chaque `NPCState`.<br>- Gère l'obtention des fragments d'histoire. | **Majeur** : Cohérence de progression. La réputation globale découle directement de nos actions avec chaque membre de la place. |
| **Triggers Extérieurs (3D)** | **Inexistant ou Codé en Dur** :<br>- Pas de structure unifiée pour lier les actions physiques du joueur (stand, méditation) aux scores des NPCs. | **Événementiel flexible** (`EventTracker.cs`) :<br>- Table d'événements associant des chaînes (`"player_sat_in_silence"`) à des deltas numériques de réputation et d'amitié. | **Majeur** : Permet au Level Designer de lier des actions 3D au système d'amitié sans toucher au code des NPCs. |

---

## 🛠️ Analyse des Gaps & Incohérences à Résoudre

1.  **Conflits de Noms de Fichiers Personas** :
    *   *Actuel* : Les fichiers JSON dans `Assets/StreamingAssets/personas/` sont nommés `driss.json` et `zahra.json`.
    *   *Coéquipier* : Les scripts (`DialogueManager` et `LLMNpcLogic`) font un chargement dynamique via :
        `Path.Combine(..., $"{npcId}_game.json")`
    *   *Résolution* : Renommer `driss.json` en `driss_game.json` et `zahra.json` en `zahra_game.json`.

2.  **Duplication de `GroqApiClient.cs`** :
    *   *Actuel* : Utilise un Singleton `GroqApiClient.Instance`.
    *   *Coéquipier* : Supprime le Singleton et utilise un `RequireComponent(typeof(GroqApiClient))` local pour chaque NPC, rendant l'API client dépendante de l'entité locale.
    *   *Résolution* : Garder la version unifiée globale de `GroqApiClient` ou centraliser son appel au niveau du `DialogueManager` global pour éviter de multiplier les composants réseau inutiles dans la scène.

3.  **Refactoring de `LLMNpcLogic.cs` (Le Pont Local)** :
    *   *Actuel* : C'est le cerveau local qui discute avec le joueur.
    *   *Coéquipier* : Dans sa nouvelle logique, il a réécrit `LLMNpcLogic.cs` pour faire les doubles appels localement, mais a aussi créé un `DialogueManager.cs` global. C'est une **duplication de logique** ! Si `DialogueManager` est global, `LLMNpcLogic` local ne devrait pas refaire de requêtes réseau directes, mais simplement déléguer au `DialogueManager` global. Nous devons clarifier cela pour notre coéquipier afin de simplifier son intégration.
