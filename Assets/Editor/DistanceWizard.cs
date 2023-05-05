using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DistanceWizard : ScriptableWizard
{
    public Transform referenceSpace;
    public Transform object1;
    public Transform object2;

    [MenuItem("GameObject/Distance")]
    static void CreateWizard()
    {
        DistanceWizard dw = ScriptableWizard.DisplayWizard<DistanceWizard>("Measure Distance", "Done");

        if (Selection.gameObjects.Length == 2)
        {
            dw.object1 = Selection.gameObjects[0].transform;
            dw.object2 = Selection.gameObjects[1].transform;
        }
        else
        {
            dw.referenceSpace = dw.object1 = Selection.activeGameObject.transform;
        }

        dw.OnWizardUpdate();
    }

    void OnWizardCreate()
    {
        
    }

    void OnWizardUpdate()
    {
        if (object1 == null || object2 == null) return;

        if (referenceSpace != null)
        {
            float d = (referenceSpace.InverseTransformPoint(object1.position) - referenceSpace.InverseTransformPoint(object2.position)).magnitude;
            Debug.Log("Distance between " + object1 + " and " + object2 + " : " + d);
        }
        else
        {
            float d = (object1.position - object2.position).magnitude;
            Debug.Log("Distance between " + object1 + " and " + object2 + " : " + d);
        }
    }

    void OnWizardOtherButton()
    {

    }
}