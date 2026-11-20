#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Installe automatiquement le modèle OBJ original, sa caméra fixe et une
/// RenderTexture dans la scène Collection. Ne remplace AUCUN ancien script.
/// </summary>
public static class InstallerGuerrier3DFixe
{
    private const string Dossier = "Assets/Modeles3D";
    private const string ModelePath = Dossier + "/Guerrier_Azur_Prototype.obj";
    private const string RenduPath = Dossier + "/Personnage3D_RT.renderTexture";
    private const string MaterialDir = Dossier + "/Materiaux";
    private const int CalqueRendu = 30;
    private const string NomPlateau = "Apercu3D_Guerrier_Fixe";

    private static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
    {
        { "AcierNuit", new Color(.06f,.13f,.25f) },
        { "ArmureAzur", new Color(.14f,.31f,.58f) },
        { "ArmureClaire", new Color(.28f,.48f,.75f) },
        { "OrMetal", new Color(.80f,.52f,.19f) },
        { "OrClair", new Color(1.00f,.79f,.35f) },
        { "AcierClair", new Color(.74f,.81f,.87f) },
        { "Peau", new Color(.80f,.60f,.45f) },
        { "Cheveux", new Color(.25f,.145f,.085f) },
        { "CapeBleue", new Color(.055f,.16f,.33f) },
        { "Cuir", new Color(.16f,.10f,.075f) },
        { "Noir", new Color(.032f,.048f,.077f) },
        { "LueurCyan", new Color(.22f,.78f,.95f) }
    };

    [MenuItem("Projet Game/3D/Installer guerrier fixe dans Encyclopédie")]
    public static void Installer()
    {
        if (SceneManager.GetActiveScene().name != "Collection")
        {
            EditorUtility.DisplayDialog("Projet Game - Personnage 3D",
                "Ouvre d'abord la scène Collection, puis relance le menu d'installation.", "OK");
            return;
        }

        CollectionRefonteUI collection = UnityEngine.Object.FindFirstObjectByType<CollectionRefonteUI>();
        if (collection == null)
        {
            EditorUtility.DisplayDialog("Projet Game - Personnage 3D",
                "CollectionRefonteUI est introuvable. Vérifie que ta scène Collection est ouverte.", "OK");
            return;
        }

        GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelePath);
        if (imported == null)
        {
            EditorUtility.DisplayDialog("Modèle 3D introuvable",
                "Le fichier " + ModelePath + " manque. Copie tout le dossier Assets du ZIP.", "OK");
            return;
        }

