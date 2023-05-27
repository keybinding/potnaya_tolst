using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;

public static class SpriteLibCreator
{
    [MenuItem("Tools/Create Sprite Lib")]
    static void CreateLib()
    {
        const string spriteLibName = "MySpriteLib.asset";
        
        var spriteLib = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
        spriteLib.AddCategoryLabel(null, "Cat", "Label");
        
            
        AssetDatabase.CreateAsset(spriteLib, "Assets/" + spriteLibName);
        
    }
}

