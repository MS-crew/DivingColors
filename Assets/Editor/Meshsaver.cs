#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class Meshsaver : MonoBehaviour
{
    [MenuItem("DivingColors/SaveMesh")]
    public static void SaveSelectedMeshes()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var mf = obj.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh)
                continue;

            string path = "Assets/Meshes/" + obj.name + ".asset";
            AssetDatabase.CreateAsset(Instantiate(mf.sharedMesh), path);
            AssetDatabase.SaveAssets();
            Debug.Log("Saved mesh for: " + obj.name);
        }

        AssetDatabase.Refresh();
    }
}
#endif