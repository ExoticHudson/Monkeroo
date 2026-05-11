using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ZiplineControlPoint
{
    public Vector3 position;
    public Color gizmoColor = Color.cyan;

    public ZiplineControlPoint(Vector3 pos, Color color)
    {
        position = pos;
        gizmoColor = color;
    }
}

public class ZiplineData : MonoBehaviour
{
    [Header("Zipline Configuration")]
    public List<ZiplineControlPoint> controlPoints = new List<ZiplineControlPoint>();
    public float radius = 0.1f;
    public float curveSmoothing = 0.5f;
    public int lengthSegments = 32;
    public int radialSegments = 16;
    public bool loopZipline = false;

    [Header("Physics Settings")]
    public LayerMask ziplineLayer;
    public bool generateColliders = true;

    [Header("Gizmo Settings")]
    public bool showGizmos = true;
    public bool showCurve = true;
    public Color curveColor = Color.white;
    public float gizmoSize = 0.3f;

    private void Start()
    {
        if (generateColliders)
        {
            GenerateColliders();
        }
    }

    private void GenerateColliders()
    {
        List<Vector3> curvePoints = GenerateCurvePoints();
        if (curvePoints.Count < 2) return;

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("ZiplineCollider_"))
            {
                Destroy(child.gameObject);
            }
        }

        int layerIndex = LayerMaskToLayer(ziplineLayer);
        if (layerIndex == -1)
        {
            Debug.LogError("Invalid zipline layer! Please set a single layer in the LayerMask.");
            return;
        }

        int colliderCount = loopZipline ? curvePoints.Count : curvePoints.Count - 1;
        
        for (int i = 0; i < colliderCount; i++)
        {
            GameObject colliderObj = new GameObject($"ZiplineCollider_{i}");
            colliderObj.transform.SetParent(transform, false);
            colliderObj.layer = layerIndex;

            CapsuleCollider collider = colliderObj.AddComponent<CapsuleCollider>();
            Vector3 start = curvePoints[i];
            Vector3 end = curvePoints[(i + 1) % curvePoints.Count];
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            colliderObj.transform.position = (start + end) / 2f;
            colliderObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            collider.radius = radius;
            collider.height = distance + radius * 2;
            collider.isTrigger = true;
        }
    }

    private int LayerMaskToLayer(LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 0)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || controlPoints == null || controlPoints.Count == 0)
            return;

        for (int i = 0; i < controlPoints.Count; i++)
        {
            var point = controlPoints[i];
            Vector3 worldPosition = transform.position + point.position;

            Gizmos.color = point.gizmoColor;
            Gizmos.DrawSphere(worldPosition, radius * gizmoSize);

            DrawWireDisc(worldPosition, Vector3.up, radius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPosition + Vector3.up * (radius + 0.5f), $"Point {i}");
#endif
        }

        if (showCurve && controlPoints.Count > 1)
        {
            Gizmos.color = curveColor;
            List<Vector3> curvePoints = GenerateCurvePoints();
            
            int lineCount = loopZipline ? curvePoints.Count : curvePoints.Count - 1;
            for (int i = 0; i < lineCount; i++)
            {
                Vector3 start = curvePoints[i];
                Vector3 end = curvePoints[(i + 1) % curvePoints.Count];
                Gizmos.DrawLine(start, end);
            }
        }
    }

    private void DrawWireDisc(Vector3 center, Vector3 normal, float radius)
    {
        Vector3 right = Vector3.Cross(normal, Vector3.forward).normalized;
        if (right.magnitude < 0.1f) right = Vector3.right;
        Vector3 up = Vector3.Cross(right, normal).normalized;

        int segments = 32;
        Vector3 prevPoint = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 newPoint = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    public List<Vector3> GenerateCurvePoints()
    {
        List<Vector3> points = new List<Vector3>();

        if (controlPoints.Count < 2) return points;

        for (int i = 0; i < lengthSegments; i++)
        {
            float t = (float)i / (lengthSegments - (loopZipline ? 0 : 1));
            Vector3 pos = GetCurvePosition(t);
            points.Add(pos);
        }

        return points;
    }

    public Vector3 GetCurvePosition(float t)
    {
        if (controlPoints.Count == 0) return transform.position;
        if (controlPoints.Count == 1) return transform.position + controlPoints[0].position;

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

            Vector3 localPos = Vector3.Lerp(
                Vector3.Lerp(p1, p2, localT),
                CatmullRom(p0, p1, p2, p3, localT),
                curveSmoothing
            );

            return transform.position + localPos;
        }
        else
        {
            float scaledT = t * (controlPoints.Count - 1);
            int index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - index;

            if (index >= controlPoints.Count - 1)
            {
                return transform.position + controlPoints[controlPoints.Count - 1].position;
            }

            Vector3 p0 = index > 0 ? controlPoints[index - 1].position : controlPoints[index].position;
            Vector3 p1 = controlPoints[index].position;
            Vector3 p2 = controlPoints[index + 1].position;
            Vector3 p3 = index < controlPoints.Count - 2 ? controlPoints[index + 2].position : controlPoints[index + 1].position;

            Vector3 localPos = Vector3.Lerp(
                Vector3.Lerp(p1, p2, localT),
                CatmullRom(p0, p1, p2, p3, localT),
                curveSmoothing
            );

            return transform.position + localPos;
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
}