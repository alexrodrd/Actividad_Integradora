using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TexturasCamionBasura
{
    private const string Carpeta = "Assets/CamionBasuraTexturas";
    private const int Resolucion = 512;

    [MenuItem("Tools/Camion de basura/Aplicar texturas al seleccionado", true)]
    private static bool PuedeAplicar()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               Selection.activeGameObject != null &&
               Selection.activeGameObject.GetComponentInChildren<Renderer>(true) != null;
    }

    [MenuItem("Tools/Camion de basura/Aplicar texturas al seleccionado")]
    private static void AplicarSeleccion()
    {
        var materiales = new HashSet<Material>();
        foreach (Renderer renderer in Selection.activeGameObject.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && Perfil(material.name) != null)
                    materiales.Add(material);

        foreach (Material material in materiales)
        {
            Undo.RecordObject(material, "Aplicar texturas del camion");
            AplicarMaterial(material);
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Texturas aplicadas a {materiales.Count} materiales del camion.");
    }

    // Las texturas se comparten entre camiones y conservan el color de cada material.
    public static void AplicarMaterial(Material material)
    {
        string perfil = Perfil(material.name);
        if (perfil == null)
            return;
        if (!AssetDatabase.IsValidFolder(Carpeta))
            AssetDatabase.CreateFolder("Assets", "CamionBasuraTexturas");
        Texture2D color = CargarOGenerar(perfil, false);
        Texture2D superficie = CargarOGenerar(perfil, true);
        string propiedadColor = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
        material.SetTexture(propiedadColor, color);
        material.SetTextureScale(propiedadColor, Vector2.one);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", color);
        if (material.HasProperty("_MetallicGlossMap"))
        {
            material.SetTexture("_MetallicGlossMap", superficie);
            material.SetTextureScale("_MetallicGlossMap", Vector2.one);
            bool urp = material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal);
            material.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 1f);
            if (material.HasProperty("_GlossMapScale"))
                material.SetFloat("_GlossMapScale", 1f);
            if (material.HasProperty("_SmoothnessTextureChannel"))
                material.SetFloat("_SmoothnessTextureChannel", 0f);
        }
    }

    private static string Perfil(string nombre)
    {
        switch (nombre)
        {
            case "Marfil": case "Verde esmeralda": case "Verde oscuro": return "Pintura";
            case "Caucho": return "Caucho";
            case "Acero claro": case "Acero pintado": return "Acero";
            case "Cristal ahumado": return "Cristal";
            case "Blanco de faros": case "Rojo de calaveras": case "Ámbar de seguridad":
            case "Ambar de seguridad": return "Lentes";
            default: return null;
        }
    }

    private static Texture2D CargarOGenerar(string perfil, bool datos)
    {
        string ruta = $"{Carpeta}/{perfil}_{(datos ? "Superficie" : "Color")}.png";
        Texture2D existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (existente != null)
            return existente;
        var textura = new Texture2D(Resolucion, Resolucion, TextureFormat.RGBA32, false, datos);
        var pixeles = new Color32[Resolucion * Resolucion];
        for (int y = 0; y < Resolucion; y++)
            for (int x = 0; x < Resolucion; x++)
            {
                float u = (float)x / Resolucion;
                float v = (float)y / Resolucion;
                float grano = Ruido(x, y);
                float manchas = .5f + .25f * Mathf.Sin(2f * Mathf.PI * (u * 3f + v * 2f)) +
                                 .25f * Mathf.Sin(2f * Mathf.PI * (u * 7f - v * 4f));
                float valor = .96f;
                float metal = 0f;
                float brillo = .4f;
                switch (perfil)
                {
                    case "Pintura":
                        float rayas = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * (u * 43f + v * 2f))), 64f);
                        valor = .91f + .045f * grano - .12f * manchas - .07f * rayas;
                        brillo = .44f - .18f * manchas + .04f * grano;
                        break;
                    case "Caucho":
                        float canal = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * (u * 18f + .25f * Mathf.Sin(v * 8f * Mathf.PI)))), 8f);
                        valor = .82f + .10f * grano - .23f * canal;
                        brillo = .11f + .06f * grano;
                        break;
                    case "Acero":
                        valor = .83f + .12f * grano + .035f * Mathf.Sin(v * 180f * Mathf.PI);
                        metal = .78f;
                        brillo = .48f + .17f * grano;
                        break;
                    case "Cristal":
                        valor = .96f - .045f * manchas + .015f * grano;
                        brillo = .82f - .11f * manchas;
                        break;
                    case "Lentes":
                        float rejilla = Mathf.Abs(Mathf.Sin(u * 32f * Mathf.PI) * Mathf.Sin(v * 32f * Mathf.PI));
                        valor = .84f + .15f * rejilla;
                        brillo = .60f + .17f * rejilla;
                        break;
                }
                pixeles[y * Resolucion + x] = datos
                    ? (Color32)new Color(metal, metal, metal, brillo)
                    : (Color32)new Color(valor, valor, valor, 1f);
            }
        textura.SetPixels32(pixeles);
        textura.Apply();
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.sRGBTexture = !datos;
        importador.alphaIsTransparency = false;
        importador.mipmapEnabled = true;
        importador.wrapMode = TextureWrapMode.Repeat;
        importador.filterMode = FilterMode.Trilinear;
        importador.maxTextureSize = Resolucion;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    private static float Ruido(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x + y * Resolucion) + 731u;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return (h & 65535u) / 65535f;
        }
    }
}
