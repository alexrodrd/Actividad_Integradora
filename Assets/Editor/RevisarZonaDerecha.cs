using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RevisarZonaDerecha
{
    [MenuItem("Tools/Mapa/Enfocar zona derecha")]
    public static void Enfocar()
    {
        GameObject zona = GameObject.Find("ZonaDerecha_Completa");
        if (zona == null)
        {
            Debug.LogWarning("Abre Assets/Scenes/Mapa.unity para revisar la zona derecha.");
            return;
        }
        Selection.activeGameObject = zona;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(19f, 1f, 12f), Quaternion.Euler(57f, -30f, 0f), 15f);
    }

    [MenuItem("Tools/Mapa/Validar zona derecha y camion")]
    public static void Validar()
    {
        GameObject zona = GameObject.Find("ZonaDerecha_Completa");
        GameObject camion = GameObject.Find("CamionBasura_Integrado");
        if (zona == null || camion == null)
            throw new InvalidOperationException("Abre Assets/Scenes/Mapa.unity. Faltan la zona derecha o el camion.");

        foreach (int numero in new[] { 12, 13, 14, 15, 16, 17 })
            if (!zona.GetComponentsInChildren<Transform>().Any(t => t.name == "Estacionamiento_" + numero))
                throw new InvalidOperationException("Falta el estacionamiento " + numero);

        foreach (GameObject root in new[] { zona, camion })
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh == null)
                    throw new InvalidOperationException("Malla sin referencia en " + filter.name);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        throw new InvalidOperationException("Material o shader sin referencia en " + renderer.name);
        }
        foreach (Material material in camion.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Distinct())
            if (material.GetTexture("_BaseMap") == null)
                throw new InvalidOperationException("Falta la textura del camion en " + material.name);

        Debug.Log("Zona derecha validada. Estacionamientos 12 a 17, mallas, materiales y texturas del camion encontrados.");
    }
}
