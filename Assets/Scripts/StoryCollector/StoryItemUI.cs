using UnityEngine;
using TMPro;

namespace JemaaGame.UI
{
    public class StoryItemUI : MonoBehaviour
    {
        [Tooltip("Glissez ici le TextMeshPro du Titre")]
        public TextMeshProUGUI titleText;
        
        [Tooltip("Glissez ici le TextMeshPro du Contenu")]
        public TextMeshProUGUI contentText;

        public void SetStory(string title, string content)
        {
            if (titleText != null) titleText.text = title;
            else Debug.LogWarning("[StoryItemUI] titleText n'est pas assigné !");

            if (contentText != null) contentText.text = content;
            else Debug.LogWarning("[StoryItemUI] contentText n'est pas assigné !");
        }
    }
}
