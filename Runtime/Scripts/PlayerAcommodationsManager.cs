using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerAcommodationsManager : MonoBehaviour
{
    public static PlayerAcommodationsManager Instance { get; private set; }

    [Header("Scenes")]
    [Tooltip("Load this scene when clicking Play, unless save data overwrites this")]
    public string defaultGameScene = "PlayerAcommodationsSampleGameScene";

    [Header("Loading Screen(optional)")]
    [Tooltip("Prefab to enable when loading a new scene")]
    public GameObject loadingScreen;
    [Tooltip("GameObject to spin during the loading screen")]
    public GameObject loadSpinner;
    [Tooltip("Speed at which to spin the logo during loading screens")]
    public float loadSpinnerSpeed = 1;

    [Header("Settings")]
    [Tooltip("Settings panel GameObjects")]
    public GameObject[] settingsPanels;

    //PRIVATE VARIABLES
    private Coroutine sceneLoadRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayerAcommGlobal.activeGameScene = defaultGameScene;
    }

    public void SetActiveGameScene(string sceneName) => PlayerAcommGlobal.activeGameScene=sceneName;

    public void LoadActiveGameScene()
    {
        if (sceneLoadRoutine == null)
            StartCoroutine(LoadSceneInternal(PlayerAcommGlobal.activeGameScene));
    }

    public void LoadSceneByName(string sceneName)
    {
        if (sceneLoadRoutine == null)
            StartCoroutine(LoadSceneInternal(sceneName));
    }

    public void LoadActiveSettingsPanel()
    {
        for (int i = 0; i < settingsPanels.Length; i++)
            settingsPanels[i].SetActive(i == PlayerAcommGlobal.activeSettingsPanel);
    }

    public void LoadSettingsPanel(int index)
    {
        PlayerAcommGlobal.activeSettingsPanel = index;
        LoadActiveSettingsPanel();
    }

    private IEnumerator LoadSceneInternal(string sceneName)
    {
        loadingScreen?.SetActive(true);
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        while (!async.isDone)
        {
            yield return null;
            loadSpinner.transform.Rotate(transform.forward, loadSpinnerSpeed);
        }
        loadingScreen?.SetActive(false);
    }
}

public static class PlayerAcommGlobal
{
    public static string activeGameScene = "";
    public static int activeSettingsPanel = 0;
}
