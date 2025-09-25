using System;
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

    [Header("Main Menu")]
    [Tooltip("Load this scene when clicking Play, unless save data overwrites this")]
    public string defaultGameScene = "PlayerAcommodationsSampleGameScene";
    [Tooltip("Name of the main menu scene")]
    public string mainMenuScene = "PlayerAcommodationsSampleScene";
    [Tooltip("List of menus to switch between")]
    public GameObject[] Menus;
    [Tooltip("Name of the main menu")]
    public string mainMenuName = "MainMenu";
    [Tooltip("Name of the pause menu")]
    public string pauseMenuName = "PauseMenu";

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
    [Tooltip("Index of video settings panel")]
    public int videoPanelIndex;
    [Tooltip("Video Preview Object")]
    public GameObject videoPreview;
    [Tooltip("Audio mixer to be used in game")]
    public AudioMixer targetMixer;
    [Tooltip("Template for volume slider panels in the Audio settings")]
    public GameObject audioSliderPrefab;
    [Tooltip("Parent for generated volume sliders")]
    public Transform audioSliderRoot;
    [Tooltip("Name of exposed volume parameters to generate sliders for")]
    public string[] volumeParameterNames = {"volume_Master", "volume_Sound Effects", "volume_Music"};
    [Tooltip("Template for quality setting options in the Video settings")]
    public GameObject graphicsDropdownPrefab;
    [Tooltip("Parent for generated graphics setting dropdowns")]
    public Transform graphicsDropdownRoot;
    [Tooltip("Name of quality setting parameters to generate settings for")]
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
        InitializeGraphicsTMP_Dropdowns();
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
        videoPreview.SetActive(PlayerAcommGlobal.activeSettingsPanel == videoPanelIndex);
    }

    public void SetSettingsPanel(int index)
    {
        PlayerAcommGlobal.activeSettingsPanel = index;
        LoadActiveSettingsPanel();
    }

    public void InitializeVolumeSliders()
    {
        for (int i = 0; i < volumeParameterNames.Length; i++)
        {
            string name = volumeParameterNames[i];
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

    public void InitializeGraphicsTMP_Dropdowns()
    {
        for (int i = 0; i < graphicsOptionNames.Length; i++)
        {
            string optionName = graphicsOptionNames[i];
            GameObject newTMP_DropdownObj = Instantiate(graphicsDropdownPrefab, graphicsDropdownRoot);
            RectTransform newTransform = newTMP_DropdownObj.GetComponent<RectTransform>();
            newTransform.anchoredPosition = Vector3.down * newTransform.rect.height * i;
            TMP_Dropdown TMP_Dropdown = newTMP_DropdownObj.GetComponentInChildren<TMP_Dropdown>();
            newTMP_DropdownObj.name = optionName;
            TMP_Dropdown.name = optionName;

            TMP_Text nameText = newTransform.Find(NAME_SETTINGTITLE).GetComponent<TMP_Text>();
            nameText.text = optionName;

            // Set TMP_Dropdown options and initial value based on optionName
            switch (optionName)
            {
                case "antiAliasing":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Disabled" },
                        new TMP_Dropdown.OptionData { text = "2x" },
                        new TMP_Dropdown.OptionData { text = "4x" },
                        new TMP_Dropdown.OptionData { text = "8x" }
                    });
                    int aaValue = 0;
                    switch (QualitySettings.antiAliasing)
                    {
                        case 2: aaValue = 1; break;
                        case 4: aaValue = 2; break;
                        case 8: aaValue = 3; break;
                        default: aaValue = 0; break;
                    }
                    TMP_Dropdown.value = aaValue;
                    break;
                case "anisotropicFiltering":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Disable" },
                        new TMP_Dropdown.OptionData { text = "Enable" },
                        new TMP_Dropdown.OptionData { text = "ForceEnable" }
                    });
                    TMP_Dropdown.value = (int)QualitySettings.anisotropicFiltering;
                    break;
                case "pixelLightCount":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "0" },
                        new TMP_Dropdown.OptionData { text = "1" },
                        new TMP_Dropdown.OptionData { text = "2" },
                        new TMP_Dropdown.OptionData { text = "3" },
                        new TMP_Dropdown.OptionData { text = "4" },
                        new TMP_Dropdown.OptionData { text = "5" },
                        new TMP_Dropdown.OptionData { text = "6" },
                        new TMP_Dropdown.OptionData { text = "7" },
                        new TMP_Dropdown.OptionData { text = "8" }
                    });
                    TMP_Dropdown.value = Mathf.Clamp(QualitySettings.pixelLightCount, 0, 8);
                    break;
                case "shadows":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Disable" },
                        new TMP_Dropdown.OptionData { text = "HardOnly" },
                        new TMP_Dropdown.OptionData { text = "All" }
                    });
                    TMP_Dropdown.value = (int)QualitySettings.shadows;
                    break;
                case "shadowResolution":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Low" },
                        new TMP_Dropdown.OptionData { text = "Medium" },
                        new TMP_Dropdown.OptionData { text = "High" },
                        new TMP_Dropdown.OptionData { text = "VeryHigh" }
                    });
                    TMP_Dropdown.value = (int)QualitySettings.shadowResolution;
                    break;
                case "shadowProjection":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "CloseFit" },
                        new TMP_Dropdown.OptionData { text = "StableFit" }
                    });
                    TMP_Dropdown.value = (int)QualitySettings.shadowProjection;
                    break;
                case "shadowCascades":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "No Cascades" },
                        new TMP_Dropdown.OptionData { text = "Two Cascades" },
                        new TMP_Dropdown.OptionData { text = "Four Cascades" }
                    });
                    int cascades = 0;
                    switch (QualitySettings.shadowCascades)
                    {
                        case 0: cascades = 0; break;
                        case 2: cascades = 1; break;
                        case 4: cascades = 2; break;
                        default: cascades = 0; break;
                    }
                    TMP_Dropdown.value = cascades;
                    break;
                case "shadowDistance":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> shadowDistOptions = new List<TMP_Dropdown.OptionData>();
                    for (int d = 0; d <= 200; d += 20)
                        shadowDistOptions.Add(new TMP_Dropdown.OptionData { text = d.ToString() });
                    TMP_Dropdown.AddOptions(shadowDistOptions);
                    TMP_Dropdown.value = Mathf.Clamp(Mathf.RoundToInt(QualitySettings.shadowDistance / 20f), 0, shadowDistOptions.Count - 1);
                    break;
                case "skinWeights":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "OneBone" },
                        new TMP_Dropdown.OptionData { text = "TwoBones" },
                        new TMP_Dropdown.OptionData { text = "FourBones" }
                    });
                    TMP_Dropdown.value = (int)QualitySettings.skinWeights;
                    break;
                case "softParticles":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.value = QualitySettings.softParticles ? 1 : 0;
                    break;
                case "softVegetation":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.value = QualitySettings.softVegetation ? 1 : 0;
                    break;
                case "realtimeReflectionProbes":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.value = QualitySettings.realtimeReflectionProbes ? 1 : 0;
                    break;
                case "billboardsFaceCameraPosition":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> 
                    {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.value = QualitySettings.billboardsFaceCameraPosition ? 1 : 0;
                    break;
                case "vSyncCount":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> 
                    {
                        new TMP_Dropdown.OptionData { text = "Don't Sync" },
                        new TMP_Dropdown.OptionData { text = "Every V Blank" },
                        new TMP_Dropdown.OptionData { text = "Every Second V Blank" }
                    });
                    TMP_Dropdown.value = Mathf.Clamp(QualitySettings.vSyncCount, 0, 2);
                    break;
                case "lodBias":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> lodBiasOptions = new List<TMP_Dropdown.OptionData>();
                    for (int l = 1; l <= 5; l++)
                        lodBiasOptions.Add(new TMP_Dropdown.OptionData { text = l.ToString() });
                    TMP_Dropdown.AddOptions(lodBiasOptions);
                    TMP_Dropdown.value = Mathf.Clamp(Mathf.RoundToInt(QualitySettings.lodBias) - 1, 0, lodBiasOptions.Count - 1);
                    break;
                case "maximumLODLevel":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> maxLodOptions = new List<TMP_Dropdown.OptionData>();
                    for (int m = 0; m <= 5; m++)
                        maxLodOptions.Add(new TMP_Dropdown.OptionData { text = m.ToString() });
                    TMP_Dropdown.AddOptions(maxLodOptions);
                    TMP_Dropdown.value = Mathf.Clamp(QualitySettings.maximumLODLevel, 0, maxLodOptions.Count - 1);
                    break;
                case "particleRaycastBudget":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> particleBudgetOptions = new List<TMP_Dropdown.OptionData>();
                    for (int p = 4; p <= 4096; p *= 2)
                        particleBudgetOptions.Add(new TMP_Dropdown.OptionData { text = p.ToString() });
                    TMP_Dropdown.AddOptions(particleBudgetOptions);
                    int particleIndex = 0;
                    int[] particleBudgets = { 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
                    for (int iB = 0; iB < particleBudgets.Length; iB++)
                        if (QualitySettings.particleRaycastBudget == particleBudgets[iB]) particleIndex = iB;
                    TMP_Dropdown.value = particleIndex;
                    break;
                case "asyncUploadTimeSlice":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> timeSliceOptions = new List<TMP_Dropdown.OptionData>();
                    for (int t = 1; t <= 16; t *= 2)
                        timeSliceOptions.Add(new TMP_Dropdown.OptionData { text = t.ToString() });
                    TMP_Dropdown.AddOptions(timeSliceOptions);
                    int timeSliceIndex = 0;
                    int[] timeSlices = { 1, 2, 4, 8, 16 };
                    for (int iT = 0; iT < timeSlices.Length; iT++)
                        if (QualitySettings.asyncUploadTimeSlice == timeSlices[iT]) timeSliceIndex = iT;
                    TMP_Dropdown.value = timeSliceIndex;
                    break;
                case "asyncUploadBufferSize":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> bufferSizeOptions = new List<TMP_Dropdown.OptionData>();
                    for (int b = 2; b <= 512; b *= 2)
                        bufferSizeOptions.Add(new TMP_Dropdown.OptionData { text = b.ToString() });
                    TMP_Dropdown.AddOptions(bufferSizeOptions);
                    int bufferSizeIndex = 0;
                    int[] bufferSizes = { 2, 4, 8, 16, 32, 64, 128, 256, 512 };
                    for (int iB = 0; iB < bufferSizes.Length; iB++)
                        if (QualitySettings.asyncUploadBufferSize == bufferSizes[iB]) bufferSizeIndex = iB;
                    TMP_Dropdown.value = bufferSizeIndex;
                    break;
                case "asyncUploadPersistentBuffer":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> 
                    {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.value = QualitySettings.asyncUploadPersistentBuffer ? 1 : 0;
                    break;
                case "resolutionScalingFixedDPIFactor":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> dpiOptions = new List<TMP_Dropdown.OptionData>();
                    for (int d = 1; d <= 5; d++)
                        dpiOptions.Add(new TMP_Dropdown.OptionData { text = d.ToString() });
                    TMP_Dropdown.AddOptions(dpiOptions);
                    TMP_Dropdown.value = Mathf.Clamp(Mathf.RoundToInt(QualitySettings.resolutionScalingFixedDPIFactor) - 1, 0, dpiOptions.Count - 1);
                    break;
            }

            TMP_Dropdown.onValueChanged.AddListener((int value) => GraphicsOptionCallback(TMP_Dropdown));
        }
    }

    public void GraphicsOptionCallback(TMP_Dropdown TMP_Dropdown)
    {
        string settingName = TMP_Dropdown.name;
        int settingValue = TMP_Dropdown.value;

        switch (settingName)
        {
            case "antiAliasing":
                // TMP_Dropdown options: 0=Disabled, 1=2x, 2=4x, 3=8x
                switch (settingValue)
                {
                    case 0: QualitySettings.antiAliasing = 0; break;
                    case 1: QualitySettings.antiAliasing = 2; break;
                    case 2: QualitySettings.antiAliasing = 4; break;
                    case 3: QualitySettings.antiAliasing = 8; break;
                }
                break;
            case "anisotropicFiltering":
                // TMP_Dropdown options: 0=Disable, 1=Enable, 2=ForceEnable
                QualitySettings.anisotropicFiltering = (AnisotropicFiltering)settingValue;
                break;
            case "pixelLightCount":
                QualitySettings.pixelLightCount = settingValue;
                break;
            case "shadows":
                // TMP_Dropdown options: 0=Disable, 1=HardOnly, 2=All
                QualitySettings.shadows = (ShadowQuality)settingValue;
                break;
            case "shadowResolution":
                // TMP_Dropdown options: 0=Low, 1=Medium, 2=High, 3=VeryHigh
                QualitySettings.shadowResolution = (ShadowResolution)settingValue;
                break;
            case "shadowProjection":
                // TMP_Dropdown options: 0=CloseFit, 1=StableFit
                QualitySettings.shadowProjection = (ShadowProjection)settingValue;
                break;
            case "shadowCascades":
                // TMP_Dropdown options: 0=No Cascades, 1=Two Cascades, 2=Four Cascades
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
                // TMP_Dropdown options: 0=OneBone, 1=TwoBones, 2=FourBones
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

    public void SwitchToActiveMainMenu()
    {
        if (SceneManager.GetActiveScene().name == mainMenuScene)
            SetMenu(mainMenuName);
        else
            SetMenu(pauseMenuName);
    }

    public void SetMenu(string menuName)
    {
        for (int i=0; i < Menus.Length; i++)
            Menus[i].SetActive(Menus[i].name == menuName);
    }

    private IEnumerator LoadSceneInternal(string sceneName)
    {
        SetMenu("None");
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
