using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

public class FPSWeaponClone : MonoBehaviour
{
    FPSWeapon ScreenWeapon;

    DaggerfallAudioSource dfAudioSource;

    class WeaponAtlas
    {
        public string FileName { get; set; }
        public MetalTypes MetalType { get; set; }
        public Texture2D AtlasTexture { get; set; }
        public Rect[] WeaponRects { get; set; }
        public RecordIndex[] WeaponIndices { get; set; }
    }
    class CustomWeaponAnimation
    {
        public string FileName { get; set; }
        public MetalTypes MetalType { get; set; }
        public Dictionary<int, Texture2D> Textures { get; set; }
    }

    CifRciFile cifFile;

    DaggerfallUnityItem SpecificWeapon;

    public bool ShowWeapon = true;

    WeaponAtlas weaponAtlas;
    readonly WeaponAtlas[] weaponAtlasCache = new WeaponAtlas[2];
    readonly CustomWeaponAnimation[] customWeaponAnimationCache = new CustomWeaponAnimation[2];

    WeaponAnimation[] weaponAnims;
    WeaponStates weaponState = WeaponStates.Idle;
    int currentFrame = 0;
    WeaponTypes currentWeaponType;
    MetalTypes currentMetalType;
    int currentTemplateIndex;

    Rect weaponPosition;
    float weaponOffsetHeight;
    Rect screenRect;
    float weaponScaleX;
    float weaponScaleY;

    const int nativeScreenWidth = 320;
    const int nativeScreenHeight = 200;

    float animTickTime;
    Rect curAnimRect;

    float lastScreenWidth, lastScreenHeight;
    bool lastLargeHUDSetting, lastLargeHUDDockSetting;
    bool lastSheathed;
    float lastWeaponOffsetHeight;
    bool lastDoubleScale;

    Dictionary<int, Texture2D> customTextures = new Dictionary<int, Texture2D>();
    Texture2D curCustomTexture;
    public Vector2 Position { get; set; } = Vector2.zero;   //Moves the sprite in screen distance
    public Vector2 Scale { get; set; } = Vector2.one;
    public Vector2 Offset { get; set; } = Vector2.zero;     //Moves the sprite relative to its own dimensions ("Offset.x = 0.5f" will move "sprite.width * 0.5f"). Applied after Scale.

    IEnumerator animating;
    bool animatingCancel;

    bool swing;
    int swingWindup; //0 = Hide, 1 = Idle, 2 = First Frame
    int swingRecovery; //0 = Hide, 1 = LastFrame
    float swingSpeed;
    bool swingAlignmentOverride;
    bool swingRecoveryOverride;
    bool swingNoDaggerRight;

    //FPS Weapon Movement refugees
    bool bob = true;
    float bobLength = 1.0f;
    float bobOffset = 0f;
    float bobSizeXMod = 0.5f;      //RECOMMEND RANGE OF 0.5-1.5
    float bobSizeYMod = 1.5f;      //RECOMMEND RANGE OF 0.5-1.5
    float moveSmoothSpeed = 1;        //RECOMMEND RANGE OF 1-3
    float bobSmoothSpeed = 1;         //RECOMMEND RANGE OF UP TO 1-3
    float bobShape = 1;
    bool bobWhileIdle = true;

    bool offset = true;
    float offsetSpeed = 1;
    float offsetSpeedLive
    {
        get
        {
            return ((float)GameManager.Instance.PlayerEntity.Stats.LiveSpeed / 100) * offsetSpeed;
        }
    }
    Vector2 offsetCurrent;
    Vector2 offsetTarget;

    bool inertia;
    float inertiaScale = 500;
    float inertiaSpeed = 50;
    float inertiaSpeedMod = 1;
    float inertiaForwardSpeed = 1;
    float inertiaForwardScale = 1;

    bool stepTransforms;
    int stepLength = 8;
    int stepCondition = 0;

    float moveSmooth = 0;
    Vector2 bobSmooth = Vector2.zero;

    Vector2 inertiaCurrent = Vector2.zero;
    Vector2 inertiaTarget;
    Vector2 inertiaForwardCurrent = Vector2.zero;
    Vector2 inertiaForwardTarget;

    bool leftHanded;
    bool ambidexterity;
    bool doubleScale;
    bool recoil;

    float recoilChance = 0.5f;
    int recoilCondition = 0;    //0 = hits only, 1 = hits and parries, 2 = all attacks
    bool hasCurrentAttackHit;

    bool recoilEnvironment;
    bool playMissVFXEntity;
    bool playMissVFXEnvironment;
    int playMissVFXPos; //0 = body, 1 = crosshair

    bool mirrorBows;

    //Mod compatibility
    public event Action<Vector2> OnPositionChange;
    public event Action<Vector2> OnScaleChange;
    public event Action<Vector2> OnOffsetChange;
    bool isInThirdPerson;

    Mod tomeOfBattle;
    float tomeOfBattleWeaponReach = 2.25f;
    KeyCode tomeOfBattleSwingKeyCode = KeyCode.None;

