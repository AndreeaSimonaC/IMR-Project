
using System.Collections.Generic;
using UnityEngine;

public class HandGloveBinder : MonoBehaviour
{
    [SerializeField] private Transform wrist;
    [SerializeField] private SkinnedMeshRenderer gloveMain;
    [SerializeField] private SkinnedMeshRenderer gloveStripes;

    [ContextMenu("Bind Gloves To XR Skeleton")]
    public void BindGloves()
    {
        if (wrist == null || gloveMain == null || gloveStripes == null)
        {
            Debug.LogError("HandGloveBinder: Missing references!", this);
            return;
        }

        var skeleton = new Dictionary<string, Transform>();

        foreach (Transform bone in wrist.GetComponentsInChildren<Transform>(true))
        {
            if (!skeleton.TryAdd(bone.name, bone))
            {
                Debug.LogWarning($"Duplicate XR bone name: {bone.name}", this);
            }
        }
        BindRenderer(gloveMain, skeleton);
        BindRenderer(gloveStripes, skeleton);
    }

    private void BindRenderer(
        SkinnedMeshRenderer renderer,
        Dictionary<string, Transform> skeleton)
    {
        Transform[] originalBones = renderer.bones;

        if (originalBones == null || originalBones.Length == 0)
        {
            Debug.LogError($"{renderer.name}: No source bones found!", renderer);
            return;
        }

        Transform[] newBones = new Transform[originalBones.Length];

        for (int i = 0; i < originalBones.Length; i++)
        {
            Transform sourceBone = originalBones[i];

            if (sourceBone == null ||
                !skeleton.TryGetValue(sourceBone.name, out newBones[i]))
            {
                Debug.LogError($"{renderer.name}: Missing matching bone at index {i}: " + $"{(sourceBone != null ? sourceBone.name : "NULL")}", renderer);
                return;
            }
        }

        #if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(renderer, "Bind XR Glove Bones");
        #endif

        renderer.bones = newBones;
        renderer.rootBone = wrist;

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(renderer);
        #endif

        Debug.Log($"{renderer.name}: Bound {newBones.Length} bones successfully!", renderer);
    }
}
