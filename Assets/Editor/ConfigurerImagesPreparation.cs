#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ConfigurerImagesPreparation
{
    [MenuItem("Projet Game/Configurer les images de preparation")]
    private static void Configurer()
    {
        string[] chemins = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/PreparationUI" });
        int modifie = 0;
        foreach (string guid in chemins)
        {
            string chemin = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter texture = AssetImporter.GetAtPath(chemin) as TextureImporter;
            if (texture == null || texture.textureType == TextureImporterType.Sprite)
                continue;

            texture.textureType = TextureImporterType.Sprite;
            texture.spriteImportMode = SpriteImportMode.Single;
            texture.mipmapEnabled = false;
            texture.SaveAndReimport();
            modifie++;
        }
        AssetDatabase.Refresh();
        Debug.Log("Preparation combat : " + modifie + " image(s) configuree(s) comme Sprite (2D and UI).");
    }
}
#endif
