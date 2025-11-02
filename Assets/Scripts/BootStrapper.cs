using UnityEngine;
using UnityEngine.SceneManagement;

public class BootStrapper : MonoBehaviour
{
    [SerializeField] private GameObject UIRootPrefab;
    [SerializeField] private GameObject managersPrefab;

    [SerializeField] private int vSyncount = 0;
    [SerializeField] private int targetFrameRate = 60;

    private void Awake()
    {
        QualitySettings.vSyncCount = vSyncount;
        Application.targetFrameRate = targetFrameRate;

        GameObject managersInstance = Instantiate(managersPrefab);
        DontDestroyOnLoad(managersInstance);

        GameObject ui = Instantiate(UIRootPrefab);
        DontDestroyOnLoad(ui);

        SceneManager.LoadScene(GameManager.menuSceneName);
    }
}
