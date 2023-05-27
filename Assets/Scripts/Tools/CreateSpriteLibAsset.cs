using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;
using UnityEditor.Animations;

public class CreateSpriteLibAsset : EditorWindow
{
    string texturesFolder = "";
    private string _spritesRoot = "Assets/Sprites 1/";
    private string _prefabsFolder = "Assets/Prefabs";
    private string _animationsFolder = "Assets/Animation";
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
        if (GUILayout.Button("All"))
        {
            if (string.IsNullOrEmpty(texturesFolder)) {
                Debug.Log("texturesFolder is empty");
                return;
            }
            GenerateAssetLibrary();
            GeneratePrefab();
        }
        if (GUILayout.Button("AssetLibrary"))
        {
            if (string.IsNullOrEmpty(texturesFolder)) {
                Debug.Log("texturesFolder is empty");
                return;
            }
            GenerateAssetLibrary();
        }
        if (GUILayout.Button("GeneratePrefab"))
        {
            if (string.IsNullOrEmpty(texturesFolder)) {
                Debug.Log("texturesFolder is empty");
                return;
            }
            GeneratePrefab();
        }
    }

    private void GeneratePrefab()
    {
        if (!AssetDatabase.IsValidFolder(_prefabsFolder + "/" + texturesFolder))
            AssetDatabase.CreateFolder(_prefabsFolder, texturesFolder);
        string prefabPath = _prefabsFolder + "/" + texturesFolder + "/" + texturesFolder + ".prefab";
        AssetDatabase.DeleteAsset(prefabPath);
        var go = new GameObject();
        var animController = GenerateAnimController();
        go.name = texturesFolder;
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = animController;
        var spriteResolver = go.AddComponent<SpriteResolver>();
        var spriteLibraryPath = _spritesRoot + "SpriteLibraries/" + texturesFolder + ".asset";
        var spriteLibraryAsset = (SpriteLibraryAsset)AssetDatabase.LoadAssetAtPath(spriteLibraryPath, typeof(SpriteLibraryAsset));
        var spriteLibrary = go.AddComponent<SpriteLibrary>();
        spriteLibrary.spriteLibraryAsset = spriteLibraryAsset;
        spriteResolver.SetCategoryAndLabel("idle", "0");
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out bool success);
        if (!success)
            Debug.Log($"Can't save prefab {prefabPath}");
        //GameObject.DestroyImmediate(go);
    }

    private AnimatorController GenerateAnimController(){
        if (!AssetDatabase.IsValidFolder(_animationsFolder + "/" + texturesFolder))
            AssetDatabase.CreateFolder(_animationsFolder, texturesFolder);
        var controllerPath = _animationsFolder + "/" + texturesFolder + "/" + texturesFolder + ".controller";
        AssetDatabase.DeleteAsset(controllerPath);

        const string isWalking = "IsWalking";
        const string roll = "Roll";
        
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter(isWalking, AnimatorControllerParameterType.Bool);
        controller.AddParameter(roll, AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Punch", AnimatorControllerParameterType.Trigger);

        var idleState = controller.layers[0].stateMachine.AddState("Idle");
        var walkState = controller.layers[0].stateMachine.AddState("Walk");
        var rollState = controller.layers[0].stateMachine.AddState(roll);

        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, isWalking);

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, isWalking);

        var walkToRoll = walkState.AddTransition(rollState);
        walkToRoll.AddCondition(AnimatorConditionMode.If, 0, roll);

        var rollToWalk = rollState.AddTransition(walkState);
        rollToWalk.AddCondition(AnimatorConditionMode.If, 0, isWalking);
        rollToWalk.hasExitTime = true;

        var rollToIdle = rollState.AddTransition(idleState);
        rollToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, isWalking);
        rollToIdle.hasExitTime = true;

        var idleToRoll = idleState.AddTransition(rollState);
        idleToRoll.AddCondition(AnimatorConditionMode.If, 0, roll);

        return controller;
    }

    private void GenerateAssetLibrary()
    {
        var fullPath = _spritesRoot + texturesFolder;
        var assetName = _spritesRoot + "SpriteLibraries/" + texturesFolder + ".asset";
        AssetDatabase.DeleteAsset(assetName);
        var searchResult = AssetDatabase
            .FindAssets("t:Texture2D", new string[] { fullPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToList();
        if (searchResult.Any())
        {
            var groups = searchResult.GroupBy(sprite =>
            {
                var match = _groupsRegEx.Match(sprite);
                return match.Groups[1].Value;
            },
            element =>
            {
                var match = _groupsRegEx.Match(element);
                return match.Groups[2].Value + ';' + element;
            }).ToDictionary(g => g.Key, g => g.ToList());
            var spriteLib = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            foreach (var (k, v) in groups)
            {
                foreach (var str in v.OrderBy(el =>
                {
                    var idx = el.IndexOf(';');
                    var i = int.Parse(el.Substring(0, idx));
                    return i;
                }))
                {
                    var values = str.Split(';');
                    var t = (Sprite)AssetDatabase.LoadAssetAtPath(values[1], typeof(Sprite));
                    Debug.Log($"Creating category = {k} label = {values[0]} sprite = {t.GetType()}");
                    spriteLib.AddCategoryLabel(t, k, values[0]);
                }
            }
            AssetDatabase.CreateAsset(spriteLib, assetName);
        }
        else
        {
            Debug.Log($"Sprites in folder {fullPath} not found");
        }
    }
}