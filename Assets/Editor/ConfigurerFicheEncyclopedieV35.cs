#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ConfigurerFicheEncyclopedieV35
{
    [MenuItem("Projet Game/Collections/Configurer images fiche V3.5")]
    public static void Configurer()
    {
        string dossier = "Assets/Resources/CollectionUI/Encyclopedie";
        string[] ids = AssetDatabase.FindAssets("t:Texture2D", new[] { dossier });
        int configures = 0;
        foreach (string id in ids)
        {
            string chemin = AssetDatabase.GUIDToAssetPath(id);
            var importer = AssetImporter.GetAtPath(chemin) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            configures++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Encyclopedie V3.5 : " + configures +
                  " images configurees en Sprite (2D and UI). Lance Play.");
    }
}
#endif
