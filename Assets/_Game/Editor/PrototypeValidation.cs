using UnityEditor;
using UnityEngine;

namespace LifeSimulator.Editor
{
    // One entry point so a developer (or batch mode) runs every suite in dependency order.
    public static class PrototypeValidation
    {
        [MenuItem("Tools/Life Simulator/Validate everything (reopens scene)")]
        public static void ValidateAll()
        {
            ClockValidation.Validate();
            JobValidation.Validate();
            VehiclePrototypeValidation.Validate();
            Debug.Log("ALL VALIDATION PASSED");
        }
    }
}
