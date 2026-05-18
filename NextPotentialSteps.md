# 🚀 Prochaines Étapes & Perspectives d'Avancement

Ce document trace la feuille de route (*Roadmap*) des prochaines étapes techniques immédiates et présente les fonctionnalités d'immersion et de gameplay majeures que nous pouvons ajouter au projet **Jamaa El Fna Simulator**.

---

## ⚡ 1. Les Étapes Techniques Immédiates (Court Terme)

Ce sont les chantiers prioritaires pour stabiliser l'architecture et finaliser la structure Git commune avant que le reste de l'équipe ne reprenne le flambeau.

### 🔔 A. Développer le `NotificationManager`
*   **Objectif** : Implémenter notre gestionnaire de notifications optimisé avec **Object Pooling** et **Capped Queue (Max 3)**.
*   **Esthétique** : Utilisation du design **Gilded Codex** (Dégradé marron-bronze `#1B1106` avec opacité Alpha 180, liseré doré fin).
*   **Type `Warning`** : Intégrer les alertes rouges (`#EF4444`) pour signaler les erreurs réseau ou les plantages de scripts.

### 🏢 B. Unifier et Intégrer la Logique du Coéquipier
*   **Migration des Fichiers** : Copier les scripts depuis `HlaikiSim_data/logic organisation/` vers `Assets/Scripts/Npc/` dans le namespace `JemaaGame.NPC`.
*   **Mise en place des Singletons** : Créer le GameObject persistant **`_NPC_Singletons`** dans la scène et lui attacher les gestionnaires globaux (`DialogueManager`, `NPCManager`, `GameState`, `SessionManager`, `EventTracker`).
*   **Refactoring de `LLMNpcLogic.cs`** : Alléger le cerveau physique 3D pour qu'il délègue entièrement la discussion au `DialogueManager.Instance` (passant du double appel lent à l'**appel unique fusionné**).
*   **Renommer les Personas** : Renommer `driss.json` en `driss_game.json` et `zahra.json` en `zahra_game.json` dans le dossier `StreamingAssets/personas/`.

---

## 🌟 2. Nouvelles Fonctionnalités & Perspectives (Moyen/Long Terme)

Ces idées enrichissent l'expérience joueur, améliorent l'immersion narrative et exploitent pleinement la puissance de la simulation sociale basée sur l'IA.

### 📓 A. Le Codex Narratif & Journal d'Histoire (`StoryJournalUI`)
Pour donner un but concret au joueur, nous pouvons concevoir une interface de livre stylisée (un Journal d'enquêteur de la place Jamaa El Fna) :
*   **Fragments Découverts** : Afficher la liste des fragments d'histoire persistants lus depuis le JSON (ex: *"Le secret de la recette secrète de Zahra"*, *"La rumeur sur le tapis de Driss"*).
*   **Progression par Arc** : Suivre l'avancement des secrets découverts (ex: *Mystère de Driss : 2/5 indices trouvés*).
*   **Jauges d'Affinité** : Afficher un aperçu des relations globales avec chaque NPC (Amitié, Confiance, Émotion dominante).

### 📍 B. Intégration Triggers Physiques & Gameplay Spatial (`EventTracker`)
Lier le monde 3D à l'état d'esprit des NPCs sans surcharger le LLM :
*   **Actions Contextuelles** : Si le joueur s'assoit pour boire un thé à la menthe à proximité de Driss, l'`EventTracker` capture l'événement `"player_drinking_tea"` et envoie un bonus d'amitié à Driss car il apprécie que l'on prenne du temps sur la place.
*   **Aumône et Performance** : Donner une pièce à un musicien Gnawa ou écouter un conteur dans sa *Halqa* (cercle) génère des événements qui augmentent la réputation globale du joueur dans `GameState.cs`.

### 🎭 C. Animations Corporelles & Expressions Facialess Dynamiques
Exploiter le retour d'émotions renvoyé par l'appel unique fusionné du LLM (`SCORING: { "emotion_shift": "..." }`) :
*   **Animations 3D** : Associer chaque état émotionnel détecté (*Surprise, Colère, Joie, Méfiance, Tristesse*) à des triggers dans l'Animator Unity de l'avatar 3D du NPC.
*   *Exemple* : Si le LLM renvoie `"emotion_shift": "anger"`, l'NPC croise immédiatement les bras ou fronce les sourcils physiquement dans le jeu en parlant.

### 🌅 D. Cycle Jour/Nuit & Emploi du Temps des NPCs
Rendre la place Jamaa El Fna vivante et dynamique :
*   **Déplacements Physiques** : Les NPCs ne restent pas immobiles. Le matin, Driss range son étal, l'après-midi il discute au café, et le soir il ferme boutique.
*   **Dialogue Contextuel** : Injecter l'heure de la journée (matin, après-midi, soir) dans le système de prompt du `DialogueManager` pour que les répliques s'adaptent naturellement (ex: *"Le soleil commence à se coucher sur la Koutoubia..."*).
