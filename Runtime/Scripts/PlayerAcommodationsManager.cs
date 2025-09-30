using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
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
    [Tooltip("Pause Button")]
    public InputAction pauseInput;

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
    public string[] volumeParameterNames = { "volume_Master", "volume_Sound Effects", "volume_Music" };
    [Tooltip("Template for quality setting options in the Video settings")]
    public GameObject graphicsDropdownPrefab;
    [Tooltip("Parent for generated graphics setting dropdowns")]
    public Transform graphicsDropdownRoot;
    [Tooltip("Name of quality setting parameters to generate settings for")]
    public string[] graphicsOptionNames = { "antiAliasing", "anisotropicFiltering" };
    [Tooltip("Dropdown to populate with available screen resolutions")]
    public TMP_Dropdown resolutionDropdown;
    [Tooltip("Dropdown to populate with available screen refresh rates")]
    public TMP_Dropdown refreshRateDropdown;
    [Tooltip("Dropdown to populate with available QualitySettings tiers")]
    public TMP_Dropdown qualityTierDropdown;
    [Tooltip("Dropdown for Fullscreen mode")]
    public TMP_Dropdown fullscreenModeDropdown;
    [Tooltip("Panel for apply, discard, and reset to default buttons")]
    public GameObject changeControlPanel;
    [Tooltip("Index of default quality tier")]
    public int defaultQualityTier = 1;
    [Tooltip("Prompt to confirm changes if leaving settings without saving")]
    public GameObject confirmChangesPrompt;
    [Tooltip("Automatically commit save data on a regular interval")]
    public bool doAutosave = true;
    [Tooltip("Auto-save interval, in seconds")]
    public float autosaveInterval = 1;

    [Header("Save/Load")]
    [Tooltip("Input Fields for save slot renaming")]
    public TMP_InputField[] saveNames = new TMP_InputField[3];
    [Tooltip("Save Interface used to retrieve active game scene")]
    public PlayerAcommodationsSaveInterface gameSceneSaveInterface;

    //PRIVATE VARIABLES
    private Coroutine sceneLoadRoutine;
    private Resolution[] availableResolutions;
    private List<RefreshRate> availableRefreshRates = new List<RefreshRate>();
    private List<GameObject> graphicsDropdowns = new List<GameObject>();
    private float prevTimeScale = 1;
    private bool changeMade = false;
    private bool paused = false;

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
        if (PlayerPrefs.HasKey("activeGameScene"))
            PlayerAcommGlobal.activeGameScene = PlayerPrefs.GetString("activeGameScene");
        else
        {
            PlayerAcommGlobal.activeGameScene = defaultGameScene;
            PlayerPrefs.SetString("activeGameScene", defaultGameScene);
        }

        LoadSettings();
        InitializeVolumeSliders();
        InitializeDropdowns();

        pauseInput.Enable();
        pauseInput.started += TogglePause;

        for (int i = 0; i < 3; i++)
        {
            string key = "saveName_" + i;
            if (PlayerPrefs.HasKey(key))
            {
                string newName = PlayerPrefs.GetString(key);
                PlayerAcommSaveGlobal.saveSlots[i] = newName;
                saveNames[i]?.SetTextWithoutNotify(newName);
            }
            else
                PlayerPrefs.SetString(key, PlayerAcommSaveGlobal.saveSlots[i]);
        }

        if (PlayerPrefs.HasKey("activeSaveSlot"))
            PlayerAcommSaveGlobal.activeSaveSlot = PlayerPrefs.GetInt("activeSaveSlot");
        else
            PlayerPrefs.SetInt("activeSaveSlot", 0);

        if (gameSceneSaveInterface != null)
            gameSceneSaveInterface.fallbackString = defaultGameScene;

        if (doAutosave)
            StartCoroutine(Autosave(autosaveInterval));
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == mainMenuScene)
            SwitchToActiveMainMenu();
    }

    public void InitializeDropdowns()
    {
        PopulateDisplayOptions();
        InitializeGraphicsDropdowns();
        InitializeQualityTierDropdown();
    }

    public void SetActiveGameScene(string sceneName) => PlayerAcommGlobal.activeGameScene = sceneName;

    public void LoadActiveGameScene()
    {
        if (sceneLoadRoutine == null)
            StartCoroutine(LoadSceneInternal(PlayerAcommGlobal.activeGameScene));
    }

    public void LoadMainMenuScene()
    {
        if (sceneLoadRoutine == null)
        {
            StartCoroutine(LoadSceneInternal(mainMenuScene));
            PlayerAcommSaveGlobal.CommitSaveData();
        }
    }

    public void LoadSceneByName(string sceneName)
    {
        if (sceneLoadRoutine == null)
            StartCoroutine(LoadSceneInternal(sceneName));
    }

    public void LoadSceneByIndex(int index)
    {
        if (sceneLoadRoutine == null)
            StartCoroutine(LoadSceneInternal(index));
    }

    public void LoadNextScene()
    {
        if (sceneLoadRoutine == null)
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            StartCoroutine(LoadSceneInternal(currentScene + 1));
        }
    }

    public void LoadPrevScene()
    {
        if (sceneLoadRoutine != null)
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            StartCoroutine(LoadSceneInternal(currentScene - 1));
        }
    }

    public void LoadNextSceneAndUnlock()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        PlayerAcommSaveGlobal.SetBool("unlockSceneIndex_" + (currentScene + 1).ToString(), true);
        LoadSceneByIndex(currentScene + 1);
    }

    public void LoadPrevSceneAndUnlock()
    {
        if (sceneLoadRoutine != null)
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            PlayerAcommSaveGlobal.SetBool("unlockSceneIndex_" + (currentScene - 1).ToString(), true);
            LoadSceneByIndex(currentScene - 1);
        }
    }

    public void LoadSceneByNameAndUnlock(string sceneName)
    {
        if (sceneLoadRoutine != null)
        {
            StartCoroutine(LoadSceneInternal(sceneName));
            PlayerAcommSaveGlobal.SetBool("unlockSceneName_" + sceneName, true);
        }
    }

    public void LoadSceneByIndexAndUnlock(int index)
    {
        if (sceneLoadRoutine != null)
        {
            PlayerAcommSaveGlobal.SetBool("unlockSceneIndex_" + index, true);
            LoadSceneByIndex(index);
        }
    }

    public void LoadActiveSettingsPanel()
    {
        for (int i = 0; i < settingsPanels.Length; i++)
            settingsPanels[i].SetActive(i == PlayerAcommGlobal.activeSettingsPanel);
        videoPreview.SetActive(PlayerAcommGlobal.activeSettingsPanel == videoPanelIndex);
        changeControlPanel.SetActive(PlayerAcommGlobal.activeSettingsPanel == videoPanelIndex);
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

    public void InitializeQualityTierDropdown()
    {
        qualityTierDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> qualityLevelOptions = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Custom") };
        foreach (var tier in QualitySettings.names)
            qualityLevelOptions.Add(new TMP_Dropdown.OptionData(tier));
        qualityTierDropdown.AddOptions(qualityLevelOptions);
        qualityTierDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel() + 1);
        qualityTierDropdown.onValueChanged.AddListener((int value) => SetQualityTier(value));
    }

    public void InitializeGraphicsDropdowns()
    {
        for (int i = 0; i < graphicsOptionNames.Length; i++)
        {
            string optionName = graphicsOptionNames[i];
            GameObject newDropdownObj = Instantiate(graphicsDropdownPrefab, graphicsDropdownRoot);
            RectTransform newTransform = newDropdownObj.GetComponent<RectTransform>();
            newTransform.anchoredPosition = Vector3.down * newTransform.rect.height * i;
            TMP_Dropdown TMP_Dropdown = newDropdownObj.GetComponentInChildren<TMP_Dropdown>();
            newDropdownObj.name = optionName;
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
                    TMP_Dropdown.SetValueWithoutNotify(aaValue);
                    break;
                case "anisotropicFiltering":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Disable" },
                        new TMP_Dropdown.OptionData { text = "Enable" },
                        new TMP_Dropdown.OptionData { text = "ForceEnable" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify((int)QualitySettings.anisotropicFiltering);
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
                    TMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(QualitySettings.pixelLightCount, 0, 8));
                    break;
                case "shadows":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Disable" },
                        new TMP_Dropdown.OptionData { text = "HardOnly" },
                        new TMP_Dropdown.OptionData { text = "All" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify((int)QualitySettings.shadows);
                    break;
                case "shadowResolution":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Low" },
                        new TMP_Dropdown.OptionData { text = "Medium" },
                        new TMP_Dropdown.OptionData { text = "High" },
                        new TMP_Dropdown.OptionData { text = "VeryHigh" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify((int)QualitySettings.shadowResolution);
                    break;
                case "shadowProjection":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "CloseFit" },
                        new TMP_Dropdown.OptionData { text = "StableFit" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify((int)QualitySettings.shadowProjection);
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
                    TMP_Dropdown.SetValueWithoutNotify(cascades);
                    break;
                case "shadowDistance":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> shadowDistOptions = new List<TMP_Dropdown.OptionData>();
                    for (int d = 0; d <= 200; d += 20)
                        shadowDistOptions.Add(new TMP_Dropdown.OptionData { text = d.ToString() });
                    TMP_Dropdown.AddOptions(shadowDistOptions);
                    TMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(Mathf.RoundToInt(QualitySettings.shadowDistance / 20f), 0, shadowDistOptions.Count - 1));
                    break;
                case "skinWeights":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "OneBone" },
                        new TMP_Dropdown.OptionData { text = "TwoBones" },
                        new TMP_Dropdown.OptionData { text = "FourBones" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify((int)QualitySettings.skinWeights);
                    break;
                case "softParticles":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify(QualitySettings.softParticles ? 1 : 0);
                    break;
                case "softVegetation":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify(QualitySettings.softVegetation ? 1 : 0);
                    break;
                case "realtimeReflectionProbes":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify(QualitySettings.realtimeReflectionProbes ? 1 : 0);
                    break;
                case "billboardsFaceCameraPosition":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> 
                    {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify(QualitySettings.billboardsFaceCameraPosition ? 1 : 0);
                    break;
                case "vSyncCount":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> 
                    {
                        new TMP_Dropdown.OptionData { text = "Don't Sync" },
                        new TMP_Dropdown.OptionData { text = "Every V Blank" },
                        new TMP_Dropdown.OptionData { text = "Every Second V Blank" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(QualitySettings.vSyncCount, 0, 2));
                    break;
                case "lodBias":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> lodBiasOptions = new List<TMP_Dropdown.OptionData>();
                    for (int l = 1; l <= 5; l++)
                        lodBiasOptions.Add(new TMP_Dropdown.OptionData { text = l.ToString() });
                    TMP_Dropdown.AddOptions(lodBiasOptions);
                    TMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(Mathf.RoundToInt(QualitySettings.lodBias) - 1, 0, lodBiasOptions.Count - 1));
                    break;
                case "maximumLODLevel":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> maxLodOptions = new List<TMP_Dropdown.OptionData>();
                    for (int m = 0; m <= 5; m++)
                        maxLodOptions.Add(new TMP_Dropdown.OptionData { text = m.ToString() });
                    TMP_Dropdown.AddOptions(maxLodOptions);
                    TMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(QualitySettings.maximumLODLevel, 0, maxLodOptions.Count - 1));
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
                    TMP_Dropdown.SetValueWithoutNotify(particleIndex);
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
                    TMP_Dropdown.SetValueWithoutNotify(timeSliceIndex);
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
                    TMP_Dropdown.SetValueWithoutNotify(bufferSizeIndex);
                    break;
                case "asyncUploadPersistentBuffer":
                    TMP_Dropdown.ClearOptions();
                    TMP_Dropdown.AddOptions(new List<TMP_Dropdown.OptionData> 
                    {
                        new TMP_Dropdown.OptionData { text = "Off" },
                        new TMP_Dropdown.OptionData { text = "On" }
                    });
                    TMP_Dropdown.SetValueWithoutNotify(QualitySettings.asyncUploadPersistentBuffer ? 1 : 0);
                    break;
                case "resolutionScalingFixedDPIFactor":
                    TMP_Dropdown.ClearOptions();
                    List<TMP_Dropdown.OptionData> dpiOptions = new List<TMP_Dropdown.OptionData>();
                    for (int d = 1; d <= 5; d++)
                        dpiOptions.Add(new TMP_Dropdown.OptionData { text = d.ToString() });
                    TMP_Dropdown.AddOptions(dpiOptions);
                    TMP_Dropdown.SetValueWithoutNotify(Mathf.Clamp(Mathf.RoundToInt(QualitySettings.resolutionScalingFixedDPIFactor) - 1, 0, dpiOptions.Count - 1));
                    break;
            }

            TMP_Dropdown.onValueChanged.AddListener((int value) => GraphicsOptionCallback(TMP_Dropdown));
            graphicsDropdowns.Add(newDropdownObj);
        }
    }

    public void GraphicsOptionCallback(TMP_Dropdown TMP_Dropdown)
    {
        if (PlayerAcommGlobal.queuedQualitySettingChanges.ContainsKey(TMP_Dropdown.name))
            PlayerAcommGlobal.queuedQualitySettingChanges[TMP_Dropdown.name] = TMP_Dropdown.value;
        else
            PlayerAcommGlobal.queuedQualitySettingChanges.Add(TMP_Dropdown.name, TMP_Dropdown.value);
        PlayerAcommGlobal.targetQualityTier = -1;
        qualityTierDropdown.SetValueWithoutNotify(0);
        changeMade = true;
    }

    public void SetQualityTier(int index)
    {
        PlayerAcommGlobal.targetQualityTier = index - 1;
        int returnToQualityLevel = QualitySettings.GetQualityLevel();
        if (PlayerAcommGlobal.targetQualityTier != -1)
            QualitySettings.SetQualityLevel(index);
        UpdateGraphicsDropdowns();
        QualitySettings.SetQualityLevel(returnToQualityLevel);
        changeMade = true;
    }

    private static void ChangeQualitySettingByName(string settingName, int settingValue)
    {
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

    public void SetFullscreenMode(int index)
    {
        PlayerAcommGlobal.targetFullscreenMode = (FullScreenMode)index;
        changeMade = true;
    }

    public void PopulateDisplayOptions()
    {
        resolutionDropdown.ClearOptions();
        refreshRateDropdown.ClearOptions();
        availableResolutions = (Resolution[])Screen.resolutions.Clone();
        availableRefreshRates.Clear();
        List<TMP_Dropdown.OptionData> resolutionOptions = new();
        List<TMP_Dropdown.OptionData> refreshRateOptions = new();
        int defaultRefreshRateOption = 0;
        int defaultResolutionOption = 0;
        foreach(Resolution resolution in availableResolutions)
        {
            if (!availableRefreshRates.Contains(resolution.refreshRateRatio))
            {
                availableRefreshRates.Add(resolution.refreshRateRatio);
                refreshRateOptions.Add(new TMP_Dropdown.OptionData(resolution.refreshRateRatio.ToString()));
                if (resolution.refreshRateRatio.value == Screen.currentResolution.refreshRateRatio.value)
                    defaultRefreshRateOption = refreshRateOptions.Count - 1;
            }
            string resolutionText = resolution.width.ToString() + "x" + resolution.height.ToString();
            resolutionOptions.Add(new TMP_Dropdown.OptionData(resolutionText));
            if (resolution.width == Screen.currentResolution.width && resolution.height == Screen.currentResolution.height)
                defaultResolutionOption = resolutionOptions.Count - 1;
        }
        resolutionDropdown.AddOptions(resolutionOptions);
        refreshRateDropdown.AddOptions(refreshRateOptions);
        resolutionDropdown.SetValueWithoutNotify(defaultResolutionOption);
        refreshRateDropdown.SetValueWithoutNotify(defaultRefreshRateOption);
        resolutionDropdown.onValueChanged.AddListener((int value) => SetResolution(value));
        refreshRateDropdown.onValueChanged.AddListener((int value) => SetRefreshRate(value));
        PlayerAcommGlobal.targetResolution = Screen.currentResolution;
        PlayerAcommGlobal.targetRefreshRate = Screen.currentResolution.refreshRateRatio;
        fullscreenModeDropdown.SetValueWithoutNotify((int)Screen.fullScreenMode);
    }

    public void SetResolution(int index)
    {
        Resolution newRes = availableResolutions[index];
        newRes.refreshRateRatio = PlayerAcommGlobal.targetRefreshRate;
        PlayerAcommGlobal.targetResolution = newRes;
        changeMade = true;
    }

    public void SetRefreshRate(int index)
    {
        Resolution newRes = PlayerAcommGlobal.targetResolution;
        newRes.refreshRateRatio = availableRefreshRates[index];
        PlayerAcommGlobal.targetResolution = newRes;
        changeMade = true;
    }

    public void ApplyChanges()
    {
        Screen.SetResolution(PlayerAcommGlobal.targetResolution.width, PlayerAcommGlobal.targetResolution.height, PlayerAcommGlobal.targetFullscreenMode, PlayerAcommGlobal.targetRefreshRate);
        if (PlayerAcommGlobal.targetQualityTier != -1)
        {
            QualitySettings.SetQualityLevel(PlayerAcommGlobal.targetQualityTier);
        }
        else if (PlayerAcommGlobal.queuedQualitySettingChanges.Count > 0)
        {
            foreach (string s in PlayerAcommGlobal.queuedQualitySettingChanges.Keys)
                ChangeQualitySettingByName(s, PlayerAcommGlobal.queuedQualitySettingChanges[s]);
        }
        SaveSettings();
        changeMade = false;
    }

    public void UpdateGraphicsDropdowns()
    {
        foreach (GameObject g in graphicsDropdowns)
            Destroy(g);
        graphicsDropdowns.Clear();
        PlayerAcommGlobal.queuedQualitySettingChanges.Clear();
        InitializeGraphicsDropdowns();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("qualityTier", PlayerAcommGlobal.targetQualityTier);
        if (PlayerAcommGlobal.targetQualityTier == -1) return;
        PlayerPrefs.SetInt("fullscreenMode", (int)PlayerAcommGlobal.targetFullscreenMode);
        PlayerPrefs.SetInt("resolutionWidth", PlayerAcommGlobal.targetResolution.width);
        PlayerPrefs.SetInt("resolutionHeight", PlayerAcommGlobal.targetResolution.height);
        PlayerPrefs.SetString("refreshRateNum", PlayerAcommGlobal.targetRefreshRate.numerator.ToString());
        PlayerPrefs.SetString("refreshRateDen", PlayerAcommGlobal.targetRefreshRate.denominator.ToString());
        PlayerPrefs.SetInt("antiAliasing", QualitySettings.antiAliasing);
        PlayerPrefs.SetInt("anisotropicFiltering", (int)QualitySettings.anisotropicFiltering);
        PlayerPrefs.SetInt("pixelLightCount", QualitySettings.pixelLightCount);
        PlayerPrefs.SetInt("shadows", (int)QualitySettings.shadows);
        PlayerPrefs.SetInt("shadowResolution", (int)QualitySettings.shadowResolution);
        PlayerPrefs.SetInt("shadowProjection", (int)QualitySettings.shadowProjection);
        PlayerPrefs.SetInt("shadowCascades", QualitySettings.shadowCascades);
        PlayerPrefs.SetFloat("shadowDistance", QualitySettings.shadowDistance);
        PlayerPrefs.SetInt("skinWeights", (int)QualitySettings.skinWeights);
        PlayerPrefs.SetInt("softParticles", QualitySettings.softParticles ? 1 : 0);
        PlayerPrefs.SetInt("softVegetation", QualitySettings.softVegetation ? 1 : 0);
        PlayerPrefs.SetInt("realtimeReflectionProbes", QualitySettings.realtimeReflectionProbes ? 1 : 0);
        PlayerPrefs.SetInt("billboardsFaceCameraPosition", QualitySettings.billboardsFaceCameraPosition ? 1 : 0);
        PlayerPrefs.SetInt("vSyncCount", QualitySettings.vSyncCount);
        PlayerPrefs.SetFloat("lodBias", QualitySettings.lodBias);
        PlayerPrefs.SetInt("maximumLODLevel", QualitySettings.maximumLODLevel);
        PlayerPrefs.SetInt("particleRaycastBudget", QualitySettings.particleRaycastBudget);
        PlayerPrefs.SetInt("asyncUploadTimeSlice", QualitySettings.asyncUploadTimeSlice);
        PlayerPrefs.SetInt("asyncUploadBufferSize", QualitySettings.asyncUploadBufferSize);
        PlayerPrefs.SetInt("asyncUploadPersistentBuffer", QualitySettings.asyncUploadPersistentBuffer ? 1 : 0);
        PlayerPrefs.SetFloat("resolutionScalingFixedDPIFactor", QualitySettings.resolutionScalingFixedDPIFactor);

        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        // PlayerAcommGlobal
        PlayerAcommGlobal.targetFullscreenMode =
            (FullScreenMode)PlayerPrefs.GetInt("fullscreenMode", (int)FullScreenMode.FullScreenWindow);

        PlayerAcommGlobal.targetResolution = new Resolution
        {
            width = PlayerPrefs.GetInt("resolutionWidth", Screen.currentResolution.width),
            height = PlayerPrefs.GetInt("resolutionHeight", Screen.currentResolution.height),
            refreshRateRatio = new RefreshRate
            {
                numerator = uint.Parse(PlayerPrefs.GetString("refreshRateNum", Screen.currentResolution.refreshRateRatio.numerator.ToString())),
                denominator = uint.Parse(PlayerPrefs.GetString("refreshRateDen", Screen.currentResolution.refreshRateRatio.denominator.ToString()))
            }
        };

        PlayerAcommGlobal.targetQualityTier = PlayerPrefs.GetInt("qualityTier", defaultQualityTier);
        if (PlayerAcommGlobal.targetQualityTier != -1)
        {
            QualitySettings.SetQualityLevel(PlayerAcommGlobal.targetQualityTier);
            return;
        }

        // QualitySettings
        QualitySettings.antiAliasing = PlayerPrefs.GetInt("antiAliasing", QualitySettings.antiAliasing);
        QualitySettings.anisotropicFiltering =
            (AnisotropicFiltering)PlayerPrefs.GetInt("anisotropicFiltering", (int)QualitySettings.anisotropicFiltering);
        QualitySettings.pixelLightCount = PlayerPrefs.GetInt("pixelLightCount", QualitySettings.pixelLightCount);
        QualitySettings.shadows =
            (ShadowQuality)PlayerPrefs.GetInt("shadows", (int)QualitySettings.shadows);
        QualitySettings.shadowResolution =
            (ShadowResolution)PlayerPrefs.GetInt("shadowResolution", (int)QualitySettings.shadowResolution);
        QualitySettings.shadowProjection =
            (ShadowProjection)PlayerPrefs.GetInt("shadowProjection", (int)QualitySettings.shadowProjection);
        QualitySettings.shadowCascades = PlayerPrefs.GetInt("shadowCascades", QualitySettings.shadowCascades);
        QualitySettings.shadowDistance = PlayerPrefs.GetFloat("shadowDistance", QualitySettings.shadowDistance);
        QualitySettings.skinWeights =
            (SkinWeights)PlayerPrefs.GetInt("skinWeights", (int)QualitySettings.skinWeights);
        QualitySettings.softParticles = PlayerPrefs.GetInt("softParticles", QualitySettings.softParticles ? 1 : 0) == 1;
        QualitySettings.softVegetation = PlayerPrefs.GetInt("softVegetation", QualitySettings.softVegetation ? 1 : 0) == 1;
        QualitySettings.realtimeReflectionProbes = PlayerPrefs.GetInt("realtimeReflectionProbes", QualitySettings.realtimeReflectionProbes ? 1 : 0) == 1;
        QualitySettings.billboardsFaceCameraPosition = PlayerPrefs.GetInt("billboardsFaceCameraPosition", QualitySettings.billboardsFaceCameraPosition ? 1 : 0) == 1;
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("vSyncCount", QualitySettings.vSyncCount);
        QualitySettings.lodBias = PlayerPrefs.GetFloat("lodBias", QualitySettings.lodBias);
        QualitySettings.maximumLODLevel = PlayerPrefs.GetInt("maximumLODLevel", QualitySettings.maximumLODLevel);
        QualitySettings.particleRaycastBudget = PlayerPrefs.GetInt("particleRaycastBudget", QualitySettings.particleRaycastBudget);
        QualitySettings.asyncUploadTimeSlice = PlayerPrefs.GetInt("asyncUploadTimeSlice", QualitySettings.asyncUploadTimeSlice);
        QualitySettings.asyncUploadBufferSize = PlayerPrefs.GetInt("asyncUploadBufferSize", QualitySettings.asyncUploadBufferSize);
        QualitySettings.asyncUploadPersistentBuffer = PlayerPrefs.GetInt("asyncUploadPersistentBuffer", QualitySettings.asyncUploadPersistentBuffer ? 1 : 0) == 1;
        QualitySettings.resolutionScalingFixedDPIFactor = PlayerPrefs.GetFloat("resolutionScalingFixedDPIFactor", QualitySettings.resolutionScalingFixedDPIFactor);
    }

    public void ResetToDefaults()
    {
        qualityTierDropdown.value = defaultQualityTier;
        resolutionDropdown.value = resolutionDropdown.options.Count - 1;
        refreshRateDropdown.value = refreshRateDropdown.options.Count - 1;
        fullscreenModeDropdown.value = fullscreenModeDropdown.options.Count - 1;
        changeMade = false;
    }

    public void CheckChangePrompt()
    {
        if (changeMade)
            confirmChangesPrompt.SetActive(true);
        else
            SwitchToActiveMainMenu();
    }

    private void TogglePause(InputAction.CallbackContext obj)
    {
        if (SceneManager.GetActiveScene().name == mainMenuScene)
        {
            UpdateGraphicsDropdowns();
            PopulateDisplayOptions();
            SwitchToActiveMainMenu();
        }
        else
            SetPaused(!paused);
    }

    public void SetPaused(bool newState)
    {
        paused = newState;
        if (!paused)
        {
            Time.timeScale = prevTimeScale;
            SetMenu("None");
        }
        else
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0;
            SwitchToActiveMainMenu();
        }
    }

    public void ApplicationExit() => Application.Quit();

    private IEnumerator LoadSceneInternal(string sceneName)
    {
        SetPaused(false);
        if (sceneName != mainMenuScene)
            SetMenu("None");
        else
            SetMenu(mainMenuName);
        loadingScreen?.SetActive(true);
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        while (!async.isDone)
        {
            yield return null;
            loadSpinner.transform.Rotate(transform.forward, loadSpinnerSpeed);
        }
        loadingScreen?.SetActive(false);
    }

    private IEnumerator LoadSceneInternal(int sceneIndex)
    {
        SetPaused(false);
        int menuSceneBuildIndex = SceneManager.GetSceneByName(mainMenuScene).buildIndex;
        if (sceneIndex != menuSceneBuildIndex)
            SetMenu("None");
        else
            SetMenu(mainMenuName);
        loadingScreen?.SetActive(true);
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneIndex);
        while (!async.isDone)
        {
            yield return null;
            loadSpinner.transform.Rotate(transform.forward, loadSpinnerSpeed);
        }
        loadingScreen?.SetActive(false);
    }

    private IEnumerator Autosave(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            if (SceneManager.GetActiveScene().name != mainMenuScene)
                PlayerAcommSaveGlobal.CommitSaveData();
        }
    }
}

public static class PlayerAcommGlobal
{
    public static string activeGameScene = "";
    public static int activeSettingsPanel = 0;
    public static int targetQualityTier = -1;
    public static FullScreenMode targetFullscreenMode;
    public static Resolution targetResolution;
    public static RefreshRate targetRefreshRate;
    public static Dictionary<string, int> queuedQualitySettingChanges = new Dictionary<string, int>();
}
