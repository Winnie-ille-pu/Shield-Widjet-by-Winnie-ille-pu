using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

public class TailOfArgonia : MonoBehaviour
{
    public static TailOfArgonia Instance;

    WeatherManager weatherManager;
    WorldTime worldTime;
    PlayerAmbientLight playerAmbientLight;
    PlayerEnterExit playerEnterExit;
    DaggerfallSky sky;

    bool view;
    int viewDistance = 64;
    int[] viewDistances = new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };

    bool fog;
    float fogStartDistanceMultiplier = 0.2f;
    float fogEndDistanceMultiplier = 0.8f;
    float fogDensityModifier = 1;
    float[] fogDensities = new float[] { 0.1f, 0.05f, 0.025f, 0.0125f, 0.00625f, 0.003125f, 0.0015625f, 0.00078125f };

    int[] terrainDistances = new int[] { 1, 1, 2, 2, 3, 3, 4, 4 };

    public static WeatherManager.FogSettings SunnyFogSettings = new WeatherManager.FogSettings { fogMode = FogMode.Linear, density = 0.0f, startDistance = 0, endDistance = 2560, excludeSkybox = true };
    public static WeatherManager.FogSettings SunnyFogSettingsDefault;
    public static WeatherManager.FogSettings OvercastFogSettings = new WeatherManager.FogSettings { fogMode = FogMode.Linear, density = 0.0f, startDistance = 0, endDistance = 2048, excludeSkybox = true };
    public static WeatherManager.FogSettings OvercastFogSettingsDefault;
    public static WeatherManager.FogSettings RainyFogSettings = new WeatherManager.FogSettings { fogMode = FogMode.Linear, density = 0.0f, startDistance = 0, endDistance = 1536, excludeSkybox = true };
    public static WeatherManager.FogSettings RainyFogSettingsDefault;
    public static WeatherManager.FogSettings SnowyFogSettings = new WeatherManager.FogSettings { fogMode = FogMode.Linear, density = 0.0f, startDistance = 0, endDistance = 1280, excludeSkybox = true };
    public static WeatherManager.FogSettings SnowyFogSettingsDefault;
    public static WeatherManager.FogSettings HeavyFogSettings = new WeatherManager.FogSettings { fogMode = FogMode.Linear, density = 0.0f, startDistance = 0, endDistance = 1024, excludeSkybox = true };
    public static WeatherManager.FogSettings HeavyFogSettingsDefault;

    FogMode currentFogMode;

    bool debugShowMessages;

    bool smoothClip;
    float smoothClipStartDistance = 0.75f;
    float smoothClipLastUpdate;

    public Texture textureDitherSmall;
    public Texture textureDitherLarge;
    int textureDitherSize;

    public Shader shaderDefault;
    public Shader shaderBillboard;
    public Shader shaderBillboardBatch;
    public Shader shaderBillboardBatchNoShadows;
    public Shader shaderTilemap;
    public Shader shaderTilemapTextureArray;

    bool shearing;
    Camera shearingEye;
    PostProcessLayer shearingPostprocessingLayer;
    bool isShearing;
    KeyCode shearingKeyCode;

    const string defaultCrosshairFilename = "Crosshair";
    public Vector2 crosshairSize;
    public Texture2D CrosshairTexture;
    public float CrosshairScale = 1.0f;

    public int nativeScreenWidth = 320;
    public int nativeScreenHeight = 200;
    Rect screenRect;
    Vector3 crosshairOffset;

    bool ambientLighting;
    Color lastAmbientColor;
    Color lastCameraClearColor;

    Light sunLight;
    float sunLightScale = 1;
    float moonLightScale = 1;
    int lightColor;             //0 = sky, 1 = fog
    float lightColorScale = 1;

    float ambientLightExteriorDayScale = 1;
    float ambientLightExteriorNightScale = 1;
    float ambientLightInteriorDayScale = 1;
    float ambientLightInteriorNightScale = 1;
    float ambientLightCastleScale = 1;
    float ambientLightDungeonScale = 1;

    bool ambientLightingInitialized = false;
    Color ExteriorNoonAmbientLightDefault = new Color(0.9f, 0.9f, 0.9f);
    Color ExteriorNightAmbientLightDefault = new Color(0.25f, 0.25f, 0.25f);
    Color InteriorAmbientLightDefault = new Color(0.18f, 0.18f, 0.18f);
    Color InteriorNightAmbientLightDefault = new Color(0.20f, 0.18f, 0.20f);
    Color InteriorAmbientLight_AmbientOnlyDefault = new Color(0.8f, 0.8f, 0.8f);
    Color InteriorNightAmbientLight_AmbientOnlyDefault = new Color(0.5f, 0.5f, 0.5f);
    Color DungeonAmbientLightDefault = new Color(0.12f, 0.12f, 0.12f);
    Color CastleAmbientLightDefault = new Color(0.58f, 0.58f, 0.58f);

    float OvercastSunlightScaleDefault = 0.65f;
    float RainSunlightScaleDefault = 0.45f;
    float StormSunlightScaleDefault = 0.25f;
    float SnowSunlightScaleDefault = 0.45f;
    float WinterSunlightScaleDefault = 0.65f;

    Mod DynamicSkies;



    IEnumerator worldUpdateMessage;

    static Mod mod;
    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;

        var go = new GameObject(mod.Title);
        TailOfArgonia toa = go.AddComponent<TailOfArgonia>();

        toa.textureDitherSmall = mod.GetAsset<Texture>("Textures/BayerDither4x4.png");
        toa.textureDitherLarge = mod.GetAsset<Texture>("Textures/BayerDither8x8.png");
        toa.shaderDefault = mod.GetAsset<Shader>("Shaders/DaggerfallDefaultDither.shader");
        toa.shaderBillboard = mod.GetAsset<Shader>("Shaders/DaggerfallBillboardDither.shader");
        toa.shaderBillboardBatch = mod.GetAsset<Shader>("Shaders/DaggerfallBillboardBatchDither.shader");
        toa.shaderBillboardBatchNoShadows = mod.GetAsset<Shader>("Shaders/DaggerfallBillboardBatchNoShadowsDither.shader");
        toa.shaderTilemap = mod.GetAsset<Shader>("Shaders/DaggerfallTilemapDither.shader");
        toa.shaderTilemapTextureArray = mod.GetAsset<Shader>("Shaders/DaggerfallTilemapTextureArrayDither.shader");
    }
    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Modules"))
        {
            view = settings.GetValue<bool>("Modules", "ViewDistance");
            fog = settings.GetValue<bool>("Modules", "Fog");
            smoothClip = settings.GetValue<bool>("Modules", "SmoothClipping");
            shearing = settings.GetValue<bool>("Modules", "YShearing");
            ambientLighting = settings.GetValue<bool>("Modules", "ImprovedAmbientLight");
        }
        if (change.HasChanged("ViewDistance"))
        {
            viewDistance = settings.GetValue<int>("ViewDistance", "Maximum");
        }
        if (change.HasChanged("Fog"))
        {
            fogStartDistanceMultiplier = settings.GetValue<int>("Fog", "LinearStartDistance") * 0.01f;
            fogEndDistanceMultiplier = settings.GetValue<int>("Fog", "LinearEndDistance") * 0.01f;
            fogDensityModifier = settings.GetValue<float>("Fog", "ExponentialDensityOffset");
        }

        if (change.HasChanged("SmoothClipping"))
        {
            smoothClipStartDistance = settings.GetValue<int>("SmoothClipping", "StartDistance")*0.01f;
            textureDitherSize = settings.GetValue<int>("SmoothClipping", "DitherSize");
        }

        if (change.HasChanged("ImprovedAmbientLight"))
        {
            sunLightScale = settings.GetValue<float>("ImprovedAmbientLight", "SunLightScale");
            moonLightScale = settings.GetValue<float>("ImprovedAmbientLight", "MoonLightScale");
            lightColor = settings.GetValue<int>("ImprovedAmbientLight", "LightColor");
            lightColorScale = settings.GetValue<float>("ImprovedAmbientLight", "LightColorScale");
            ambientLightExteriorDayScale = settings.GetValue<float>("ImprovedAmbientLight", "ExteriorDayLightScale");
            ambientLightExteriorNightScale = settings.GetValue<float>("ImprovedAmbientLight", "ExteriorNightLightScale");
            ambientLightInteriorDayScale = settings.GetValue<float>("ImprovedAmbientLight", "InteriorDayLightScale");
            ambientLightInteriorNightScale = settings.GetValue<float>("ImprovedAmbientLight", "InteriorNightLightScale");
            ambientLightCastleScale = settings.GetValue<float>("ImprovedAmbientLight", "CastleLightScale");
            ambientLightDungeonScale = settings.GetValue<float>("ImprovedAmbientLight", "DungeonLightScale");
        }

        if (change.HasChanged("YShearing"))
        {
            shearingKeyCode = SetKeyFromText(settings.GetString("YShearing", "ToggleInput"));
        }

        if (change.HasChanged("Debug"))
        {
            debugShowMessages = settings.GetValue<bool>("Debug", "ShowMessages");
        }

        if (change.HasChanged("Modules") || change.HasChanged("ViewDistance") || change.HasChanged("ImprovedAmbientLight") || change.HasChanged("Fog") || change.HasChanged("SmoothClipping"))
        {
            if (view)
                ApplyViewDistance();
            else
                ResetViewDistance();

            if (fog)
                ApplyFog((FogMode)settings.GetValue<int>("Fog", "Type") + 1);
            else
                ResetFog();

            if (smoothClip)
                ApplyFadeShader();
            else
                RemoveFadeShader();

            ToggleImprovedExteriorLighting(ambientLighting);

            isShearing = shearing;
        }
    }

    private void ModCompatibilityChecking()
    {
        DynamicSkies = ModManager.Instance.GetModFromGUID("53a9b8f5-6271-4f74-9b8b-9220dd105a04");
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        weatherManager = GameManager.Instance.WeatherManager;
        worldTime = DaggerfallUnity.Instance.WorldTime;
        playerEnterExit = GameManager.Instance.PlayerEnterExit;
        playerAmbientLight = GameManager.Instance.PlayerObject.GetComponent<PlayerAmbientLight>();
        sky = GameManager.Instance.SkyRig;

        SunnyFogSettingsDefault = weatherManager.SunnyFogSettings;
        OvercastFogSettingsDefault = weatherManager.OvercastFogSettings;
        RainyFogSettingsDefault = weatherManager.RainyFogSettings;
        SnowyFogSettingsDefault = weatherManager.SnowyFogSettings;
        HeavyFogSettingsDefault = weatherManager.HeavyFogSettings;

        SpawnShearingCamera();

        WorldTime.OnCityLightsOn += ApplyFadeShader_OnCityLights;
        WorldTime.OnCityLightsOff += ApplyFadeShader_OnCityLights;

        PlayerEnterExit.OnTransitionInterior += ApplyViewDistance_OnTransitionInterior;
        PlayerEnterExit.OnTransitionDungeonInterior += ApplyViewDistance_OnTransitionInterior;
        PlayerEnterExit.OnTransitionExterior += ApplyViewDistance_OnTransitionExterior;
        PlayerEnterExit.OnTransitionDungeonExterior += ApplyViewDistance_OnTransitionExterior;

        StreamingWorld.OnUpdateTerrainsEnd += ApplyFadeShader_OnUpdateTerrainsEnd;
        PlayerEnterExit.OnTransitionExterior += ApplyFadeShader_OnTransitionExterior;
        PlayerEnterExit.OnTransitionDungeonExterior += ApplyFadeShader_OnTransitionExterior;
        SaveLoadManager.OnLoad += ApplyFadeShader_OnLoad;
        DaggerfallTravelPopUp.OnPostFastTravel += ApplyFadeShader_OnPostFastTravel;

        PlayerEnterExit.OnTransitionInterior += ToggleShearingCamera_OnTransition;
        PlayerEnterExit.OnTransitionDungeonInterior += ToggleShearingCamera_OnTransition;
        PlayerEnterExit.OnTransitionExterior += ToggleShearingCamera_OnTransition;
        PlayerEnterExit.OnTransitionDungeonExterior += ToggleShearingCamera_OnTransition;
        SaveLoadManager.OnLoad += ToggleShearingCamera_OnLoad;

        sunLight = GameManager.Instance.SunlightManager.GetComponent<Light>();

        CrosshairTexture = DaggerfallUI.GetTextureFromResources(defaultCrosshairFilename, out crosshairSize);
        crosshairSize /= 16;
            
        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

        ModCompatibilityChecking();

        mod.IsReady = true;
    }

    void ToggleImprovedExteriorLighting(bool setting)
    {
        if (playerAmbientLight == null)
            return;

        if (!ambientLightingInitialized)
        {
            ambientLightingInitialized = true;

            ExteriorNoonAmbientLightDefault = playerAmbientLight.ExteriorNoonAmbientLight;
            ExteriorNightAmbientLightDefault = playerAmbientLight.ExteriorNightAmbientLight;
            InteriorAmbientLightDefault = playerAmbientLight.InteriorAmbientLight;
            InteriorNightAmbientLightDefault = playerAmbientLight.InteriorNightAmbientLight;
            InteriorAmbientLight_AmbientOnlyDefault = playerAmbientLight.InteriorAmbientLight_AmbientOnly;
            InteriorNightAmbientLight_AmbientOnlyDefault = playerAmbientLight.InteriorNightAmbientLight_AmbientOnly;
            DungeonAmbientLightDefault = playerAmbientLight.DungeonAmbientLight;
            CastleAmbientLightDefault = playerAmbientLight.CastleAmbientLight;
        }

        if (setting && DynamicSkies == null)
        {
            playerAmbientLight.enabled = false;
            GameManager.Instance.SunlightManager.IndirectLight.enabled = false;

            if (sunLight != null)
            {
                if (DaggerfallUnity.Settings.ExteriorLightShadows)
                    sunLight.shadows = LightShadows.Hard;
                else
                    sunLight.shadows = LightShadows.None;
            }

            //set colors to scaled
            playerAmbientLight.ExteriorNoonAmbientLight = ExteriorNoonAmbientLightDefault * ambientLightExteriorDayScale;
            playerAmbientLight.ExteriorNightAmbientLight = ExteriorNightAmbientLightDefault * ambientLightExteriorNightScale;
            playerAmbientLight.InteriorAmbientLight = InteriorAmbientLightDefault * ambientLightInteriorDayScale;
            playerAmbientLight.InteriorNightAmbientLight = InteriorNightAmbientLightDefault * ambientLightInteriorNightScale;
            playerAmbientLight.InteriorAmbientLight_AmbientOnly = InteriorAmbientLight_AmbientOnlyDefault * ambientLightInteriorDayScale;
            playerAmbientLight.InteriorNightAmbientLight_AmbientOnly = InteriorNightAmbientLight_AmbientOnlyDefault * ambientLightInteriorNightScale;
            playerAmbientLight.DungeonAmbientLight = DungeonAmbientLightDefault * ambientLightDungeonScale;
            playerAmbientLight.CastleAmbientLight = CastleAmbientLightDefault * ambientLightCastleScale;
        }
        else
        {
            playerAmbientLight.enabled = true;
            GameManager.Instance.SunlightManager.IndirectLight.enabled = true;

            if (sunLight != null)
            {
                if (DaggerfallUnity.Settings.ExteriorLightShadows)
                    sunLight.shadows = LightShadows.Soft;
                else
                    sunLight.shadows = LightShadows.None;
            }

            //set colors to default
            playerAmbientLight.ExteriorNoonAmbientLight = ExteriorNoonAmbientLightDefault;
            playerAmbientLight.ExteriorNightAmbientLight = ExteriorNightAmbientLightDefault;
            playerAmbientLight.InteriorAmbientLight = InteriorAmbientLightDefault;
            playerAmbientLight.InteriorNightAmbientLight = InteriorNightAmbientLightDefault;
            playerAmbientLight.InteriorAmbientLight_AmbientOnly = InteriorAmbientLight_AmbientOnlyDefault;
            playerAmbientLight.InteriorNightAmbientLight_AmbientOnly = InteriorNightAmbientLight_AmbientOnlyDefault;
            playerAmbientLight.DungeonAmbientLight = DungeonAmbientLightDefault;
            playerAmbientLight.CastleAmbientLight = CastleAmbientLightDefault;
        }
    }

    void SpawnShearingCamera()
    {
        Camera mainCamera = GameManager.Instance.MainCamera;
        PostProcessLayer mainPostprocessingLayer = mainCamera.gameObject.GetComponent<PostProcessLayer>();

        var go = new GameObject(mod.Title + " - Shearing Camera");

        //disable the gameobject to prevent post-processing layer from shitting itself immediately after being copied
        go.SetActive(false);

        shearingEye = go.AddComponent<Camera>();
        shearingEye.transform.SetParent(mainCamera.transform.parent);
        shearingEye.enabled = false;

        shearingEye.CopyFrom(mainCamera);
        shearingEye.depth = 0;

        shearingPostprocessingLayer = CopyComponent<PostProcessLayer>(mainPostprocessingLayer,go);
        shearingPostprocessingLayer.volumeTrigger = shearingEye.transform;

        //enable the gameobject after adding in the post-processing layer
        go.SetActive(true);
    }

    void ToggleShearingCamera(bool state)
    {
        Camera mainCamera = GameManager.Instance.MainCamera;
        PostProcessLayer mainPostprocessingLayer = mainCamera.gameObject.GetComponent<PostProcessLayer>();

        shearingEye.enabled = state;

        if (state)
        {
            shearingEye.gameObject.tag = "MainCamera";
            mainCamera.gameObject.tag = "Untagged";

            mainCamera.depth = -5;
            sky.SkyCamera.depth = -3;

            //set variables
            shearingEye.CopyFrom(mainCamera);
            shearingEye.depth = 0;

            DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = false;
        }
        else
        {
            mainCamera.gameObject.tag = "MainCamera";
            shearingEye.gameObject.tag = "Untagged";

            mainCamera.depth = 0;
            sky.SkyCamera.depth = -3;

            DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = DaggerfallUnity.Settings.Crosshair;
        }
    }

    void UpdateShearingCamera()
    {
        //copy main camera viewport rect and such
        Camera mainCamera = GameManager.Instance.MainCamera;
        shearingEye.rect = mainCamera.rect;
        shearingEye.nearClipPlane = mainCamera.nearClipPlane;
        shearingEye.farClipPlane = mainCamera.farClipPlane;

        //copy world position
        shearingEye.transform.position = mainCamera.transform.position;

        //get Y world rotation
        shearingEye.transform.eulerAngles = new Vector3(0,mainCamera.transform.eulerAngles.y,0);

        //derive pitch angle
        float angle = Vector3.SignedAngle(shearingEye.transform.forward, mainCamera.transform.forward, shearingEye.transform.right);

        Matrix4x4 mat = shearingEye.projectionMatrix;
        mat[1, 2] = -angle / (shearingEye.fieldOfView/2f);
        shearingEye.projectionMatrix = mat;
    }

    void UpdateShearingCrosshair()
    {
        if (isShearing)
        {
            Camera mainCamera = GameManager.Instance.MainCamera;
            Vector3 crosshairPoint = mainCamera.transform.position + mainCamera.transform.forward * 5;
            crosshairOffset = shearingEye.WorldToViewportPoint(crosshairPoint);
        }
        else
            crosshairOffset = new Vector3(0.5f,0.5f,0);
    }

    private void OnGUI()
    {
        // Do not draw crosshair when cursor is active - i.e. player is now using mouse to point and click not crosshair target
        if (GameManager.Instance.PlayerMouseLook.cursorActive || GameManager.IsGamePaused || !DaggerfallUI.Instance.DaggerfallHUD.Enabled)
            return;

        if (shearing)
        {
            GUI.depth = 0;

            if (DaggerfallUI.Instance.CustomScreenRect != null)
                screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
            else
                screenRect = new Rect(0, 0, Screen.width, Screen.height);

            float screenScaleY = (float)screenRect.height / nativeScreenHeight;
            float screenScaleX = (float)screenRect.width / nativeScreenWidth;

            //Vector2 crosshairTextureScale = new Vector2(CrosshairTexture.width * screenScaleX, CrosshairTexture.height * screenScaleY);
            Vector2 crosshairTextureScale = new Vector2(CrosshairTexture.width, CrosshairTexture.height) * crosshairSize;

            Rect crosshairRect = new Rect(
                screenRect.x + (screenRect.width * 0.5f) - (crosshairTextureScale.x * 0.5f),
                screenRect.y + (screenRect.height * (1-crosshairOffset.y)) - (crosshairTextureScale.y * 0.5f),
                crosshairTextureScale.x,
                crosshairTextureScale.y
                );

            if (!isShearing && !DaggerfallUnity.Settings.Crosshair)
                return;

            DaggerfallUI.DrawTexture(crosshairRect, CrosshairTexture, ScaleMode.StretchToFill, false, Color.white);
        }
    }

    T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        Component copy = destination.AddComponent(type);
        System.Reflection.FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (System.Reflection.FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }
        return copy as T;
    }

    public void StartToggleShearingCameraDelayed(bool state)
    {
        StartCoroutine(ToggleShearingCameraDelayed(state));
    }

    IEnumerator ToggleShearingCameraDelayed(bool state, float delay = 0.1f)
    {
        yield return new WaitForSeconds(delay);

        ToggleShearingCamera(state);
    }

    public static void ToggleShearingCamera_OnLoad(SaveData_v1 saveData)
    {
        Instance.StartToggleShearingCameraDelayed(Instance.shearing);
    }

    public static void ToggleShearingCamera_OnTransition(PlayerEnterExit.TransitionEventArgs args)
    {
        Instance.StartToggleShearingCameraDelayed(Instance.shearing);
    }

    private void LateUpdate()
    {
        if (shearing)
        {
            if (GameManager.Instance.IsPlayingGame())
            {
                if (InputManager.Instance.GetKeyUp(shearingKeyCode))
                    isShearing = !isShearing;

                if (isShearing)
                {
                    if (!Instance.shearingEye.enabled)
                        ToggleShearingCamera(shearing);

                    UpdateShearingCamera();
                }
                else
                {
                    if (Instance.shearingEye.enabled)
                        ToggleShearingCamera(shearing);
                }

                UpdateShearingCrosshair();
            }
        }
        else
        {
            if (GameManager.Instance.IsPlayingGame())
            {
                if (Instance.shearingEye.enabled)
                    ToggleShearingCamera(shearing);
            }
        }

        if (ambientLighting && sunLight != null && DynamicSkies == null)
        {
            if (GameManager.Instance.IsPlayingGame())
            {
                Color skyColor = Color.white;

                if (lightColor == 1)
                    skyColor = RenderSettings.fogColor;
                else
                    skyColor = sky.skyColors.west[Mathf.RoundToInt(sky.skyColors.west.Length/2)];

                lastCameraClearColor = Scale(skyColor, 0.5f * lightColorScale, 2 * sunLightScale);
                if (sunLight.color != lastCameraClearColor)
                    sunLight.color = lastCameraClearColor;

                //mess with ambient light
                if (playerAmbientLight != null)
                {
                    UpdateAmbientLight();

                    if (!playerEnterExit.IsPlayerInside)
                    {
                        if (worldTime.Now.IsNight)
                        {
                            //always use sky color at night
                            //skyColor = sky.skyColors.west[sky.skyColors.west.Length - 1];
                            skyColor = sky.skyColors.west[Mathf.RoundToInt(sky.skyColors.west.Length / 2)];
                            lastAmbientColor *= Scale(skyColor, 0.5f * lightColorScale, 10f * moonLightScale);
                        }
                        else
                            lastAmbientColor *= Scale(skyColor, 0.5f * lightColorScale, 0.5f);
                    }

                    if (RenderSettings.ambientLight != lastAmbientColor)
                    {
                        RenderSettings.ambientLight = Vector4.MoveTowards(RenderSettings.ambientLight, lastAmbientColor, 1f * Time.deltaTime);
                    }
                }
            }
        }
    }
    public void UpdateAmbientLight()
    {
        if (!playerEnterExit)
            return;

        if (!playerEnterExit.IsPlayerInside && !playerEnterExit.IsPlayerInsideDungeon)
        {
            lastAmbientColor = CalcDaytimeAmbientLight();
        }
        else if (playerEnterExit.IsPlayerInside && !playerEnterExit.IsPlayerInsideDungeon)
        {
            if (worldTime.Now.IsNight)
                lastAmbientColor = (DaggerfallUnity.Settings.AmbientLitInteriors) ? playerAmbientLight.InteriorNightAmbientLight_AmbientOnly : playerAmbientLight.InteriorNightAmbientLight;
            else
                lastAmbientColor = (DaggerfallUnity.Settings.AmbientLitInteriors) ? playerAmbientLight.InteriorAmbientLight_AmbientOnly : playerAmbientLight.InteriorAmbientLight;
        }
        else if (playerEnterExit.IsPlayerInside && playerEnterExit.IsPlayerInsideDungeon)
        {
            if (playerEnterExit.IsPlayerInsideDungeonCastle)
                lastAmbientColor = playerAmbientLight.CastleAmbientLight;
            else if (playerEnterExit.IsPlayerInsideSpecialArea)
                lastAmbientColor = playerAmbientLight.SpecialAreaLight;
            else
                lastAmbientColor = playerAmbientLight.DungeonAmbientLight;
        }
    }

    Color Scale(Color color, float saturation, float brightness)
    {
        float h;
        float s;
        float v;

        Color.RGBToHSV(color, out h, out s, out v);

        s *= saturation;
        v *= brightness;

        return Color.HSVToRGB(h,s,v);
    }

    Color CalcDaytimeAmbientLight()
    {
        float scale = GameManager.Instance.SunlightManager.DaylightScale * GameManager.Instance.SunlightManager.ScaleFactor;

        float weather = 1;
        // Apply rain, storm, snow light scale
        if (weatherManager.IsRaining && !weatherManager.IsStorming)
        {
            weather = RainSunlightScaleDefault;
        }
        else if (weatherManager.IsRaining && weatherManager.IsStorming)
        {
            weather = StormSunlightScaleDefault;
        }
        else if (weatherManager.IsSnowing)
        {
            weather = SnowSunlightScaleDefault;
        }
        else if (weatherManager.IsOvercast)
        {
            weather = OvercastSunlightScaleDefault;
        }

        Color startColor = playerAmbientLight.ExteriorNightAmbientLight * weather;

        return Color.Lerp(startColor, playerAmbientLight.ExteriorNoonAmbientLight * weather, scale);
    }

    void ResetViewDistance()
    {
        GameManager.Instance.MainCamera.farClipPlane = 2600;

        bool updateWorld = false;
        if (GameManager.Instance.StreamingWorld.TerrainDistance != DaggerfallUnity.Settings.TerrainDistance)
            updateWorld = true;

        GameManager.Instance.StreamingWorld.TerrainDistance = DaggerfallUnity.Settings.TerrainDistance;

        if (updateWorld)
            ForceUpdateWorld();
    }

    void ResetFog()
    {
        weatherManager.SunnyFogSettings = SunnyFogSettingsDefault;
        weatherManager.OvercastFogSettings = OvercastFogSettingsDefault;
        weatherManager.RainyFogSettings = RainyFogSettingsDefault;
        weatherManager.SnowyFogSettings = SnowyFogSettingsDefault;
        weatherManager.HeavyFogSettings = HeavyFogSettingsDefault;

        //Reset the current weather for the fog settings to take effect
        weatherManager.SetWeather(weatherManager.PlayerWeather.WeatherType);
    }

    void ApplyFog(FogMode fogMode)
    {
        if (!fog)
            return;

        if (currentFogMode != fogMode)
        {
            currentFogMode = fogMode;
            SunnyFogSettings.fogMode = currentFogMode;
            OvercastFogSettings.fogMode = currentFogMode;
            RainyFogSettings.fogMode = currentFogMode;
            SnowyFogSettings.fogMode = currentFogMode;
            HeavyFogSettings.fogMode = currentFogMode;
        }

        float distance = GameManager.Instance.MainCamera.farClipPlane;

        if (fogMode == FogMode.Linear)
        {
            float fogStartDistance = distance * fogStartDistanceMultiplier;
            float fogEndDistance = distance * fogEndDistanceMultiplier;

            float multiplier = distance / 64;

            SunnyFogSettings.startDistance = fogStartDistance - (2 * multiplier);
            SunnyFogSettings.endDistance = fogEndDistance - (2 * multiplier);

            OvercastFogSettings.startDistance = fogStartDistance - (4 * multiplier);
            OvercastFogSettings.endDistance = fogEndDistance - (4 * multiplier);

            RainyFogSettings.startDistance = fogStartDistance - (8 * multiplier);
            RainyFogSettings.endDistance = fogEndDistance - (8 * multiplier);

            SnowyFogSettings.startDistance = fogStartDistance - (16 * multiplier);
            SnowyFogSettings.endDistance = fogEndDistance - (16 * multiplier);

            HeavyFogSettings.startDistance = fogStartDistance - (32 * multiplier);
            HeavyFogSettings.endDistance = fogEndDistance - (32 * multiplier);
        }

        if (fogMode == FogMode.Exponential || fogMode == FogMode.ExponentialSquared)
        {
            //default fog density is 0.0005, too thin for my taste
            float density = 0.001f * fogDensityModifier;
            if (view)
                density = fogDensities[viewDistance] * fogDensityModifier;

            SunnyFogSettings.density = density * 1;
            OvercastFogSettings.density = density * 2f;
            RainyFogSettings.density = density * 3;
            SnowyFogSettings.density = density * 3;
            HeavyFogSettings.density = density * 4;
        }

        weatherManager.SunnyFogSettings = SunnyFogSettings;
        weatherManager.OvercastFogSettings = OvercastFogSettings;
        weatherManager.RainyFogSettings = RainyFogSettings;
        weatherManager.SnowyFogSettings = SnowyFogSettings;
        weatherManager.HeavyFogSettings = HeavyFogSettings;

        //Reset the current weather for the fog settings to take effect
        weatherManager.SetWeather(weatherManager.PlayerWeather.WeatherType);
    }

    void ApplyViewDistance()
    {
        if (!view)
            return;

        bool updateWorld = false;
        if (GameManager.Instance.StreamingWorld.TerrainDistance != terrainDistances[viewDistance])
            updateWorld = true;

        GameManager.Instance.StreamingWorld.TerrainDistance = terrainDistances[viewDistance];

        if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
        {
            GameManager.Instance.MainCamera.farClipPlane = 2600;
        }
        else
        {
            GameManager.Instance.MainCamera.farClipPlane = viewDistances[viewDistance];
            if (updateWorld)
                ForceUpdateWorld();
        }
    }

    void ForceUpdateWorld()
    {
        Debug.Log("TAIL OF ARGONIA - FORCE UPDATE WORLD");
        if (mod.IsReady && worldUpdateMessage == null)
        {
            worldUpdateMessage = ForceUpdateWorldCoroutine();
            StartCoroutine(worldUpdateMessage);
        }
    }

    IEnumerator ForceUpdateWorldCoroutine()
    {
        while (!GameManager.Instance.IsPlayingGame())
            yield return new WaitForSeconds(1);

        DaggerfallUI.MessageBox("TerrainDistance setting has changed. Reload a save or travel to another location to apply the new setting.",true);

        worldUpdateMessage = null;
    }

    public void SetDefaultViewDistance()
    {
        if (!view)
            return;

        GameManager.Instance.MainCamera.farClipPlane = 2600;
    }

    public void SetCustomViewDistance()
    {
        if (!view)
            return;

        GameManager.Instance.MainCamera.farClipPlane = viewDistances[viewDistance];
    }

    public static void ApplyViewDistance_OnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
    {
        Instance.SetDefaultViewDistance();
    }

    public static void ApplyViewDistance_OnTransitionExterior(PlayerEnterExit.TransitionEventArgs args)
    {
        Instance.SetCustomViewDistance();
    }

    public static void ApplyFadeShader_OnUpdateTerrainsEnd()
    {
        Instance.ApplyFadeShaderDelayed();
    }

    public static void ApplyFadeShader_OnTransitionExterior (PlayerEnterExit.TransitionEventArgs args)
    {
        Instance.ApplyFadeShaderDelayed();
    }

    public static void ApplyFadeShader_OnLoad(SaveData_v1 saveData)
    {
        Instance.ApplyFadeShaderDelayed();
    }

    public static void ApplyFadeShader_OnPostFastTravel()
    {
        Instance.ApplyFadeShaderDelayed();
    }

    public static void ApplyFadeShader_OnCityLights()
    {
        Instance.ApplyFadeShaderDelayed();
    }

    public void ApplyFadeShaderDelayed()
    {
        StartCoroutine(ApplyFadeShaderCoroutine());
    }

    IEnumerator ApplyFadeShaderCoroutine()
    {
        yield return new WaitForSeconds(0.1f);

        ApplyFadeShader();
    }

    public void RemoveFadeShader()
    {
        if (Time.time - smoothClipLastUpdate < 2)
            return;

        smoothClipLastUpdate = Time.time;

        if (debugShowMessages)
        {
            Debug.Log("TAIL OF ARGONIA - Removing fade shader!");
            DaggerfallUI.Instance.PopupMessage("Removing fade shader!");
        }

        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();

        //Debug.Log("TAIL OF ARGONIA - INITIALIZING OBJECT SMOOTH CLIPPING");
        foreach (MeshRenderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                Material material = renderer.materials[i];
                if (material.shader == shaderDefault)
                {
                    //Debug.Log("TAIL OF ARGONIA - FOUND A DEFAULT MATERIAL");
                    Color color = material.GetColor("_Color");
                    Color colorSpec = material.GetColor("_SpecColor");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");
                    Texture texParallax = material.GetTexture("_ParallaxMap");
                    float scaleParallax = material.GetFloat("_Parallax");
                    Texture texMetallic = material.GetTexture("_MetallicGlossMap");
                    float scaleSmoothness = material.GetFloat("_Smoothness");

                    Material newMat = new Material(Shader.Find("Daggerfall/Default"));
                    newMat.SetColor("_Color", color);
                    newMat.SetColor("_SpecColor", colorSpec);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);
                    newMat.SetTexture("_ParallaxMap", texParallax);
                    newMat.SetFloat("_Parallax", scaleParallax);
                    newMat.SetTexture("_MetallicGlossMap", texMetallic);
                    newMat.SetFloat("_Smoothness", scaleSmoothness);

                    materials[i] = newMat;
                }
                else if (material.shader == shaderBillboard)
                {
                    Billboard billboard = renderer.GetComponent<Billboard>();
                    if (billboard != null)
                    {
                        if (billboard.Summary.IsMobile)
                            continue;
                    }
                    //Debug.Log("TAIL OF ARGONIA - FOUND A BILLBOARD MATERIAL");
                    Color color = material.GetColor("_Color");
                    float scaleCutoff = material.GetFloat("_Cutoff");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");

                    Material newMat = new Material(Shader.Find("Daggerfall/Billboard"));
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Cutoff", scaleCutoff);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);

                    materials[i] = newMat;
                }
                else if (material.shader == shaderBillboardBatch)
                {
                    //Debug.Log("TAIL OF ARGONIA - FOUND A BILLBOARD BATCH MATERIAL");
                    Color color = material.GetColor("_Color");
                    float scaleCutoff = material.GetFloat("_Cutoff");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");
                    Vector4 vectorUp = material.GetVector("_UpVector");

                    Material newMat = new Material(Shader.Find("Daggerfall/BillboardBatch"));
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Cutoff", scaleCutoff);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);
                    newMat.SetVector("_UpVector", vectorUp);

                    materials[i] = newMat;
                }
                else if (material.shader == shaderBillboardBatchNoShadows)
                {
                    //Debug.Log("TAIL OF ARGONIA - FOUND A BILLBOARD BATCH NO SHADOWS MATERIAL");
                    Color color = material.GetColor("_Color");
                    float scaleCutoff = material.GetFloat("_Cutoff");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");
                    Vector4 vectorUp = material.GetVector("_UpVector");

                    Material newMat = new Material(Shader.Find("Daggerfall/BillboardBatchNoShadows"));
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Cutoff", scaleCutoff);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);
                    newMat.SetVector("_UpVector", vectorUp);

                    materials[i] = newMat;
                }
            }
            renderer.materials = materials;
        }

        Terrain[] terrains = FindObjectsOfType<Terrain>();

        foreach (Terrain terrain in terrains)
        {
            if (SystemInfo.supports2DArrayTextures && DaggerfallUnity.Settings.EnableTextureArrays && terrain.materialTemplate.shader == shaderTilemapTextureArray)
            {
                Texture tileTextureArray = terrain.materialTemplate.GetTexture("_TileTexArr");
                Texture tileNormalMapTextureArray = terrain.materialTemplate.GetTexture("_TileNormalMapTexArr");
                Texture tileMetallicGlossMapTextureArray = terrain.materialTemplate.GetTexture("_TileMetallicGlossMapTexArr");
                Texture tileMapTexture = terrain.materialTemplate.GetTexture("_TilemapTex");
                int tileMapDim = terrain.materialTemplate.GetInt("_TilemapDim");

                Material newMat = new Material(Shader.Find("Daggerfall/TilemapTextureArray"));
                newMat.SetTexture("_TileTexArr", tileTextureArray);
                newMat.SetTexture("_TileNormalMapTexArr", tileNormalMapTextureArray);
                if (terrain.materialTemplate.IsKeywordEnabled("_NORMALMAP"))
                    newMat.EnableKeyword("_NORMALMAP");
                else
                    newMat.DisableKeyword("_NORMALMAP");
                newMat.SetTexture("_TileMetallicGlossMapTexArr", tileMetallicGlossMapTextureArray);
                newMat.SetTexture("_TilemapTex", tileMapTexture);
                newMat.SetInt("_TilemapDim", tileMapDim);

                terrain.materialTemplate = newMat;
            }
            else if (terrain.materialTemplate.shader == shaderTilemap)
            {
                Texture tileSetTexture = terrain.materialTemplate.GetTexture("_TileAtlasTex");
                Texture tileMapTexture = terrain.materialTemplate.GetTexture("_TilemapTex");
                int tileMapDim = terrain.materialTemplate.GetInt("_TilemapDim");

                Material newMat = new Material(Shader.Find("Daggerfall/Tilemap"));
                newMat.SetTexture("_TileAtlasTex", tileSetTexture);
                newMat.SetTexture("_TilemapTex", tileMapTexture);
                newMat.SetInt("_TilemapDim", tileMapDim);

                if (textureDitherSize == 1)
                    newMat.SetTexture("_DitherPattern", textureDitherLarge);
                else
                    newMat.SetTexture("_DitherPattern", textureDitherSmall);
                newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                terrain.materialTemplate = newMat;
            }
        }
    }

    public void ApplyFadeShader()
    {
        if (!smoothClip)
            return;

        if (Time.time-smoothClipLastUpdate < 2)
            return;

        if (
            shaderDefault == null ||
            shaderBillboard == null ||
            shaderBillboardBatch == null ||
            shaderBillboardBatchNoShadows == null ||
            shaderTilemap == null ||
            shaderTilemapTextureArray == null
            )
        {
            if (debugShowMessages)
            {
                Debug.Log("TAIL OF ARGONIA - A FADE SHADER WAS NOT FOUND. ABORTING!");
                DaggerfallUI.Instance.PopupMessage("Applying fade shader!");
            }
            return;
        }

        smoothClipLastUpdate = Time.time;

        if (debugShowMessages)
        {
            Debug.Log("TAIL OF ARGONIA - Applying fade shader!");
            DaggerfallUI.Instance.PopupMessage("Applying fade shader!");
        }

        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();

        //Debug.Log("TAIL OF ARGONIA - INITIALIZING OBJECT SMOOTH CLIPPING");
        foreach (MeshRenderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                Material material = renderer.materials[i];
                if (material.shader.name == "Daggerfall/Default" || material.shader == shaderDefault)
                {
                    //Debug.Log("TAIL OF ARGONIA - FOUND A DEFAULT MATERIAL");
                    Color color = material.GetColor("_Color");
                    Color colorSpec = material.GetColor("_SpecColor");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");
                    Texture texParallax = material.GetTexture("_ParallaxMap");
                    float scaleParallax = material.GetFloat("_Parallax");
                    Texture texMetallic = material.GetTexture("_MetallicGlossMap");
                    float scaleSmoothness = material.GetFloat("_Smoothness");

                    Material newMat = new Material(shaderDefault);
                    newMat.SetColor("_Color", color);
                    newMat.SetColor("_SpecColor", colorSpec);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);
                    newMat.SetTexture("_ParallaxMap", texParallax);
                    newMat.SetFloat("_Parallax", scaleParallax);
                    newMat.SetTexture("_MetallicGlossMap", texMetallic);
                    newMat.SetFloat("_Smoothness", scaleSmoothness);

                    if (textureDitherSize == 1)
                        newMat.SetTexture("_DitherPattern", textureDitherLarge);
                    else
                        newMat.SetTexture("_DitherPattern", textureDitherSmall);
                    newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                    materials[i] = newMat;
                }
                else if (material.shader.name == "Daggerfall/Billboard" || material.shader == shaderBillboard)
                {
                    Billboard billboard = renderer.GetComponent<Billboard>();
                    if (billboard != null)
                    {
                        if (billboard.Summary.IsMobile)
                            continue;
                    }
                    //Debug.Log("TAIL OF ARGONIA - FOUND A BILLBOARD MATERIAL");
                    Color color = material.GetColor("_Color");
                    float scaleCutoff = material.GetFloat("_Cutoff");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");

                    Material newMat = new Material(shaderBillboard);
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Cutoff", scaleCutoff);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);

                    if (textureDitherSize == 1)
                        newMat.SetTexture("_DitherPattern", textureDitherLarge);
                    else
                        newMat.SetTexture("_DitherPattern", textureDitherSmall);
                    newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                    materials[i] = newMat;
                }
                else if (material.shader.name == "Daggerfall/BillboardBatch" || material.shader == shaderBillboardBatch)
                {
                    //Debug.Log("TAIL OF ARGONIA - FOUND A BILLBOARD BATCH MATERIAL");
                    Color color = material.GetColor("_Color");
                    float scaleCutoff = material.GetFloat("_Cutoff");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");
                    Vector4 vectorUp = material.GetVector("_UpVector");

                    Material newMat = new Material(shaderBillboardBatch);
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Cutoff", scaleCutoff);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);
                    newMat.SetVector("_UpVector", vectorUp);

                    if (textureDitherSize == 1)
                        newMat.SetTexture("_DitherPattern", textureDitherLarge);
                    else
                        newMat.SetTexture("_DitherPattern", textureDitherSmall);
                    newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                    materials[i] = newMat;
                }
                else if (material.shader.name == "Daggerfall/BillboardBatchNoShadows" || material.shader == shaderBillboardBatchNoShadows)
                {
                    //Debug.Log("TAIL OF ARGONIA - FOUND A BILLBOARD BATCH NO SHADOWS MATERIAL");
                    Color color = material.GetColor("_Color");
                    float scaleCutoff = material.GetFloat("_Cutoff");
                    Texture texMain = material.GetTexture("_MainTex");
                    Texture texBump = material.GetTexture("_BumpMap");
                    Texture texEmission = material.GetTexture("_EmissionMap");
                    Color colorEmission = material.GetColor("_EmissionColor");
                    Vector4 vectorUp = material.GetVector("_UpVector");

                    Material newMat = new Material(shaderBillboardBatchNoShadows);
                    newMat.SetColor("_Color", color);
                    newMat.SetFloat("_Cutoff", scaleCutoff);
                    newMat.SetTexture("_MainTex", texMain);
                    newMat.SetTexture("_BumpMap", texBump);
                    newMat.SetTexture("_EmissionMap", texEmission);
                    newMat.SetColor("_EmissionColor", colorEmission);
                    if (material.IsKeywordEnabled(KeyWords.Emission))
                        newMat.EnableKeyword(KeyWords.Emission);
                    newMat.SetVector("_UpVector", vectorUp);

                    if (textureDitherSize == 1)
                        newMat.SetTexture("_DitherPattern", textureDitherLarge);
                    else
                        newMat.SetTexture("_DitherPattern", textureDitherSmall);
                    newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                    materials[i] = newMat;
                }
            }
            renderer.materials = materials;
        }

        Terrain[] terrains = FindObjectsOfType<Terrain>();

        foreach (Terrain terrain in terrains)
        {
            if (SystemInfo.supports2DArrayTextures && DaggerfallUnity.Settings.EnableTextureArrays)
            {
                Texture tileTextureArray = terrain.materialTemplate.GetTexture("_TileTexArr");
                Texture tileNormalMapTextureArray = terrain.materialTemplate.GetTexture("_TileNormalMapTexArr");
                Texture tileMetallicGlossMapTextureArray = terrain.materialTemplate.GetTexture("_TileMetallicGlossMapTexArr");
                Texture tileMapTexture = terrain.materialTemplate.GetTexture("_TilemapTex");
                int tileMapDim = terrain.materialTemplate.GetInt("_TilemapDim");

                Material newMat = new Material(shaderTilemapTextureArray);
                newMat.SetTexture("_TileTexArr", tileTextureArray);
                newMat.SetTexture("_TileNormalMapTexArr", tileNormalMapTextureArray);
                if (terrain.materialTemplate.IsKeywordEnabled("_NORMALMAP"))
                    newMat.EnableKeyword("_NORMALMAP");
                else
                    newMat.DisableKeyword("_NORMALMAP");
                newMat.SetTexture("_TileMetallicGlossMapTexArr", tileMetallicGlossMapTextureArray);
                newMat.SetTexture("_TilemapTex", tileMapTexture);
                newMat.SetInt("_TilemapDim", tileMapDim);

                if (textureDitherSize == 1)
                    newMat.SetTexture("_DitherPattern", textureDitherLarge);
                else
                    newMat.SetTexture("_DitherPattern", textureDitherSmall);
                newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                terrain.materialTemplate = newMat;
            }
            else
            {
                Texture tileSetTexture = terrain.materialTemplate.GetTexture("_TileAtlasTex");
                Texture tileMapTexture = terrain.materialTemplate.GetTexture("_TilemapTex");
                int tileMapDim = terrain.materialTemplate.GetInt("_TilemapDim");

                Material newMat = new Material(shaderTilemap);
                newMat.SetTexture("_TileAtlasTex", tileSetTexture);
                newMat.SetTexture("_TilemapTex", tileMapTexture);
                newMat.SetInt("_TilemapDim", tileMapDim);

                if (textureDitherSize == 1)
                    newMat.SetTexture("_DitherPattern", textureDitherLarge);
                else
                    newMat.SetTexture("_DitherPattern", textureDitherSmall);
                newMat.SetFloat("_DitherStart", smoothClipStartDistance);

                terrain.materialTemplate = newMat;
            }
        }
    }
    private KeyCode SetKeyFromText(string text)
    {
        Debug.Log("Setting Key");
        if (System.Enum.TryParse(text, false, out KeyCode result))
        {
            Debug.Log("Key set to " + result.ToString());
            return result;
        }
        else
        {
            Debug.Log("Detected an invalid key code. Setting to default.");
            return KeyCode.None;
        }
    }
}
