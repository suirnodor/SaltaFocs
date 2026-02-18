using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        // Dirección hacia la cámara
        Vector3 lookDir = Camera.main.transform.forward;

        // Eliminamos la inclinación vertical (X y Z)
        lookDir.y = 0;

        // Si la dirección es válida, rotamos
        if (lookDir != Vector3.zero)
            transform.forward = lookDir;
    }
}

