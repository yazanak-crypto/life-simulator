using UnityEngine;
using UnityEngine.UI;

namespace LifeSimulator
{
    public sealed class ObjectiveUI : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;

        public void SetObjective(string message)
        {
            if (objectiveText != null)
                objectiveText.text = message;
        }
    }
}
