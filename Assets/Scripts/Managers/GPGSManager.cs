using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPGSManager : MonoBehaviour
{
    public static GPGSManager Instance { get; private set; }

    private const string cloudSaveFilename = "diving_colors_cloud_save";

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
    }

}
