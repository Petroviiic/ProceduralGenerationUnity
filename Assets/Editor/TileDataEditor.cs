using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using System.IO;
using static Codice.CM.Common.Serialization.PacketFileReader;

[CustomEditor(typeof(TileData), false)]
public class TileDataEditor : Editor
{
    private TileData tileData => target as TileData;
    private DefaultAsset tileFolder;
    private string path;

    public override void OnInspectorGUI()
    {
        tileFolder = (DefaultAsset)EditorGUILayout.ObjectField("Tile Folder", tileFolder, typeof(DefaultAsset), false);

        if (tileFolder != null)
        {
            path = AssetDatabase.GetAssetPath(tileFolder);
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorGUILayout.HelpBox("Selected asset is not a folder.", MessageType.Warning);
                tileFolder = null;
                path = string.Empty;
            }
        }


        if (GUILayout.Button("Browse for tile folder"))
        {
            path = EditorUtility.OpenFolderPanel("Tile Folder", "", "");
            
            if (path != null && path != "" && path.StartsWith("Resources/"))
            {
                tileFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath(path, typeof(DefaultAsset));
            }
            else if (path != null && path != "")
            {
                tileFolder = null;
            }
        }

        if (GUILayout.Button("Generate tile data"))
        {
            GenerateTileData();
        }

    }

    public void GenerateTileData()
    {
        string[] files = Directory.GetFiles(path);

        byte[] fileData;
        Texture2D tex2D;
        foreach (string file in files)
        {
            if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg") || file.EndsWith(".psd"))
            {
                fileData = File.ReadAllBytes(file);
                tex2D = new Texture2D(2, 2);           // Create new "empty" texture
                if (tex2D.LoadImage(fileData))           // Load the imagedata into the texture (size is set automatically)
                {
                    Debug.Log("moze");
                }
                else
                {
                    Debug.Log("ne moze");
                }
            }
        }
    }
}
