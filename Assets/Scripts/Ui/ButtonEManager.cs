using UnityEngine;

namespace JemaaGame.UI
{
    public class ButtonEManager : MonoBehaviour
    {
        public static ButtonEManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Glissez ici le GameObject du Bouton E depuis l'éditeur")]
        public GameObject buttonE;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (buttonE != null)
            {
                buttonE.SetActive(false);
            }
        }

        public void ShowButtonE(bool show)
        {
            if (buttonE != null)
            {
                buttonE.SetActive(show);
            }
        }
    }
}
