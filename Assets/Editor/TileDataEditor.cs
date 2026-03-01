using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using System.IO;
using static Codice.CM.Common.Serialization.PacketFileReader;
using NUnit.Framework;

[CustomEditor(typeof(TileData), false)]
public class TileDataEditor : Editor
{
    private TileData tileData => target as TileData;
    
    private DefaultAsset tileFolder;
    private string path;


    private int colorMappingIndex = 1;
    private Dictionary<Color, int> colorMapping = new Dictionary<Color, int>();
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


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
            if (path != null && path != "")
            {
               if (EditorUtility.DisplayDialog("Are you sure?",
              "This action cannot be undone. All previosly generated data will be overriden.",
              "Continue", "Cancel"))
                {
                    GenerateTileData();
                }
            }
            else
                Debug.Log("Image root folder is not defined!");
        }

    }
    private List<Sprite> LoadSprites()
    {
        List<Sprite> sprites = new List<Sprite>();
        // If selected folder is in Resources folder use built-in loading 
        //if (path.StartsWith(Application.dataPath + "/Resources/") || path.StartsWith("Assets/Resources/"))
        if (false)
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
                        sprite.name = file;
                        AssetDatabase.AddObjectToAsset(sprite, tileData);
                        AssetDatabase.AddObjectToAsset(tex2D, tileData);
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

    private void DeleteAssets()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            string[] guids = AssetDatabase.FindAssets("t:" + tileData);
            int deletedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    // Delete the asset file at the path
                    if (AssetDatabase.DeleteAsset(path))
                    {
                        deletedCount++;
                        Debug.Log("Deleted asset: " + path);
                    }
                    else
                    {
                        Debug.LogError("Could not delete asset: " + path);
                    }
                }
            }

            Debug.Log(deletedCount + " assets of type " + tileData + " deleted.");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }
    }

    public void GenerateTileData()
    {
        DeleteAssets();

        tileData.Sprites.Clear();
        tileData.SpriteMarks.Clear();

        List<Sprite> sprites = LoadSprites();
        if (sprites.Count == 0)
        {
            Debug.LogError("Zero sprites loaded");
            return;
        }
        

        colorMappingIndex = 1;
        List<Marks> spriteMarks = new List<Marks>();

        Vector2 spriteSize = sprites[0].rect.size;

        Vector2 offset = spriteSize / (tileData.colorDiversity * 2);
        
        List<Sprite> rotatedSprites = new List<Sprite>();
        List<Marks> rotatatedMarksList = new List<Marks>();
        foreach (Sprite sprite in sprites)
        {
            int[] marks = ProcessColorMarks(offset, sprite, spriteSize);

            spriteMarks.Add(new Marks { marks = marks});

            if (marks[0] == marks[1] && marks[1] == marks[2] && marks[2] == marks[3])
                continue;

            Color32[] original = sprite.texture.GetPixels32();
            int size = sprite.texture.width;
            for (int n = 0; n < 3; n++)
            {
                Color32[] rotated = RotateTexture(original, size);

                Texture2D texture = new Texture2D(size, size, sprite.texture.format, false);
                texture.filterMode = FilterMode.Point;
                texture.SetPixels32(rotated);
                texture.Apply();
                Sprite rotation = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                rotation.name = sprite.name + "rotation_" + ((n + 1) * 90).ToString();

                AssetDatabase.AddObjectToAsset(rotation, tileData);
                AssetDatabase.AddObjectToAsset(texture, tileData);
                rotatedSprites.Add(rotation);         

                int[] rotatedMarks = ProcessColorMarks(offset, rotation, spriteSize);

                original = rotated;

                rotatatedMarksList.Add(new Marks { marks = rotatedMarks});
            }

        }
        sprites.AddRange(rotatedSprites);
        spriteMarks.AddRange(rotatatedMarksList);

        //hideFlags = HideFlags.HideAndDontSave;

        tileData.SpriteMarks = new List<Marks>(spriteMarks);
        tileData.Sprites = new List<Sprite>(sprites);

        EditorUtility.SetDirty(tileData);
        AssetDatabase.SaveAssets();

        Debug.Log(tileData.SpriteMarks.Count);
    }

    private int[] ProcessColorMarks(Vector2 offset, Sprite sprite, Vector2 spriteSize)
    {
        int[] marks = new int[4];
        string[] marksString = new string[4];
        int x = 0, y = 0;
        for (int side = 0; side < 4; side++)    // origin corner: bottom-left;  directions : up, right, down, left
        {


            //TODO - for (int step = 0; step < tileData.colorDiversity; step++)   ali kad popravis sve ostalo
            for (int i = 0; i < tileData.colorDiversity; i++)
            {
                // int step = (side == 0 || side == 1) ? i : (colorDiversity - 1 - i);
                int step = (side == 0 || side == 1) ? i : i;

                if (side == 0) //bottom - top
                {
                    y = (int)(offset.y + step * spriteSize.y / tileData.colorDiversity);
                    x = (int)(offset.y);
                }
                else if (side == 1) //left - right
                {
                    x = (int)(offset.x + step * spriteSize.x / tileData.colorDiversity);
                    y = (int)(offset.y + (tileData.colorDiversity - 1) * spriteSize.y / tileData.colorDiversity);
                }
                else if (side == 2) //top - bot
                {
                    y = (int)(offset.y + step * spriteSize.y / tileData.colorDiversity);
                    x = (int)(offset.x + (tileData.colorDiversity - 1) * spriteSize.x / tileData.colorDiversity);
                }
                else if (side == 3) //right - left
                {
                    x = (int)(offset.x + step * spriteSize.x / tileData.colorDiversity);
                    y = (int)(offset.y);
                }
                Color32 color = sprite.texture.GetPixel(x, y);
                color.r = (byte)((color.r / 8) * 8);
                color.g = (byte)((color.g / 8) * 8);
                color.b = (byte)((color.b / 8) * 8);
                if (!colorMapping.ContainsKey(color))
                {
                    colorMapping[color] = colorMappingIndex++;
                }
                marks[side] = (int)(marks[side] * 31 + colorMapping[color]) % 1000000;
                marksString[side] += colorMapping[color];

            }
        }
        //  Debug.Log((sprite.name + ": " + marksString[0] + " " + marksString[1] + " " + marksString[2] + " " + marksString[3]));
        return marks;
    }

    private Color32[] RotateTexture(Color32[] original, int size)
    {
        //rotates clockwise
        Color32[] ret = new Color32[size * size];

        for (int j = 0; j < size; j++)
        {
            for (int i = 0; i < size; i++)
            {
                ret[(i + 1) * size - j - 1] = original[original.Length - 1 - (j * size + i)];
            }
        }

        return ret;
    }
}