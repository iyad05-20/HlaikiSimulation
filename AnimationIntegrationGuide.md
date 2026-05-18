# Guide d'Intégration des Animations Joueur

Ce document explique comment connecter les animations 3D (Idle/Repos et Walk/Marche) au système de mouvement du joueur.

## 1. Configuration de l'Animator (Unity Editor)

L'**Animator Controller** est la machine à états qui gère les transitions entre vos animations.

### Étape A : Création
1.  Dans ton dossier `Assets/Animations`, fais un clic droit : `Create > Animator Controller`.
2.  Nomme-le **PlayerAnimator**.
3.  Sélectionne ton personnage dans la hiérarchie et glisse ce `PlayerAnimator` dans la case **Controller** de son composant **Animator**.

### Étape B : Les États
1.  Double-clique sur **PlayerAnimator** pour ouvrir la fenêtre Animator.
2.  Glisse ton clip d'animation **Idle** (Repos). Il doit devenir orange (État par défaut).
3.  Glisse ton clip d'animation **Walk** (Marche) juste à côté.

### Étape C : Le Paramètre
1.  Dans la fenêtre Animator, va sur l'onglet **Parameters** (en haut à gauche).
2.  Clique sur le **+** et choisis **Bool**.
3.  Nomme-le exactement : `isWalking`.

### Étape D : Les Transitions
1.  Fais un clic droit sur l'état **Idle** > `Make Transition` > clique sur **Walk**.
    *   Sélectionne la flèche de transition.
    *   Dans **Conditions**, ajoute `isWalking` | `true`.
    *   Décoche **Has Exit Time** pour que la marche commence immédiatement.
2.  Fais un clic droit sur l'état **Walk** > `Make Transition` > clique sur **Idle**.
    *   Sélectionne la flèche.
    *   Dans **Conditions**, ajoute `isWalking` | `false`.
    *   Décoche **Has Exit Time**.

---

## 2. Implémentation du Code (C#)

On doit modifier le script de mouvement pour qu'il communique avec l'Animator.

### Script : `PlayerLogic.cs`

#### Variables à ajouter
Au début de la classe, ajoute une variable pour stocker la référence à l'Animator :
```csharp
private Animator animator;
```

#### Initialisation
Dans la méthode `Start()` ou `Awake()`, récupère le composant :
```csharp
void Start() {
    animator = GetComponent<Animator>();
}
```

#### Mise à jour de l'animation
Dans ta boucle de mouvement (généralement `Update()` ou une méthode appelée par `Update`), ajoute cette logique :
```csharp
void HandleMovement() {
    Vector2 inputVector = gameInputManager.InputVector();
    
    // Déterminer si le joueur bouge
    // Si la magnitude du vecteur d'entrée est > 0, alors on marche
    bool moving = inputVector.magnitude > 0;

    // Envoyer l'info à l'Animator
    if (animator != null) {
        animator.SetBool("isWalking", moving);
    }
    
    // ... ton code de mouvement physique ...
}
```

---

## 3. Points de Contrôle pour l'Étude
*   **Pourquoi un Bool ?** On utilise un Booléen car l'état de marche est binaire (soit on marche, soit on ne marche pas). Pour un jeu plus complexe avec de la course, on utiliserait un **Float** (`Speed`) et un **Blend Tree**.
*   **Has Exit Time** : Si cette option est cochée, Unity attend que l'animation de repos soit finie avant de commencer à marcher. En la décochant, le jeu est beaucoup plus réactif.
*   **GetComponent** : Cette fonction est coûteuse, c'est pourquoi on ne l'appelle qu'une seule fois dans `Start()` pour mettre l'Animator "en mémoire".
