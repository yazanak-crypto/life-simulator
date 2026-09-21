using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LifeSimulator.Editor
{
    // One-shot world construction for gameplay slice v0.1. Building the district from script keeps
    // the scene reproducible and keeps hand-edited YAML out of the milestone.
    public static class DistrictBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/PrototypeMovement.unity";
        private const string PrefabDir = "Assets/_Game/Prefabs";

        // Home -> dealership -> warehouse along one street. The dealership sits on the commute so
        // the player walks past the car they are saving for twice a day.
        private static readonly Vector3 HomePosition = new Vector3(-120f, 0f, 0f);
        private static readonly Vector3 WarehouseCentre = new Vector3(130f, 0f, 0f);
        private const float GroundHalfX = 200f;
        private const float GroundHalfZ = 120f;

        [MenuItem("Tools/Life Simulator/Build district (rewrites scene)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(PrefabDir);

            RemoveMissingScripts();
            RemovePrototypeWarehouse();
            ExpandGround();
            GameClock clock = EnsureClock();
            EnsureClockUI(clock);
            WarehouseBox boxPrefab = EnsureBoxPrefab();
            BuildHome();
            BuildWarehouse(clock, boxPrefab);
            UpgradePlayer();
            MoveParkingToHome();
            GroupLooseRoots();
            WireDevelopmentOverrides(clock);

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("DISTRICT BUILD COMPLETE");
        }

        // ---------- helpers ----------

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            return go;
        }

        private static GameObject Decal(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject go = Box(name, parent, position, scale);
            // Paint must never catch the car's underside; see the vehicle milestone notes.
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        private static void Sign(string text, Transform parent, Vector3 position)
        {
            var go = new GameObject(text + " Sign");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.35f;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.black;
        }

        private static GameObject Root(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) return existing;
            return new GameObject(name);
        }

        private static void RemoveMissingScripts()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        private static void RemovePrototypeWarehouse()
        {
            foreach (string name in new[] { "Warehouse Shift Start", "Warehouse Pickup", "Warehouse Box",
                         "Warehouse Delivery Area", "Green Delivery Pad", "Blue Pickup Pad",
                         "Pickup Sign", "Delivery Sign", "Workplace Sign" })
            {
                GameObject go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        // ---------- world ----------

        private static void ExpandGround()
        {
            GameObject ground = GameObject.Find("Ground (120m x 100m)") ?? GameObject.Find("Ground (400m x 240m)");
            if (ground != null)
            {
                ground.name = "Ground (400m x 240m)";
                // Unity's built-in plane is 10 m per unit of scale.
                ground.transform.localScale = new Vector3(GroundHalfX * 2f / 10f, 1f, GroundHalfZ * 2f / 10f);
                ground.transform.position = Vector3.zero;
            }

            Place("North Boundary", new Vector3(0f, 2f, GroundHalfZ - 2f), new Vector3(GroundHalfX * 2f, 4f, 1f));
            Place("South Boundary", new Vector3(0f, 2f, -GroundHalfZ + 2f), new Vector3(GroundHalfX * 2f, 4f, 1f));
            Place("East Boundary", new Vector3(GroundHalfX - 2f, 2f, 0f), new Vector3(1f, 4f, GroundHalfZ * 2f));
            Place("West Boundary", new Vector3(-GroundHalfX + 2f, 2f, 0f), new Vector3(1f, 4f, GroundHalfZ * 2f));

            // The commute street, running the length of the district past the dealership.
            Transform roads = Root("Street").transform;
            Decal("Street Surface", roads, new Vector3(5f, 0.005f, -6f), new Vector3(330f, 0.01f, 14f));
            for (int i = 0; i < 22; i++)
                Decal("Street Dash", roads, new Vector3(-150f + i * 15f, 0.012f, -6f), new Vector3(4f, 0.01f, 0.25f));
        }

        private static void Place(string name, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) go = Box(name, null, Vector3.zero, Vector3.one);
            go.name = name;
            go.transform.SetParent(null, true);
            go.transform.position = position;
            go.transform.localScale = scale;
        }

        private static GameClock EnsureClock()
        {
            GameClock clock = Object.FindFirstObjectByType<GameClock>();
            if (clock == null) clock = new GameObject("Game Clock").AddComponent<GameClock>();
            return clock;
        }

        private static void EnsureClockUI(GameClock clock)
        {
            GameObject canvas = GameObject.Find("Interaction UI");
            if (canvas == null) return;
            var existing = Object.FindFirstObjectByType<ClockUI>();
            Text template = GameObject.Find("Balance")?.GetComponent<Text>();

            GameObject host = GameObject.Find("Clock");
            if (host == null)
            {
                host = new GameObject("Clock", typeof(RectTransform));
                host.transform.SetParent(canvas.transform, false);
            }
            var text = host.GetComponent<Text>() ?? host.AddComponent<Text>();
            if (template != null)
            {
                text.font = template.font;
                text.fontSize = template.fontSize;
                text.color = template.color;
            }
            text.alignment = TextAnchor.UpperRight;
            text.text = "Monday\n6:00 AM";
            var rect = host.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.sizeDelta = new Vector2(240f, 60f);

            var ui = existing != null ? existing : host.AddComponent<ClockUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("clock").objectReferenceValue = clock;
            so.FindProperty("clockText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- prefabs ----------

        private static WarehouseBox EnsureBoxPrefab()
        {
            string path = PrefabDir + "/WarehouseBox.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<WarehouseBox>();

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.name = "WarehouseBox";
            temp.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            temp.AddComponent<CarryableItem>();
            temp.AddComponent<WarehouseBox>();
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return asset.GetComponent<WarehouseBox>();
        }

        private static void SavePrefab(GameObject instance, string fileName)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, PrefabDir + "/" + fileName + ".prefab",
                InteractionMode.AutomatedAction);
        }

        // ---------- locations ----------

        private static void BuildHome()
        {
            GameObject old = GameObject.Find("Home");
            if (old != null) Object.DestroyImmediate(old);

            var home = new GameObject("Home");
            home.transform.position = HomePosition;
            Transform t = home.transform;
            Box("Floor", t, new Vector3(0f, 0.05f, 0f), new Vector3(14f, 0.1f, 12f));
            Box("Wall North", t, new Vector3(0f, 1.6f, 6f), new Vector3(14f, 3.2f, 0.4f));
            Box("Wall West", t, new Vector3(-7f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 12f));
            Box("Wall East", t, new Vector3(7f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 12f));
            Box("Wall South Left", t, new Vector3(-4.5f, 1.6f, -6f), new Vector3(5f, 3.2f, 0.4f));
            Box("Wall South Right", t, new Vector3(4.5f, 1.6f, -6f), new Vector3(5f, 3.2f, 0.4f));
            Box("Bed", t, new Vector3(-4f, 0.35f, 3.5f), new Vector3(2f, 0.5f, 4f));
            Sign("HOME", t, new Vector3(0f, 3.8f, -6f));

            // Driveway the owned car is parked on.
            var bay = new GameObject("Home Parking Bay");
            bay.transform.SetParent(t, false);
            bay.transform.localPosition = new Vector3(11f, 0f, -2f);
            Decal("Bay Pad", bay.transform, new Vector3(0f, 0.04f, 0f), new Vector3(5f, 0.08f, 6f));
            Sign("YOUR CAR", bay.transform, new Vector3(0f, 1.6f, 3.6f));

            SavePrefab(home, "Home");
        }

        private static void BuildWarehouse(GameClock clock, WarehouseBox boxPrefab)
        {
            GameObject old = GameObject.Find("Warehouse");
            if (old != null) Object.DestroyImmediate(old);

            var warehouse = new GameObject("Warehouse");
            warehouse.transform.position = WarehouseCentre;
            Transform t = warehouse.transform;

            // 70 x 45 m floor, open on the west side where the player arrives.
            Decal("Floor", t, new Vector3(0f, 0.03f, 0f), new Vector3(70f, 0.06f, 45f));
            Box("Wall North", t, new Vector3(0f, 2f, 22.5f), new Vector3(70f, 4f, 0.5f));
            Box("Wall South", t, new Vector3(0f, 2f, -22.5f), new Vector3(70f, 4f, 0.5f));
            Box("Wall East", t, new Vector3(35f, 2f, 0f), new Vector3(0.5f, 4f, 45f));
            Box("Wall West Upper", t, new Vector3(-35f, 2f, 15f), new Vector3(0.5f, 4f, 15f));
            Box("Wall West Lower", t, new Vector3(-35f, 2f, -15f), new Vector3(0.5f, 4f, 15f));
            Sign("WAREHOUSE", t, new Vector3(-35f, 5f, 0f));

            var racksRoot = new GameObject("Racks");
            racksRoot.transform.SetParent(t, false);
            string[] aisles = { "A", "B", "C", "D" };
            float[] aisleZ = { 15f, 5f, -5f, -15f };
            float[] rackX = { -22f, -8f, 6f, 20f };
            var racks = new List<WarehouseRack>();
            for (int a = 0; a < aisles.Length; a++)
            {
                var aisleRoot = new GameObject("Aisle " + aisles[a]);
                aisleRoot.transform.SetParent(racksRoot.transform, false);
                Sign("AISLE " + aisles[a], aisleRoot.transform, new Vector3(-30f, 2.2f, aisleZ[a]));
                for (int r = 0; r < rackX.Length; r++)
                {
                    GameObject shelf = Box("Rack " + (r + 1), aisleRoot.transform,
                        new Vector3(rackX[r], 0.6f, aisleZ[a]), new Vector3(6f, 1.2f, 2.5f));
                    var rack = shelf.AddComponent<WarehouseRack>();
                    var so = new SerializedObject(rack);
                    so.FindProperty("aisle").stringValue = aisles[a];
                    so.FindProperty("rackNumber").intValue = r + 1;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var anchor = new GameObject("Box Anchor");
                    anchor.transform.SetParent(shelf.transform, false);
                    // Local space is scaled by the shelf, so sit the anchor just above its top.
                    anchor.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    var so2 = new SerializedObject(rack);
                    so2.FindProperty("boxAnchor").objectReferenceValue = anchor.transform;
                    so2.ApplyModifiedPropertiesWithoutUndo();
                    racks.Add(rack);
                }
            }

            var shift = warehouse.AddComponent<ShiftAssignments>();
            var jobSite = warehouse.AddComponent<JobSite>();

            var dropRoot = new GameObject("Destinations");
            dropRoot.transform.SetParent(t, false);
            var drops = new List<WarehouseDropOff>();
            drops.Add(MakeDropOff("Loading Bay 1", dropRoot.transform, new Vector3(31f, 0f, 14f), shift));
            drops.Add(MakeDropOff("Loading Bay 2", dropRoot.transform, new Vector3(31f, 0f, -14f), shift));
            drops.Add(MakeDropOff("Packing Station 1", dropRoot.transform, new Vector3(-30f, 0f, 18f), shift));
            drops.Add(MakeDropOff("Packing Station 2", dropRoot.transform, new Vector3(-30f, 0f, -18f), shift));

            GameObject post = Box("Time Clock", t, new Vector3(-31f, 0.9f, 0f), new Vector3(0.8f, 1.8f, 0.8f));
            Sign("TIME CLOCK", post.transform, new Vector3(0f, 1.4f, -0.7f));
            var terminal = post.AddComponent<TimeClockTerminal>();

            ObjectiveUI objective = Object.FindFirstObjectByType<ObjectiveUI>();

            var shiftSo = new SerializedObject(shift);
            shiftSo.FindProperty("boxPrefab").objectReferenceValue = boxPrefab;
            shiftSo.FindProperty("objectiveUI").objectReferenceValue = objective;
            FillArray(shiftSo.FindProperty("racks"), racks.ToArray());
            FillArray(shiftSo.FindProperty("dropOffs"), drops.ToArray());
            shiftSo.ApplyModifiedPropertiesWithoutUndo();

            var jobSo = new SerializedObject(jobSite);
            jobSo.FindProperty("clock").objectReferenceValue = clock;
            jobSo.FindProperty("assignments").objectReferenceValue = shift;
            jobSo.ApplyModifiedPropertiesWithoutUndo();

            var termSo = new SerializedObject(terminal);
            termSo.FindProperty("jobSite").objectReferenceValue = jobSite;
            termSo.ApplyModifiedPropertiesWithoutUndo();

            // Saved last so the prefab captures its internal wiring. References that point out of
            // the prefab (clock, objective UI) stay as instance overrides, which is expected.
            SavePrefab(warehouse, "Warehouse");
        }

        private static void FillArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static WarehouseDropOff MakeDropOff(string label, Transform parent, Vector3 position, ShiftAssignments shift)
        {
            GameObject pad = Box(label, parent, position + new Vector3(0f, 0.4f, 0f), new Vector3(4f, 0.8f, 4f));
            Sign(label, pad.transform, new Vector3(0f, 1.2f, 0f));
            var drop = pad.AddComponent<WarehouseDropOff>();
            var so = new SerializedObject(drop);
            so.FindProperty("displayName").stringValue = label;
            so.FindProperty("assignments").objectReferenceValue = shift;
            so.ApplyModifiedPropertiesWithoutUndo();
            return drop;
        }

        // ---------- player ----------

        private static void UpgradePlayer()
        {
            PlayerInteraction player = Object.FindFirstObjectByType<PlayerInteraction>();
            if (player == null) return;
            GameObject go = player.gameObject;

            Transform carryPoint = go.transform.Find("Carry Point");
            if (carryPoint == null)
            {
                var anchor = new GameObject("Carry Point");
                anchor.transform.SetParent(go.transform, false);
                anchor.transform.localPosition = new Vector3(0f, 1.1f, 0.7f);
                carryPoint = anchor.transform;
            }

            PlayerCarry carry = go.GetComponent<PlayerCarry>() ?? go.AddComponent<PlayerCarry>();
            var carrySo = new SerializedObject(carry);
            carrySo.FindProperty("carryPoint").objectReferenceValue = carryPoint;
            carrySo.ApplyModifiedPropertiesWithoutUndo();

            if (go.GetComponent<PlayerEmployment>() == null) go.AddComponent<PlayerEmployment>();
            go.transform.position = HomePosition + new Vector3(0f, 0.1f, -3f);
        }

        private static void MoveParkingToHome()
        {
            GameObject bay = GameObject.Find("Home Parking Bay");
            PlayerParking parking = Object.FindFirstObjectByType<PlayerParking>();
            if (bay == null || parking == null) return;
            var so = new SerializedObject(parking);
            so.FindProperty("carSpot").objectReferenceValue = bay.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject oldBay = GameObject.Find("Player Owned Parking");
            if (oldBay != null) Object.DestroyImmediate(oldBay);
        }

        // Development-only shortcuts, wired to the same public entry points the player uses.
        private static void WireDevelopmentOverrides(GameClock clock)
        {
            var dev = Object.FindFirstObjectByType<DevelopmentOverrides>();
            if (dev == null) dev = new GameObject("Development Overrides").AddComponent<DevelopmentOverrides>();
            var player = Object.FindFirstObjectByType<PlayerInteraction>();
            var so = new SerializedObject(dev);
            so.FindProperty("clock").objectReferenceValue = clock;
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("wallet").objectReferenceValue = player != null ? player.GetComponent<PlayerWallet>() : null;
            so.FindProperty("employment").objectReferenceValue = player != null ? player.GetComponent<PlayerEmployment>() : null;
            so.FindProperty("starterCar").objectReferenceValue = Object.FindFirstObjectByType<CarPurchase>();
            so.FindProperty("jobSite").objectReferenceValue = Object.FindFirstObjectByType<JobSite>();
            so.FindProperty("assignments").objectReferenceValue = Object.FindFirstObjectByType<ShiftAssignments>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GroupLooseRoots()
        {
            Transform street = Root("Street").transform;
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.transform.parent == null && go.name == "Road Center Dash")
                    go.transform.SetParent(street, true);

            Transform bounds = Root("District Bounds").transform;
            foreach (string name in new[] { "North Boundary", "South Boundary", "East Boundary", "West Boundary" })
            {
                GameObject go = GameObject.Find(name);
                if (go != null && go.transform.parent == null) go.transform.SetParent(bounds, true);
            }
        }
    }
}
