using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D.Animation;

public class Something : EditorWindow
{
    // Add menu item
    [MenuItem("CONTEXT/Rigidbody/Do Something")]
    static void DoSomething(MenuCommand command)
    {
        Rigidbody body = (Rigidbody)command.context;
        body.mass = 5;
        Debug.Log("Changed Rigidbody's Mass to " + body.mass + " from Context Menu...");
    }
}