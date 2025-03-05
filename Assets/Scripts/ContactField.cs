using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(LineRenderer))]
public class ContactField : MonoBehaviour
{
    public int segments = 32;
    public Color sphereColor = Color.red;
    private LineRenderer lineRenderer;
    private MeshRenderer meshRenderer;
    private float radius;
    public float lineWidth = 0.02f;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = sphereColor;
        lineRenderer.endColor = sphereColor;
        meshRenderer = GetComponent<MeshRenderer>();
        UpdateSphere();
    }

    void Update() => UpdateSphere();

    private void UpdateSphere()
    {
       
        if (meshRenderer)
            radius = meshRenderer.bounds.extents.magnitude;

        List<Vector3> points = new();

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            points.Add(transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius);
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            points.Add(transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            points.Add(transform.position + new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
