using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LifeSimulator.Editor
{
    // Prints the district's real distances so commute tuning is measured rather than guessed.
    public static class LayoutReport
    {
        [MenuItem("Tools/Life Simulator/Report district layout")]
        public static void Report()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PrototypeMovement.unity");
            var player = Object.FindFirstObjectByType<PlayerInteraction>();
            var walk = player.GetComponent<ThirdPersonPlayer>();
            float speed = (float)typeof(ThirdPersonPlayer)
                .GetField("moveSpeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(walk);

            Vector3 home = GameObject.Find("Home").transform.position;
            Vector3 lot = Object.FindFirstObjectByType<CarPurchase>().transform.position;
            Vector3 clockIn = Object.FindFirstObjectByType<TimeClockTerminal>().transform.position;

            Log("Home -> Dealership", home, lot, speed);
            Log("Dealership -> Warehouse time clock", lot, clockIn, speed);
            Log("Home -> Warehouse time clock", home, clockIn, speed);
            Debug.Log("Player spawn: " + player.transform.position + "   walk speed: " + speed + " m/s");
            Debug.Log("LAYOUT REPORT COMPLETE");
        }

        private static void Log(string label, Vector3 a, Vector3 b, float speed)
        {
            float metres = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
            Debug.Log(label + ": " + metres.ToString("0") + " m   walk " + (metres / speed).ToString("0")
                + " s   at a future 2 m/s walk " + (metres / 2f).ToString("0") + " s");
        }
    }
}
