using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public GameObject audioSliderPrefab;
    public Transform audioSliderRoot;
    public AudioMixer targetMixer;
    public string[] audioGroupNames = {"Master", "Sound Effects", "Music"};
    public GameObject graphicsDropdownPrefab;
    public Transform graphicsDropdownRoot;
    public string[] graphicsOptionNames = { "antiAliasing", "anisotropicFiltering" };

    //PRIVATE VARIABLES
    private Coroutine sceneLoadRoutine;

    //MACROS
    private readonly string NAME_SETTINGTITLE = "SettingName";
    private readonly string NAME_SETTINGVALUE = "SettingValue";

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

        InitializeVolumeSliders();
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

    public void InitializeVolumeSliders()
    {
        for (int i = 0; i < audioGroupNames.Length; i++)
        {
            string name = audioGroupNames[i];
            GameObject newSetting = Instantiate(audioSliderPrefab, audioSliderRoot);
            RectTransform newTransform = newSetting.GetComponent<RectTransform>();
            newTransform.anchoredPosition = Vector3.down * newTransform.rect.height * i;
            Slider newSlider = newSetting.GetComponentInChildren<Slider>();
            targetMixer.GetFloat(name, out float currentVolume);
            float linearVolume = Mathf.Pow(10f, currentVolume / 20f);
            newSlider.value = linearVolume;
            TMP_Text nameText = newTransform.Find(NAME_SETTINGTITLE).GetComponent<TMP_Text>();
            nameText.text = name.Replace("volume_", "");
            TMP_Text valueText = newTransform.Find(NAME_SETTINGVALUE).GetComponent<TMP_Text>();
            int percentage = Mathf.RoundToInt(linearVolume * 100);
            valueText.text = percentage.ToString() + "%";
            newSlider.onValueChanged.AddListener((float value) => AudioSliderCallback(name, value, valueText));
        }
    }

    public void AudioSliderCallback(string name, float linearValue, TMP_Text display)
    {
        targetMixer.SetFloat(name, Mathf.Log10(linearValue) * 20);
        int percentage = Mathf.RoundToInt(linearValue * 100);
        display.text = percentage.ToString() + "%";
    }
    public void GraphicsOptionCallback(Dropdown dropdown)
    {
        string settingName = dropdown.name;
        int settingValue = dropdown.value;

        switch (settingName)
        {
            case "antiAliasing":
                // Dropdown options: 0=Disabled, 1=2x, 2=4x, 3=8x
                switch (settingValue)
                {
                    case 0: QualitySettings.antiAliasing = 0; break;
                    case 1: QualitySettings.antiAliasing = 2; break;
                    case 2: QualitySettings.antiAliasing = 4; break;
                    case 3: QualitySettings.antiAliasing = 8; break;
                }
                break;
            case "anisotropicFiltering":
                // Dropdown options: 0=Disable, 1=Enable, 2=ForceEnable
                QualitySettings.anisotropicFiltering = (AnisotropicFiltering)settingValue;
                break;
            case "pixelLightCount":
                QualitySettings.pixelLightCount = settingValue;
                break;
            case "shadows":
                // Dropdown options: 0=Disable, 1=HardOnly, 2=All
                QualitySettings.shadows = (ShadowQuality)settingValue;
                break;
            case "shadowResolution":
                // Dropdown options: 0=Low, 1=Medium, 2=High, 3=VeryHigh
                QualitySettings.shadowResolution = (ShadowResolution)settingValue;
                break;
            case "shadowProjection":
                // Dropdown options: 0=CloseFit, 1=StableFit
                QualitySettings.shadowProjection = (ShadowProjection)settingValue;
                break;
            case "shadowCascades":
                // Dropdown options: 0=No Cascades, 1=Two Cascades, 2=Four Cascades
                switch (settingValue)
                {
                    case 0: QualitySettings.shadowCascades = 0; break;
                    case 1: QualitySettings.shadowCascades = 2; break;
                    case 2: QualitySettings.shadowCascades = 4; break;
                }
                break;
            case "shadowDistance":
                QualitySettings.shadowDistance = settingValue;
                break;
            case "skinWeights":
                // Dropdown options: 0=OneBone, 1=TwoBones, 2=FourBones
                QualitySettings.skinWeights = (SkinWeights)settingValue;
                break;
            case "softParticles":
                QualitySettings.softParticles = settingValue != 0;
                break;
            case "softVegetation":
                QualitySettings.softVegetation = settingValue != 0;
                break;
            case "realtimeReflectionProbes":
                QualitySettings.realtimeReflectionProbes = settingValue != 0;
                break;
            case "billboardsFaceCameraPosition":
                QualitySettings.billboardsFaceCameraPosition = settingValue != 0;
                break;
            case "vSyncCount":
                QualitySettings.vSyncCount = settingValue;
                break;
            case "lodBias":
                QualitySettings.lodBias = settingValue;
                break;
            case "maximumLODLevel":
                QualitySettings.maximumLODLevel = settingValue;
                break;
            case "particleRaycastBudget":
                QualitySettings.particleRaycastBudget = settingValue;
                break;
            case "asyncUploadTimeSlice":
                QualitySettings.asyncUploadTimeSlice = settingValue;
                break;
            case "asyncUploadBufferSize":
                QualitySettings.asyncUploadBufferSize = settingValue;
                break;
            case "asyncUploadPersistentBuffer":
                QualitySettings.asyncUploadPersistentBuffer = settingValue != 0;
                break;
            case "resolutionScalingFixedDPIFactor":
                QualitySettings.resolutionScalingFixedDPIFactor = settingValue;
                break;
            default:
                Debug.LogWarning($"Unknown graphics setting: {settingName}");
                break;
        }
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
