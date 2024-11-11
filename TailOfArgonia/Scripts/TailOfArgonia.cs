using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

public class TailOfArgonia : MonoBehaviour
{
    public static TailOfArgonia Instance;

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

        if (change.HasChanged("Debug"))
        {
            debugShowMessages = settings.GetValue<bool>("Debug", "ShowMessages");
        }

        if (change.HasChanged("Modules") || change.HasChanged("ViewDistance") || change.HasChanged("Fog") || change.HasChanged("SmoothClipping"))
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

        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        WeatherManager weatherManager = GameManager.Instance.WeatherManager;

        SunnyFogSettingsDefault = weatherManager.SunnyFogSettings;
        OvercastFogSettingsDefault = weatherManager.OvercastFogSettings;
        RainyFogSettingsDefault = weatherManager.RainyFogSettings;
        SnowyFogSettingsDefault = weatherManager.SnowyFogSettings;
        HeavyFogSettingsDefault = weatherManager.HeavyFogSettings;

        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

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

        mod.IsReady = true;
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
        WeatherManager weatherManager = GameManager.Instance.WeatherManager;

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

        WeatherManager weatherManager = GameManager.Instance.WeatherManager;
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
            OvercastFogSettings.density = density * 1.5f;
            RainyFogSettings.density = density * 2;
            SnowyFogSettings.density = density * 1.5f;
            HeavyFogSettings.density = density * 2;
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
        /*var world = FindObjectOfType<StreamingWorld>();

        if (!world.IsReady)
            return;

        MethodInfo updateWorld = typeof(StreamingWorld)
            .GetMethod("UpdateWorld", BindingFlags.NonPublic | BindingFlags.Instance);
        updateWorld.Invoke(world, Array.Empty<object>());
        Debug.Log("Updated World");

        MethodInfo initPlayerTerrain = typeof(StreamingWorld)
            .GetMethod("InitPlayerTerrain", BindingFlags.NonPublic | BindingFlags.Instance);
        initPlayerTerrain.Invoke(world, Array.Empty<object>());
        Debug.Log("Init'd Player Terrain");

        MethodInfo updateTerrain = typeof(StreamingWorld)
            .GetMethod("UpdateTerrain", BindingFlags.NonPublic | BindingFlags.Instance);
        IEnumerator coroutine = (IEnumerator)updateTerrain.Invoke(world, Array.Empty<object>());
        StartCoroutine(coroutine);
        Debug.Log("Updated Terrain");*/
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
}
