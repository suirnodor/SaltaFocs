using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SortingOrderByY : MonoBehaviour
{
    private SpriteRenderer sr;

    [Tooltip("Punto que marca la posición de los pies del personaje.")]
    public Transform sortingPoint;

    [Tooltip("Ajusta este valor si el personaje es más alto o más bajo que otros.")]
    public float sortingOffset = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Si no se asignó un sortingPoint, usamos el transform actual (compatibilidad)
        if (sortingPoint == null)
            sortingPoint = transform;
    }

    void LateUpdate()
    {
        // Usamos la posición Y del punto de los pies, NO la del sprite completo
        float y = sortingPoint.position.y + sortingOffset;

        sr.sortingOrder = Mathf.RoundToInt(-y * 100);
    }
}