    static Mod mod;
    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<FPSWeaponClone>();
    }

    void Awake()
    {
        ScreenWeapon = GameManager.Instance.WeaponManager.ScreenWeapon;

        // Ensure cif reader is ready
        if (cifFile == null)
        {
            cifFile = new CifRciFile();
            cifFile.Palette.Load(Path.Combine(DaggerfallUnity.Instance.Arena2Path, cifFile.PaletteName));
        }

        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

        ModCompatibilityChecking();

        if (DaggerfallUnity.Settings.Handedness == 1)
            leftHanded = true;

        dfAudioSource = gameObject.AddComponent<DaggerfallAudioSource>();

        DaggerfallAudioSource vanillaDFAS = ScreenWeapon.GetComponent<DaggerfallAudioSource>();
        if (vanillaDFAS)
            vanillaDFAS.AudioSource.mute = true;

        mod.MessageReceiver = MessageReceiver;
        mod.IsReady = true;
    }
    void MessageReceiver(string message, object data, DFModMessageCallback callBack)
    {
        switch (message)
        {
            case "isWeaponVisible":
                callBack?.Invoke("isWeaponVisible", ShowWeapon);
                break;

            case "showWeapon":
                ShowWeapon = true;
                break;

            case "hideWeapon":
                ShowWeapon = false;
                break;

            case "addPosition":
                OnPositionChange += data as Action<Vector2>;
                break;

            case "addScale":
                OnScaleChange += data as Action<Vector2>;
                break;

            case "addOffset":
                OnOffsetChange += data as Action<Vector2>;
                break;

            default:
                Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                break;
        }
    }
    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Modules"))
        {
            swing = settings.GetValue<bool>("Modules", "Swings");
            ambidexterity = settings.GetValue<bool>("Modules", "Ambidexterity");
            bob = settings.GetValue<bool>("Modules", "Bob");
            offset = settings.GetValue<bool>("Modules", "Offset");
            stepTransforms = settings.GetValue<bool>("Modules", "Step");
            inertia = settings.GetValue<bool>("Modules", "Inertia");
            doubleScale = settings.GetValue<bool>("Modules", "DoubleScaleTextures");
            recoil = settings.GetValue<bool>("Modules", "Recoil");
        }
        if (change.HasChanged("Swings"))
        {
            swingWindup = settings.GetValue<int>("Swings", "Windup");
            swingRecovery = settings.GetValue<int>("Swings", "Recovery");
            swingSpeed = settings.GetValue<float>("Swings", "Speed");
            swingAlignmentOverride = settings.GetValue<bool>("Swings", "VanillaAlignmentOverride");
            swingRecoveryOverride = settings.GetValue<bool>("Swings", "VanillaRecoveryOverride");
            swingNoDaggerRight = settings.GetValue<bool>("Swings", "NoDaggerMirroredStrikes");
        }
        if (change.HasChanged("Offset"))
        {
            offsetSpeed = settings.GetValue<float>("Offset", "Speed") * 10;
        }
        if (change.HasChanged("Bob"))
        {
            bobLength = (float)settings.GetValue<int>("Bob", "Length")/100;
            bobOffset = settings.GetValue<float>("Bob", "Offset");
            bobSizeXMod = settings.GetValue<float>("Bob", "SizeX") * 2;
            bobSizeYMod = settings.GetValue<float>("Bob", "SizeY") * 2;
            moveSmoothSpeed = settings.GetValue<float>("Bob", "SpeedMove") * 4;
            bobSmoothSpeed = settings.GetValue<float>("Bob", "SpeedState") * 500;
            bobShape = settings.GetValue<int>("Bob", "Shape") * 0.5f;
            bobWhileIdle = settings.GetValue<bool>("Bob", "BobWhileIdle");
        }
        if (change.HasChanged("Inertia"))
        {
            inertiaScale = settings.GetValue<float>("Inertia", "Scale") * 500;
            inertiaSpeed = settings.GetValue<float>("Inertia", "Speed") * 500;
            inertiaForwardScale = settings.GetValue<float>("Inertia", "ForwardDepth") * 0.2f;
            inertiaForwardSpeed = settings.GetValue<float>("Inertia", "ForwardSpeed") * 0.2f;
        }
        if (change.HasChanged("Step"))
        {
            stepLength = settings.GetValue<int>("Step", "Length");
            stepCondition = settings.GetValue<int>("Step", "Condition");
        }
        if (change.HasChanged("Recoil"))
        {
            recoilChance = (float)settings.GetValue<int>("Recoil", "Chance")/100;
            recoilCondition = settings.GetValue<int>("Recoil", "Condition");
            recoilEnvironment = settings.GetValue<bool>("Recoil", "DetectEnvironment");
            playMissVFXEntity = settings.GetValue<bool>("Recoil", "PlayEntityMissEffects");
            playMissVFXEnvironment = settings.GetValue<bool>("Recoil", "PlayEnvironmentMissEffects");
            playMissVFXPos = settings.GetValue<int>("Recoil", "MissEffectPlacement");
        }
        if (change.HasChanged("Miscellaneous"))
        {
            mirrorBows = settings.GetValue<bool>("Miscellaneous", "MirrorBows");
        }
    }
    private void ModCompatibilityChecking()
    {
        //listen to Eye Of The Beholder for changes in POV
        Mod eotb = ModManager.Instance.GetModFromGUID("2942ea8c-dbd4-42af-bdf9-8199d2f4a0aa");
        if (eotb != null)
        {
            ModManager.Instance.SendModMessage(eotb.Title, "onToggleOffset", (Action<bool>)(toggleState => {
                isInThirdPerson = toggleState;
            }));
        }

        //listen to Combat Event Handler for attacks
        Mod ceh = ModManager.Instance.GetModFromGUID("fb086c76-38e7-4d83-91dc-f29e6f1bb17e");
        if (ceh != null)
        {
            ModManager.Instance.SendModMessage(ceh.Title, "onAttackDamageCalculated", (Action<DaggerfallEntity, DaggerfallEntity, DaggerfallUnityItem, int, int>)OnAttackDamageCalculated);
        }

        //check if Tome of Battle is installed
        tomeOfBattle = ModManager.Instance.GetModFromGUID("a166c215-0a5a-4582-8bf3-8be8df80d5e5");
        if (tomeOfBattle != null)
        {
            ModManager.Instance.SendModMessage(tomeOfBattle.Title, "getSwingKeyCode", null, (string message, object data) =>
            {
                tomeOfBattleSwingKeyCode = (KeyCode)data;
            });
        }
    }
    public void OnAttackDamageCalculated(DaggerfallEntity attacker, DaggerfallEntity target, DaggerfallUnityItem weapon, int bodyPart, int damage)
    {
        //if attacker is not the player or there is no target or recoil is disabled, do nothing
        if (attacker != GameManager.Instance.PlayerEntity || target == null || !recoil)
            return;

        float value = UnityEngine.Random.value;

        if (damage > 0)
        {
            if (recoil && value < recoilChance && (recoilCondition == 0 || recoilCondition == 1 || recoilCondition == 5))
                hasCurrentAttackHit = true;
        }
        else
        {
            EnemyEntity targetEnemy = target as EnemyEntity;
            if (targetEnemy != null)
            {

                if (targetEnemy.MobileEnemy.ParrySounds)
                {
                    if (recoil && value < recoilChance && (recoilCondition == 1 || recoilCondition == 2 || recoilCondition == 3 || recoilCondition == 5))
                        hasCurrentAttackHit = true;

                    //block
                    if (playMissVFXEntity)
                        DoClang(targetEnemy.EntityBehaviour.transform);
                }
                else
                {
                    if (recoil && value < recoilChance && (recoilCondition == 3 || recoilCondition == 4 || recoilCondition == 5))
                        hasCurrentAttackHit = true;

                    //dodge
                    if (playMissVFXEntity)
                        DoThud(targetEnemy.EntityBehaviour.transform);
                }
            }
        }
    }
    public void DoClang(Transform target)
    {
        DoClang(target.position);
    }
    public void DoClang(Vector3 point)
    {
        //Debug.Log("WEAPON WIDGET - Did CLANG");
        //Create oneshot animated billboard for clang effect
        Camera eye = GameManager.Instance.MainCamera;
        Vector3 pos = eye.transform.position + ((point - eye.transform.position) * 0.75f);
        if (playMissVFXPos == 1)
            pos = eye.transform.position + (eye.transform.forward * (Vector3.Distance(eye.transform.position, point) * 0.75f));
        GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(380, 2, null);
        go.name = "CLANG";
        Billboard c = go.GetComponent<Billboard>();
        go.transform.position = pos;
        go.transform.localScale *= 2f;
        c.FaceY = true;
        c.OneShot = true;
        c.FramesPerSecond = 20;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.material.SetColor("_Color", Color.yellow);
        renderer.material.SetColor("_EmissionColor", Color.yellow);
        renderer.material.SetTexture("_EmissionMap", renderer.material.mainTexture);
        renderer.material.EnableKeyword(KeyWords.Emission);
    }
    public void DoThud(Transform target)
    {
        DoThud(target.position);
    }
    public void DoThud(Vector3 point)
    {
        //Debug.Log("WEAPON WIDGET - Did THUD");
        //Create oneshot animated billboard for thud effect
        Camera eye = GameManager.Instance.MainCamera;
        Vector3 pos = eye.transform.position + ((point-eye.transform.position) * 0.75f);
        if (playMissVFXPos == 1)
            pos = eye.transform.position + (eye.transform.forward * (Vector3.Distance(eye.transform.position, point) * 0.75f));
        GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(380, 2, null);
        go.name = "THUD";
        Billboard c = go.GetComponent<Billboard>();
        go.transform.position = pos;
        go.transform.localScale *= 2f;
        c.FaceY = true;
        c.OneShot = true;
        c.FramesPerSecond = 20;
    }
    void PlayAttackAnimation(WeaponStates state)
    {   
        if (animating != null)
        {
            if (!animatingCancel)
                return;
            else
            {
                StopCoroutine(animating);
                animating = null;
            }
        }

        if (currentWeaponType == WeaponTypes.Bow)
            animating = PlayBowAnimation();
        else
        {
            if (swing)
                animating = PlayWeaponAnimation(state);
            else
                animating = PlayVanillaWeaponAnimation(state);
        }

        hasCurrentAttackHit = false;
        StartCoroutine(animating);
    }
    IEnumerator PlayWeaponAnimation(WeaponStates state)
    {
        hasCurrentAttackHit = false;
        animatingCancel = false;

        float tickTime = (GetAnimTickTime()/5)/swingSpeed;

        if (swingWindup == 2)
            ChangeWeaponState(state);
        else
            ChangeWeaponState(WeaponStates.Idle);

        if (swingRecoveryOverride && state == WeaponStates.StrikeUp)
            tickTime *= 0.5f;

        //wait for hitframe
        while (ScreenWeapon.GetCurrentFrame() < ScreenWeapon.GetHitFrame())
        {
            if (swingWindup == 2)
                currentFrame = 0;
            else if (swingWindup == 1)
            {
                if (currentWeaponType == WeaponTypes.Werecreature)
                {
                    offsetTarget = new Vector2(0, 1);
                }
                else if (currentWeaponType == WeaponTypes.Melee)
                {
                    offsetTarget = new Vector2(1, 1);
                }
                else
                {
                    if (ScreenWeapon.FlipHorizontal)
                    {
                        if (state == WeaponStates.StrikeUp)
                            offsetTarget = new Vector2(-1, 1);
                        else if (state == WeaponStates.StrikeDownLeft || state == WeaponStates.StrikeLeft)
                            offsetTarget = new Vector2(-1, 1);
                        else if (state == WeaponStates.StrikeDown || state == WeaponStates.StrikeDownRight)
                            offsetTarget = new Vector2(1, -1);
                        else if (state == WeaponStates.StrikeRight)
                            offsetTarget = new Vector2(1, 0);
                    }
                    else
                    {
                        if (state == WeaponStates.StrikeUp)
                            offsetTarget = new Vector2(-1, 1);
                        else if (state == WeaponStates.StrikeRight || state == WeaponStates.StrikeDownRight)
                            offsetTarget = new Vector2(-1, 1);
                        else if (state == WeaponStates.StrikeDown || state == WeaponStates.StrikeDownLeft)
                            offsetTarget = new Vector2(1, -1);
                        else if (state == WeaponStates.StrikeLeft)
                            offsetTarget = new Vector2(1, 0);
                    }
                }
            }
            else
                currentFrame = -1;
            yield return new WaitForEndOfFrame();
        }

        ChangeWeaponState(state);

        PlaySwingSound();

        // Chance to play attack voice
        if (DaggerfallUnity.Settings.CombatVoices)
        {
            // Racial override can suppress optional attack voice
            RacialOverrideEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect();
            bool suppressCombatVoices = racialOverride != null && racialOverride.SuppressOptionalCombatVoices;

            if (!suppressCombatVoices && ScreenWeapon.WeaponType != WeaponTypes.Bow && Dice100.SuccessRoll(20))
                PlayAttackVoice();
        }

        //check for static object
        if (recoil && recoilEnvironment)
        {
            if (CheckForEnvDamage())
                hasCurrentAttackHit = true;
        }

        //yield return new WaitForSeconds(tickTime);
        yield return new WaitForEndOfFrame();

        //play the swing animation
        if (hasCurrentAttackHit)
        {
            int hitFrame = ScreenWeapon.GetHitFrame();

            while (currentFrame < hitFrame)
            {
                offsetTarget = Vector2.zero;
                offsetCurrent = Vector2.zero;
                currentFrame++;
                UpdateWeapon();
                yield return new WaitForSeconds(tickTime);
            }

            yield return new WaitForSeconds(tickTime * 3);
        }
        else
        {
            while (currentFrame < weaponAnims[(int)weaponState].NumFrames - 1)
            {
                offsetTarget = Vector2.zero;
                offsetCurrent = Vector2.zero;
                currentFrame++;
                UpdateWeapon();
                yield return new WaitForSeconds(tickTime);
            }
        }

        //wait for end of attack
        animatingCancel = true;

        bool recoveryOverride = false;
        if (swingRecoveryOverride)
            recoveryOverride = CheckForRecoveryOverride();

        while (ScreenWeapon.IsAttacking())
        {
            if (recoveryOverride || hasCurrentAttackHit)
            {
                while (currentFrame > 0)
                {
                    offsetCurrent = Vector2.zero;
                    offsetTarget = Vector2.zero;

                    currentFrame--;
                    UpdateWeapon();
                    yield return new WaitForSeconds(tickTime);
                }

                if (swingRecovery == 1)
                    currentFrame = weaponAnims[(int)weaponState].NumFrames - 1;
                else
                    currentFrame = -1;

                yield return new WaitForEndOfFrame();
            }
            else
            {
                if (swingRecovery == 1)
                    currentFrame = weaponAnims[(int)weaponState].NumFrames - 1;
                else
                    currentFrame = -1;

                    yield return new WaitForEndOfFrame();
            }
        }

        //reset to idle
        ChangeWeaponState(WeaponStates.Idle);

        //set current offset
        float offsetX = 1f;
        if (currentWeaponType == WeaponTypes.Werecreature)
        {
            offsetX = 0f;
        }
        else
        {
            if (state == WeaponStates.StrikeDown || state == WeaponStates.StrikeUp)
                offsetX = 0f;
            else if (state == WeaponStates.StrikeLeft || state == WeaponStates.StrikeDownLeft)
                offsetX = -1f;
        }
        offsetCurrent = new Vector2(offsetX, 1f);

        //clear coroutine and reset variables
        hasCurrentAttackHit = false;
        animating = null;
    }
    IEnumerator PlayVanillaWeaponAnimation(WeaponStates state)
    {
        hasCurrentAttackHit = false;
        animatingCancel = false;

        float tickTime = GetAnimTickTime();

        ChangeWeaponState(state);
        //yield return new WaitForSeconds(tickTime);

        while (currentFrame < weaponAnims[(int)weaponState].NumFrames - 1 && !hasCurrentAttackHit)
        {
            offsetTarget = Vector2.zero;
            offsetCurrent = Vector2.zero;

            currentFrame++;
            UpdateWeapon();

            if (currentFrame == ScreenWeapon.GetHitFrame())
            {
                PlaySwingSound();

                // Chance to play attack voice
                if (DaggerfallUnity.Settings.CombatVoices)
                {
                    // Racial override can suppress optional attack voice
                    RacialOverrideEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect();
                    bool suppressCombatVoices = racialOverride != null && racialOverride.SuppressOptionalCombatVoices;

                    if (!suppressCombatVoices && ScreenWeapon.WeaponType != WeaponTypes.Bow && Dice100.SuccessRoll(20))
                        PlayAttackVoice();
                }

                if (recoil && recoilEnvironment)
                {
                    if (CheckForEnvDamage())
                        hasCurrentAttackHit = true;
                }
            }

            yield return new WaitForSeconds(tickTime);
        }

        while (currentFrame > 0 && hasCurrentAttackHit)
        {
            offsetTarget = Vector2.zero;
            offsetCurrent = Vector2.zero;

            currentFrame--;
            UpdateWeapon();

            if (currentFrame == ScreenWeapon.GetHitFrame())
                animatingCancel = true;

            yield return new WaitForSeconds(tickTime);
        }

        while (ScreenWeapon.IsAttacking())
        {
            if (swingRecovery == 1)
                currentFrame = weaponAnims[(int)weaponState].NumFrames - 1;
            else
                currentFrame = -1;

            yield return new WaitForEndOfFrame();
        }

        //reset to idle
        ChangeWeaponState(WeaponStates.Idle);

        //set current offset
        float offsetX = 1f;
        if (currentWeaponType == WeaponTypes.Werecreature)
        {
            offsetX = 0f;
        }
        else
        {
            if (state == WeaponStates.StrikeDown || state == WeaponStates.StrikeUp)
                offsetX = 0f;
            else if (state == WeaponStates.StrikeLeft || state == WeaponStates.StrikeDownLeft)
                offsetX = -1f;
        }
        offsetCurrent = new Vector2(offsetX, 1f);

        //clear coroutine and reset variables
        animatingCancel = true;
        hasCurrentAttackHit = false;
        animating = null;
    }
    IEnumerator PlayBowAnimation()
    {
        if (DaggerfallUnity.Settings.BowDrawback)
        {
            currentFrame = 0;
            ChangeWeaponState(WeaponStates.StrikeUp);
            UpdateWeapon();


            float drawTime = 12;
            float drawTimer = 0;
            bool drawOver = false;
            //play the draw animation
            while ((tomeOfBattleSwingKeyCode == KeyCode.None && InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon)) ||
                (tomeOfBattleSwingKeyCode != KeyCode.None && InputManager.Instance.GetKey(tomeOfBattleSwingKeyCode))
                )
            {
                if (drawTimer > drawTime || InputManager.Instance.ActionStarted(InputManager.Actions.ActivateCenterObject))
                {
                    drawOver = true;
                    break;
                }

                offsetTarget = Vector2.zero;
                offsetCurrent = Vector2.zero;

                if (currentFrame < 3)
                {
                    currentFrame++;
                    UpdateWeapon();
                    drawTimer += GameManager.classicUpdateInterval;
                    yield return new WaitForSeconds(GameManager.classicUpdateInterval);
                }
                else
                {
                    drawTimer += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }

            if (!drawOver)
            {
                ChangeWeaponState(WeaponStates.StrikeDown);
                UpdateWeapon();
                yield return new WaitForSeconds(GameManager.classicUpdateInterval);

                PlaySwingSound();

                //play the rest of the animation
                while (currentFrame < weaponAnims[(int)weaponState].NumFrames - 1)
                {
                    offsetTarget = Vector2.zero;
                    offsetCurrent = Vector2.zero;

                    currentFrame++;
                    UpdateWeapon();
                    yield return new WaitForSeconds(GameManager.classicUpdateInterval);
                }

                float timeCooldown = FormulaHelper.GetBowCooldownTime(GameManager.Instance.PlayerEntity);
                float timerCooldown = 0;
                //wait for end of cooldown
                while (timerCooldown < timeCooldown)
                {
                    offsetTarget = Vector2.up;

                    timerCooldown += Time.deltaTime;

                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                //reverse the animation
                while (currentFrame > 0)
                {
                    offsetTarget = Vector2.zero;
                    offsetCurrent = Vector2.zero;

                    currentFrame--;
                    UpdateWeapon();
                    yield return new WaitForSeconds(GameManager.classicUpdateInterval);
                }
            }
            //reset to idle
            currentFrame = 0;
            ChangeWeaponState(WeaponStates.Idle);
            UpdateWeapon();
        }
        else
        {
            currentFrame = 3;
            ChangeWeaponState(WeaponStates.StrikeDown);
            UpdateWeapon();

            yield return new WaitForSeconds(GameManager.classicUpdateInterval);

            PlaySwingSound();

            //play the shoot animation
            while (currentFrame < weaponAnims[(int)weaponState].NumFrames - 1)
            {
                offsetTarget = Vector2.zero;
                offsetCurrent = Vector2.zero;

                currentFrame++;
                UpdateWeapon();

                yield return new WaitForSeconds(GameManager.classicUpdateInterval);
            }

            //wait for end of attack
            while (ScreenWeapon.IsAttacking())
            {
                offsetTarget = Vector2.up;
                yield return new WaitForEndOfFrame();
            }

            //reset to idle
            currentFrame = 3;
            ChangeWeaponState(WeaponStates.StrikeDown);
            UpdateWeapon();
        }

        //set current offset
        offsetCurrent = new Vector2(0, 1);


        //clear coroutine
        animating = null;
    }
    public void ChangeWeaponState(WeaponStates state)
    {
        weaponState = state;

        // Only reset frame to 0 for bows if idle state
        if (ScreenWeapon.WeaponType != WeaponTypes.Bow || state == WeaponStates.Idle)
            currentFrame = 0;

        if (state != WeaponStates.Idle)
        {
            offsetCurrent = Vector2.zero;
            offsetTarget = Vector2.zero;
        }

        UpdateWeapon();
    }

    private void OnGUI()
    {
        /*if (isInThirdPerson)
            return;*/

        bool updateWeapon = false;
        GUI.depth = 1;

        if (DaggerfallUI.Instance.CustomScreenRect != null)
            screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
        else
            screenRect = new Rect(0, 0, Screen.width, Screen.height);

        // Must be ready and not loading the game
        if (ScreenWeapon.WeaponType == WeaponTypes.None || GameManager.IsGamePaused || SaveLoadManager.Instance.LoadInProgress)
            return;

        // Must have current weapon texture atlas
        if (weaponAtlas == null ||
            ScreenWeapon.WeaponType != currentWeaponType ||
            ScreenWeapon.MetalType != currentMetalType ||
            (FPSWeapon.moddedWeaponHUDAnimsEnabled && SpecificWeapon != null && SpecificWeapon.TemplateIndex != currentTemplateIndex)
            || doubleScale != lastDoubleScale) //weapon widget specific code
        {
            LoadWeaponAtlas();
            if (weaponAtlas == null)
                return;
            updateWeapon = true;
        }

        // Offset weapon by large HUD height when both large HUD and undocked weapon offset enabled
        // Weapon is forced to offset when using docked HUD else it would appear underneath HUD
        // This helps user avoid such misconfiguration or it might be interpreted as a bug
        weaponOffsetHeight = 0;
        if (DaggerfallUI.Instance.DaggerfallHUD != null &&
            DaggerfallUnity.Settings.LargeHUD &&
            (DaggerfallUnity.Settings.LargeHUDUndockedOffsetWeapon || DaggerfallUnity.Settings.LargeHUDDocked))
        {
            weaponOffsetHeight = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
        }

        // Update weapon when resolution or large HUD state changes
        if (screenRect.width != lastScreenWidth ||
            screenRect.height != lastScreenHeight ||
            DaggerfallUnity.Settings.LargeHUD != lastLargeHUDSetting ||
            DaggerfallUnity.Settings.LargeHUDDocked != lastLargeHUDDockSetting ||
            GameManager.Instance.WeaponManager.Sheathed != lastSheathed ||
            weaponOffsetHeight != lastWeaponOffsetHeight)
        {
            lastScreenWidth = screenRect.width;
            lastScreenHeight = screenRect.height;
            lastLargeHUDSetting = DaggerfallUnity.Settings.LargeHUD;
            lastLargeHUDDockSetting = DaggerfallUnity.Settings.LargeHUDDocked;

            if (GameManager.Instance.WeaponManager.Sheathed != lastSheathed)
            {
                //sheathed the weapon
                if (lastSheathed == false)
                    PlaySheatheSound();
                else
                    PlayUnsheatheSound();
                lastSheathed = GameManager.Instance.WeaponManager.Sheathed;
            }
            lastWeaponOffsetHeight = weaponOffsetHeight;
            updateWeapon = true;
        }

        // Update weapon state only as needed
        if (updateWeapon)
            UpdateWeapon();

        if (Event.current.type.Equals(EventType.Repaint) && currentFrame != -1 && ShowWeapon && !isInThirdPerson && !(!offset && (GameManager.Instance.WeaponManager.Sheathed || GameManager.Instance.PlayerSpellCasting.IsPlayingAnim || GameManager.Instance.PlayerEffectManager.HasReadySpell)))
        {
            // Draw weapon texture behind other HUD elements
            Texture2D tex = curCustomTexture ? curCustomTexture : weaponAtlas.AtlasTexture;
            DaggerfallUI.DrawTextureWithTexCoords(GetWeaponRect(), tex, curAnimRect, true, ScreenWeapon.Tint);
        }

        lastDoubleScale = doubleScale;
    }

    //Combines the base WeaponPosition Rect with Position, Scale and Offset
    public Rect GetWeaponRect()
    {
        Rect weaponPositionOffset = weaponPosition;
        bool mirror = GameManager.Instance.WeaponManager.ScreenWeapon.FlipHorizontal;

        //Adds the Position to the Rect's position)
        if (mirror)
            weaponPositionOffset.x -= Position.x;
        else
            weaponPositionOffset.x += Position.x;
        weaponPositionOffset.y += Position.y;

        weaponPositionOffset.width *= Scale.x;
        weaponPositionOffset.height *= Scale.y;

        /*//Adds the Offset to the Rect's position
        weaponPositionOffset.x -= weaponPositionOffset.width * 0.5f;
        weaponPositionOffset.y -= weaponPositionOffset.height * 0.5f;*/

        //Adds the Offset to the Rect's position
        if (mirror)
            weaponPositionOffset.x -= weaponPositionOffset.width * Offset.x;
        else
            weaponPositionOffset.x += weaponPositionOffset.width * Offset.x;
        weaponPositionOffset.y += weaponPositionOffset.height * Offset.y;

        if (stepTransforms)
        {
            float length = stepLength * (screenRect.height / 64);
            weaponPositionOffset.x = Snapping.Snap(weaponPositionOffset.x, length);
            weaponPositionOffset.y = Snapping.Snap(weaponPositionOffset.y, length);
        }

        //stop the texture from going higher than its bottom edge
        weaponPositionOffset.y = Mathf.Clamp(weaponPositionOffset.y, screenRect.height - weaponPositionOffset.height - weaponOffsetHeight, screenRect.height);

        return weaponPositionOffset;
    }

    public bool CheckForEnvDamage()
    {
        Transform body = GameManager.Instance.PlayerObject.transform;
        Transform eye = GameManager.Instance.MainCameraObject.transform;
        float reach = GameManager.Instance.WeaponManager.ScreenWeapon.Reach;

        if (tomeOfBattle != null)
            reach = tomeOfBattleWeaponReach;

        LayerMask layerMask = ~(1 << LayerMask.NameToLayer("Player"));
        layerMask = layerMask & ~(1 << LayerMask.NameToLayer("Ignore Raycast"));

        // Fire ray along player facing using weapon range
        RaycastHit hit;
        Ray ray = new Ray(body.position + (Vector3.up * (GameManager.Instance.PlayerController.height*0.5f)), eye.forward);
        //Debug.DrawLine(ray.origin, ray.origin + (ray.direction * reach), Color.green, 1f, false);
        if (Physics.SphereCast(ray, 0.25f, out hit, reach, layerMask))
        {
            DaggerfallUnityItem strikingWeapon = GameManager.Instance.WeaponManager.ScreenWeapon.SpecificWeapon;
            return WeaponEnvDamage(strikingWeapon, hit);
        }

        return false;
    }

    // Returns true if hit the environment
    public bool WeaponEnvDamage(DaggerfallUnityItem strikingWeapon, RaycastHit hit)
    {
        // Check if hit has an DaggerfallActionDoor component
        DaggerfallActionDoor actionDoor = hit.transform.gameObject.GetComponent<DaggerfallActionDoor>();
        if (actionDoor)
        {
            if (playMissVFXEnvironment)
                DoThud(hit.point);
            return true;
        }

        // Check if player hit a static door
        if (GameManager.Instance.PlayerActivate.AttemptExteriorDoorBash(hit))
            return true;

        // Make hitting walls do a thud or clinging sound (not in classic)
        if (GameObjectHelper.IsStaticGeometry(hit.transform.gameObject))
        {
            if (playMissVFXEnvironment)
                DoThud(hit.point);
            return true;
        }

        return false;
    }

    private void LateUpdate()
    {
        Position = Vector2.zero;
        Scale = Vector2.one;
        Offset = Vector2.zero;

        if (ScreenWeapon.SpecificWeapon != SpecificWeapon)
        {
            if (ScreenWeapon.SpecificWeapon != null)
            {
                SpecificWeapon = ScreenWeapon.SpecificWeapon;

                if (!DaggerfallUnity.Settings.BowDrawback && DaggerfallUnity.Instance.ItemHelper.ConvertItemToAPIWeaponType(SpecificWeapon) == WeaponTypes.Bow)
                {
                    currentFrame = 3;
                    ChangeWeaponState(WeaponStates.StrikeDown);
                }
                else
                    ChangeWeaponState(WeaponStates.Idle);
            }
            else
            {
                SpecificWeapon = null;
                ChangeWeaponState(WeaponStates.Idle);
            }
        }

        if (ScreenWeapon == null)
            return;

        if (ambidexterity && !GameManager.Instance.WeaponManager.Sheathed)
        {
            if (!GameManager.Instance.WeaponManager.UsingRightHand)
            {
                if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                {
                    if (mirrorBows)
                    {
                        //if off-hand is a bow, use flip
                        if (ScreenWeapon.FlipHorizontal != !leftHanded)
                        {
                            ScreenWeapon.FlipHorizontal = !leftHanded;

                            ChangeWeaponState(WeaponStates.Idle);
                        }
                    }
                    else
                    {
                        //if off-hand is a bow, use default
                        if (ScreenWeapon.FlipHorizontal != leftHanded)
                        {
                            ScreenWeapon.FlipHorizontal = leftHanded;

                            ChangeWeaponState(WeaponStates.Idle);
                        }
                    }
                }
                else
                {
                    if (ScreenWeapon.FlipHorizontal != !leftHanded)
                    {
                        ScreenWeapon.FlipHorizontal = !leftHanded;
                        ChangeWeaponState(WeaponStates.Idle);
                    }
                }
            }
            else
            {
                if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                {
                    if (mirrorBows)
                    {
                        //if main-hand is a bow, use flip
                        if (ScreenWeapon.FlipHorizontal != !leftHanded)
                        {
                            ScreenWeapon.FlipHorizontal = !leftHanded;

                            ChangeWeaponState(WeaponStates.Idle);
                        }
                    }
                    else
                    {
                        //if main-hand is a bow, use default
                        if (ScreenWeapon.FlipHorizontal != leftHanded)
                        {
                            ScreenWeapon.FlipHorizontal = leftHanded;

                            ChangeWeaponState(WeaponStates.Idle);
                        }
                    }
                }
                else
                {
                    if (ScreenWeapon.FlipHorizontal != leftHanded)
                    {
                        ScreenWeapon.FlipHorizontal = leftHanded;
                        ChangeWeaponState(WeaponStates.Idle);
                    }
                }
            }
        }

        //check if weapon is attacking and if clone isn't
        if (ScreenWeapon.IsAttacking() && animating == null)
        {
            //Stop dagger from performing an off-hand attack
            if (swingNoDaggerRight)
            {
                if (currentWeaponType == WeaponTypes.Dagger || currentWeaponType == WeaponTypes.Dagger_Magic)
                {
                    if (ScreenWeapon.FlipHorizontal)
                    {
                        if (ScreenWeapon.WeaponState == WeaponStates.StrikeLeft)
                            PlayAttackAnimation(WeaponStates.StrikeRight);
                        else if (ScreenWeapon.WeaponState == WeaponStates.StrikeDownLeft)
                            PlayAttackAnimation(WeaponStates.StrikeLeft);
                        else
                            PlayAttackAnimation(ScreenWeapon.WeaponState);
                    }
                    else
                    {
                        if (ScreenWeapon.WeaponState == WeaponStates.StrikeRight)
                            PlayAttackAnimation(WeaponStates.StrikeLeft);
                        else if (ScreenWeapon.WeaponState == WeaponStates.StrikeDownRight)
                            PlayAttackAnimation(WeaponStates.StrikeDownLeft);
                        else
                            PlayAttackAnimation(ScreenWeapon.WeaponState);
                    }
                }
                else
                    PlayAttackAnimation(ScreenWeapon.WeaponState);
            }
            else
                PlayAttackAnimation(ScreenWeapon.WeaponState);
        }

        float currentSpeed = GameManager.Instance.PlayerMotor.Speed;
        float baseSpeed = GameManager.Instance.SpeedChanger.GetBaseSpeed();
        float speed = currentSpeed / baseSpeed;

        //bool attacking = GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking();
        bool attacking = animating != null ? true : false;
        bool unsheathed = ScreenWeapon.ShowWeapon;
        bool mirror = GameManager.Instance.WeaponManager.ScreenWeapon.FlipHorizontal;

        if (offset)
        {
            if (!attacking)
            {
                if (!unsheathed)
                    offsetTarget = Vector2.one * 2;
                else
                    offsetTarget = Vector2.zero;

                if (GameManager.Instance.WeaponManager.UsingRightHand)
                {
                    if (GameManager.Instance.WeaponManager.EquipCountdownRightHand > 0)
                        offsetTarget = Vector2.one * 2;
                }
                else
                {
                    if (GameManager.Instance.WeaponManager.EquipCountdownLeftHand > 0)
                        offsetTarget = Vector2.one * 2;
                }
            }

            /*if (ScreenWeapon.FlipHorizontal)
                offsetTarget *= new Vector2(-1,1);*/

            offsetCurrent = Vector2.MoveTowards(offsetCurrent, offsetTarget, Time.deltaTime * offsetSpeedLive);

            Offset += offsetCurrent;
        }

        //bob

        bool canBob = false;
        if (weaponState == WeaponStates.Idle || (currentWeaponType == WeaponTypes.Bow && (weaponState == WeaponStates.StrikeUp || weaponState == WeaponStates.StrikeDown)))
            canBob = true;


        if (bob && canBob)
        {
            //SHAPE OF MOVE BOB
            float bobYOffset = bobShape;

            //BOB ONLY WHEN GROUNDED
            float move = 1;
            if (!GameManager.Instance.PlayerMotor.IsGrounded)
                move = 0;
            moveSmooth = Mathf.MoveTowards(moveSmooth, move, Time.deltaTime * moveSmoothSpeed);

            //SCALE BOB TO SPEED AND MOVEMENT
            float bob = speed;

            //DAMPEN BOB WHEN CROUCHED
            if (GameManager.Instance.PlayerMotor.IsCrouching)
                bob *= 0.5f;

            //DAMPEN BOB WHEN RIDING A HORSE OR CART
            if (GameManager.Instance.PlayerMotor.IsRiding)
                bob *= 0.5f;

            //DAMPEN BOB WHEN NOT MOVING
            if (GameManager.Instance.PlayerMotor.IsStandingStill)
            {
                if (bobWhileIdle)
                    bob = 0.1f;
                else
                    bob = 0;
            }

            //HORIZONTAL BOB
            float bobXSpeed = baseSpeed * 1.25f * bob * bobLength; //SYNC IT WITH THE FOOTSTEP SOUNDS
            float bobYSpeed = bobXSpeed * 2f; //MAKE IT A MULTIPLE OF THE HORIZONTAL BOB SPEED
            float factor = 0.01f;
            Vector2 bobSize = new Vector2(screenRect.width * factor * bob * bobSizeXMod, screenRect.height * factor * bob * bobSizeYMod);

            //ADD OFFSET SO SPRITE DOESN'T EXPOSE EDGES WHEN BOBBING
            float screenOffsetX = -1f;
            float screenOffsetY = 1f;

            //IF DOUBLE SCALE TEXTURES ARE BEING USED, DON'T OFFSET
            if (doubleScale && weaponState == WeaponStates.Idle)
            {
                screenOffsetX = 0;
                screenOffsetY = 0;
            }

            //REVERSE OFFSET IF LEFT-HANDED
            /*if (ScreenWeapon.FlipHorizontal)
                screenOffsetX *= -1;*/

            //GET CURRENT BOB VALUES
            Vector2 bobRaw = new Vector2((screenOffsetX + Mathf.Sin(bobOffset + Time.time * bobXSpeed)) * -bobSize.x, (screenOffsetY - Mathf.Sin(bobOffset + bobYOffset + Time.time * bobYSpeed)) * bobSize.y);

            //SMOOTH TRANSITIONS BETWEEN WALKING, RUNNING, CROUCHING, ETC
            bobSmooth = Vector2.MoveTowards(bobSmooth, bobRaw, Time.deltaTime * bobSmoothSpeed) * moveSmooth;

            Position += bobSmooth;
        }

        //inertia
        if (inertia)
        {
            if (weaponState == WeaponStates.Idle)
            {
                float mod = 1;

                Vector3 MoveDirectionLocal = GameManager.Instance.PlayerObject.transform.InverseTransformVector(GameManager.Instance.PlayerMotor.MoveDirection);

                float speedX = Mathf.Clamp(MoveDirectionLocal.x / 10,-1,1);
                float speedY = 0;
                if (!GameManager.Instance.PlayerMotor.IsGrounded)
                    speedY = Mathf.Clamp(MoveDirectionLocal.y / 10, -1, 1);
                float speedZ = Mathf.Clamp(MoveDirectionLocal.z / 10, -1, 1);

                int mirrorDir = mirror ? -1 : 1;

                if (GameManager.Instance.PlayerMouseLook.cursorActive || ((tomeOfBattleSwingKeyCode == KeyCode.None && InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon)) || (tomeOfBattleSwingKeyCode != KeyCode.None && InputManager.Instance.GetKey(tomeOfBattleSwingKeyCode))))
                    inertiaTarget = new Vector2(-speedX * 0.5f * inertiaScale, 0);
                else
                    inertiaTarget = new Vector2(((InputManager.Instance.LookX + speedX)*mirrorDir) * 0.5f * -inertiaScale, (InputManager.Instance.LookY + speedY) * 0.5f * inertiaScale);

                inertiaSpeedMod = Vector2.Distance(inertiaCurrent, inertiaTarget) / inertiaScale;

                if (inertiaTarget != Vector2.zero)
                    mod = 3;

                inertiaCurrent = Vector2.MoveTowards(inertiaCurrent, inertiaTarget, Time.deltaTime * inertiaSpeed * inertiaSpeedMod * mod);

                Position += inertiaCurrent;

                mod = 1;

                if (inertiaForwardTarget != Vector2.zero)
                    mod = 3;

                inertiaForwardTarget = Vector2.one * speedZ * inertiaForwardScale;
                inertiaForwardCurrent = Vector2.MoveTowards(inertiaForwardCurrent, inertiaForwardTarget, Time.deltaTime * inertiaForwardSpeed * mod);

                Scale += inertiaForwardCurrent;
                Position -= inertiaForwardCurrent * (screenRect.width * 0.25f);
                //Offset += inertiaForwardCurrent * 0.5f;
            }
        }

        if (doubleScale)
        {
            //set center of sprite to corner
            if (weaponState == WeaponStates.Idle || (currentWeaponType == WeaponTypes.Bow && currentFrame == 0))
            {
                Scale += Vector2.one;
                if (currentWeaponType == WeaponTypes.Werecreature)
                    Offset -= new Vector2(0.25f, 0);
                if (mirror)
                    Offset += new Vector2(0.5f, 0);
            }
        }

        if (Position != Vector2.zero && OnPositionChange != null)
            OnPositionChange(Position);

        if (Scale != Vector2.one && OnScaleChange != null)
            OnScaleChange(Scale);

        if (Offset != Vector2.zero && OnOffsetChange != null)
            OnOffsetChange(Offset);

        if (ScreenWeapon.ShowWeapon)
            ScreenWeapon.ShowWeapon = false;
    }

    private void UpdateWeapon()
    {
        // Do nothing if weapon not ready
        if (weaponAtlas == null || weaponAnims == null ||
            weaponAtlas.WeaponRects == null || weaponAtlas.WeaponIndices == null)
        {
            return;
        }

        // Reset state if weapon not visible
        //if (!ShowWeapon || WeaponType == WeaponTypes.None)
        if (ScreenWeapon.WeaponType == WeaponTypes.None)
        {
            weaponState = WeaponStates.Idle;
            currentFrame = 0;
        }

        // Handle bow with no arrows
        if (!GameManager.Instance.WeaponManager.Sheathed && ScreenWeapon.WeaponType == WeaponTypes.Bow && GameManager.Instance.PlayerEntity.Items.GetItem(ItemGroups.Weapons, (int)Weapons.Arrow, allowQuestItem: false) == null)
        {
            GameManager.Instance.WeaponManager.SheathWeapons();
            DaggerfallUI.SetMidScreenText(TextManager.Instance.GetLocalizedText("youHaveNoArrows"));
        }

        // Store rect and anim
        int weaponAnimRecordIndex;
        if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
            weaponAnimRecordIndex = 0; // Bow has only 1 animation
        else
            weaponAnimRecordIndex = weaponAnims[(int)weaponState].Record;

        try
        {
            bool isImported = customTextures.TryGetValue(MaterialReader.MakeTextureKey(0, (byte)weaponAnimRecordIndex, (byte)currentFrame), out curCustomTexture);
            if (ScreenWeapon.FlipHorizontal && (weaponState == WeaponStates.Idle || weaponState == WeaponStates.StrikeDown || weaponState == WeaponStates.StrikeUp))
            {
                // Mirror weapon rect
                if (isImported)
                {
                    curAnimRect = new Rect(1, 0, -1, 1);
                }
                else
                {
                    Rect rect = weaponAtlas.WeaponRects[weaponAtlas.WeaponIndices[weaponAnimRecordIndex].startIndex + currentFrame];
                    curAnimRect = new Rect(rect.xMax, rect.yMin, -rect.width, rect.height);
                }
            }
            else
            {
                curAnimRect = isImported ? new Rect(0, 0, 1, 1) : weaponAtlas.WeaponRects[weaponAtlas.WeaponIndices[weaponAnimRecordIndex].startIndex + currentFrame];
            }

            WeaponAnimation anim = weaponAnims[(int)weaponState];

            //if (weaponState == WeaponStates.StrikeDown && (ScreenWeapon.WeaponType == WeaponTypes.Mace || ScreenWeapon.WeaponType != WeaponTypes.Mace_Magic))
            if (swingAlignmentOverride)
            {
                if ((weaponState == WeaponStates.StrikeDown || (weaponState == WeaponStates.StrikeUp && !(currentWeaponType == WeaponTypes.Battleaxe || currentWeaponType == WeaponTypes.Battleaxe_Magic || currentWeaponType == WeaponTypes.Warhammer || currentWeaponType == WeaponTypes.Warhammer_Magic))) && !(currentWeaponType == WeaponTypes.Melee || currentWeaponType == WeaponTypes.Bow || currentWeaponType == WeaponTypes.Dagger || currentWeaponType == WeaponTypes.Dagger_Magic))
                    anim.Alignment = WeaponAlignment.Center;
            }

            // Get weapon dimensions
            int width = weaponAtlas.WeaponIndices[weaponAnimRecordIndex].width;
            int height = weaponAtlas.WeaponIndices[weaponAnimRecordIndex].height;

            // Get weapon scale
            weaponScaleX = (float)screenRect.width / (float)nativeScreenWidth;
            weaponScaleY = (float)screenRect.height / (float)nativeScreenHeight;

            // Adjust scale to be slightly larger when not using point filtering
            // This reduces the effect of filter shrink at edge of display
            if (DaggerfallUnity.Instance.MaterialReader.MainFilterMode != FilterMode.Point)
            {
                weaponScaleX *= 1.01f;
                weaponScaleY *= 1.01f;
            }

            // Source weapon images are designed to overlay a fixed 320x200 display.
            // Some weapons need to align with both top, bottom, and right of display.
            // This means they might be a little stretched on widescreen displays.
            switch (anim.Alignment)
            {
                case WeaponAlignment.Left:
                    AlignLeft(anim, width, height);
                    break;

                case WeaponAlignment.Center:
                    AlignCenter(anim, width, height);
                    break;

                case WeaponAlignment.Right:
                    AlignRight(anim, width, height);
                    break;
            }
            // Set the frame time (attack speed)
            animTickTime = GetAnimTickTime();
        }
        catch (IndexOutOfRangeException)
        {
            DaggerfallUnity.LogMessage("Index out of range exception for weapon animation. Probably due to weapon breaking + being unequipped during animation.");
        }

        if (tomeOfBattle != null)
        {
            ModManager.Instance.SendModMessage(tomeOfBattle.Title, "getWeaponReach", null, (string message, object data) =>
            {
                tomeOfBattleWeaponReach = (float)data;
            });
        }
    }

    private void AlignLeft(WeaponAnimation anim, int width, int height)
    {
        weaponPosition = new Rect(
            screenRect.x + screenRect.width * anim.Offset,
            screenRect.y + screenRect.height - height * weaponScaleY - weaponOffsetHeight,
            width * weaponScaleX,
            height * weaponScaleY);
    }

    private void AlignCenter(WeaponAnimation anim, int width, int height)
    {
        if (swingAlignmentOverride && (weaponState == WeaponStates.StrikeDown || weaponState == WeaponStates.StrikeUp) && !(currentWeaponType == WeaponTypes.Bow || currentWeaponType == WeaponTypes.Melee || currentWeaponType == WeaponTypes.Dagger || currentWeaponType == WeaponTypes.Dagger_Magic || currentWeaponType == WeaponTypes.Warhammer || currentWeaponType == WeaponTypes.Warhammer_Magic))
        {
            if (ScreenWeapon.FlipHorizontal)
            {
                weaponPosition = new Rect(
                    screenRect.x + screenRect.width / 2f - (width * weaponScaleX),
                    screenRect.y + screenRect.height - height * weaponScaleY - weaponOffsetHeight,
                    width * weaponScaleX,
                    height * weaponScaleY);
            }
            else
            {
                weaponPosition = new Rect(
                    screenRect.x + screenRect.width / 2f,
                    screenRect.y + screenRect.height - height * weaponScaleY - weaponOffsetHeight,
                    width * weaponScaleX,
                    height * weaponScaleY);
            }
        }
        else
        {
            weaponPosition = new Rect(
                screenRect.x + screenRect.width / 2f - (width * weaponScaleX) / 2f,
                screenRect.y + screenRect.height - height * weaponScaleY - weaponOffsetHeight,
                width * weaponScaleX,
                height * weaponScaleY);
        }
    }

    private void AlignRight(WeaponAnimation anim, int width, int height)
    {
        if (ScreenWeapon.FlipHorizontal && (weaponState == WeaponStates.Idle || weaponState == WeaponStates.StrikeDown || weaponState == WeaponStates.StrikeUp))
        {
            // Flip alignment
            AlignLeft(anim, width, height);
            return;
        }

        weaponPosition = new Rect(
            screenRect.x + screenRect.width * (1f - anim.Offset) - width * weaponScaleX,
            screenRect.y + screenRect.height - height * weaponScaleY - weaponOffsetHeight,
            width * weaponScaleX,
            height * weaponScaleY);
    }

    private void LoadWeaponAtlas()
    {
        // Get weapon filename
        string filename = WeaponBasics.GetWeaponFilename(ScreenWeapon.WeaponType);

        // Load the weapon texture atlas
        // Texture is dilated into a transparent coloured border to remove dark edges when filtered
        // Important to use returned UV rects when drawing to get right dimensions
        weaponAtlas = GetWeaponTextureAtlas(filename, ScreenWeapon.MetalType, 2, 2, out var customAnimation, true);
        if (customAnimation != null)
            customTextures = customAnimation.Textures;
        else
            customTextures = new Dictionary<int, Texture2D>();
        weaponAtlas.AtlasTexture.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;

        // Get weapon anims
        weaponAnims = (WeaponAnimation[])WeaponBasics.GetWeaponAnims(ScreenWeapon.WeaponType).Clone();

        // Store current weapon
        currentWeaponType = ScreenWeapon.WeaponType;
        currentMetalType = ScreenWeapon.MetalType;

        if (SpecificWeapon != null)
            currentTemplateIndex = SpecificWeapon.TemplateIndex;
        else
            currentTemplateIndex = -1;

        animTickTime = GetAnimTickTime();
    }

    const float max = 0.045918f;
    const float mid = 0.198979f;
    const float min = 0.352041f;

    private float GetAnimTickTime()
    {
        float tickTime = GameManager.classicUpdateInterval;
        PlayerEntity player = GameManager.Instance.PlayerEntity;

        if (ScreenWeapon.WeaponType != WeaponTypes.Bow && player != null)
            tickTime = FormulaHelper.GetMeleeWeaponAnimTime(player, ScreenWeapon.WeaponType, ScreenWeapon.WeaponHands);

        if (!swing)
            return tickTime;

        //dampen the value somehow
        float diff = max - mid;

        //at max, it should equal to "mid + (diff/2)"
        //at mid, it should equal to "mid"
        //at min, it should equal to "mid - (diff/2)"

        float t = Mathf.InverseLerp(0, 2, tickTime/mid);
        tickTime = Mathf.Lerp(max, min, t);

        return tickTime;
    }

    private WeaponAtlas GetWeaponTextureAtlas(
            string filename,
            MetalTypes metalType,
            int padding,
            int border,
            out CustomWeaponAnimation customWeaponAnimation,
            bool dilate = false)
    {
        // Check caches
        var cachedAtlas = GetCachedWeaponAtlas(filename, metalType);
        var cachedAnimation = GetCachedCustomWeaponAnimation(filename, metalType);
        customWeaponAnimation = null;
        var modTextures = new Dictionary<int, Texture2D>();
        if (cachedAnimation != null)
            modTextures = cachedAnimation.Textures;
        // Nothing to load if both vanilla atlas and custom texture are currently cached.
        if (cachedAtlas != null && cachedAnimation != null)
        {
            customWeaponAnimation = new CustomWeaponAnimation() { FileName = filename, MetalType = metalType, Textures = modTextures };
            return cachedAtlas;
        }

        // Load texture file
        cifFile.Load(Path.Combine(DaggerfallUnity.Instance.Arena2Path, filename), FileUsage.UseMemory, true);

        // Read every image in archive
        Rect rect;
        List<Texture2D> textures = new List<Texture2D>();
        List<RecordIndex> indices = new List<RecordIndex>();
        for (int record = 0; record < cifFile.RecordCount; record++)
        {
            int frames = cifFile.GetFrameCount(record);
            DFSize size = cifFile.GetSize(record);
            RecordIndex ri = new RecordIndex()
            {
                startIndex = textures.Count,
                frameCount = frames,
                width = size.Width,
                height = size.Height,
            };
            indices.Add(ri);
            if (cachedAtlas == null) // Load atlas if not already cached.
            {
                for (int frame = 0; frame < frames; frame++)
                    textures.Add(GetWeaponTexture2D(filename, record, frame, metalType, out rect, border, dilate));
            }

            if (cachedAnimation != null) // No need to load frames if cached.
                continue;

            for (int frame = 0; frame < frames; frame++)
            {
                string moddedFileName = filename;

                if (FPSWeapon.moddedWeaponHUDAnimsEnabled && SpecificWeapon != null)
                {
                    moddedFileName = WeaponBasics.GetModdedWeaponFilename(SpecificWeapon);

                    if (string.IsNullOrEmpty(moddedFileName))
                        moddedFileName = WeaponBasics.GetWeaponFilename(ScreenWeapon.WeaponType); // Possibly make support for custom weapon types for the HUD in the future.
                }

                Texture2D tex;

                string doubleScaleFileName = "w_" + moddedFileName;

                Debug.Log("WEAPON WIDGET - LOADING " + doubleScaleFileName);

                if (doubleScale && TextureReplacement.TryImportCifRci(doubleScaleFileName, record, frame, metalType, true, out tex))
                {
                    tex.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;
                    tex.wrapMode = TextureWrapMode.Mirror;
                    modTextures.Add(MaterialReader.MakeTextureKey(0, (byte)record, (byte)frame), tex);
                }
                else if(TextureReplacement.TryImportCifRci(moddedFileName, record, frame, metalType, true, out tex))
                {
                    tex.filterMode = DaggerfallUnity.Instance.MaterialReader.MainFilterMode;
                    tex.wrapMode = TextureWrapMode.Mirror;
                    modTextures.Add(MaterialReader.MakeTextureKey(0, (byte)record, (byte)frame), tex);
                }
            }
        }

        if (cachedAnimation == null && modTextures.Count > 0)
            customWeaponAnimation = new CustomWeaponAnimation() { FileName = filename, MetalType = metalType, Textures = modTextures };
        if (cachedAtlas != null)
            return cachedAtlas;

        // Pack textures into atlas
        Texture2D atlasTexture = new Texture2D(2048, 2048, TextureFormat.ARGB32, false);
        Rect[] rectsOut = atlasTexture.PackTextures(textures.ToArray(), padding, 2048);
        RecordIndex[] indicesOut = indices.ToArray();

        // Shrink UV rect to compensate for internal border
        float ru = 1f / atlasTexture.width;
        float rv = 1f / atlasTexture.height;
        for (int i = 0; i < rectsOut.Length; i++)
        {
            Rect rct = rectsOut[i];
            rct.xMin += border * ru;
            rct.xMax -= border * ru;
            rct.yMin += border * rv;
            rct.yMax -= border * rv;
            rectsOut[i] = rct;
        }

        return new WeaponAtlas()
        {
            FileName = filename,
            MetalType = metalType,
            AtlasTexture = atlasTexture,
            WeaponRects = rectsOut,
            WeaponIndices = indicesOut
        };
    }

    private WeaponAtlas GetCachedWeaponAtlas(string fileName, MetalTypes metalType)
    {
        foreach (var atlas in weaponAtlasCache)
        {
            if (atlas != null && atlas.FileName == fileName && atlas.MetalType == metalType)
                return atlas;
        }

        return null;
    }
    private CustomWeaponAnimation GetCachedCustomWeaponAnimation(string fileName, MetalTypes metalType)
    {
        foreach (var animation in customWeaponAnimationCache)
        {
            if (animation != null && animation.FileName == fileName && animation.MetalType == metalType)
                return animation;
        }

        return null;
    }
    private Texture2D GetWeaponTexture2D(
        string filename,
        int record,
        int frame,
        MetalTypes metalType,
        out Rect rectOut,
        int border = 0,
        bool dilate = false)
    {
        // Get source bitmap
        DFBitmap dfBitmap = cifFile.GetDFBitmap(record, frame);

        // Tint based on metal type
        // But not for steel as that is default colour in files
        if (metalType != MetalTypes.Steel && metalType != MetalTypes.None)
            dfBitmap = ImageProcessing.ChangeDye(dfBitmap, ImageProcessing.GetMetalDyeColor(metalType), DyeTargets.WeaponsAndArmor);

        // Get Color32 array
        DFSize sz;
        Color32[] colors = cifFile.GetColor32(dfBitmap, 0, border, out sz);

        // Dilate edges
        if (border > 0 && dilate)
            ImageProcessing.DilateColors(ref colors, sz);

        // Create Texture2D
        Texture2D texture = new Texture2D(sz.Width, sz.Height, TextureFormat.ARGB32, false);
        texture.SetPixels32(colors);
        texture.Apply(true);

        // Shrink UV rect to compensate for internal border
        float ru = 1f / sz.Width;
        float rv = 1f / sz.Height;
        rectOut = new Rect(border * ru, border * rv, (sz.Width - border * 2) * ru, (sz.Height - border * 2) * rv);

        return texture;
    }

    bool CheckForRecoveryOverride()
    {
        bool hasOverride = false;

        //bare hand
        if (SpecificWeapon == null)
        {
            /*if (weaponState == WeaponStates.StrikeLeft)
                hasOverride = true;*/
        }
        else
        {
            WeaponTypes weaponType = GameManager.Instance.ItemHelper.ConvertItemToAPIWeaponType(SpecificWeapon);

            //if (weaponType != WeaponTypes.Dagger && weaponType != WeaponTypes.Dagger_Magic && weaponType != WeaponTypes.Flail && weaponType != WeaponTypes.Flail_Magic)
            if (weaponType != WeaponTypes.Dagger && weaponType != WeaponTypes.Dagger_Magic)
            {
                if (weaponState == WeaponStates.StrikeUp)
                    hasOverride = true;
            }
        }

        return hasOverride;
    }

    public void PlayUnsheatheSound()
    {
        if (dfAudioSource)
        {
            dfAudioSource.AudioSource.pitch = 1f;// *AttackSpeedScale;
            dfAudioSource.PlayOneShot(ScreenWeapon.DrawWeaponSound, 0);
        }
    }

    void PlaySheatheSound()
    {
        if (dfAudioSource)
        {
            dfAudioSource.AudioSource.pitch = 1f;// *AttackSpeedScale;
            dfAudioSource.PlayOneShot(SoundClips.EquipLeather, 0);
        }
    }

    public void PlaySwingSound()
    {
        if (dfAudioSource)
        {
            dfAudioSource.AudioSource.pitch = 1f * ScreenWeapon.AttackSpeedScale;
            dfAudioSource.PlayOneShot(ScreenWeapon.SwingWeaponSound, 0, 1.1f);
        }
    }

    public void PlayAttackVoice(SoundClips customSound = SoundClips.None)
    {
        if (dfAudioSource)
        {
            if (customSound == SoundClips.None)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                SoundClips sound = DaggerfallEntity.GetRaceGenderAttackSound(playerEntity.Race, playerEntity.Gender, true);
                float pitch = dfAudioSource.AudioSource.pitch;
                dfAudioSource.AudioSource.pitch = pitch + UnityEngine.Random.Range(0, 0.3f);
                dfAudioSource.PlayOneShot(sound, 0, 1f);
                dfAudioSource.AudioSource.pitch = pitch;
            }
            else
            {
                dfAudioSource.PlayOneShot(customSound, 0, 1f);
            }
        }
    }
}
