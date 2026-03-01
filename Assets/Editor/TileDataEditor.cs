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
        // Drag and drop folder
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


        // Select folder using file explorer
        if (GUILayout.Button("Browse for tile folder"))
        {
            path = EditorUtility.OpenFolderPanel("Tile Folder", "", "");
            
            if (path != null && path != "" && path.StartsWith(Application.dataPath + "/Resources/"))
            {
                tileFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath("Assets" + path.Substring(path.LastIndexOf("/Resources")), typeof(DefaultAsset));
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
    private List<Sprite> LoadSprites()
    {
        List<Sprite> sprites = new List<Sprite>();

        // If selected folder is in Resources folder use built-in loading 
        if (path.StartsWith(Application.dataPath + "/Resources/") || path.StartsWith("Assets/Resources/"))
        {
            foreach (Sprite item in Resources.LoadAll<Sprite>(path.Substring(path.LastIndexOf("/Resources") + 11)))
            {
                sprites.Add(item);
            }
        }
        else
        {
            // Selected folder is somewhere else, so load images manually
            string[] files = Directory.GetFiles(path);

            byte[] fileData;
            Texture2D tex2D;
            foreach (string file in files)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".jpeg") || file.EndsWith(".psd"))
                {
                    fileData = File.ReadAllBytes(file);
                    tex2D = new Texture2D(2, 2);           
                    if (tex2D.LoadImage(fileData))           
                    {
                        Debug.Log("Image processed! " + file);
                        Sprite sprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f));
                        sprites.Add(sprite);
                    }
                    else
                    {
                        Debug.Log("Couldn't process image " + file);
                    }
                }
            }
        }


        Debug.Log("Loaded: " + sprites.Count + " sprites.");
        return sprites;
    }
    public void GenerateTileData()
    {
        LoadSprites();
    }
}
