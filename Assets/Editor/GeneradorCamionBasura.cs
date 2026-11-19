using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class GeneradorCamionBasura
{
    private const string Menu = "Tools/Camion de basura/Generar camion";

    [MenuItem(Menu, true)]
    private static bool PuedeGenerar()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               PrefabStageUtility.GetCurrentPrefabStage() == null;
    }

    [MenuItem(Menu)]
    public static void Generar()
    {
        Shader shader = BuscarShader();
        string carpeta = AssetDatabase.GenerateUniqueAssetPath("Assets/CamionBasura");
        string identificador = AssetDatabase.CreateFolder("Assets", carpeta.Substring("Assets/".Length));
        if (string.IsNullOrEmpty(identificador))
            throw new InvalidOperationException("Unity no pudo crear la carpeta para el camión.");
        carpeta = AssetDatabase.GUIDToAssetPath(identificador);

        GameObject camion = null;
        try
        {
            camion = new GameObject("Camion de basura");
            var modelo = new Modelo(carpeta, shader);
            modelo.Construir(camion.transform);
            string rutaPrefab = carpeta + "/CamionBasura.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(camion, rutaPrefab, InteractionMode.AutomatedAction, out bool guardado);
            if (!guardado)
                throw new InvalidOperationException("Unity no pudo guardar el prefab del camión.");
        }
        catch
        {
            if (camion != null)
                Object.DestroyImmediate(camion);
            AssetDatabase.DeleteAsset(carpeta);
            throw;
        }

        Undo.RegisterCreatedObjectUndo(camion, "Generar camión de basura");
        EditorSceneManager.MarkSceneDirty(camion.scene);
        Selection.activeGameObject = camion;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();
    }

    private static Shader BuscarShader()
    {
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        string tipo = pipeline == null ? "" : pipeline.GetType().Name;
        string nombre = tipo switch
        {
            "UniversalRenderPipelineAsset" => "Universal Render Pipeline/Lit",
            "HDRenderPipelineAsset" => "HDRP/Lit",
            _ => "Standard"
        };
        Shader shader = Shader.Find(nombre);
        if (shader == null || !shader.isSupported)
            throw new InvalidOperationException($"El shader {nombre} no está disponible en este proyecto.");
        return shader;
    }

    private sealed class Modelo
    {
        private readonly string carpeta;
        private readonly Shader shader;
        private readonly Material crema;
        private readonly Material verde;
        private readonly Material verdeOscuro;
        private readonly Material vidrio;
        private readonly Material goma;
        private readonly Material metal;
        private readonly Material cromo;
        private readonly Material faro;
        private readonly Material rojo;
        private readonly Material amarillo;

        public Modelo(string carpeta, Shader shader)
        {
            this.carpeta = carpeta;
            this.shader = shader;
            crema = Pintura("Marfil", 239, 236, 218, .32f);
            verde = Pintura("Verde esmeralda", 28, 131, 84, .39f);
            verdeOscuro = Pintura("Verde oscuro", 17, 83, 58, .32f);
            vidrio = Pintura("Cristal ahumado", 38, 69, 88, .88f);
            goma = Pintura("Caucho", 24, 29, 31, .17f);
            metal = Pintura("Acero pintado", 50, 63, 68, .28f);
            cromo = Pintura("Acero claro", 151, 170, 169, .60f);
            faro = Pintura("Blanco de faros", 240, 246, 237, .45f);
            rojo = Pintura("Rojo de calaveras", 194, 42, 40, .52f);
            amarillo = Pintura("Ámbar de seguridad", 249, 167, 42, .38f);
        }

        public void Construir(Transform raiz)
        {
            Transform chasis = Grupo(raiz, "Chasis", Vector3.zero);
            Caja(chasis, "Bastidor", metal, 0, .84f, -.05f, 1.71f, .36f, 7.26f);
            Caja(chasis, "Depósito", metal, 0, .76f, .81f, 1.92f, .52f, 1.03f);
            Caja(chasis, "Defensa delantera", metal, 0, .78f, 3.81f, 2.52f, .32f, .27f);
            Caja(chasis, "Defensa trasera", metal, 0, .65f, -4.02f, 2.59f, .20f, .26f);
            AgregarCabina(raiz);
            AgregarCaja(raiz);
            AgregarCompuerta(raiz);
            AgregarRuedas(raiz);

            Colisionador(raiz, new Vector3(0, .79f, -.11f), new Vector3(2.60f, .41f, 8.04f));
            Colisionador(raiz, new Vector3(0, 1.99f, 2.69f), new Vector3(2.36f, 2.09f, 2.23f));
            Colisionador(raiz, new Vector3(0, 2.19f, -1.16f), new Vector3(2.52f, 2.43f, 5.60f));
        }

        private void AgregarCabina(Transform raiz)
        {
            Transform cabina = Grupo(raiz, "Cabina", Vector3.zero);
            Perfil(cabina, "Casco", crema, 1.19f, .09f,
                new Vector2(1.56f, 1.03f), new Vector2(3.73f, 1.03f),
                new Vector2(3.86f, 1.36f), new Vector2(3.84f, 1.96f),
                new Vector2(3.55f, 2.88f), new Vector2(3.35f, 3.02f),
                new Vector2(1.72f, 3.02f), new Vector2(1.56f, 2.80f));
            Caja(cabina, "Marco del parabrisas", goma, 0, 2.40f, 3.733f, 2.15f, .87f, .076f, -17.5f);
            Caja(cabina, "Parabrisas", vidrio, 0, 2.40f, 3.777f, 2.04f, .76f, .043f, -17.5f);
            Caja(cabina, "Frente inferior", crema, 0, 1.31f, 3.83f, 2.37f, .43f, .15f);
            Caja(cabina, "Parrilla", metal, 0, 1.44f, 3.917f, 1.32f, .37f, .08f);
            for (int fila = 0; fila < 3; fila++)
                Caja(cabina, "Rejilla horizontal", cromo, 0, 1.34f + fila * .10f, 3.963f, 1.19f, .019f, .015f);

            foreach (int lado in new[] { -1, 1 })
            {
                Caja(cabina, "Faro delantero", metal, lado * .93f, 1.36f, 3.915f, .40f, .30f, .08f);
                Caja(cabina, "Lente", faro, lado * .91f, 1.38f, 3.964f, .29f, .19f, .035f);
                Caja(cabina, "Direccional", amarillo, lado * 1.10f, 1.36f, 3.958f, .065f, .17f, .032f);
                Caja(cabina, "Puerta", crema, lado * 1.186f, 1.87f, 2.51f, .065f, 1.39f, 1.45f);
                Caja(cabina, "Ventana lateral", vidrio, lado * 1.225f, 2.43f, 2.55f, .028f, .67f, 1.49f);
                Caja(cabina, "Manija", metal, lado * 1.234f, 1.99f, 2.03f, .050f, .054f, .20f);
                Caja(cabina, "Estribo", metal, lado * 1.14f, .93f, 1.97f, .29f, .13f, .65f);
                Caja(cabina, "Brazo del espejo", metal, lado * 1.34f, 2.48f, 3.36f, .41f, .074f, .085f);
                Caja(cabina, "Retrovisor", goma, lado * 1.53f, 2.57f, 3.33f, .21f, .36f, .13f);
                Caja(cabina, "Cristal del retrovisor", cromo, lado * 1.53f, 2.57f, 3.255f, .155f, .29f, .025f);
                Caja(cabina, "Base de torreta", metal, lado * .83f, 3.04f, 2.22f, .29f, .09f, .28f);
                Cilindro(cabina, "Torreta ámbar", amarillo, new Vector3(lado * .83f, 3.15f, 2.22f), .12f, .17f, false);
                Caja(cabina, "Limpiaparabrisas", goma, lado * .51f, 2.09f, 3.905f, .64f, .032f, .030f);
            }
        }

        private void AgregarCaja(Transform raiz)
        {
            Transform caja = Grupo(raiz, "Caja compactadora", Vector3.zero);
            Perfil(caja, "Cuerpo de la caja", verde, 1.25f, .12f,
                new Vector2(-3.23f, 1.26f), new Vector2(1.37f, 1.26f),
                new Vector2(1.46f, 1.46f), new Vector2(1.46f, 3.15f),
                new Vector2(1.15f, 3.46f), new Vector2(-3.07f, 3.46f),
                new Vector2(-3.30f, 3.22f), new Vector2(-3.34f, 1.59f));
            foreach (int lado in new[] { -1, 1 })
            {
                Caja(caja, "Larguero inferior", verdeOscuro, lado * 1.23f, 1.43f, -.91f, .13f, .22f, 4.37f);
                Caja(caja, "Larguero superior", verdeOscuro, lado * 1.23f, 3.15f, -.92f, .12f, .13f, 4.37f);
                foreach (float z in new[] { -2.95f, -1.54f, -.10f, 1.19f })
                    Caja(caja, "Nervadura", verdeOscuro, lado * 1.243f, 2.29f, z, .105f, 1.63f, .135f);
                Caja(caja, "Franja reflectante", crema, lado * 1.302f, 1.59f, -.89f, .032f, .082f, 4.13f);
                Caja(caja, "Luz de gálibo", amarillo, lado * 1.302f, 1.61f, 1.30f, .052f, .105f, .17f);
                Caja(caja, "Guardabarros trasero", metal, lado * 1.14f, 1.23f, -1.89f, .62f, .13f, 2.48f);
                Caja(caja, "Salpicadera", goma, lado * 1.14f, .73f, -3.10f, .60f, .83f, .10f);
            }
        }

        private void AgregarCompuerta(Transform raiz)
        {
            Transform pivote = Grupo(raiz, "Compuerta trasera (giro en X)", new Vector3(0, 3.18f, -3.18f));
            Transform tolva = Grupo(pivote, "Tolva y mecanismo", -pivote.localPosition);
            Perfil(tolva, "Cubierta inclinada", verde, 1.23f, .10f,
                new Vector2(-3.31f, 2.33f), new Vector2(-3.20f, 2.39f),
                new Vector2(-3.20f, 3.27f), new Vector2(-3.47f, 3.24f),
                new Vector2(-4.02f, 2.51f), new Vector2(-3.97f, 2.33f));
            Caja(tolva, "Fondo de la tolva", goma, 0, 1.84f, -3.59f, 1.98f, .98f, .11f);
            Caja(tolva, "Placa compactadora", metal, 0, 1.96f, -3.69f, 1.61f, .43f, .10f, -18f);
            Caja(tolva, "Piso inclinado", metal, 0, 1.31f, -3.84f, 1.99f, .16f, .63f, -15f);
            Caja(tolva, "Borde de carga", verdeOscuro, 0, 1.29f, -4.12f, 2.54f, .17f, .15f);
            for (int marca = -4; marca <= 4; marca += 2)
                Caja(tolva, "Reflectante de carga", amarillo, marca * .245f, 1.29f, -4.204f, .215f, .10f, .024f);

            foreach (int lado in new[] { -1, 1 })
            {
                Caja(tolva, "Montante trasero", verdeOscuro, lado * 1.13f, 1.86f, -4.02f, .27f, 1.19f, .25f, 7f);
                Caja(tolva, "Calavera", rojo, lado * 1.13f, 1.92f, -4.178f, .165f, .24f, .042f, 7f);
                Caja(tolva, "Luz de reversa", faro, lado * 1.13f, 1.68f, -4.178f, .16f, .10f, .042f, 7f);
                Caja(tolva, "Bisagra", cromo, lado * 1.17f, 3.17f, -3.21f, .21f, .13f, .20f);
                Barra(tolva, "Cilindro hidráulico", metal,
                    new Vector3(lado * 1.24f, 2.06f, -3.86f), new Vector3(lado * 1.24f, 2.66f, -3.53f), .065f);
                Barra(tolva, "Vástago", cromo,
                    new Vector3(lado * 1.24f, 2.60f, -3.565f), new Vector3(lado * 1.24f, 3.12f, -3.28f), .036f);
            }
        }

        private void AgregarRuedas(Transform raiz)
        {
            Transform ruedas = Grupo(raiz, "Ruedas", Vector3.zero);
            float[] ejes = { 2.64f, -1.33f, -2.53f };
            for (int eje = 0; eje < ejes.Length; eje++)
            {
                foreach (int lado in new[] { -1, 1 })
                {
                    string nombre = $"Eje {eje + 1} - {(lado < 0 ? "izquierda" : "derecha")}";
                    Transform rueda = Grupo(ruedas, nombre, new Vector3(lado * 1.12f, .56f, ejes[eje]));
                    Cilindro(rueda, "Neumático", goma, Vector3.zero, .56f, .31f, true);
                    if (eje > 0)
                        Cilindro(rueda, "Neumático interior", goma, new Vector3(-lado * .35f, 0, 0), .56f, .29f, true);
                    Cilindro(rueda, "Rin", cromo, new Vector3(lado * .158f, 0, 0), .329f, .065f, true);
                    Cilindro(rueda, "Centro", metal, new Vector3(lado * .196f, 0, 0), .218f, .045f, true);
                    Cilindro(rueda, "Cubo", cromo, new Vector3(lado * .228f, 0, 0), .104f, .060f, true);
                    for (int birlo = 0; birlo < 6; birlo++)
                    {
                        float angulo = birlo * Mathf.PI / 3f;
                        var posicion = new Vector3(lado * .228f, Mathf.Cos(angulo) * .16f, Mathf.Sin(angulo) * .16f);
                        Cilindro(rueda, "Birlo", cromo, posicion, .023f, .026f, true);
                    }
                }
            }
        }

        private Material Pintura(string nombre, byte r, byte g, byte b, float brillo)
        {
            var material = new Material(shader) { name = nombre };
            Color color = new Color32(r, g, b, 255);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", brillo);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", brillo);
            TexturasCamionBasura.AplicarMaterial(material);
            AssetDatabase.CreateAsset(material, carpeta + "/" + nombre + ".mat");
            return material;
        }

        // El contorno convexo se define con coordenadas (Z, Y) y se extruye a lo ancho (X).
        private void Perfil(Transform padre, string nombre, Material material, float mitad, float bisel, params Vector2[] contorno)
        {
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            var uv = new List<Vector2>();
            Vector2 centro = Vector2.zero;
            foreach (Vector2 punto in contorno)
                centro += punto;
            centro /= contorno.Length;
            Vector3 centroMalla = new Vector3(0, centro.y, centro.x);
            var anillos = new Vector3[4][];
            for (int anillo = 0; anillo < anillos.Length; anillo++)
            {
                float signo = anillo < 2 ? -1f : 1f;
                bool tapa = anillo == 0 || anillo == 3;
                float x = signo * (mitad - (tapa ? 0f : bisel));
                anillos[anillo] = new Vector3[contorno.Length];
                for (int i = 0; i < contorno.Length; i++)
                {
                    Vector2 punto = tapa ? Vector2.Lerp(contorno[i], centro, .075f) : contorno[i];
                    anillos[anillo][i] = new Vector3(x, punto.y, punto.x);
                }
            }

            Cara(anillos[0]);
            Cara(anillos[3]);
            for (int anillo = 0; anillo < 3; anillo++)
            {
                for (int i = 0; i < contorno.Length; i++)
                {
                    int siguiente = (i + 1) % contorno.Length;
                    Cara(new[] { anillos[anillo][i], anillos[anillo][siguiente],
                        anillos[anillo + 1][siguiente], anillos[anillo + 1][i] });
                }
            }

            var malla = new Mesh { name = nombre };
            malla.SetVertices(vertices);
            malla.SetTriangles(triangulos, 0);
            malla.SetUVs(0, uv);
            malla.RecalculateNormals();
            malla.RecalculateBounds();
            AssetDatabase.CreateAsset(malla, carpeta + "/" + nombre + ".asset");

            Transform pieza = Grupo(padre, nombre, Vector3.zero);
            pieza.gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
            pieza.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;

            void Cara(Vector3[] puntos)
            {
                int inicio = vertices.Count;
                vertices.AddRange(puntos);
                foreach (Vector3 punto in puntos)
                    uv.Add(new Vector2(punto.z + punto.x, punto.y));
                Vector3 normal = Vector3.Cross(puntos[1] - puntos[0], puntos[2] - puntos[0]);
                bool haciaFuera = Vector3.Dot(normal, puntos[0] - centroMalla) > 0;
                for (int i = 1; i < puntos.Length - 1; i++)
                {
                    triangulos.Add(inicio);
                    triangulos.Add(inicio + (haciaFuera ? i : i + 1));
                    triangulos.Add(inicio + (haciaFuera ? i + 1 : i));
                }
            }
        }
    }

    private static Transform Grupo(Transform padre, string nombre, Vector3 posicion)
    {
        Transform grupo = new GameObject(nombre).transform;
        grupo.SetParent(padre, false);
        grupo.localPosition = posicion;
        return grupo;
    }

    private static void Caja(Transform padre, string nombre, Material material, float x, float y, float z,
        float ancho, float alto, float largo, float inclinacion = 0)
    {
        Transform pieza = Primitiva(padre, nombre, material, PrimitiveType.Cube,
            new Vector3(x, y, z), new Vector3(ancho, alto, largo));
        pieza.localRotation = Quaternion.Euler(inclinacion, 0, 0);
    }

    private static Transform Cilindro(Transform padre, string nombre, Material material, Vector3 posicion,
        float radio, float longitud, bool ejeX)
    {
        Transform pieza = Primitiva(padre, nombre, material, PrimitiveType.Cylinder, posicion,
            new Vector3(radio * 2, longitud / 2, radio * 2));
        if (ejeX)
            pieza.localRotation = Quaternion.Euler(0, 0, 90);
        return pieza;
    }

    private static void Barra(Transform padre, string nombre, Material material, Vector3 inicio, Vector3 fin, float radio)
    {
        Vector3 direccion = fin - inicio;
        Transform barra = Cilindro(padre, nombre, material, (inicio + fin) / 2, radio, direccion.magnitude, false);
        barra.localRotation = Quaternion.FromToRotation(Vector3.up, direccion);
    }

    private static Transform Primitiva(Transform padre, string nombre, Material material, PrimitiveType tipo,
        Vector3 posicion, Vector3 escala)
    {
        GameObject objeto = GameObject.CreatePrimitive(tipo);
        objeto.name = nombre;
        Collider colisionador = objeto.GetComponent<Collider>();
        if (colisionador != null)
            Object.DestroyImmediate(colisionador);
        objeto.GetComponent<MeshRenderer>().sharedMaterial = material;
        Transform pieza = objeto.transform;
        pieza.SetParent(padre, false);
        pieza.localPosition = posicion;
        pieza.localScale = escala;
        return pieza;
    }

    private static void Colisionador(Transform padre, Vector3 centro, Vector3 tamano)
    {
        BoxCollider colisionador = padre.gameObject.AddComponent<BoxCollider>();
        colisionador.center = centro;
        colisionador.size = tamano;
    }
}
