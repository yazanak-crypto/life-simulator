using UnityEngine;
using UnityEngine.UI;

namespace LifeSimulator
{
    public sealed class InteractionUI : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private GameObject aimMarker;
        [SerializeField, Min(0.1f)] private float feedbackDuration = 2f;

        private float feedbackUntil;

        private void Awake() => Clear();

        public void SetTarget(IInteractable target)
        {
            aimMarker.SetActive(true);
            promptText.gameObject.SetActive(target != null);
            promptText.text = target != null ? "E \u2014 " + target.Prompt : string.Empty;
        }

        public void ShowFeedback(string message)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            feedbackUntil = Time.unscaledTime + feedbackDuration;
        }

        public void Clear()
        {
            promptText.gameObject.SetActive(false);
            feedbackText.gameObject.SetActive(false);
            aimMarker.SetActive(false);
        }

        private void Update()
        {
            if (feedbackText.gameObject.activeSelf && Time.unscaledTime >= feedbackUntil)
                feedbackText.gameObject.SetActive(false);
        }

        private void OnDisable() => Clear();
    }
}
