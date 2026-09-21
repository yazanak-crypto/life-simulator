using UnityEngine;

namespace LifeSimulator
{
    // A world-space destination assigned to this player, never parented to the moving character.
    public sealed class PlayerParking : MonoBehaviour
    {
        [SerializeField] private Transform carSpot;
        public Transform CarSpot => carSpot;
    }
}
