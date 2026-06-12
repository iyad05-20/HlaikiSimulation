using UnityEngine;
using TMPro;

namespace JemaaGame.UI
{
    public class StoryItemUI : MonoBehaviour
    {
        [Tooltip("Glissez ici le TextMeshPro du Titre")]
        public TextMeshProUGUI titleText;

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
            else Debug.LogWarning("[StoryItemUI] titleText n'est pas assigné !");
        }
    }
}
