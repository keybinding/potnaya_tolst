using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;
using UnityEditor.Animations;
using System.Collections.Generic;
using System;

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
        go.name = texturesFolder;
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        
        var spriteResolver = go.AddComponent<SpriteResolver>();
        var spriteLibraryPath = _spritesRoot + "SpriteLibraries/" + texturesFolder + ".asset";
        var spriteLibraryAsset = (SpriteLibraryAsset)AssetDatabase.LoadAssetAtPath(spriteLibraryPath, typeof(SpriteLibraryAsset));
        var spriteLibrary = go.AddComponent<SpriteLibrary>();
        spriteLibrary.spriteLibraryAsset = spriteLibraryAsset;
        spriteResolver.SetCategoryAndLabel("idle", "0");
        var animator = go.AddComponent<Animator>();
        animator.enabled = false;
        var animController = GenerateAnimController(spriteLibraryAsset);
        animator.runtimeAnimatorController = animController;
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out bool success);
        if (!success)
            Debug.Log($"Can't save prefab {prefabPath}");
        //GameObject.DestroyImmediate(go);
    }

    private AnimatorController GenerateAnimController(SpriteLibraryAsset spriteLibrary){
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
        Dictionary<string, AnimatorState> categoriesToStates = new Dictionary<string, AnimatorState>();
        foreach(var c in spriteLibrary.GetCategoryNames().ToList()){
            var state = controller.layers[0].stateMachine.AddState(c);
            var clip = generateAnimationClip(c, spriteLibrary);
            state.motion = clip;
            var animPath = _animationsFolder + "/" + texturesFolder + "/" + c + ".anim";
            AssetDatabase.DeleteAsset(animPath);
            AssetDatabase.CreateAsset(clip, animPath);
            categoriesToStates.Add(c, state);
        }
        var idleState = categoriesToStates["idle"];
        var walkState = categoriesToStates["walk"];
        var rollState = categoriesToStates["roll"];

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

    private AnimationClip generateAnimationClip(string clipName, SpriteLibraryAsset spriteLibrary) {
        var clip = new AnimationClip();
        
        var animationCurves = new List<AnimationCurve>();
        var labels = spriteLibrary.GetCategoryLabelNames(clipName).ToList();
        var curve = new AnimationCurve();
        for (int i = 0; i < labels.Count; ++i){
            var value = GetHash($"{clipName}_{labels[i]}");
            var fvalue = BitConverter.ToSingle(BitConverter.GetBytes(value));
            Debug.Log($"Hash = {value} Label = {labels[i]} Category = {clipName}");
            curve.AddKey(i * 5 / 60f, fvalue);
        }
        if (labels.Count > 0) {
            var value = GetHash($"{clipName}_{labels[0]}");
            var fvalue = BitConverter.ToSingle(BitConverter.GetBytes(value));
            curve.AddKey(labels.Count * 5 / 60f, fvalue);
        }
        for(var i = 0; i < labels.Count + 1; i++)
        {
            AnimationUtility.SetKeyBroken(curve, i, true);
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        clip.SetCurve("", typeof(SpriteResolver), "m_SpriteKey", curve);
        return clip;
    }

    static int Bit30Hash_GetStringHash(string value)
    {
            var hash = Animator.StringToHash(value);
            hash = PreserveFirst30Bits(hash);
            return hash;
    }
     
    static int PreserveFirst30Bits(int input)
    {
            const int mask = 0x3FFFFFFF;
            return input & mask;
    }


    private int GetHash(string value){
        var hash = Animator.StringToHash(value);
        var bytes = BitConverter.GetBytes(hash);
        var exponentialBit = BitConverter.IsLittleEndian ? 3 : 1;
        if (bytes[exponentialBit] == 0xFF)
            bytes[exponentialBit] -= 1;
        return BitConverter.ToInt32(bytes, 0);
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