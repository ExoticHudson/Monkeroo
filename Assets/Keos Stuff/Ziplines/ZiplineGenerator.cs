#if UNITY_EDITOR
using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ZiplineGenerator : EditorWindow // I call this an Zipline Generator, rhymes with grug
{
    [System.Serializable]
    public class ControlPoint
    {
        public Vector3 position;
        public Color gizmoColor = Color.cyan;
        public float radius = 1f;
        public bool expanded = false;

        public ControlPoint(Vector3 pos)
        {
            position = pos;
        }
    }

    private List<ControlPoint> controlPoints = new List<ControlPoint>();
    private int selectedPointIndex = -1;
    private bool isPlacingPoints = false;
    private bool showGizmos = true;
    private bool snapToGround = true;
    private LayerMask groundLayer = 1;

    private int radialSegments = 16;
    private int lengthSegments = 32;
    private bool generateCollider = true;
    private LayerMask ziplineLayer = 1;
    private Material cylinderMaterial;
    private string cylinderName = "Zipline";
    private string meshSavePath = "Assets/Keos Stuff/Ziplines/GeneratedMeshes/";

    private float globalRadius = 0.1f;
    private bool useIndividualRadius = false;
    private float curveSmoothing = 0.5f;
    private Vector2 scrollPos;
    private GameObject previewCylinder;
    private bool autoPreview = true;
    private bool realTimePreview = false;

    private enum TabMode { Points, Settings, Preview, Advanced }
    private TabMode currentTab = TabMode.Points;

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;

    private bool loopZipline = false;
    private bool insertPointMode = false;

    [MenuItem("Keos stuff/Zipline Generator")]
    public static void ShowWindow()
    {
        ZiplineGenerator window = GetWindow<ZiplineGenerator>("Zipline Generator");
        window.minSize = new Vector2(350, 600);
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        CleanupPreview();
    }

    private void InitializeStyles()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold
        };

        boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 10, 10)
        };
    }

    private void OnGUI()
    {
        if (headerStyle == null) InitializeStyles();

        DrawHeader();
        DrawTabs();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (currentTab)
        {
            case TabMode.Points:
                DrawPointsTab();
                break;
            case TabMode.Settings:
                DrawSettingsTab();
                break;
            case TabMode.Preview:
                DrawPreviewTab();
                break;
            case TabMode.Advanced:
                DrawAdvancedTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        DrawActionButtons();

        if (GUI.changed)
        {
            if (realTimePreview && controlPoints.Count >= 2)
            {
                CreatePreview();
            }
            SceneView.RepaintAll();
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Zipline Generator Pro", headerStyle);
        EditorGUILayout.Space(5);

        Rect rect = GUILayoutUtility.GetRect(0, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        EditorGUILayout.Space(10);
    }

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = currentTab == TabMode.Points ? Color.cyan : Color.white;
        if (GUILayout.Button($"Points ({controlPoints.Count})", buttonStyle))
            currentTab = TabMode.Points;

        GUI.backgroundColor = currentTab == TabMode.Settings ? Color.cyan : Color.white;
        if (GUILayout.Button("Settings", buttonStyle))
            currentTab = TabMode.Settings;

        GUI.backgroundColor = currentTab == TabMode.Preview ? Color.cyan : Color.white;
        if (GUILayout.Button("Preview", buttonStyle))
            currentTab = TabMode.Preview;

        GUI.backgroundColor = currentTab == TabMode.Advanced ? Color.cyan : Color.white;
        if (GUILayout.Button("Advanced", buttonStyle))
            currentTab = TabMode.Advanced;

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }

    private void DrawPointsTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.LabelField("Point Placement", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = isPlacingPoints ? Color.red : Color.green;
        if (GUILayout.Button(isPlacingPoints ? "Stop Placing" : "Start Placing", buttonStyle))
        {
            isPlacingPoints = !isPlacingPoints;
            insertPointMode = false;
            selectedPointIndex = -1;
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = insertPointMode ? Color.red : Color.yellow;
        if (GUILayout.Button(insertPointMode ? "Stop Insert" : "Insert Mode", buttonStyle))
        {
            insertPointMode = !insertPointMode;
            isPlacingPoints = false;
            selectedPointIndex = -1;
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear All", buttonStyle))
        {
            if (EditorUtility.DisplayDialog("Clear All Points", "Are you sure you want to clear all control points?", "Yes", "Cancel"))
            {
                controlPoints.Clear();
                selectedPointIndex = -1;
                insertPointMode = false;
                CleanupPreview();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (insertPointMode)
        {
            EditorGUILayout.HelpBox("Insert Mode: Click on the white line between points to add a new point at that location.", MessageType.Info);
        }
        else if (isPlacingPoints)
        {
            EditorGUILayout.HelpBox("Placement Mode: Click in the scene to add new points at the end.", MessageType.Info);
        }

        snapToGround = EditorGUILayout.Toggle("Snap to Ground", snapToGround);
        if (snapToGround)
        {
            groundLayer = EditorGUILayout.MaskField("Ground Layer", groundLayer, InternalEditorUtility.layers);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (controlPoints.Count == 0)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("No control points yet!", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Click 'Start Placing' and click in the scene to add points", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }
        else
        {
            DrawControlPointsList();
        }
    }

    private void DrawControlPointsList()
    {
        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint point = controlPoints[i];
            bool isSelected = selectedPointIndex == i;

            EditorGUILayout.BeginVertical(isSelected ? GUI.skin.box : boxStyle);

            EditorGUILayout.BeginHorizontal();

            string pointIcon = isSelected ? "🔹" : "🔸";
            GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

            if (GUILayout.Button($"{pointIcon} Point {i}", isSelected ? EditorStyles.boldLabel : EditorStyles.label))
            {
                selectedPointIndex = selectedPointIndex == i ? -1 : i;
                Selection.activeTransform = null;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("📋", GUILayout.Width(25)))
            {
                EditorGUIUtility.systemCopyBuffer = $"{point.position.x:F2}, {point.position.y:F2}, {point.position.z:F2}";
                Debug.Log($"Copied Point {i} position to clipboard");
            }

            if (GUILayout.Button("❌", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Delete Point", $"Delete Point {i}?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(this, "Delete Control Point");
                    controlPoints.RemoveAt(i);
                    if (selectedPointIndex == i) selectedPointIndex = -1;
                    else if (selectedPointIndex > i) selectedPointIndex--;
                    CleanupPreview();
                    break;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (isSelected || point.expanded)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = EditorGUILayout.Vector3Field("Position", point.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Modify Control Point");
                    point.position = newPos;
                    if (realTimePreview) CreatePreview();
                }

                point.gizmoColor = EditorGUILayout.ColorField("Gizmo Color", point.gizmoColor);

                if (useIndividualRadius)
                {
                    point.radius = EditorGUILayout.FloatField("Radius", point.radius);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point", buttonStyle))
        {
            Undo.RecordObject(this, "Add Control Point");
            Vector3 newPos = controlPoints.Count > 0 ? 
                controlPoints[controlPoints.Count - 1].position + Vector3.forward * 5 : 
                Vector3.zero;
            controlPoints.Add(new ControlPoint(newPos));
        }

        if (controlPoints.Count > 0 && GUILayout.Button("Reverse Order", buttonStyle))
        {
            Undo.RecordObject(this, "Reverse Control Points");
            controlPoints.Reverse();
            CleanupPreview();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettingsTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Zipline Properties", EditorStyles.boldLabel);
        
        cylinderName = EditorGUILayout.TextField("Name", cylinderName);
        cylinderMaterial = (Material)EditorGUILayout.ObjectField("Material", cylinderMaterial, typeof(Material), false);
        loopZipline = EditorGUILayout.Toggle("Loop Zipline", loopZipline);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Radius Settings", EditorStyles.boldLabel);
        
        useIndividualRadius = EditorGUILayout.Toggle("Individual Point Radius", useIndividualRadius);
        if (!useIndividualRadius)
        {
            globalRadius = EditorGUILayout.FloatField("Global Radius", globalRadius);
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Curve Settings", EditorStyles.boldLabel);
        curveSmoothing = EditorGUILayout.Slider("Curve Smoothing", curveSmoothing, 0f, 1f);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Mesh Quality", EditorStyles.boldLabel);
        
        radialSegments = EditorGUILayout.IntSlider("Radial Segments", radialSegments, 3, 64);
        lengthSegments = EditorGUILayout.IntSlider("Length Segments", lengthSegments, 4, 128);
        
        EditorGUILayout.HelpBox($"Vertices: ~{radialSegments * lengthSegments}\nTriangles: ~{radialSegments * (lengthSegments - 1) * 2}", MessageType.Info);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
        
        generateCollider = EditorGUILayout.Toggle("Generate Mesh Collider", generateCollider);

        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);

        showGizmos = EditorGUILayout.Toggle("Show Gizmos", showGizmos);
        autoPreview = EditorGUILayout.Toggle("Auto Preview", autoPreview);
        realTimePreview = EditorGUILayout.Toggle("Real-time Preview", realTimePreview);

        if (realTimePreview)
        {
            EditorGUILayout.HelpBox("Real-time preview updates as you modify settings. May impact performance with complex meshes.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(boxStyle);
        if (controlPoints.Count < 2)
        {
            EditorGUILayout.LabelField("Need at least 2 points for preview", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Preview", buttonStyle)) // how does mine look like?
            {
                CreatePreview();
            }
            GUI.backgroundColor = Color.white;

            if (previewCylinder != null)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Clear Preview", buttonStyle))
                {
                    CleanupPreview();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (previewCylinder != null)
            {
                EditorGUILayout.HelpBox("Preview active! The preview object will be automatically cleaned up when you bake the zipline.", MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAdvancedTab()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Asset Management", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Mesh Save Path:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        meshSavePath = EditorGUILayout.TextField(meshSavePath);
        if (GUILayout.Button("📁", GUILayout.Width(25)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Mesh Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.Contains(Application.dataPath))
                {
                    meshSavePath = "Assets" + selectedPath.Substring(Application.dataPath.Length) + "/";
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!Directory.Exists(meshSavePath))
        {
            EditorGUILayout.HelpBox("Directory doesn't exist. It will be created when saving.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Utility Functions", EditorStyles.boldLabel);

        if (GUILayout.Button("Calculate Length")) // can this thing do the same with my ding a ling, NO it would take to long to calculate (cause it cant find it bruh 😭)
        {
            float totalLength = CalculateTotalLength();
            EditorUtility.DisplayDialog("Zipline Length", $"Total zipline length: {totalLength:F2} units", "OK");
        }

        if (GUILayout.Button("Export Points to JSON"))
        {
            ExportPointsToJSON();
        }

        if (GUILayout.Button("Import Points from JSON"))
        {
            ImportPointsFromJSON();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.Space(10);
        Rect rect = GUILayoutUtility.GetRect(0, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = controlPoints.Count >= 2;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Bake Zipline", buttonStyle, GUILayout.Height(40))) // i like bread
        {
            CreateCylinder();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (controlPoints.Count < 2)
        {
            EditorGUILayout.HelpBox("Add at least 2 control points to create a zipline.", MessageType.Warning);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showGizmos) return;

        Event e = Event.current;

        if (isPlacingPoints && selectedPointIndex == -1 && e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPos = GetWorldPositionFromRay(ray);

            Undo.RecordObject(this, "Add Control Point");
            controlPoints.Add(new ControlPoint(worldPos));
            e.Use();
            Repaint();

            if (realTimePreview && controlPoints.Count >= 2)
            {
                CreatePreview();
            }
        }

        if (insertPointMode && selectedPointIndex == -1 && e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control && controlPoints.Count >= 2)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            float closestT;
            int insertIndex;
            if (FindClosestPointOnCurve(ray, out closestT, out insertIndex))
            {
                Vector3 insertPosition = GetCurvePosition(closestT);

                Undo.RecordObject(this, "Insert Control Point");
                controlPoints.Insert(insertIndex + 1, new ControlPoint(insertPosition));
                e.Use();
                Repaint();

                if (realTimePreview)
                {
                    CreatePreview();
                }
            }
        }

        DrawSceneGizmos();
    }

    private Vector3 GetWorldPositionFromRay(Ray ray)
    {
        Vector3 worldPos;

        if (snapToGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                worldPos = hit.point;
            }
            else
            {
                worldPos = ray.origin + ray.direction * 10f;
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                worldPos = hit.point;
            }
            else
            {
                worldPos = ray.origin + ray.direction * 10f;
            }
        }

        return worldPos;
    }

    private bool FindClosestPointOnCurve(Ray ray, out float closestT, out int insertIndex)
    {
        closestT = 0f;
        insertIndex = 0;

        if (controlPoints.Count < 2) return false;

        float minDistance = float.MaxValue;
        bool foundPoint = false;

        int samples = lengthSegments * 4;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / (samples - 1);
            Vector3 curvePoint = GetCurvePosition(t);

            Vector3 rayToPoint = curvePoint - ray.origin;
            float projectionLength = Vector3.Dot(rayToPoint, ray.direction);
            Vector3 closestPointOnRay = ray.origin + ray.direction * projectionLength;

            float distance = Vector3.Distance(curvePoint, closestPointOnRay);

            if (distance < 2f && distance < minDistance)
            {
                minDistance = distance;
                closestT = t;
                foundPoint = true;

                if (loopZipline)
                {
                    insertIndex = Mathf.FloorToInt(t * controlPoints.Count);
                }
                else
                {
                    insertIndex = Mathf.FloorToInt(t * (controlPoints.Count - 1));
                }
            }
        }

        return foundPoint;
    }

    private void DrawSceneGizmos()
    {
        for (int i = 0; i < controlPoints.Count; i++)
        {
            ControlPoint point = controlPoints[i];
            float radius = useIndividualRadius ? point.radius : globalRadius;

            Handles.color = point.gizmoColor;

            if (selectedPointIndex == i)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(point.position, Vector3.up, radius * 1.2f);
                Handles.DrawWireDisc(point.position, Vector3.right, radius * 1.2f);
                Handles.DrawWireDisc(point.position, Vector3.forward, radius * 1.2f);
            }

            Handles.color = point.gizmoColor;
            Handles.SphereHandleCap(0, point.position, Quaternion.identity, radius * 0.5f, EventType.Repaint);
            Handles.DrawWireDisc(point.position, Vector3.up, radius);

            Handles.Label(point.position + Vector3.up * (radius + 1f),
                $"Point {i}\nPos: {point.position.x:F1}, {point.position.y:F1}, {point.position.z:F1}");

            if (selectedPointIndex == i)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(point.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Control Point");
                    point.position = newPos;
                    if (realTimePreview && controlPoints.Count >= 2) CreatePreview();
                    Repaint();
                }
            }
        }

        if (controlPoints.Count > 1)
        {
            List<Vector3> curvePoints = GenerateCurvePoints();

            if (insertPointMode)
            {
                Handles.color = Color.green;
                for (int i = 0; i < curvePoints.Count - 1; i++)
                {
                    Handles.DrawLine(curvePoints[i], curvePoints[i + 1]);
                }
            }
            else
            {
                Handles.color = Color.white;
                for (int i = 0; i < curvePoints.Count - 1; i++)
                {
                    Handles.DrawLine(curvePoints[i], curvePoints[i + 1]);
                }
            }

            Handles.color = Color.gray;
            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Handles.DrawDottedLine(controlPoints[i].position, controlPoints[i + 1].position, 5f);
            }

            if (loopZipline && controlPoints.Count > 2)
            {
                Handles.DrawDottedLine(controlPoints[controlPoints.Count - 1].position, controlPoints[0].position, 5f);
            }
        }
    }

    private float CalculateTotalLength()
    {
        if (controlPoints.Count < 2) return 0f;

        List<Vector3> curvePoints = GenerateCurvePoints();
        float totalLength = 0f;

        for (int i = 0; i < curvePoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(curvePoints[i], curvePoints[i + 1]);
        }

        return totalLength;
    }

    private void ExportPointsToJSON()
    {
        if (controlPoints.Count == 0)
        {
            EditorUtility.DisplayDialog("Export Error", "No points to export!", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Export Control Points", "", "zipline_points", "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = JsonUtility.ToJson(new SerializablePointList { points = controlPoints }, true);
            File.WriteAllText(path, json);
            EditorUtility.DisplayDialog("Export Complete", $"Points exported to {path}", "OK");
        }
    }

    private void ImportPointsFromJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import Control Points", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SerializablePointList pointList = JsonUtility.FromJson<SerializablePointList>(json);
                controlPoints = pointList.points;
                selectedPointIndex = -1;
                CleanupPreview();
                EditorUtility.DisplayDialog("Import Complete", $"Imported {controlPoints.Count} points", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Import Error", $"Failed to import points: {ex.Message}", "OK");
            }
        }
    }

    [System.Serializable]
    private class SerializablePointList
    {
        public List<ControlPoint> points;
    }

    private List<Vector3> GenerateCurvePoints()
    {
        List<Vector3> points = new List<Vector3>();

        if (controlPoints.Count < 2) return points;

        for (int i = 0; i < lengthSegments; i++)
        {
            float t = (float)i / (lengthSegments - 1);
            Vector3 pos = GetCurvePosition(t);
            points.Add(pos);
        }

        return points;
    }

    private Vector3 GetCurvePosition(float t)
    {
        if (controlPoints.Count == 0) return Vector3.zero;
        if (controlPoints.Count == 1) return controlPoints[0].position;

        if (loopZipline)
        {
            float scaledT = t * controlPoints.Count;
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            if (index >= controlPoints.Count)
            {
                index = 0;
                localT = 0;
            }

            Vector3 p0 = controlPoints[(index - 1 + controlPoints.Count) % controlPoints.Count].position;
            Vector3 p1 = controlPoints[index].position;
            Vector3 p2 = controlPoints[(index + 1) % controlPoints.Count].position;
            Vector3 p3 = controlPoints[(index + 2) % controlPoints.Count].position;

            return Vector3.Lerp(
                Vector3.Lerp(p1, p2, localT),
                CatmullRom(p0, p1, p2, p3, localT),
                curveSmoothing
            );
        }
        else
        {
            float scaledT = t * (controlPoints.Count - 1);
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            if (index >= controlPoints.Count - 1)
            {
                return controlPoints[controlPoints.Count - 1].position;
            }

            Vector3 p0 = index > 0 ? controlPoints[index - 1].position : controlPoints[index].position;
            Vector3 p1 = controlPoints[index].position;
            Vector3 p2 = controlPoints[index + 1].position;
            Vector3 p3 = index < controlPoints.Count - 2 ? controlPoints[index + 2].position : controlPoints[index + 1].position;

            return Vector3.Lerp(
                Vector3.Lerp(p1, p2, localT),
                CatmullRom(p0, p1, p2, p3, localT),
                curveSmoothing
            );
        }
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * t +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private float GetCurveRadius(float t)
    {
        if (!useIndividualRadius) return globalRadius;

        if (controlPoints.Count == 0) return globalRadius;
        if (controlPoints.Count == 1) return controlPoints[0].radius;

        if (loopZipline)
        {
            float scaledT = t * controlPoints.Count;
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            if (index >= controlPoints.Count)
            {
                index = 0;
                localT = 0;
            }

            float r1 = controlPoints[index].radius;
            float r2 = controlPoints[(index + 1) % controlPoints.Count].radius;

            return Mathf.Lerp(r1, r2, localT);
        }
        else
        {
            float scaledT = t * (controlPoints.Count - 1);
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            if (index >= controlPoints.Count - 1)
            {
                return controlPoints[controlPoints.Count - 1].radius;
            }

            return Mathf.Lerp(controlPoints[index].radius, controlPoints[index + 1].radius, localT);
        }
    }

    private void CreatePreview()
    {
        CleanupPreview();
        previewCylinder = CreateCylinderMesh(true);
        if (previewCylinder != null)
        {
            previewCylinder.name = cylinderName + "_Preview";
            previewCylinder.hideFlags = HideFlags.DontSave;
        }
    }

    private void CleanupPreview()
    {
        if (previewCylinder != null)
        {
            DestroyImmediate(previewCylinder);
            previewCylinder = null;
        }
    }

    private void CreateCylinder()
    {
        GameObject cylinder = CreateCylinderMesh(false);
        if (cylinder == null) return;

        cylinder.name = cylinderName;

        ZiplineData ziplineData = cylinder.AddComponent<ZiplineData>();
        ziplineData.controlPoints = new List<ZiplineControlPoint>();

        foreach (var point in controlPoints)
        {
            ziplineData.controlPoints.Add(new ZiplineControlPoint(point.position, point.gizmoColor));
        }

        ziplineData.radius = globalRadius;
        ziplineData.curveSmoothing = curveSmoothing;
        ziplineData.lengthSegments = lengthSegments;
        ziplineData.radialSegments = radialSegments;
        ziplineData.generateColliders = generateCollider;
        ziplineData.ziplineLayer = ziplineLayer;
        ziplineData.loopZipline = loopZipline;

        Undo.RegisterCreatedObjectUndo(cylinder, "Create Zipline");
        Selection.activeGameObject = cylinder;

        CleanupPreview();

        float totalLength = CalculateTotalLength();
        Debug.Log($"Zipline '{cylinderName}' created successfully!\n" +
                  $"Points: {controlPoints.Count}\n" +
                  $"Length: {totalLength:F2} units\n" +
                  $"Vertices: {radialSegments * lengthSegments}\n" +
                  $"Triangles: {radialSegments * (lengthSegments - 1) * 2}");

        EditorUtility.DisplayDialog("Zipline Created!",
            $"Successfully created '{cylinderName}'\n\n" +
            $"Points: {controlPoints.Count}\n" +
            $"Length: {totalLength:F2} units", "OK");
    }

    private GameObject CreateCylinderMesh(bool isPreview)
    {
        if (controlPoints.Count < 2)
        {
            if (!isPreview)
                Debug.LogWarning("Need at least 2 control points to create a Zipline!");
            return null;
        }

        GameObject go = new GameObject();
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

        Mesh mesh = GenerateCylinderMesh();

        if (!isPreview)
        {
            string savedMeshPath = SaveMeshAsset(mesh);
            if (!string.IsNullOrEmpty(savedMeshPath))
            {
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(savedMeshPath);
            }
        }

        meshFilter.mesh = mesh;

        if (cylinderMaterial != null)
        {
            meshRenderer.material = cylinderMaterial;
        }
        else
        {
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = Color.gray;
            meshRenderer.material = defaultMat;
        }

        return go;
    }

    private string SaveMeshAsset(Mesh mesh)
    {
        if (!Directory.Exists(meshSavePath))
        {
            Directory.CreateDirectory(meshSavePath);
        }

        string fileName = $"{cylinderName}_Mesh.asset";
        string fullPath = Path.Combine(meshSavePath, fileName);

        int counter = 1;
        while (File.Exists(fullPath))
        {
            fileName = $"{cylinderName}_Mesh_{counter}.asset";
            fullPath = Path.Combine(meshSavePath, fileName);
            counter++;
        }

        try
        {
            AssetDatabase.CreateAsset(mesh, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Mesh saved to: {fullPath}");
            return fullPath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save mesh: {e.Message}");
            return null;
        }
    }

    private Mesh GenerateCylinderMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = $"{cylinderName}_Mesh";

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        int actualLengthSegments = loopZipline ? lengthSegments : lengthSegments;

        for (int i = 0; i < actualLengthSegments; i++)
        {
            float t = (float)i / (actualLengthSegments - (loopZipline ? 0 : 1));
            Vector3 centerPos = GetCurvePosition(t);
            float radius = GetCurveRadius(t);

            Vector3 forward = GetForwardDirection(t);
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
            if (right.magnitude < 0.1f) right = Vector3.right;
            Vector3 up = Vector3.Cross(right, forward).normalized;

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = (float)j / radialSegments * 2f * Mathf.PI;
                Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;

                vertices.Add(centerPos + offset);
                normals.Add(offset.normalized);
                uvs.Add(new Vector2((float)j / radialSegments, t));
            }
        }

        if (!loopZipline)
        {
            Vector3 firstCenter = GetCurvePosition(0);
            Vector3 lastCenter = GetCurvePosition(1);
            vertices.Add(firstCenter);
            vertices.Add(lastCenter);
            normals.Add(-GetForwardDirection(0));
            normals.Add(GetForwardDirection(1));
            uvs.Add(new Vector2(0.5f, 0));
            uvs.Add(new Vector2(0.5f, 1));
        }

        int segmentCount = loopZipline ? actualLengthSegments : actualLengthSegments - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * radialSegments + j;
                int next = i * radialSegments + (j + 1) % radialSegments;
                int currentNext = ((i + 1) % actualLengthSegments) * radialSegments + j;
                int nextNext = ((i + 1) % actualLengthSegments) * radialSegments + (j + 1) % radialSegments;

                triangles.Add(current);
                triangles.Add(currentNext);
                triangles.Add(next);

                triangles.Add(next);
                triangles.Add(currentNext);
                triangles.Add(nextNext);
            }
        }

        if (!loopZipline)
        {
            int firstCenterIndex = vertices.Count - 2;
            int lastCenterIndex = vertices.Count - 1;

            for (int j = 0; j < radialSegments; j++)
            {
                int current = j;
                int next = (j + 1) % radialSegments;

                triangles.Add(firstCenterIndex);
                triangles.Add(next);
                triangles.Add(current);
            }

            int lastRingStart = (actualLengthSegments - 1) * radialSegments;
            for (int j = 0; j < radialSegments; j++)
            {
                int current = lastRingStart + j;
                int next = lastRingStart + (j + 1) % radialSegments;

                triangles.Add(lastCenterIndex);
                triangles.Add(current);
                triangles.Add(next);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
    
    private Vector3 GetForwardDirection(float t)
    {
        if (controlPoints.Count < 2) return Vector3.forward;
        
        float epsilon = 0.01f;
        Vector3 pos1 = GetCurvePosition(Mathf.Clamp01(t - epsilon));
        Vector3 pos2 = GetCurvePosition(Mathf.Clamp01(t + epsilon));
        
        return (pos2 - pos1).normalized;
    }
}
#endif