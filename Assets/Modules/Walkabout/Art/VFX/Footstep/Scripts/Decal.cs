using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For footprints for Doty
/// </summary>
public class Decal : MonoBehaviour
{
    private float startTime;
    private float endTime;
    private float duration = 10f;
    private float baseAlpha = 1;

    [SerializeField] MeshRenderer meshRenderer;

    void Start()
    {
        Color color = meshRenderer.material.GetColor("_BaseColor");
        baseAlpha = color.a;

        startTime = Time.time;
        endTime = startTime + duration;
    }

    void Update()
    {
        float t = (Time.time - startTime) / (endTime - startTime);
        Color color = meshRenderer.material.GetColor("_BaseColor");
        color.a = (1 - t) * baseAlpha;
        meshRenderer.material.SetColor("_BaseColor", color);

        if (Time.time > endTime) Destroy(gameObject);
    }
}
