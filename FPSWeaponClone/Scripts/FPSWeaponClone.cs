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
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

public class FPSWeaponClone : MonoBehaviour
{
    FPSWeapon ScreenWeapon;

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

    Dictionary<int, Texture2D> customTextures = new Dictionary<int, Texture2D>();
    Texture2D curCustomTexture;
    public Vector2 Position { get; set; } = Vector2.zero;   //Moves the sprite in screen distance
    public Vector2 Scale { get; set; } = Vector2.one;
    public Vector2 Offset { get; set; } = Vector2.zero;     //Moves the sprite relative to its own dimensions ("Offset.x = 0.5f" will move "sprite.width * 0.5f"). Applied after Scale.

    IEnumerator animating;

    bool swing;
    int swingWindup; //0 = Hide, 1 = Offset
    int swingRecovery; //0 = Hide, 1 = LastFrame
    float swingSpeed;

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
    bool offsetted;
    float offsetSpeed = 1;
    float offsetSpeedLive
    {
        get
        {
            return ((float)GameManager.Instance.PlayerEntity.Stats.LiveSpeed / 50) * offsetSpeed;
        }
    }
    WeaponStates offsetState = WeaponStates.Idle;
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

    //EOTB compatibility
    bool isInThirdPerson;

    //Mod compatibility
    public event Action<Vector2> OnPositionChange;
    public event Action<Vector2> OnScaleChange;
    public event Action<Vector2> OnOffsetChange;

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
        }
        if (change.HasChanged("Swings"))
        {
            swingWindup = settings.GetValue<int>("Swings", "Windup");
            swingRecovery = settings.GetValue<int>("Swings", "Recovery");
            swingSpeed = settings.GetValue<float>("Swings", "Speed");
        }
        if (change.HasChanged("Offset"))
        {
            offsetSpeed = settings.GetValue<float>("Offset", "Speed") * 10;
        }
        if (change.HasChanged("Bob"))
        {
            bobLength = settings.GetValue<float>("Bob", "Length");
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
            stepLength = settings.GetValue<int>("Step", "Length") * 16;
            stepCondition = settings.GetValue<int>("Step", "Condition");
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
    }

    private void Update()
    {
    }

    void PlayAttackAnimation(WeaponStates state)
    {
        if (animating != null)
        {
            return;
            /*StopCoroutine(animating);
            animating = null;*/
        }

        if (swing)
            animating = PlayWeaponAnimation(state);
        else
            animating = PlayVanillaWeaponAnimation(state);

        StartCoroutine(animating);
    }

    IEnumerator PlayWeaponAnimation(WeaponStates state)
    {
        float tickTime = (GetAnimTickTime()/5)/swingSpeed;

        if (currentWeaponType != WeaponTypes.Bow)
        {
            currentFrame = 0;
            ChangeWeaponState(WeaponStates.Idle);
            UpdateWeapon();

            //wait for hitframe
            while (ScreenWeapon.GetCurrentFrame() < ScreenWeapon.GetHitFrame())
            {
                if (swingWindup == 1)
                {
                    if (currentWeaponType == WeaponTypes.Melee)
                    {
                        offsetTarget = new Vector2(-1, 1);
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

            currentFrame = 0;
            ChangeWeaponState(state);
            UpdateWeapon();
            yield return new WaitForSeconds(tickTime);

            //play the swing animation
            while (currentFrame < weaponAnims[(int)weaponState].NumFrames-1)
            {
                offsetTarget = Vector2.zero;
                offsetCurrent = Vector2.zero;

                currentFrame++;
                UpdateWeapon();
                yield return new WaitForSeconds(tickTime);
            }

            //wait for end of attack
            while (ScreenWeapon.IsAttacking())
            {
                if (swingRecovery == 1)
                {
                    currentFrame = weaponAnims[(int)weaponState].NumFrames - 1;
                }
                else
                {
                    if (state != WeaponStates.StrikeUp)
                        currentFrame = -1;
                    else
                        currentFrame = weaponAnims[(int)weaponState].NumFrames - 1;
                }
                yield return new WaitForEndOfFrame();
            }

            //reset to idle
            currentFrame = 0;
            ChangeWeaponState(WeaponStates.Idle);
            UpdateWeapon();
        }
        else
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
                while (InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon))
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

                    //play the rest of the animation
                    while (currentFrame < weaponAnims[(int)weaponState].NumFrames-1)
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
                } else
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

                //play the shoot animation
                while (currentFrame < weaponAnims[(int)weaponState].NumFrames-1)
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
        }

        //clear coroutine
        animating = null;
    }

    IEnumerator PlayVanillaWeaponAnimation(WeaponStates state)
    {
        float tickTime = GetAnimTickTime();
        if (currentWeaponType != WeaponTypes.Bow)
        {
            currentFrame = 0;
            ChangeWeaponState(state);
            UpdateWeapon();

            while (currentFrame < weaponAnims[(int)weaponState].NumFrames - 1)
            {
                offsetTarget = Vector2.zero;
                offsetCurrent = Vector2.zero;

                currentFrame++;
                UpdateWeapon();
                yield return new WaitForSeconds(tickTime);
            }
            //wait for end of attack
            while (ScreenWeapon.IsAttacking())
            {
                currentFrame = -1;
                yield return new WaitForEndOfFrame();
            }

            //reset to idle
            currentFrame = 0;
            ChangeWeaponState(WeaponStates.Idle);
            UpdateWeapon();
        }
        else
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
                while (InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon))
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
        }

        //clear coroutine
        animating = null;
    }

    public void ChangeWeaponState(WeaponStates state)
    {
        weaponState = state;

        // Only reset frame to 0 for bows if idle state
        if (ScreenWeapon.WeaponType != WeaponTypes.Bow || state == WeaponStates.Idle)
            currentFrame = 0;

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
        if (weaponAtlas == null || ScreenWeapon.WeaponType != currentWeaponType || ScreenWeapon.MetalType != currentMetalType)
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
                lastSheathed = GameManager.Instance.WeaponManager.Sheathed;
            }
            lastWeaponOffsetHeight = weaponOffsetHeight;
            updateWeapon = true;
        }

        // Update weapon state only as needed
        if (updateWeapon)
            UpdateWeapon();

        if (Event.current.type.Equals(EventType.Repaint) && currentFrame != -1 && ShowWeapon && !isInThirdPerson)
        {
            // Draw weapon texture behind other HUD elements
            Texture2D tex = curCustomTexture ? curCustomTexture : weaponAtlas.AtlasTexture;
            DaggerfallUI.DrawTextureWithTexCoords(GetWeaponRect(), tex, curAnimRect, true, ScreenWeapon.Tint);
        }
    }

    //Combines the base WeaponPosition Rect with Position, Scale and Offset
    public Rect GetWeaponRect()
    {
        Rect weaponPositionOffset = weaponPosition;

        if (stepTransforms)
        {
            Position = new Vector2(
                Snapping.Snap(Position.x, stepLength),
                Snapping.Snap(Position.y, stepLength)
                );
        }

        //Adds the Position to the Rect's position
        weaponPositionOffset.x += Position.x;
        weaponPositionOffset.y += Position.y;

        weaponPositionOffset.width *= Scale.x;
        weaponPositionOffset.height *= Scale.y;

        if (stepTransforms && stepCondition > 0)
        {
            Offset = new Vector2(
                Snapping.Snap(Offset.x, stepLength * 0.01f),
                Snapping.Snap(Offset.y, stepLength * 0.01f)
                );
        }

        //Adds the Offset to the Rect's position
        weaponPositionOffset.x += weaponPositionOffset.width * Offset.x;
        weaponPositionOffset.y += weaponPositionOffset.height * Offset.y;

        return weaponPositionOffset;
    }

    void PlaySheatheSound()
    {
        GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>().PlayOneShot(SoundClips.EquipLeather, 1, 0.5f * DaggerfallUnity.Settings.SoundVolume);
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

                if (!DaggerfallUnity.Settings.BowDrawback && SpecificWeapon.GetWeaponType() == WeaponTypes.Bow)
                    currentFrame = 3;

                UpdateWeapon();
            }
            else
            {
                SpecificWeapon = null;
                UpdateWeapon();
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
                    //if off-hand is a bow, use default
                    if (ScreenWeapon.FlipHorizontal != leftHanded)
                    {
                        ScreenWeapon.FlipHorizontal = leftHanded;
                        ScreenWeapon.ChangeWeaponState(WeaponStates.Idle);
                    }
                }
                else
                {
                    if (ScreenWeapon.FlipHorizontal != !leftHanded)
                    {
                        ScreenWeapon.FlipHorizontal = !leftHanded;
                        ScreenWeapon.ChangeWeaponState(WeaponStates.Idle);
                    }
                }
            }
            else
            {
                if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                {
                    //if main-hand is a bow, use flip
                    if (ScreenWeapon.FlipHorizontal != !leftHanded)
                    {
                        ScreenWeapon.FlipHorizontal = !leftHanded;
                        ScreenWeapon.ChangeWeaponState(WeaponStates.Idle);
                    }
                }
                else
                {
                    if (ScreenWeapon.FlipHorizontal != leftHanded)
                    {
                        ScreenWeapon.FlipHorizontal = leftHanded;
                        ScreenWeapon.ChangeWeaponState(WeaponStates.Idle);
                    }
                }
            }
        }

        //check if weapon is attacking and if clone isn't
        if (ScreenWeapon.IsAttacking() && animating == null)
        {
            PlayAttackAnimation(ScreenWeapon.WeaponState);
        }

        float currentSpeed = GameManager.Instance.PlayerMotor.Speed;
        float baseSpeed = GameManager.Instance.SpeedChanger.GetBaseSpeed();
        float speed = currentSpeed / baseSpeed;

        //bool attacking = GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking();
        bool attacking = animating != null ? true : false;
        bool unsheathed = ScreenWeapon.ShowWeapon;

        if (offset)
        {
            if (attacking)
            {
                if (!offsetted)
                {
                    offsetState = ScreenWeapon.WeaponState;
                    offsetted = true;
                }
            }
            else
            {
                if (offsetted)
                {
                    float offsetX = 1f;

                    if (offsetState != WeaponStates.Idle)
                    {
                        if (offsetState == WeaponStates.StrikeDown || offsetState == WeaponStates.StrikeUp)
                            offsetX = 0f;
                        else if (offsetState == WeaponStates.StrikeLeft || offsetState == WeaponStates.StrikeDownLeft)
                            offsetX = -1f;
                    }

                    Vector2 newOffset = new Vector2(offsetX, 1f);

                    /*if (inertia && inertiaTransform)
                        newOffset *= 0.5f;*/

                    offsetCurrent = newOffset;

                    offsetted = false;
                }

                if (!unsheathed)
                    offsetTarget = Vector2.one * 2;
                else
                    offsetTarget = Vector2.zero;
            }

            if (ScreenWeapon.FlipHorizontal)
                offsetTarget *= new Vector2(-1,1);

            offsetCurrent = Vector2.MoveTowards(offsetCurrent, offsetTarget, Time.deltaTime * offsetSpeedLive);

            Offset += offsetCurrent;
        }

        //bob
        if (bob && !attacking)
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
            //REVERSE OFFSET IF LEFT-HANDED
            if (ScreenWeapon.FlipHorizontal)
            {
                screenOffsetX *= -1;
                //screenOffsetY *= -1;
            }

            //GET CURRENT BOB VALUES
            Vector2 bobRaw = new Vector2((screenOffsetX + Mathf.Sin(bobOffset + Time.time * bobXSpeed)) * -bobSize.x, (screenOffsetY - Mathf.Sin(bobOffset + bobYOffset + Time.time * bobYSpeed)) * bobSize.y);

            //SMOOTH TRANSITIONS BETWEEN WALKING, RUNNING, CROUCHING, ETC
            bobSmooth = Vector2.MoveTowards(bobSmooth, bobRaw, Time.deltaTime * bobSmoothSpeed) * moveSmooth;

            Position += bobSmooth;
        }

        //inertia
        if (inertia)
        {
            if (!attacking)
            {
                float mod = 1;

                if (GameManager.Instance.PlayerMouseLook.cursorActive || InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon))
                    inertiaTarget = new Vector2(-InputManager.Instance.Horizontal * 0.5f * inertiaScale, 0);
                else
                    inertiaTarget = new Vector2(-(InputManager.Instance.LookX + InputManager.Instance.Horizontal) * 0.5f * inertiaScale, InputManager.Instance.LookY * inertiaScale);

                inertiaSpeedMod = Vector2.Distance(inertiaCurrent, inertiaTarget) / inertiaScale;

                if (inertiaTarget != Vector2.zero)
                    mod = 3;

                inertiaCurrent = Vector2.MoveTowards(inertiaCurrent, inertiaTarget, Time.deltaTime * inertiaSpeed * inertiaSpeedMod * mod);

                Position += inertiaCurrent;

                mod = 1;

                if (inertiaForwardTarget != Vector2.zero)
                    mod = 3;

                inertiaForwardTarget = new Vector2(InputManager.Instance.Vertical, InputManager.Instance.Vertical) * inertiaForwardScale * speed;
                inertiaForwardCurrent = Vector2.MoveTowards(inertiaForwardCurrent, inertiaForwardTarget, Time.deltaTime * inertiaForwardSpeed * mod);

                Scale += inertiaForwardCurrent;

                /*if (GameManager.Instance.WeaponManager.ScreenWeapon.FlipHorizontal)
                    inertiaForwardCurrent *= Vector2.left;*/

                //transforms
                /*if (inertiaTransform)
                {
                    GameManager.Instance.WeaponManager.ScreenWeapon.Scale += Vector2.one;
                    GameManager.Instance.WeaponManager.ScreenWeapon.Offset += inertiaForwardCurrent * 0.25f;
                }
                else*/
                Offset += inertiaForwardCurrent;
            }

            //set center of sprite to corner
            if (weaponState == WeaponStates.Idle)
                Scale += Vector2.one;
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
        weaponPosition = new Rect(
            screenRect.x + screenRect.width / 2f - (width * weaponScaleX) / 2f,
            screenRect.y + screenRect.height - height * weaponScaleY - weaponOffsetHeight,
            width * weaponScaleX,
            height * weaponScaleY);
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
        animTickTime = GetAnimTickTime();
    }
    private float GetAnimTickTime()
    {
        PlayerEntity player = GameManager.Instance.PlayerEntity;
        if (ScreenWeapon.WeaponType == WeaponTypes.Bow || player == null)
            return GameManager.classicUpdateInterval;
        else
            return FormulaHelper.GetMeleeWeaponAnimTime(player, ScreenWeapon.WeaponType, ScreenWeapon.WeaponHands);
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
                if (TextureReplacement.TryImportCifRci(moddedFileName, record, frame, metalType, true, out tex))
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
}