        CreerDossier(Dossier);
        CreerDossier(MaterialDir);
        RenderTexture rendu = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenduPath);
        if (rendu == null)
        {
            rendu = new RenderTexture(768, 1152, 24, RenderTextureFormat.ARGB32);
            rendu.name = "Personnage3D_RT";
            rendu.antiAliasing = 4;
            rendu.useMipMap = false;
            AssetDatabase.CreateAsset(rendu, RenduPath);
        }

        var materiaux = CreerMateriaux();
        // Réinstallation propre : aucun doublon de plateau ou de caméra.
        GameObject ancien = GameObject.Find(NomPlateau);
        if (ancien != null) UnityEngine.Object.DestroyImmediate(ancien);

        var plateau = new GameObject(NomPlateau);
        plateau.transform.position = new Vector3(1000f, -100f, 1000f);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(imported);
        instance.name = "Guerrier_Azur_3D";
        instance.transform.SetParent(plateau.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        List<string> ordreMateriaux = LireOrdreMateriaux();
        Dictionary<string, string> materiauxParObjet = LireMateriauxParObjet();
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] existants = renderer.sharedMaterials;
            if (existants.Length == 0) continue;
            Material[] nouveaux = new Material[existants.Length];
            for (int i = 0; i < existants.Length; i++)
            {
                string ancienNom = existants[i] != null ? existants[i].name.Replace(" (Instance)", "").Trim() : "";
                if (!materiaux.TryGetValue(ancienNom, out Material remplacement))
                {
                    // Sur certaines versions de l'importeur OBJ, les matériaux
                    // sont anonymes ; on reprend l'ordre des usemtl du fichier.
                    string depuisObj;
                    if (!materiauxParObjet.TryGetValue(renderer.gameObject.name, out depuisObj))
                        depuisObj = i < ordreMateriaux.Count ? ordreMateriaux[i] : "ArmureAzur";
                    if (!materiaux.TryGetValue(depuisObj, out remplacement))
                        remplacement = materiaux["ArmureAzur"];
                }
                nouveaux[i] = remplacement;
            }
            renderer.sharedMaterials = nouveaux;
            renderer.gameObject.layer = CalqueRendu;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
        AffecterCalqueRecursif(instance.transform, CalqueRendu);

        var cameraGO = new GameObject("Camera3D_Fixe");
        cameraGO.transform.SetParent(plateau.transform, false);
        cameraGO.transform.localPosition = new Vector3(.10f, 1.90f, 7.2f);
        cameraGO.transform.LookAt(plateau.transform.TransformPoint(new Vector3(0f, 1.80f, 0f)));
        var cam = cameraGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 2.14f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.nearClipPlane = .1f;
        cam.farClipPlane = 25f;
        cam.cullingMask = 1 << CalqueRendu;
        cam.targetTexture = rendu;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        cam.depth = -5;

        AjouterLumiere(plateau.transform, "Lumiere_Or_Principale", new Vector3(50f, -38f, 0f),
            new Color(1f,.91f,.74f), 1.55f);
        AjouterLumiere(plateau.transform, "Lumiere_Azur_Secondaire", new Vector3(25f, 145f, 0f),
            new Color(.53f,.73f,1f), .72f);

        ApercuPersonnage3DFixe integration = collection.GetComponent<ApercuPersonnage3DFixe>();
        if (integration == null)
            integration = collection.gameObject.AddComponent<ApercuPersonnage3DFixe>();
        integration.DefinirTexture(rendu);
        EditorUtility.SetDirty(integration);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        EditorUtility.DisplayDialog("Personnage 3D installé",
            "Le guerrier 3D fixe est installé. Lance Play puis Collection > Encyclopédie > Garrick.\n\n" +
            "Le modèle est provisoire et identique dans chaque fiche pour ce premier test. Aucun système de rotation n'a été ajouté.",
            "OK");
        Debug.Log("ProjetGame : personnage 3D fixe, caméra et RenderTexture installés.");
    }

    private static void CreerDossier(string chemin)
    {
        if (AssetDatabase.IsValidFolder(chemin)) return;
        string parent = Path.GetDirectoryName(chemin).Replace('\\','/');
        string name = Path.GetFileName(chemin);
        if (!AssetDatabase.IsValidFolder(parent)) CreerDossier(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Dictionary<string, Material> CreerMateriaux()
    {
        var resultat = new Dictionary<string, Material>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        foreach (KeyValuePair<string, Color> item in Palette)
        {
            string chemin = MaterialDir + "/" + item.Key + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = item.Key;
                AssetDatabase.CreateAsset(mat, chemin);
            }
            // Ajuste la palette sans écraser les autres ressources du projet.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", item.Value);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", item.Value);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",
                item.Key.StartsWith("Or") || item.Key.StartsWith("Acier") ? .52f : .10f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .42f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", .42f);
            EditorUtility.SetDirty(mat);
            resultat[item.Key] = mat;
        }
        return resultat;
    }

    private static List<string> LireOrdreMateriaux()
    {
        var ordre = new List<string>();
        foreach (string ligne in File.ReadAllLines(ModelePath))
        {
            if (!ligne.StartsWith("usemtl ")) continue;
            string nom = ligne.Substring(7).Trim();
            if (!ordre.Contains(nom)) ordre.Add(nom);
        }
        return ordre;
    }

    private static Dictionary<string, string> LireMateriauxParObjet()
    {
        var resultat = new Dictionary<string, string>();
        string objet = null;
        foreach (string ligne in File.ReadAllLines(ModelePath))
        {
            if (ligne.StartsWith("o "))
                objet = ligne.Substring(2).Trim();
            else if (objet != null && ligne.StartsWith("usemtl "))
                resultat[objet] = ligne.Substring(7).Trim();
        }
        return resultat;
    }

    private static void AffecterCalqueRecursif(Transform parent, int calque)
    {
        parent.gameObject.layer = calque;
        for (int i = 0; i < parent.childCount; i++)
            AffecterCalqueRecursif(parent.GetChild(i), calque);
    }

    private static void AjouterLumiere(Transform parent, string nom, Vector3 rotation,
        Color couleur, float intensite)
    {
        var go = new GameObject(nom);
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(rotation);
        var lumiere = go.AddComponent<Light>();
        lumiere.type = LightType.Directional;
        lumiere.color = couleur;
        lumiere.intensity = intensite;
        lumiere.cullingMask = 1 << CalqueRendu;
        lumiere.shadows = LightShadows.None;
    }
}
#endif
