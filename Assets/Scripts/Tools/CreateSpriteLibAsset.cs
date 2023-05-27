using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;

public class CreateSpriteLibAsset : EditorWindow
{
    string texturesFolder = "Sprites folder (in Assets/Sprites 1/)";
    private string _spritesRoot = "Assets/Sprites 1/";
    private static Regex _groupsRegEx = new Regex(@".*\/([a-zA-Z]*)_([0-9])*.png", RegexOptions.Compiled);

    // Add menu named "My Window" to the Window menu
    [MenuItem("Tools/Create SpriteLib Asset")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        CreateSpriteLibAsset window = (CreateSpriteLibAsset)EditorWindow.GetWindow(typeof(CreateSpriteLibAsset));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        texturesFolder = EditorGUILayout.TextField("Sprites Folder", texturesFolder);
        if (GUILayout.Button("Ok")) {
            var fullPath = _spritesRoot + texturesFolder;
            var searchResult = AssetDatabase
                .FindAssets("t:Texture2D", new string[]{fullPath})
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToList();
            if (searchResult.Any()){
                var groups = searchResult.GroupBy(sprite => {
                    var match = _groupsRegEx.Match(sprite);
                    return match.Groups[1].Value;
                },
                element => {
                    var match = _groupsRegEx.Match(element);
                    return match.Groups[2].Value + ';' + element;
                }).ToDictionary(g => g.Key, g => g.ToList());
                var spriteLib = ScriptableObject.CreateInstance<SpriteLibraryAsset>();                
                foreach (var (k, v) in groups)
                {
                    foreach(var str in v.OrderBy(el => {
                        var idx = el.IndexOf(';');
                        var i = int.Parse(el.Substring(0, idx));
                        return i;
                    })){
                        var values = str.Split(';');
                        var t = (Sprite)AssetDatabase.LoadAssetAtPath(values[1], typeof(Sprite));
                        Debug.Log($"Creating category = {k} label = {values[0]} sprite = {t.GetType()}");
                        spriteLib.AddCategoryLabel(t, k, values[0]);
                    }
                }
                AssetDatabase.CreateAsset(spriteLib, _spritesRoot + "SpriteLibraries/" + texturesFolder + ".asset");
            }
            else {
                Debug.Log($"Sprites in folder {fullPath} not found");
            }
        }
    }
}