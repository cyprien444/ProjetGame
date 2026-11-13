#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigurerCollectionRefonte
{
    [MenuItem("Projet Game/Collections/1 - Configurer les images")]
    public static void ConfigurerImages()
    {
        string[] chemins = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/CollectionUI" });
        int nb = 0;
        foreach (string guid in chemins)
        {
            string chemin = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(chemin) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 100f;
            if (chemin.Contains("panneau_codex") || chemin.Contains("bouton_") ||
                chemin.Contains("cadre_portrait") || chemin.Contains("emplacement_relique"))
                importer.spriteBorder = new Vector4(18,18,18,18);
            importer.SaveAndReimport();
            nb++;
        }
        AssetDatabase.Refresh();
        Debug.Log("CollectionRefonte : " + nb + " images configurees comme Sprites.");
        EditorUtility.DisplayDialog("Projet Game", "Images de collection configurees : " + nb, "OK");
    }

    [MenuItem("Projet Game/Collections/2 - Installer sur la scene Collection")]
    public static void Installer()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Collection")
        {
            EditorUtility.DisplayDialog("Projet Game",
                "Ouvre d'abord la scene Collection dans Assets/Ecrans.", "OK");
            return;
        }
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Projet Game", "Canvas introuvable dans Collection.", "OK");
            return;
        }
        if (canvas.GetComponent<CollectionRefonteUI>() == null)
        {
            Undo.AddComponent<CollectionRefonteUI>(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Selection.activeGameObject = canvas.gameObject;
        EditorUtility.DisplayDialog("Projet Game",
            "CollectionRefonteUI ajoute sur Canvas. Enregistre la scene avec Ctrl+S, puis teste Play.", "OK");
    }
}
#endif
