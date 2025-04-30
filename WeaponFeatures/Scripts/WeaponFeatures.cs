using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallConnect;

public class WeaponFeatures : MonoBehaviour
{
    static Mod mod;

    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        WeaponFeatures wf = go.AddComponent<WeaponFeatures>();
    }

    public static WeaponFeatures Instance;

    bool modCompatibilityChecked;

    //settings
    KeyCode attackKeyCode = KeyCode.Mouse1;
    int meleeMode;    //0 = gesture, 1 = hold, 2 = movement, 3 = speed
    int bowMode; //0 = vanilla, 1 = hold and release, 2 = typed
    float meleeModeVanillaMouseDampenMod = 1.0f;
    bool safeguard;
    float cleaveInterval;
    bool cleaveRequireLOS;
    bool cleaveParries;
    float cleaveValueMultiplier = 1;
    float cleaveWeightMultiplier = 1;
    float reachMultiplier = 1;
    bool reachRadial;
    bool feedbackPause;
    float feedbackPauseScale = 1;
    bool feedbackDodge;
    float feedbackDodgeScale = 1;
    bool feedbackHurt;
    float feedbackHurtScale = 1;

    bool meleeHoldAndRelease = true;

    //configuration
    Vector2 statMelee;
    Vector2 statDagger;
    Vector2 statTanto;
    Vector2 statShortsword;
    Vector2 statWakazashi;
    Vector2 statBroadsword;
    Vector2 statSaber;
    Vector2 statLongsword;
    Vector2 statKatana;
    Vector2 statClaymore;
    Vector2 statDaikatana;
    Vector2 statStaff;
    Vector2 statMace;
    Vector2 statFlail;
    Vector2 statWarhammer;
    Vector2 statBattleAxe;
    Vector2 statWarAxe;
    Vector2 statArchersAxe;
    Vector2 statLightFlail;

    Camera playerCamera;
    CharacterController playerController;
    PlayerEntity playerEntity;
    PlayerMotor playerMotor;
    WeaponManager weaponManager;
    FPSWeapon ScreenWeapon;

    Mod SphincterVisionAttacks;
    Camera skyCamera;

    bool blockHoldInput;
    bool isAttacking;
    bool hasAttacked;

    IEnumerator cleaving;

    DaggerfallEntity attackTarget;
    int attackDamage;

    public LayerMask layerMask;

    int swingCount = 0;
    float swingTime = 0.2f;
    float swingTimer = 0;

    bool reloaded = true;
    float reloadTime = 10;
    float reloadTimer = 0;
    float reloadTimeMod
    {
        get
        {
            if ((Weapons)ScreenWeapon.SpecificWeapon.TemplateIndex == Weapons.Long_Bow)
                return 1.5f;
            else
                return 1f;
        }
    }

    float drawTime = 10;
    float drawTimer;

    int archery;    //0 = vanilla, 1 = global, 2 = typed
    float archeryMissileRadius;
    float archeryMissileAngle;
    float archeryMissileAngleTyped
    {
        get
        {
            if (archery == 2)
            {
                if ((Weapons)ScreenWeapon.SpecificWeapon.TemplateIndex == Weapons.Long_Bow)
                    return archeryMissileAngle/2;
                else
                    return archeryMissileAngle;
            }
            else
                return archeryMissileAngle;
        }
    }
    float archeryMissileSpeed;
    float archeryMissileSpeedMod = 1;
    float archeryMissileSpeedTyped
    {
        get
        {
            if (archery == 2)
            {
                if ((Weapons)ScreenWeapon.SpecificWeapon.TemplateIndex == Weapons.Long_Bow)
                    return archeryMissileSpeed * archeryMissileSpeedMod * 2;
                else
                    return archeryMissileSpeed * archeryMissileSpeedMod;
            }
            else
                return archeryMissileSpeed * archeryMissileSpeedMod;
        }
    }
    float archeryMissileSpread;
    float archeryMissileSpreadTyped
    {
        get
        {
            if (archery == 2)
            {
                if ((Weapons)ScreenWeapon.SpecificWeapon.TemplateIndex == Weapons.Long_Bow)
                    return archeryMissileSpread/2;
                else
                    return archeryMissileSpread;
            }
            else
                return archeryMissileSpread;
        }
    }

    float archeryMissileGravity;
    float archeryMissileGravityMod = 1;
    float archeryMissileGravityTyped
    {
        get
        {
            return archeryMissileGravity * archeryMissileGravityMod;
        }
    }
    bool archeryTrajectory;
    float archeryDrawZoom;
    float archeryDrawZoomTyped
    {
        get
        {
            if (archery == 2)
            {
                if ((Weapons)ScreenWeapon.SpecificWeapon.TemplateIndex == Weapons.Long_Bow)
                    return archeryDrawZoom * 2;
                else
                    return archeryDrawZoom;
            }
            else
                return archeryDrawZoom;
        }
    }
    float archeryDrawZoomTime;
    float archeryDrawZoomDelay;
    bool archeryOverdraw;
    float archeryOverdrawTime;
    float archeryOverdrawTimeTyped
    {
        get
        {
            if (powerAttacks)
            {
                return powerMaxDrawTime/4;
            }
            else
            {
                if (archery == 2)
                {
                    if ((Weapons)ScreenWeapon.SpecificWeapon.TemplateIndex == Weapons.Long_Bow)
                        return archeryOverdrawTime * 2;
                    else
                        return archeryOverdrawTime;
                }
                else
                    return archeryOverdrawTime;
            }
        }
    }

    LineRenderer lineRenderer;
    List<Vector3> positions;

    int archeryDefaultModelID = 99800;
    int archeryDefaultItemTemplate = 131;

    public int archeryCustomModelID = 0;
    public int archeryCustomItemTemplate = 0;
    int archeryModelID
    {
        get
        {
            if (archeryCustomModelID != 0)
                return archeryCustomModelID;
            else
                return archeryDefaultModelID;
        }
    }
    int archeryItemTemplate
    {
        get
        {
            if (archeryCustomItemTemplate != 0)
                return archeryCustomItemTemplate;
            else
                return archeryDefaultItemTemplate;
        }
    }
    int archeryBonusAccuracy = 0;
    int archeryBonusDamage = 0;

    public bool archeryAutoReset = true;

    bool archeryAmmoCounter = true;
    int archeryAmmoCount;
    string archeryAmmoLabel;
    float archeryAmmoLabelLength;

    TextLabel archeryAmmoCounterTextLabel = new TextLabel();
    Color archeryAmmoCounterColorDefault = new Color(0.6f, 0.6f, 0.6f);
    Color archeryAmmoCounterColorCustom = Color.black;
    Color archeryAmmoCounterColor
    {
        get
        {
            if (archeryAmmoCounterColorCustom != Color.black)
                return archeryAmmoCounterColorCustom;
            else
                return archeryAmmoCounterColorDefault;
        }
    }

    bool archeryShowAmmoCounter
    {
        get
        {
            if (archeryAmmoCounter && !DaggerfallUnity.Settings.LargeHUD && !GameManager.Instance.WeaponManager.Sheathed && ScreenWeapon.WeaponType == WeaponTypes.Bow)
                return true;
            else
                return false;
        }
    }

    const int nativeScreenWidth = 320;
    const int nativeScreenHeight = 200;

    Rect screenRect;
    float ScaleX;
    float ScaleY;

    //Weapon Power
    bool powerAttacks;
    bool overrideRegistered;

    int powerAccuracy;
    int powerDamage;

    float powerCurrent = 0;

    public float LastPower
    {
        get
        {
            return lastPower;
        }
    }

    float lastPower = 0;

    int powerMax
    {
        get
        {
            return Mathf.CeilToInt(
                100f *
                ((float)playerEntity.Stats.LiveStrength / 50f) +  //50% @ 25 stat, 100% @ 50 stat, 200% @ 100 stat
                ((float)playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.CriticalStrike) / 2f)    //+50% @ 100 skill
                );
        }
    }

    int powerMin
    {
        get
        {
            return Mathf.CeilToInt(
                -100 +
                100f * ((float)playerEntity.Stats.LiveStrength / 50f) +  //50% @ 25 stat, 100% @ 50 stat, 200% @ 100 stat
                50f * ((float)playerEntity.Stats.LiveAgility / 50f)  //50% @ 25 stat, 100% @ 50 stat, 200% @ 100 stat
                );
        }
    }

    float powerMaxDrawTime
    {
        get
        {
            return (100/powerSpeed) +
                ((float)playerEntity.Stats.LiveStrength / 25f) +
                ((float)playerEntity.Skills.GetLiveSkillValue(DFCareer.Skills.Archery) / 25f);
        }
    }

    float powerSpeed
    {
        get
        {
            short weaponSkillID = (short)DFCareer.Skills.HandToHand;

            DaggerfallUnityItem weapon = GameManager.Instance.RightHandWeapon.SpecificWeapon;
            if (weapon != null)
            {
                weaponSkillID = weapon.GetWeaponSkillIDAsShort();

                if (GameManager.Instance.RightHandWeapon.WeaponType == WeaponTypes.Bow)
                {
                    return Mathf.CeilToInt(
                        ((float)playerEntity.Skills.GetLiveSkillValue(weaponSkillID)) *
                        ((float)playerEntity.Stats.LiveStrength/50)
                        );
                }
            }

            return Mathf.CeilToInt(
                ((float)playerEntity.Skills.GetLiveSkillValue(weaponSkillID)) *
                Mathf.Clamp(2-(weapon.EffectiveUnitWeightInKg() / 15f),0f,1.9f)
                );
        }
    }

    float powerCost
    {
        get
        {
            return Mathf.CeilToInt(
                100 -
                ((float)playerEntity.Stats.LiveAgility / 2)
                );
        }
    }

    bool powerDrawIcon
    {
        get
        {
            if (powerIconStyle == 1)
            {
                if (GameManager.Instance.WeaponManager.Sheathed ||
                    GameManager.Instance.WeaponManager.EquipCountdownRightHand > 0 ||
                    GameManager.Instance.WeaponManager.EquipCountdownLeftHand > 0)
                    return false;

                if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                {
                    if (DaggerfallUnity.Settings.BowDrawback)
                    {
                        if (ScreenWeapon.WeaponState != WeaponStates.StrikeUp && ScreenWeapon.GetCurrentFrame() != 3)
                            return false;
                    }
                }
                else
                {
                    if (ScreenWeapon.IsAttacking())
                        return false;
                }
            }

            return true;
        }
    }
    bool powerMessage;

    int powerIconStyle = 0; //0 = none, 1 = crosshairs+position, 2 = widget+tint
    float powerIconScale = 1;

    Texture2D powerIconCrosshairTexture;
    Rect powerIconCrosshairLeftRect;
    Rect powerIconCrosshairRightRect;
    float powerIconCrosshairDistance;

    Texture2D powerIconWidgetTexture;
    Texture2D powerIconWidgetBaseTexture;
    Vector2 powerIconWidgetOffset;
    Rect powerIconWidgetRect;
    Rect powerIconWidgetBaseRect;
    Color powerIconWidgetColorCurrent;
    Color powerIconWidgetColorMin = Color.red;
    Color powerIconWidgetColorMax = Color.green;

    bool lastUsedHand;

    IEnumerator conflict;

    //events
    public event Action<int> OnMissileChange;
    public event Action<GameObject, DaggerfallUnityItem, float> OnMissileSpawn;
    public event Action<GameObject, DaggerfallUnityItem ,GameObject, Vector3> OnMissileHit;

    //reflection
    FieldInfo LastBowUsed;

    //Vanilla attack control stuff
    private Gesture _gesture;
    private int _longestDim;
    private const float MaxGestureSeconds = 1.0f;
    bool joystickSwungOnce = false;
    private const float resetJoystickSwingRadius = 0.4f;

    private class Gesture
    {
        // The cursor is auto-centered every frame so the x/y becomes delta x/y
        private readonly List<TimestampedMotion> _points;
        // The result of the sum of all points in the gesture trail
        private Vector2 _sum;
        // The total travel distance of the gesture trail
        // This isn't equal to the magnitude of the sum because the trail may bend
        public float TravelDist { get; private set; }

        public Gesture()
        {
            _points = new List<TimestampedMotion>();
            _sum = new Vector2();
            TravelDist = 0f;
        }

        // Trims old gesture points & keeps the sum and travel variables up to date
        private void TrimOld()
        {
            var old = 0;
            foreach (var point in _points)
            {
                if (Time.time - point.Time <= MaxGestureSeconds)
                    continue;
                old++;
                _sum -= point.Delta;
                TravelDist -= point.Delta.magnitude;
            }
            _points.RemoveRange(0, old);
        }

        /// <summary>
        /// Adds the given delta mouse x/ys top the gesture trail
        /// </summary>
        /// <param name="dx">Mouse delta x</param>
        /// <param name="dy">Mouse delta y</param>
        /// <returns>The summed vector of the gesture (not the trail itself)</returns>
        public Vector2 Add(float dx, float dy)
        {
            TrimOld();

            _points.Add(new TimestampedMotion
            {
                Time = Time.time,
                Delta = new Vector2 { x = dx, y = dy }
            });
            _sum += _points.Last().Delta;
            TravelDist += _points.Last().Delta.magnitude;

            return new Vector2 { x = _sum.x, y = _sum.y };
        }

        /// <summary>
        /// Clears the gesture
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            _sum *= 0;
            TravelDist = 0f;
        }
    }

    private struct TimestampedMotion
    {
        public float Time;
        public Vector2 Delta;

        public override string ToString()
        {
            return string.Format("t={0}s, dx={1}, dy={2}", Time, Delta.x, Delta.y);
        }
    }

    public Vector3 GetEyePos
    {
        get
        {
            return playerController.transform.position + playerController.center + (Vector3.up * (playerController.height * 0.5f));
        }
    }

    void Awake()
    {
        Instance = this;

        //load textures
        TextureReplacement.TryImportTexture(9638, 0, 0, out powerIconCrosshairTexture);
        TextureReplacement.TryImportTexture(9638, 1, 0, out powerIconWidgetTexture);
        TextureReplacement.TryImportTexture(9638, 1, 1, out powerIconWidgetBaseTexture);

        layerMask = ~(1 << LayerMask.NameToLayer("Player"));
        layerMask = layerMask & ~(1 << LayerMask.NameToLayer("Ignore Raycast"));

        skyCamera = GameManager.Instance.SkyRig.SkyCamera;

        playerCamera = GameManager.Instance.MainCamera;
        playerController = GameManager.Instance.PlayerController;
        playerEntity = GameManager.Instance.PlayerEntity;
        playerMotor = GameManager.Instance.PlayerMotor;
        weaponManager = GameManager.Instance.WeaponManager;
        ScreenWeapon = GameManager.Instance.WeaponManager.ScreenWeapon;

        LastBowUsed = weaponManager.GetType().GetField("lastBowUsed", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Instance);

        _gesture = new Gesture();
        _longestDim = Math.Max(Screen.width, Screen.height);

        //setup trajectory line renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        Color startColor = new Color(1, 1, 1, 0);
        Color endColor = new Color(1, 1, 1, 1);
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
        lineRenderer.enabled = false;

        mod.LoadSettingsCallback = LoadSettings;

        SaveLoadManager.OnLoad += OnLoad;
        StartGameBehaviour.OnNewGame += OnNewGame;
        StartGameBehaviour.OnStartMenu += OnStartMenu;

        mod.MessageReceiver = MessageReceiver;
        mod.IsReady = true;
    }

    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Controls"))
        {
            attackKeyCode = SetKeyFromText(settings.GetString("Controls", "AttackInput"));
            meleeMode = settings.GetValue<int>("Controls", "MeleeMode");
            meleeModeVanillaMouseDampenMod = 1-settings.GetValue<float>("Controls", "VanillaMeleeViewDampenStrength");
        }
        if (change.HasChanged("HitDetection"))
        {
            safeguard = settings.GetValue<bool>("HitDetection", "Safeguard");
            reachRadial = settings.GetValue<bool>("HitDetection", "RadialDistance");
            reachMultiplier = settings.GetValue<float>("HitDetection", "ReachMultiplier");
            cleaveParries = settings.GetValue<bool>("HitDetection", "ParriesPreventCleave");
            cleaveInterval = settings.GetValue<float>("HitDetection", "CleaveInterval");
            cleaveRequireLOS = settings.GetValue<bool>("HitDetection", "RequireLineOfSight");
            cleaveValueMultiplier = settings.GetValue<float>("HitDetection", "StartingCleaveMultiplier");
            cleaveWeightMultiplier = settings.GetValue<float>("HitDetection", "TargetWeightMultiplier");
        }
        if (change.HasChanged("PowerAttacks"))
        {
            powerAttacks = settings.GetValue<bool>("PowerAttacks", "Enable");
            powerAccuracy = settings.GetValue<int>("PowerAttacks", "AccuracyScaling");
            powerDamage = settings.GetValue<int>("PowerAttacks", "Damage");
            powerIconStyle = settings.GetValue<int>("PowerAttacks", "Indicator");
            powerIconScale = settings.GetValue<float>("PowerAttacks", "IndicatorScale");
            powerIconCrosshairDistance = settings.GetValue<float>("PowerAttacks", "CrosshairDistance");
            powerIconWidgetOffset = new Vector2(settings.GetTupleFloat("PowerAttacks", "WidgetOffset").First, settings.GetTupleFloat("PowerAttacks", "WidgetOffset").Second);
            powerMessage = settings.GetValue<bool>("PowerAttacks", "DebugMessages");
            powerIconWidgetColorMin = settings.GetColor("PowerAttacks", "WidgetColorMin");
            powerIconWidgetColorMax = settings.GetColor("PowerAttacks", "WidgetColorMax");
        }
        if (change.HasChanged("Feedback"))
        {
            feedbackPause = settings.GetValue<bool>("Feedback", "PauseEnemyParries");
            feedbackPauseScale = settings.GetValue<float>("Feedback", "PauseDurationScale");
            feedbackDodge = settings.GetValue<bool>("Feedback", "VisibleEnemyDodges");
            feedbackDodgeScale = settings.GetValue<float>("Feedback", "DodgeDistanceScale");
            feedbackHurt = settings.GetValue<bool>("Feedback", "SavingThrowKnockback");
            feedbackHurtScale = settings.GetValue<float>("Feedback", "KnockbackScale");
        }
        if (change.HasChanged("ImprovedArchery"))
        {
            archery = settings.GetValue<int>("ImprovedArchery", "Mode");
            archeryMissileRadius = settings.GetValue<float>("ImprovedArchery", "CollisionRadius");
            archeryMissileSpeed = settings.GetValue<float>("ImprovedArchery", "Speed") * 35f;
            archeryMissileAngle = settings.GetValue<float>("ImprovedArchery", "AngleOffset");
            archeryMissileSpread = settings.GetValue<float>("ImprovedArchery", "MaxDispersion");
            archeryMissileGravity = settings.GetValue<float>("ImprovedArchery", "Gravity");
            archeryTrajectory = settings.GetValue<bool>("ImprovedArchery", "ShowTrajectory");
            archeryDrawZoom = settings.GetValue<float>("ImprovedArchery", "DrawZoomMagnification");
            archeryDrawZoomTime = settings.GetValue<float>("ImprovedArchery", "DrawZoomDuration");
            archeryDrawZoomDelay = settings.GetValue<float>("ImprovedArchery", "DrawZoomDelay");
            archeryOverdraw = settings.GetValue<bool>("ImprovedArchery", "DrawScaling");
            archeryOverdrawTime = settings.GetValue<float>("ImprovedArchery", "DrawScaleDuration");
        }
        if (change.HasChanged("Variables"))
        {
            statMelee = new Vector2(settings.GetTupleFloat("Variables", "HandToHand").First,settings.GetTupleFloat("Variables", "HandToHand").Second);
            statDagger = new Vector2(settings.GetTupleFloat("Variables", "Dagger").First,settings.GetTupleFloat("Variables", "Dagger").Second);
            statTanto = new Vector2(settings.GetTupleFloat("Variables", "Tanto").First,settings.GetTupleFloat("Variables", "Tanto").Second);
            statShortsword = new Vector2(settings.GetTupleFloat("Variables", "Shortsword").First,settings.GetTupleFloat("Variables", "Shortsword").Second);
            statWakazashi = new Vector2(settings.GetTupleFloat("Variables", "Wakazashi").First,settings.GetTupleFloat("Variables", "Wakazashi").Second);
            statBroadsword = new Vector2(settings.GetTupleFloat("Variables", "Broadsword").First,settings.GetTupleFloat("Variables", "Broadsword").Second);
            statSaber = new Vector2(settings.GetTupleFloat("Variables", "Saber").First,settings.GetTupleFloat("Variables", "Saber").Second);
            statLongsword = new Vector2(settings.GetTupleFloat("Variables", "Longsword").First,settings.GetTupleFloat("Variables", "Longsword").Second);
            statKatana = new Vector2(settings.GetTupleFloat("Variables", "Katana").First,settings.GetTupleFloat("Variables", "Katana").Second);
            statClaymore = new Vector2(settings.GetTupleFloat("Variables", "Claymore").First,settings.GetTupleFloat("Variables", "Claymore").Second);
            statDaikatana = new Vector2(settings.GetTupleFloat("Variables", "Daikatana").First,settings.GetTupleFloat("Variables", "Daikatana").Second);
            statStaff = new Vector2(settings.GetTupleFloat("Variables", "Staff").First,settings.GetTupleFloat("Variables", "Staff").Second);
            statMace = new Vector2(settings.GetTupleFloat("Variables", "Mace").First,settings.GetTupleFloat("Variables", "Mace").Second);
            statFlail = new Vector2(settings.GetTupleFloat("Variables", "Flail").First,settings.GetTupleFloat("Variables", "Flail").Second);
            statWarhammer = new Vector2(settings.GetTupleFloat("Variables", "Warhammer").First,settings.GetTupleFloat("Variables", "Warhammer").Second);
            statBattleAxe = new Vector2(settings.GetTupleFloat("Variables", "BattleAxe").First,settings.GetTupleFloat("Variables", "BattleAxe").Second);
            statWarAxe = new Vector2(settings.GetTupleFloat("Variables", "WarAxe").First,settings.GetTupleFloat("Variables", "WarAxe").Second);
            statArchersAxe = new Vector2(settings.GetTupleFloat("Variables", "Archer'sAxe").First,settings.GetTupleFloat("Variables", "Archer'sAxe").Second);
            statLightFlail = new Vector2(settings.GetTupleFloat("Variables", "LightFlail").First,settings.GetTupleFloat("Variables", "LightFlail").Second);
        }

        if (!overrideRegistered)
        {

            if (powerAttacks || archery > 0)
            {
                //Damage and Accuracy scaling based on Power and Improved Archery overrides
                FormulaHelper.RegisterOverride(mod, "AdjustWeaponHitChanceMod", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)AdjustWeaponHitChanceMod);
                FormulaHelper.RegisterOverride(mod, "AdjustWeaponAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)AdjustWeaponAttackDamage);

                if (powerAttacks && powerDamage == 2)
                {
                    //Deterministic attack damage based on Power
                    if (SphincterVisionAttacks != null)
                    {
                        FormulaHelper.RegisterOverride(mod, "CalculateWeaponAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)CalculateWeaponAttackDamage_SphincterVision);
                        FormulaHelper.RegisterOverride(mod, "CalculateHandToHandAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, bool, int>)CalculateHandToHandAttackDamage_SphincterVision);
                    }
                    else
                    {
                        FormulaHelper.RegisterOverride(mod, "CalculateWeaponAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, int, DaggerfallUnityItem, int>)CalculateWeaponAttackDamage);
                        FormulaHelper.RegisterOverride(mod, "CalculateHandToHandAttackDamage", (Func<DaggerfallEntity, DaggerfallEntity, int, bool, int>)CalculateHandToHandAttackDamage);
                    }
                }

                overrideRegistered = true;
            }
        }
    }

    void MessageReceiver(string message, object data, DFModMessageCallback callBack)
    {
        switch (message)
        {
            case "getSwingKeyCode":
                callBack?.Invoke("getSwingKeyCode", attackKeyCode);
                break;

            case "getWeaponReach":
                callBack?.Invoke("getWeaponReach", GetWeaponReach());
                break;

            case "overrideMissile":
                OverrideMissileProperties(data as object[]);
                break;

            case "resetMissile":
                ResetMissileProperties();
                break;

            case "onMissileChange":
                OnMissileChange += data as Action<int>;
                break;

            case "onMissileSpawn":
                OnMissileSpawn += data as Action<GameObject,DaggerfallUnityItem,float>;
                break;

            case "onMissileHit":
                OnMissileHit += data as Action<GameObject, DaggerfallUnityItem, GameObject, Vector3>;
                break;

            default:
                Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                break;
        }
    }

    private void OnGUI()
    {
        if (GameManager.IsGamePaused || SaveLoadManager.Instance.LoadInProgress)
            return;

        GUI.depth = -10;

        if (DaggerfallUI.Instance.CustomScreenRect != null)
            screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
        else
            screenRect = new Rect(0, 0, Screen.width, Screen.height);
        ScaleX = (float)screenRect.width / (float)nativeScreenWidth;
        ScaleY = (float)screenRect.height / (float)nativeScreenHeight;

        if (archeryShowAmmoCounter)
        {
            if (!archeryAmmoCounterTextLabel.Enabled)
            {
                archeryAmmoCounterTextLabel.Enabled = true;
                UpdateAmmoCount();
            }

            string message = archeryAmmoLabel;

            //place it next to the vitals bar
            Vector2 arrowLabelPos = new Vector2(screenRect.x + (screenRect.width * 0.02f) + DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Rectangle.width, screenRect.y + (screenRect.height*0.75f) - DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Rectangle.height - DaggerfallUI.Instance.DaggerfallHUD.HUDVitals.Parent.Position.y);
            Vector2 arrowLabelScale = new Vector2(ScaleX, ScaleY);

            if (Event.current.type == EventType.Repaint)
            {
                DaggerfallUI.DefaultFont.DrawText(message, arrowLabelPos, arrowLabelScale, archeryAmmoCounterColor, Color.black, Vector2.one);
            }
        }
        else
        {
            if (archeryAmmoCounterTextLabel.Enabled)
                archeryAmmoCounterTextLabel.Enabled = false;
        }

        if (powerAttacks)
        {
            float scale = powerIconScale;

            //draw Power indicator
            if (powerDrawIcon && powerIconStyle > 0)
            {
                if (powerIconStyle == 2)
                {
                    powerIconWidgetBaseRect = new Rect(
                        new Vector2(screenRect.x + (screenRect.width * powerIconWidgetOffset.x) - (powerIconWidgetBaseTexture.width * 0.5f * scale * ScaleX), screenRect.y + (screenRect.height * powerIconWidgetOffset.y) - (powerIconWidgetBaseTexture.height * scale * 0.5f * ScaleY)),
                        new Vector2(powerIconWidgetBaseTexture.width * scale * ScaleX, powerIconWidgetBaseTexture.height * scale * ScaleY)
                        );

                    powerIconWidgetRect = new Rect(
                        new Vector2(screenRect.x + (screenRect.width * powerIconWidgetOffset.x) - (powerIconWidgetTexture.width * 0.5f * scale * ScaleX), screenRect.y + (screenRect.height * powerIconWidgetOffset.y) - (powerIconWidgetTexture.height * scale * 0.5f * ScaleY)),
                        new Vector2(powerIconWidgetTexture.width * scale * ScaleX, powerIconWidgetTexture.height * scale * ScaleY)
                        );

                    if (Event.current.type == EventType.Repaint)
                    {
                        DaggerfallUI.DrawTexture(powerIconWidgetBaseRect, powerIconWidgetBaseTexture, ScaleMode.StretchToFill, true, Color.white);
                        DaggerfallUI.DrawTexture(powerIconWidgetRect, powerIconWidgetTexture, ScaleMode.StretchToFill, true, powerIconWidgetColorCurrent);
                    }
                }
                else
                {
                    scale *= 0.5f;

                    powerIconCrosshairLeftRect = new Rect(
                        new Vector2(screenRect.x + ((screenRect.width * 0.475f) - (screenRect.width * 0.125f * powerIconCrosshairDistance)) + ((screenRect.width * 0.125f * powerIconCrosshairDistance) * (powerCurrent / 100)), screenRect.y + (screenRect.height * 0.5f) - (powerIconCrosshairTexture.height * scale * 0.5f * ScaleY)),
                        new Vector2(-powerIconCrosshairTexture.width * scale * ScaleX, powerIconCrosshairTexture.height * scale * ScaleY)
                        );
                    powerIconCrosshairRightRect = new Rect(
                        new Vector2(screenRect.x + ((screenRect.width * 0.525f) + (screenRect.width * 0.125f * powerIconCrosshairDistance)) - ((screenRect.width * 0.125f * powerIconCrosshairDistance) * (powerCurrent / 100)), screenRect.y + (screenRect.height * 0.5f) - (powerIconCrosshairTexture.height * scale * 0.5f * ScaleY)),
                        new Vector2(powerIconCrosshairTexture.width * scale * ScaleX, powerIconCrosshairTexture.height * scale * ScaleY)
                        );

                    if (Event.current.type == EventType.Repaint)
                    {
                        DaggerfallUI.DrawTexture(powerIconCrosshairLeftRect, powerIconCrosshairTexture, ScaleMode.StretchToFill, true, Color.white);
                        DaggerfallUI.DrawTexture(powerIconCrosshairRightRect, powerIconCrosshairTexture, ScaleMode.StretchToFill, true, Color.white);
                    }
                }
            }
        }
    }

    void SetPower()
    {
        lastPower = Mathf.Lerp(powerMin,powerMax,powerCurrent/100)/100;
        powerCurrent -= powerCost;

        Debug.Log("TOME OF BATTLE - POWER ATTACKS - MAX DRAW TIME IS " + powerMaxDrawTime.ToString() + " SECONDS!");
        Debug.Log("TOME OF BATTLE - POWER ATTACKS - POWER SPEED IS " + powerSpeed.ToString("0") + "%!");
        Debug.Log("TOME OF BATTLE - POWER ATTACKS - MIN POWER IS " + powerMin.ToString("0") + "%!");
        Debug.Log("TOME OF BATTLE - POWER ATTACKS - MAX POWER IS " + powerMax.ToString("0") + "%!");
        Debug.Log("TOME OF BATTLE - POWER ATTACKS - LAST POWER IS " + (LastPower*100).ToString("0") + "%!");
    }

    void UpdatePower()
    {
        if (GameManager.IsGamePaused)
            return;

        if (GameManager.Instance.WeaponManager.Sheathed || GameManager.Instance.WeaponManager.EquipCountdownRightHand > 0 || GameManager.Instance.WeaponManager.EquipCountdownLeftHand > 0 || lastUsedHand != GameManager.Instance.WeaponManager.UsingRightHand)
        {
            powerCurrent = 0;
            lastUsedHand = GameManager.Instance.WeaponManager.UsingRightHand;
            powerIconWidgetColorCurrent = Color.black;
            return;
        }

        if (powerCurrent < 0)
            powerCurrent = 0;

        if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
        {
            if (DaggerfallUnity.Settings.BowDrawback)
            {
                //build power at a constant rate while bow is drawn, then decay after reaching max power
                if (ScreenWeapon.WeaponState == WeaponStates.StrikeUp || ScreenWeapon.GetCurrentFrame() == 3)
                {
                    //powerDrawIcon = true;
                    if (powerCurrent < 100 && drawTimer < powerMaxDrawTime)
                    {
                        powerCurrent += Time.deltaTime * powerSpeed;
                    }
                    else if (drawTimer < powerMaxDrawTime)
                    {
                        powerCurrent = 100;
                    }
                    else if (powerCurrent > 0)
                    {
                        powerCurrent -= Time.deltaTime * powerSpeed;
                    }
                }
            }
            else
            {
                //oscillate power while not attacking
                powerCurrent = Mathf.Lerp(0,100,0.5f + (Mathf.Sin(Time.time * (powerSpeed*0.05f))*0.5f));
            }
        }
        else
        {
            //build power at a constant rate while not attacking
            if (!ScreenWeapon.IsAttacking())
            {
                if (powerCurrent < 100)
                    powerCurrent += Time.deltaTime * powerSpeed;
                else
                    powerCurrent = 100;
            }
        }

        powerIconWidgetColorCurrent = Color.Lerp(powerIconWidgetColorMin, powerIconWidgetColorMax, Mathf.Round((powerCurrent / 100) * 10) / 10);
    }

    private void LateUpdate()
    {
        if (powerAttacks)
            UpdatePower();

        // Handle bow with no arrows
        ItemGroups itemGroup = ItemGroups.Weapons;
        if (archeryCustomItemTemplate != 0)
            itemGroup = ItemGroups.UselessItems2;

        if (!GameManager.Instance.WeaponManager.Sheathed && ScreenWeapon.WeaponType == WeaponTypes.Bow && GameManager.Instance.PlayerEntity.Items.GetItem(itemGroup, archeryItemTemplate, allowQuestItem: false) == null)
        {
            if (archeryCustomItemTemplate != 0)
            {
                if (archeryAutoReset)
                    ResetMissileProperties();
                else
                {
                    GameManager.Instance.WeaponManager.SheathWeapons();
                    DaggerfallUI.SetMidScreenText(TextManager.Instance.GetLocalizedText("youHaveNoArrows"));
                }
            }
            else
            {
                GameManager.Instance.WeaponManager.SheathWeapons();
                DaggerfallUI.SetMidScreenText(TextManager.Instance.GetLocalizedText("youHaveNoArrows"));
            }
        }

        if (GameManager.IsGamePaused || !GameManager.Instance.IsPlayingGame() || !GameManager.Instance.IsPlayerOnHUD)
            blockHoldInput = true;
        else
        {
            if (!InputManager.Instance.GetKey(attackKeyCode))
                blockHoldInput = false;
        }

        if (!reloaded)
        {
            if (reloadTimer < reloadTime)
                reloadTimer += Time.deltaTime;
            else
                reloaded = true;
        }

        //Dampen view speed when swinging a weapon
        if (meleeMode == 0 && meleeModeVanillaMouseDampenMod < 1)
        {
            if (ScreenWeapon.WeaponType != WeaponTypes.Bow && InputManager.Instance.GetKey(attackKeyCode))
                GameManager.Instance.PlayerMouseLook.sensitivityScale = DaggerfallUnity.Settings.MouseLookSensitivity * meleeModeVanillaMouseDampenMod;
            else
                GameManager.Instance.PlayerMouseLook.sensitivityScale = DaggerfallUnity.Settings.MouseLookSensitivity;
        }

        if (ScreenWeapon.IsAttacking())
        {
            //started an attack
            if (!isAttacking)
            {
                hasAttacked = false;
                isAttacking = true;

                if (powerAttacks)
                {
                    if (ScreenWeapon.WeaponType != WeaponTypes.Bow)
                        SetPower();
                }
            }

            if (!hasAttacked)
            {
                if (ScreenWeapon.GetCurrentFrame() >= ScreenWeapon.GetHitFrame())
                {
                    if (powerAttacks)
                    {
                        if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                            SetPower();
                    }

                    if (ScreenWeapon.WeaponType != WeaponTypes.Bow)
                        StartCoroutine(DoCleaveOnNextFrame());
                    else
                        SpawnMissile();

                    // Fatigue loss
                    playerEntity.DecreaseFatigue(11);

                    hasAttacked = true;
                }
            }

            if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
            {
                //do bow stuff here
                if (DaggerfallUnity.Settings.BowDrawback)
                {

                    if (drawTimer > drawTime || InputManager.Instance.ActionStarted(InputManager.Actions.ActivateCenterObject))
                    {
                        ScreenWeapon.ChangeWeaponState(WeaponStates.Idle);
                        reloaded = false;
                        reloadTime = FormulaHelper.GetBowCooldownTime(playerEntity) * reloadTimeMod * 0.5f;
                        reloadTimer = 0;
                        blockHoldInput = true;

                        if (powerAttacks)
                            powerCurrent = 0;
                    }
                    else if (!InputManager.Instance.GetKey(attackKeyCode))
                    {
                        ScreenWeapon.OnAttackDirection(WeaponManager.MouseDirections.Down);
                    }
                    else
                    {
                        drawTimer += Time.deltaTime;

                        if (archeryDrawZoom > 1)
                        {
                            float zoom = DaggerfallUnity.Settings.FieldOfView / archeryDrawZoomTyped;
                            if (playerCamera.fieldOfView != zoom)
                            {
                                playerCamera.fieldOfView = Mathf.Lerp(DaggerfallUnity.Settings.FieldOfView, zoom, (drawTimer - archeryDrawZoomDelay) / archeryDrawZoomTime);
                            }
                        }

                        if (archeryTrajectory && archeryMissileGravity > 0)
                            DrawTrajectory();
                    }
                }
            }

            swingTimer = 0;
        }
        else
        {
            if (!reloaded)
            {
                if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                {
                    ScreenWeapon.ShowWeapon = false;
                }
            }

            //attack has finished
            if (isAttacking)
            {
                attackTarget = null;
                attackDamage = 0;
                isAttacking = false;
            }
            else
            {
                if (ScreenWeapon.WeaponType == WeaponTypes.Bow)
                {
                    //do bow stuff here
                    if (DaggerfallUnity.Settings.BowDrawback)
                    {
                        if (archeryDrawZoom > 1)
                        {
                            if (playerCamera.fieldOfView != DaggerfallUnity.Settings.FieldOfView)
                            {
                                playerCamera.fieldOfView = Mathf.MoveTowards(playerCamera.fieldOfView, DaggerfallUnity.Settings.FieldOfView, DaggerfallUnity.Settings.FieldOfView * Time.deltaTime);
                            }
                        }

                        if (lineRenderer.enabled)
                            lineRenderer.enabled = false;
                    }
                }

                //reset swing count after interval
                if (swingTimer < swingTime)
                    swingTimer += Time.deltaTime;
                else
                    swingCount = 0;

                if (CanAttack())
                {
                    WeaponManager.MouseDirections swingDirection = WeaponManager.MouseDirections.None;

                    if (InputManager.Instance.GetKey(attackKeyCode))
                    {
                        if (ScreenWeapon.WeaponType != WeaponTypes.Bow)
                        {
                            //method to pick attack type here
                            if (meleeMode == 3)
                            {
                                //Speed-based
                                if (InputManager.Instance.HasAction(InputManager.Actions.Run) || GameManager.Instance.SpeedChanger.ToggleRun)
                                {
                                    //if RUN is held down
                                    if (playerMotor.IsMovingLessThanHalfSpeed)
                                    {
                                        //do a chop while moving at half-speed or less
                                        swingDirection = WeaponManager.MouseDirections.Down;
                                    }
                                    else
                                    {
                                        //do a thrust if moving normally
                                        swingDirection = WeaponManager.MouseDirections.Up;
                                    }
                                }
                                else
                                {
                                    if (playerMotor.IsMovingLessThanHalfSpeed)
                                    {
                                        //do alternating horizontal swings while moving at half-speed or less
                                        if (weaponManager.ScreenWeapon.FlipHorizontal)
                                        {
                                            swingDirection = WeaponManager.MouseDirections.Right;

                                            if (swingCount % 2 != 0)
                                                swingDirection = WeaponManager.MouseDirections.Left;
                                        }
                                        else
                                        {
                                            swingDirection = WeaponManager.MouseDirections.Left;

                                            if (swingCount % 2 != 0)
                                                swingDirection = WeaponManager.MouseDirections.Right;
                                        }
                                    }
                                    else
                                    {
                                        //do alternating horizontal swings while moving at half-speed or less
                                        if (weaponManager.ScreenWeapon.FlipHorizontal)
                                        {
                                            //do alternating diagonal swings if moving normally
                                            swingDirection = WeaponManager.MouseDirections.DownRight;

                                            if (swingCount % 2 != 0)
                                                swingDirection = WeaponManager.MouseDirections.DownLeft;
                                        }
                                        else
                                        {
                                            //do alternating diagonal swings if moving normally
                                            swingDirection = WeaponManager.MouseDirections.DownLeft;

                                            if (swingCount % 2 != 0)
                                                swingDirection = WeaponManager.MouseDirections.DownRight;
                                        }
                                    }
                                }
                            }
                            else if (meleeMode == 2)
                            {
                                //Morrowind
                                if (Mathf.Abs(InputManager.Instance.Horizontal) > 0 && Mathf.Abs(InputManager.Instance.Vertical) == 0)
                                {
                                    //do alternating horizontal swings if strafing
                                    swingDirection = WeaponManager.MouseDirections.Left;

                                    if (InputManager.Instance.Horizontal < 0)
                                        swingDirection = WeaponManager.MouseDirections.Right;

                                    if (swingCount % 2 != 0)
                                    {
                                        if (swingDirection == WeaponManager.MouseDirections.Left)
                                            swingDirection = WeaponManager.MouseDirections.Right;
                                        else
                                            swingDirection = WeaponManager.MouseDirections.Left;
                                    }
                                }
                                else if (InputManager.Instance.Vertical > 0 && Mathf.Abs(InputManager.Instance.Horizontal) == 0)
                                {
                                    //do a thrust if moving forward
                                    swingDirection = WeaponManager.MouseDirections.Up;
                                }
                                else
                                {
                                    //do a chop
                                    swingDirection = WeaponManager.MouseDirections.Down;
                                }
                            }
                            else if (meleeMode == 1)
                            {
                                //Hold to attack

                                //random swing direction
                                //swingDirection = (WeaponManager.MouseDirections)UnityEngine.Random.Range((int)WeaponManager.MouseDirections.UpRight, (int)WeaponManager.MouseDirections.DownRight + 1);

                                //iterate through swing directions
                                //start at the top because there's some weirdness with Weapon Widget startup animations when doing a StrikeDown after a StrikeDownLeft
                                int value = 8 - swingCount;
                                if (value < 3)
                                {
                                    value = 8;
                                    swingCount = 0;
                                }
                                swingDirection = (WeaponManager.MouseDirections)value;
                            }
                            else
                            {
                                //Drag to attack
                                if (meleeModeVanillaMouseDampenMod < 1)
                                    GameManager.Instance.PlayerMouseLook.sensitivityScale = DaggerfallUnity.Settings.MouseLookSensitivity * meleeModeVanillaMouseDampenMod;

                                swingDirection = TrackMouseAttack();
                            }

                            swingCount++;
                        }
                        else
                        {
                            //do bow stuff here
                            if (reloaded)
                            {
                                if (DaggerfallUnity.Settings.BowDrawback)
                                {
                                    swingDirection = WeaponManager.MouseDirections.Up;
                                    drawTimer = 0;
                                }
                                else
                                {
                                    swingDirection = WeaponManager.MouseDirections.Down;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (meleeMode == 0)
                            _gesture.Clear();
                    }

                    if (swingDirection != WeaponManager.MouseDirections.None)
                        ScreenWeapon.OnAttackDirection(swingDirection);
                }
                else
                {
                    if (meleeMode == 0)
                        _gesture.Clear();
                }
            }
        }

    }

    void DrawTrajectory()
    {
        if (!lineRenderer.enabled)
            lineRenderer.enabled = true;

        positions = new List<Vector3>();

        //get start
        Vector3 start = GetEyePos;
        // Adjust slightly downward to match bow animation
        Vector3 adjust = (GameManager.Instance.MainCamera.transform.rotation * -GameManager.Instance.PlayerObject.transform.up) * 0.11f;
        // Offset forward to avoid collision with player
        adjust += GameManager.Instance.MainCamera.transform.forward * 0.6f;
        // Adjust to the right or left to match bow animation
        if (!GameManager.Instance.WeaponManager.ScreenWeapon.FlipHorizontal)
            adjust += GameManager.Instance.MainCamera.transform.right * 0.15f;
        else
            adjust -= GameManager.Instance.MainCamera.transform.right * 0.15f;
        start += adjust;
        positions.Add(start);

        Vector3 dir = Quaternion.AngleAxis(-archeryMissileAngleTyped, GameManager.Instance.MainCameraObject.transform.right) * playerCamera.transform.forward;

        Vector3 startStep = dir * archeryMissileSpeedTyped * Mathf.Clamp(drawTimer / archeryOverdrawTimeTyped, 0.5f, 2f);

        //get all positions
        bool stopped = false;
        Vector3 currentPos = start;
        Vector3 currentGravity = Vector3.zero;
        Vector3 currentStep = startStep;
        Ray ray = new Ray(currentPos, currentStep.normalized);
        RaycastHit hit = new RaycastHit();
        while (!stopped && positions.Count < 200)
        {
            startStep = dir * archeryMissileSpeedTyped * Mathf.Clamp(drawTimer / archeryOverdrawTimeTyped, 0.5f, 2f);

            currentGravity += Vector3.down * (9.8f * (archeryMissileGravityTyped * 0.01f));
            currentStep = (startStep * (Time.fixedDeltaTime * 1)) + (currentGravity * (Time.fixedDeltaTime * 1));

            ray = new Ray(currentPos, currentStep.normalized);

            if (Physics.Raycast(ray, out hit, currentStep.magnitude, layerMask))
            {
                stopped = true;
                positions.Add(hit.point);
            }
            else
            {
                currentPos += currentStep;
                positions.Add(currentPos);
            }
        }
        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }

    void UpdateAmmoCount()
    {
        ItemGroups itemGroup = ItemGroups.Weapons;
        if (archeryCustomItemTemplate != 0)
            itemGroup = ItemGroups.UselessItems2;

        List<DaggerfallUnityItem> items = GameManager.Instance.PlayerEntity.Items.SearchItems(itemGroup, archeryItemTemplate);
        int count = items.Count;

        if (count < 1)
        {
            Debug.Log("No items of this template detected!");
            return;
        }

        foreach (DaggerfallUnityItem item in items)
        {
            if (item.IsQuestItem)
            {
                count -= 1;
                continue;
            }

            if (item.IsStackable())
                count += item.stackCount - 1;
        }

        archeryAmmoCount = count;
        archeryAmmoLabel = items[0].ItemName + " (" + archeryAmmoCount.ToString() + ")";

        //get label length;
    }

    void OverrideMissileProperties(object[] data)
    {
        archeryCustomModelID = (int)data[0];
        archeryCustomItemTemplate = (int)data[1];
        archeryMissileSpeedMod = (float)data[2];
        archeryMissileGravityMod = (float)data[3];
        archeryBonusAccuracy = (int)data[4];
        archeryBonusDamage = (int)data[5];
        archeryAmmoCounterColorCustom = (Color)data[6];
        archeryAutoReset = (bool)data[7];

        if (OnMissileChange != null)
            OnMissileChange(archeryCustomItemTemplate);

        UpdateAmmoCount();
    }

    void ResetMissileProperties()
    {
        archeryCustomModelID = 0;
        archeryCustomItemTemplate = 0;
        archeryMissileSpeedMod = 1;
        archeryMissileGravityMod = 1;
        archeryBonusAccuracy = 0;
        archeryBonusDamage = 0;
        archeryAmmoCounterColorCustom = Color.black;
        archeryAutoReset = true;

        if (OnMissileChange != null)
            OnMissileChange(archeryCustomItemTemplate);

        UpdateAmmoCount();
    }

    bool CanAttack()
    {
        if (GameManager.IsGamePaused ||
            GameManager.Instance.PlayerEntity.IsParalyzed ||
            GameManager.Instance.ClimbingMotor.IsClimbing ||
            GameManager.Instance.PlayerEffectManager.HasReadySpell ||
            GameManager.Instance.PlayerSpellCasting.IsPlayingAnim ||
            (GameManager.Instance.PlayerMouseLook.cursorActive && DaggerfallUI.Instance.DaggerfallHUD != null && DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ActiveMouseOverLargeHUD) ||
            blockHoldInput
            )
            return false;

        return true;
    }

    void SpawnMissile()
    {
        DaggerfallMissile missile = Instantiate(weaponManager.ArrowMissilePrefab);
        if (missile)
        {
            missile.Caster = GameManager.Instance.PlayerEntityBehaviour;

            // Remove ammo
            ItemGroups itemGroup = ItemGroups.Weapons;
            if (archeryCustomItemTemplate != 0)
                itemGroup = ItemGroups.UselessItems2;

            ItemCollection playerItems = playerEntity.Items;
            DaggerfallUnityItem arrow = playerItems.GetItem(itemGroup, archeryItemTemplate, allowQuestItem: false, priorityToConjured: true);
            bool isArrowSummoned = arrow.IsSummoned;
            playerItems.RemoveOne(arrow);
            UpdateAmmoCount();

            if (archery > 0)
            {
                missile.enabled = false;

                WeaponFeaturesMissile missileCustom = missile.gameObject.AddComponent<WeaponFeaturesMissile>();
                missileCustom.modelID = (uint)archeryModelID;
                missileCustom.dfMissile = missile;
                if (archeryOverdraw && DaggerfallUnity.Settings.BowDrawback)
                    missileCustom.MovementSpeed = archeryMissileSpeedTyped * Mathf.Clamp(drawTimer/archeryOverdrawTimeTyped,0.5f,2f);
                else
                    missileCustom.MovementSpeed = archeryMissileSpeedTyped;
                missileCustom.ColliderRadius = archeryMissileRadius;
                missileCustom.gravityMod = archeryMissileGravityTyped * 0.01f;
                missileCustom.EnableLight = false;
                missileCustom.ImpactSound = SoundClips.ArrowHit;
                missileCustom.Caster = GameManager.Instance.PlayerEntityBehaviour;
                missileCustom.TargetType = TargetTypes.SingleTargetAtRange;
                missileCustom.ElementType = ElementTypes.None;
                missileCustom.IsArrow = true;
                missileCustom.IsArrowSummoned = isArrowSummoned;
                missileCustom.CustomAimPosition = GetEyePos;
                missileCustom.CustomAimDirection = (Quaternion.AngleAxis(-archeryMissileAngleTyped + (archeryMissileSpreadTyped * UnityEngine.Random.Range(-1f, 1f)), GameManager.Instance.MainCameraObject.transform.right) * playerCamera.transform.forward).normalized;
            }
            else
            {
                missile.TargetType = TargetTypes.SingleTargetAtRange;
                missile.ElementType = ElementTypes.None;
                missile.IsArrow = true;
                missile.IsArrowSummoned = isArrowSummoned;
                missile.CustomAimPosition = GetEyePos;
                missile.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            ScreenWeapon.PlaySwingSound();
            reloadTime = FormulaHelper.GetBowCooldownTime(playerEntity) * reloadTimeMod;
            reloadTimer = 0;
            reloaded = false;

            //Tally weapon skill when firing a missile
            playerEntity.TallySkill(ScreenWeapon.SpecificWeapon.GetWeaponSkillID(), 1);
            playerEntity.TallySkill(DFCareer.Skills.CriticalStrike, 1);

            if (LastBowUsed != null)
            {
                DaggerfallUnityItem lastBowUsed = ScreenWeapon.SpecificWeapon;
                LastBowUsed.SetValue(weaponManager, lastBowUsed);
            }

            if (OnMissileSpawn != null)
                OnMissileSpawn(missile.gameObject, ScreenWeapon.SpecificWeapon, drawTime);
        }
    }

    private void ModCompatibilityChecking()
    {
        if (modCompatibilityChecked)
            return;

        //listen to Combat Event Handler for attacks
        Mod ceh = ModManager.Instance.GetModFromGUID("fb086c76-38e7-4d83-91dc-f29e6f1bb17e");
        if (ceh != null)
        {
            ModManager.Instance.SendModMessage(ceh.Title, "onAttackDamageCalculated", (Action<DaggerfallEntity, DaggerfallEntity, DaggerfallUnityItem, int, int>)OnAttackDamageCalculated);
            ModManager.Instance.SendModMessage(ceh.Title, "onSavingThrow", (Action<DFCareer.Elements, DFCareer.EffectFlags, DaggerfallEntity, int>)OnSavingThrow);
        }

        SphincterVisionAttacks = ModManager.Instance.GetModFromGUID("28faf48c-9fe1-46a9-b155-fefc71547e26");

        mod.LoadSettings();

        modCompatibilityChecked = true;
    }

    public void RaiseOnMissileHitEvent(GameObject missileObject, GameObject hitObject, Vector3 hitPoint)
    {
        if (OnMissileHit != null)
            OnMissileHit(missileObject, ScreenWeapon.SpecificWeapon, hitObject, hitPoint);
    }

    public void OnAttackDamageCalculated(DaggerfallEntity attacker, DaggerfallEntity target, DaggerfallUnityItem weapon, int bodyPart, int damage)
    {

        if (attacker == playerEntity)
        {
            if (cleaveParries && damage < 1)
            {
                //stop cleave if missed target can parry
                if (GameManager.Instance.WeaponManager.ScreenWeapon.WeaponType != WeaponTypes.Bow)
                {
                    EnemyEntity enemy = target as EnemyEntity;
                    if (enemy.MobileEnemy.ParrySounds && cleaving != null)
                        AbortCleave();
                }
            }
        }

        if (target != playerEntity)
        {
            EnemyEntity enemy = target as EnemyEntity;
            if (damage < 1)
            {
                if (feedbackDodge && !enemy.MobileEnemy.ParrySounds)
                {
                    //make the target dodge
                    DodgeEnemy(attacker, target);
                }
                if (feedbackPause && enemy.MobileEnemy.ParrySounds)
                {
                    //make the target pause
                    PauseEnemy(attacker, target);
                }
            }
        }
    }

    public void OnSavingThrow(DFCareer.Elements elementType, DFCareer.EffectFlags effectFlags, DaggerfallEntity target, int result)
    {
        if (target != GameManager.Instance.PlayerEntity)
        {
            if (feedbackHurt && result > 0)
            {
                //knockback the target
                HurtEnemy(target);
            }
        }
    }

    void DodgeEnemy(DaggerfallEntity attacker, DaggerfallEntity target)
    {
        EnemyEntity enemy = target as EnemyEntity;

        if (enemy == null)
            return;

        //set knockback
        EnemyMotor enemyMotor = target.EntityBehaviour.GetComponent<EnemyMotor>();

        Vector3 directionRight = Quaternion.Euler(0, 90f, 0) * attacker.EntityBehaviour.transform.forward;
        Vector3 direction = (target.EntityBehaviour.transform.position - attacker.EntityBehaviour.transform.position).normalized;
        direction = new Vector3(direction.x, 0, direction.z);

        float dot = Vector3.Dot(directionRight.normalized, direction.normalized);

        //check if attacker is player
        if (attacker == GameManager.Instance.PlayerEntity)
        {
            WeaponStates weaponState = GameManager.Instance.WeaponManager.ScreenWeapon.WeaponState;

            if (weaponState == WeaponStates.StrikeDown || weaponState == WeaponStates.StrikeUp)
            {
                //if attack is a chop or thrust
                direction = Quaternion.Euler(0, Mathf.Sign(dot) * 90f, 0) * direction;
            }
            else if (weaponState == WeaponStates.StrikeDownLeft || weaponState == WeaponStates.StrikeDownRight)
            {
                //if attack is a diagonal
                direction = Quaternion.Euler(0, Mathf.Sign(dot) * 67.5f, 0) * direction;
            }
            else
            {
                //if attack is a horizontal
                direction = Quaternion.Euler(0, Mathf.Sign(dot) * 45f, 0) * direction;
            }
        }
        else
        {
            direction = Quaternion.Euler(0, Mathf.Sign(dot) * 67.5f, 0) * direction;
        }

        if (enemy.MobileEnemy.Behaviour != MobileBehaviour.Flying && enemy.MobileEnemy.Behaviour != MobileBehaviour.Aquatic && enemy.MobileEnemy.Behaviour != MobileBehaviour.Spectral)
            direction = (Vector3.down + direction.normalized).normalized;

        StartCoroutine(DodgeEnemyCoroutine(target,direction));
    }

    void PauseEnemy(DaggerfallEntity attacker, DaggerfallEntity target)
    {
        StartCoroutine(PauseEnemyCoroutine(target, 1 * feedbackPauseScale));
    }

    IEnumerator DodgeEnemyCoroutine(DaggerfallEntity target, Vector3 direction, float time = 0.1f)
    {
        EnemyMotor enemyMotor = target.EntityBehaviour.GetComponent<EnemyMotor>();
        CharacterController controller = enemyMotor.GetComponent<CharacterController>();

        float currentTime = 0;
        while (currentTime < time)
        {
            controller.SimpleMove(direction.normalized * 3 * feedbackDodgeScale);

            currentTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        enemyMotor.KnockbackDirection = Vector3.zero;
        enemyMotor.KnockbackSpeed = 0;
    }

    IEnumerator PauseEnemyCoroutine(DaggerfallEntity target, float time = 1)
    {
        MobileUnit mobile = target.EntityBehaviour.GetComponentInChildren<MobileUnit>();

        if (mobile.EnemyState == MobileStates.Move || mobile.EnemyState == MobileStates.Idle)
        {
            EnemySenses senses = target.EntityBehaviour.GetComponent<EnemySenses>();

            Vector3 pos = target.EntityBehaviour.transform.position;

            float currentTime = 0;
            while (currentTime < time)
            {
                if (mobile.IsPlayingOneShot())
                    break;

                yield return new WaitForEndOfFrame();

                if (mobile.EnemyState == MobileStates.Move)
                {
                    mobile.ChangeEnemyState(MobileStates.Idle);
                    target.EntityBehaviour.transform.position = pos;
                }

                currentTime += Time.deltaTime;
            }
        }
        else
            yield return new WaitForEndOfFrame();
    }

    void HurtEnemy(DaggerfallEntity target)
    {
        EnemyEntity enemy = target as EnemyEntity;

        if (enemy == null)
            return;

        //set knockback
        EnemyMotor enemyMotor = target.EntityBehaviour.GetComponent<EnemyMotor>();

        Vector3 direction = -target.EntityBehaviour.transform.forward;
        direction = new Vector3(direction.x, 0, direction.z);

        float sign = UnityEngine.Random.Range(0, 2) * 2 - 1;

        enemyMotor.KnockbackSpeed = 125f * feedbackHurtScale;
        enemyMotor.KnockbackDirection = direction.normalized * 0.1f;
    }

    IEnumerator DoCleaveOnNextFrame()
    {
        ScreenWeapon.PlaySwingSound();

        yield return null;

        DoCleave();
    }

    float GetWeaponReach()
    {
        float reach = 0f;

        if (ScreenWeapon.WeaponType == WeaponTypes.Melee)
        {
            reach = statMelee.x;

            //increase the reach by 50% if kicking
            if (ScreenWeapon.WeaponState == WeaponStates.StrikeLeft || ScreenWeapon.WeaponState == WeaponStates.StrikeDown || ScreenWeapon.WeaponState == WeaponStates.StrikeUp)
                reach *= 1.5f;

            return reach * reachMultiplier;
        }

        if (ScreenWeapon.WeaponType == WeaponTypes.Werecreature)
        {
            reach = statMelee.x * 1.5f;

            return reach * reachMultiplier;
        }

        switch (ScreenWeapon.SpecificWeapon.TemplateIndex)
        {
            case (int)Weapons.Broadsword:
                reach = statBroadsword.x; break;
            case (int)Weapons.Claymore:
                reach = statClaymore.x; break;
            case (int)Weapons.Dai_Katana:
                reach = statDaikatana.x; break;
            case (int)Weapons.Katana:
                reach = statKatana.x; break;
            case (int)Weapons.Longsword:
                reach = statLongsword.x; break;
            case (int)Weapons.Saber:
                reach = statSaber.x; break;
            case (int)Weapons.Dagger:
                reach = statDagger.x; break;
            case (int)Weapons.Shortsword:
                reach = statShortsword.x; break;
            case (int)Weapons.Tanto:
                reach = statTanto.x; break;
            case (int)Weapons.Wakazashi:
                reach = statWakazashi.x; break;
            case (int)Weapons.Battle_Axe:
                reach = statBattleAxe.x; break;
            case (int)Weapons.War_Axe:
                reach = statWarAxe.x; break;
            case (int)Weapons.Flail:
                reach = statFlail.x; break;
            case (int)Weapons.Mace:
                reach = statMace.x; break;
            case (int)Weapons.Staff:
                reach = statStaff.x; break;
            case (int)Weapons.Warhammer:
                reach = statWarhammer.x; break;
            case 513:   //Archer's Axe from Roleplay & Realism - Items
                reach = statArchersAxe.x; break;
            case 514:   //Light Flail from Roleplay & Realism - Items
                reach = statLightFlail.x; break;
        }

        return reach * reachMultiplier;
    }

    int GetWeaponCleave()
    {
        int cleave = 200;

        if (ScreenWeapon.WeaponType == WeaponTypes.Melee)
        {
            cleave = (int)statMelee.y;

            //increase the cleave by 100% if kicking
            if (ScreenWeapon.WeaponState == WeaponStates.StrikeLeft || ScreenWeapon.WeaponState == WeaponStates.StrikeDown || ScreenWeapon.WeaponState == WeaponStates.StrikeUp)
                cleave = Mathf.RoundToInt(cleave * 2f);

            return Mathf.RoundToInt(cleave * cleaveValueMultiplier);
        }

        if (ScreenWeapon.WeaponType == WeaponTypes.Werecreature)
        {
            cleave = (int)statMelee.y * 2;

            return Mathf.RoundToInt(cleave * cleaveValueMultiplier);
        }

        switch (ScreenWeapon.SpecificWeapon.TemplateIndex)
        {
            case (int)Weapons.Broadsword:
                cleave = (int)statBroadsword.y; break;
            case (int)Weapons.Claymore:
                cleave = (int)statClaymore.y; break;
            case (int)Weapons.Dai_Katana:
                cleave = (int)statDaikatana.y; break;
            case (int)Weapons.Katana:
                cleave = (int)statKatana.y; break;
            case (int)Weapons.Longsword:
                cleave = (int)statLongsword.y; break;
            case (int)Weapons.Saber:
                cleave = (int)statSaber.y; break;
            case (int)Weapons.Dagger:
                cleave = (int)statDagger.y; break;
            case (int)Weapons.Shortsword:
                cleave = (int)statShortsword.y; break;
            case (int)Weapons.Tanto:
                cleave = (int)statTanto.y; break;
            case (int)Weapons.Wakazashi:
                cleave = (int)statWakazashi.y; break;
            case (int)Weapons.Battle_Axe:
                cleave = (int)statBattleAxe.y; break;
            case (int)Weapons.War_Axe:
                cleave = (int)statWarAxe.y; break;
            case (int)Weapons.Flail:
                cleave = (int)statFlail.y; break;
            case (int)Weapons.Mace:
                cleave = (int)statMace.y; break;
            case (int)Weapons.Staff:
                cleave = (int)statStaff.y; break;
            case (int)Weapons.Warhammer:
                cleave = (int)statWarhammer.y; break;
            case 513:   //Archer's Axe from Roleplay & Realism - Items
                cleave = (int)statArchersAxe.y; break;
            case 514:   //Light Flail from Roleplay & Realism - Items
                cleave = (int)statLightFlail.y; break;
        }

        return Mathf.RoundToInt(cleave * cleaveValueMultiplier);
    }

    void DoCleave()
    {
        WeaponStates swingDirection = GameManager.Instance.WeaponManager.ScreenWeapon.WeaponState;

        //create variables
        Transform body = GameManager.Instance.PlayerObject.transform;
        Transform eye = playerCamera.transform;

        //weapon determines base cleave
        int weaponCleave = GetWeaponCleave();

        //subtract weight of targeted enemy if any from the starting cleave
        int startWeight = 0;
        if (attackTarget != null && attackDamage > 0)
        {
            EnemyEntity targetEnemy = attackTarget as EnemyEntity;
            startWeight = Mathf.RoundToInt(targetEnemy.GetWeightInClassicUnits() * cleaveWeightMultiplier) ;
        }

        //scale base cleave with player strength
        int cleaveValue = Mathf.RoundToInt(weaponCleave * (playerEntity.Stats.LiveStrength/100f)) - startWeight;
        //Debug.Log("Beginning cleave with a cleave value of " + cleaveValue.ToString() + " with first enemy weighing " + startWeight.ToString());

        //weapon determines reach
        float weaponReach = GetWeaponReach();

        //construct the bounds
        float x = weaponReach;
        float y = 0.25f;
        float z = weaponReach * 0.5f;

        //if thrusting, increase the weapon reach but halve the cleave value
        if (swingDirection == WeaponStates.StrikeUp)
        {
            cleaveValue = Mathf.RoundToInt(cleaveValue * 0.5f);
            x = 0.25f;
            y = 0.25f;
            z = weaponReach * 0.625f;
        }

        Vector3 scale = new Vector3(x, y, z);
        Vector3 pos = GetEyePos + (eye.forward * ((z * 0.5f) + playerController.radius));
        Quaternion rot = eye.rotation;

        int angle = 0;
        if (swingDirection == WeaponStates.StrikeDown)
            angle = 90;
        else if (swingDirection == WeaponStates.StrikeDownLeft)
            angle = 45;
        else if (swingDirection == WeaponStates.StrikeDownRight)
            angle = -45;

        //rotate the box
        if (angle != 0)
            rot = eye.rotation * Quaternion.AngleAxis(angle,Vector3.forward);

        List<Transform> transforms = new List<Transform>();

        //overlap box using bounds + player position + camera rotation
        DrawBox(pos,rot,scale,Color.red, 3);
        Collider[] colliders = Physics.OverlapBox(pos, scale, rot, layerMask);

        //get entities inside box, exclude original target, if any
        if (colliders.Length > 0)
        {
            Vector3 eyeOrigin = GetEyePos + (eye.forward * playerController.radius);
            foreach (Collider collider in colliders)
            {

                DaggerfallEntityBehaviour behaviour = collider.GetComponent<DaggerfallEntityBehaviour>();
                EnemyMotor motor = collider.GetComponent<EnemyMotor>();
                MobilePersonNPC mobileNpc = collider.GetComponent<MobilePersonNPC>();
                if ((behaviour != null && behaviour.Entity != attackTarget) || mobileNpc != null)
                {
                    if (safeguard)
                    {
                        if ((behaviour != null && behaviour.Entity.Team == MobileTeams.PlayerAlly) || (motor != null && !motor.IsHostile) || mobileNpc != null)
                        {
                            float angleDistance = Vector3.Angle(eye.forward, collider.transform.position - eye.transform.position);
                            if (angleDistance > GameManager.Instance.MainCamera.fieldOfView * 0.25f)
                                continue;
                        }
                    }

                    //add a radial check
                    if (reachRadial && Vector3.Distance(eyeOrigin, collider.ClosestPoint(eyeOrigin)) > z)
                        continue;

                    transforms.Add(collider.transform);
                }
            }
        }

        // Fire ray along player facing using weapon range
        RaycastHit hit;
        Ray ray = new Ray(GetEyePos, eye.forward);
        if (Physics.SphereCast(ray, 0.25f, out hit, weaponReach, layerMask))
        {
            if (weaponManager.WeaponEnvDamage(ScreenWeapon.SpecificWeapon, hit))
            {

            }
        }

        if (transforms.Count < 1)
        {
            return;
        }

        Vector3 origin = GetEyePos;
        if (swingDirection == WeaponStates.StrikeDown)
            origin += (eye.forward * playerController.radius) + (eye.up * (x * 0.5f));
        if (swingDirection == WeaponStates.StrikeLeft)
            origin += (eye.forward * playerController.radius) + (eye.right * (x * 0.5f));
        if (swingDirection == WeaponStates.StrikeDownLeft)
            origin += (eye.forward * playerController.radius) + ((eye.right + eye.up).normalized * (x * 0.5f));
        if (swingDirection == WeaponStates.StrikeRight)
            origin += (eye.forward * playerController.radius) - (eye.right * (x * 0.5f));
        if (swingDirection == WeaponStates.StrikeDownRight)
            origin += (eye.forward * playerController.radius) + ((-eye.right + eye.up).normalized * (x * 0.5f));

        //Debug.DrawLine(GetEyePos + (eye.forward * playerController.radius), origin, Color.green, 3);

        //sort entities by their distance to the swing origin
        transforms = SortByDistanceToPoint(transforms, origin);

        if (cleaving != null)
            StopCoroutine(cleaving);
        cleaving = ApplyCleaveInterval(transforms, cleaveValue, z, GameManager.classicUpdateInterval * cleaveInterval);
        StartCoroutine(cleaving);

        attackTarget = null;
        attackDamage = 0;
    }

    IEnumerator ApplyCleaveInterval(List<Transform> targets, int cleaveValue, float reach, float interval = GameManager.classicUpdateInterval)
    {
        int cleaves = 0;
        int hits = 0;

        foreach (Transform target in targets)
        {
            //if cleave is out, stop cleaving
            if (cleaveValue < 1)
            {
                Debug.Log("Cleave depleted, aborting cleave!");
                break;
            }

            MobilePersonNPC mobileNpc = target.GetComponent<MobilePersonNPC>();
            if (mobileNpc == null)
            {
                //check if target is still within reach
                Vector3 eyeOrigin = GetEyePos + (playerCamera.transform.forward * playerController.radius);
                if (Vector2.Distance(eyeOrigin, target.GetComponent<Collider>().ClosestPoint(eyeOrigin)) > reach)
                    continue;

                if (cleaveRequireLOS)
                {
                    //check if LOS is obstructed
                    Vector3 rayPos = GetEyePos;
                    Vector3 rayDir = target.position - rayPos;
                    Ray ray = new Ray(rayPos, rayDir);
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(ray, out hit, 10, layerMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.transform != target)
                        {
                            //Target is obstructed by something
                            continue;
                        }
                        else
                        {
                            cleaves++;
                            DaggerfallEntityBehaviour behaviour = target.GetComponent<DaggerfallEntityBehaviour>();
                            if (weaponManager.WeaponDamage(ScreenWeapon.SpecificWeapon, false, false, hit.transform, hit.point, ray.direction))
                            {
                                hits++;
                                if (behaviour != null)
                                {
                                    EnemyEntity enemy = behaviour.Entity as EnemyEntity;
                                    if (enemy != null)
                                    {
                                        int weight = Mathf.RoundToInt(enemy.GetWeightInClassicUnits() * cleaveWeightMultiplier);
                                        cleaveValue -= weight;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    cleaves++;
                    DaggerfallEntityBehaviour behaviour = target.GetComponent<DaggerfallEntityBehaviour>();
                    if (weaponManager.WeaponDamage(ScreenWeapon.SpecificWeapon, false, false, target, target.position, target.position - playerController.transform.position))
                    {
                        hits++;
                        if (behaviour != null)
                        {
                            EnemyEntity enemy = behaviour.Entity as EnemyEntity;
                            if (enemy != null)
                            {
                                int weight = Mathf.RoundToInt(enemy.GetWeightInClassicUnits() * cleaveWeightMultiplier);
                                cleaveValue -= weight;
                            }
                        }
                    }
                }
            }
            else
            {
                //Don't LOS check MobileNPCs
                weaponManager.WeaponDamage(ScreenWeapon.SpecificWeapon, false, false, target, target.position, target.position - playerController.transform.position);
                SoundClips soundClip = DaggerfallEntity.GetRaceGenderPainSound(mobileNpc.Race, mobileNpc.Gender, false);
                GameManager.Instance.PlayerObject.GetComponent<DaggerfallAudioSource>().PlayClipAtPoint(soundClip, target.position, 1);
            }

            yield return new WaitForSeconds(interval);
        }

        // Tally skills if attack targeted at least one enemy
        if (cleaves > 0)
        {
            if (ScreenWeapon.WeaponType == WeaponTypes.Melee || ScreenWeapon.WeaponType == WeaponTypes.Werecreature)
                playerEntity.TallySkill(DFCareer.Skills.HandToHand, 1);
            else
                playerEntity.TallySkill(ScreenWeapon.SpecificWeapon.GetWeaponSkillID(), 1);

            playerEntity.TallySkill(DFCareer.Skills.CriticalStrike, 1);
        }

        cleaving = null;
    }

    public void AbortCleave()
    {
        if (cleaving != null)
        {
            StopCoroutine(cleaving);

            cleaving = null;

            if (ScreenWeapon.WeaponType == WeaponTypes.Melee || ScreenWeapon.WeaponType == WeaponTypes.Werecreature)
                playerEntity.TallySkill(DFCareer.Skills.HandToHand, 1);
            else
                playerEntity.TallySkill(ScreenWeapon.SpecificWeapon.GetWeaponSkillID(), 1);

            playerEntity.TallySkill(DFCareer.Skills.CriticalStrike, 1);
        }
    }

    List<Transform> SortByDistanceToPoint(List<Transform> transforms, Vector3 origin)
    {
        transforms.Sort(delegate (Transform a, Transform b)
        {
            return (origin - a.position).sqrMagnitude.CompareTo((origin - b.position).sqrMagnitude);
        }
        );

        return transforms;
    }

    // https://forum.unity.com/threads/debug-drawbox-function-is-direly-needed.1038499/
    public void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c, float duration)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);

        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

        Debug.DrawLine(point1, point2, c, duration);
        Debug.DrawLine(point2, point3, c, duration);
        Debug.DrawLine(point3, point4, c, duration);
        Debug.DrawLine(point4, point1, c, duration);

        Debug.DrawLine(point5, point6, c, duration);
        Debug.DrawLine(point6, point7, c, duration);
        Debug.DrawLine(point7, point8, c, duration);
        Debug.DrawLine(point8, point5, c, duration);

        Debug.DrawLine(point1, point5, c, duration);
        Debug.DrawLine(point2, point6, c, duration);
        Debug.DrawLine(point3, point7, c, duration);
        Debug.DrawLine(point4, point8, c, duration);

        /*// optional axis display
        Debug.DrawRay(m.GetPosition(), m.GetForward(), Color.magenta);
        Debug.DrawRay(m.GetPosition(), m.GetUp(), Color.yellow);
        Debug.DrawRay(m.GetPosition(), m.GetRight(), Color.red);*/
    }

    WeaponManager.MouseDirections TrackMouseAttack()
    {
        // Track action for idle plus all eight mouse directions
        var sum = _gesture.Add(InputManager.Instance.MouseX, InputManager.Instance.MouseY);

        if (InputManager.Instance.UsingController)
        {
            float x = InputManager.Instance.MouseX;
            float y = InputManager.Instance.MouseY;

            bool inResetJoystickSwingRadius = (x >= -resetJoystickSwingRadius && x <= resetJoystickSwingRadius && y >= -resetJoystickSwingRadius && y <= resetJoystickSwingRadius);

            if (joystickSwungOnce || inResetJoystickSwingRadius)
            {
                if (inResetJoystickSwingRadius)
                    joystickSwungOnce = false;

                return WeaponManager.MouseDirections.None;
            }
        }
        else if (_gesture.TravelDist / _longestDim < weaponManager.AttackThreshold)
        {
            return WeaponManager.MouseDirections.None;
        }

        joystickSwungOnce = true;

        // Treat mouse movement as a vector from the origin
        // The angle of the vector will be used to determine the angle of attack/swing
        var angle = Mathf.Atan2(sum.y, sum.x) * Mathf.Rad2Deg;
        // Put angle into 0 - 360 deg range
        if (angle < 0f) angle += 360f;
        // The swing gestures are divided into radial segments
        // Up-down and left-right attacks are in a 30 deg cone about the x/y axes
        // Up-right and up-left aren't valid so the up range is expanded to fill the range
        // The remaining 60 deg quadrants trigger the diagonal attacks
        var radialSection = Mathf.CeilToInt(angle / 15f);
        WeaponManager.MouseDirections direction;
        switch (radialSection)
        {
            case 0: // 0 - 15 deg
            case 1:
            case 24: // 345 - 365 deg
                direction = WeaponManager.MouseDirections.Right;
                break;
            case 2: // 15 - 75 deg
            case 3:
            case 4:
            case 5:
            case 6: // 75 - 105 deg
            case 7:
            case 8: // 105 - 165 deg
            case 9:
            case 10:
            case 11:
                direction = WeaponManager.MouseDirections.Up;
                break;
            case 12: // 165 - 195 deg
            case 13:
                direction = WeaponManager.MouseDirections.Left;
                break;
            case 14: // 195 - 255 deg
            case 15:
            case 16:
            case 17:
                direction = WeaponManager.MouseDirections.DownLeft;
                break;
            case 18: // 255 - 285 deg
            case 19:
                direction = WeaponManager.MouseDirections.Down;
                break;
            case 20: // 285 - 345 deg
            case 21:
            case 22:
            case 23:
                direction = WeaponManager.MouseDirections.DownRight;
                break;
            default: // Won't happen
                direction = WeaponManager.MouseDirections.None;
                break;
        }
        _gesture.Clear();
        return direction;
    }

    private KeyCode SetKeyFromText(string text)
    {
        Debug.Log("Setting Key");

        foreach (KeyCode keyCode in InputManager.Instance.KeyCodeList)
        {
            if (keyCode.ToString() == text)
                return keyCode;
        }

        Debug.Log("Detected an invalid key code. Setting to default.");
        return KeyCode.None;
    }

    public static void OnStartMenu(object sender, EventArgs e)
    {
        Instance.ModCompatibilityChecking();
    }

    public static void OnNewGame()
    {
        Instance.CheckKeyCodeConflict();
    }

    public static void OnLoad(SaveData_v1 saveData)
    {
        Instance.CheckKeyCodeConflict();
    }

    public void CheckKeyCodeConflict()
    {
        if (conflict != null)
            return;

        if (InputManager.Instance.GetBinding(InputManager.Actions.SwingWeapon) == attackKeyCode)
        {
            conflict = MessageKeyCodeConflict();
            StartCoroutine(conflict);
        }
    }

    IEnumerator MessageKeyCodeConflict()
    {
        yield return new WaitForSeconds(1);

        string[] strings = new string[2];
        strings[0] = "KeyCode conflict detected";
        strings[1] = "Please rebind your SwingWeapon key in the CONTROLS settings.";
        TextFile.Token[] texts = DaggerfallUnity.Instance.TextProvider.CreateTokens(TextFile.Formatting.JustifyCenter, strings);

        DaggerfallUI.MessageBox(texts);

        yield return new WaitForSeconds(1);

        conflict = null;
    }

    public static int AdjustWeaponHitChanceMod(DaggerfallEntity attacker, DaggerfallEntity target, int hitChanceMod, int weaponAnimTime, DaggerfallUnityItem weapon)
    {

        //if Power Attacks module is enabled, scale to-hit bonus to Last Power
        if (attacker == GameManager.Instance.PlayerEntity)
        {
            if (Instance.powerMessage)
                Debug.Log("TOME OF BATTLE - ACCURACY SCALING - Base accuracy is " + hitChanceMod.ToString() + "%!");

            if (Instance.archery > 0)
            {
                //if weapon is bow, add TOB accuracy modifier if any
                if (DaggerfallUnity.Instance.ItemHelper.ConvertItemToAPIWeaponType(weapon) == WeaponTypes.Bow)
                    hitChanceMod += Instance.archeryBonusAccuracy;
            }


            if (Instance.powerAttacks)
            {
                if (Instance.powerAccuracy == 2 || (Instance.powerAccuracy == 1 && DaggerfallUnity.Instance.ItemHelper.ConvertItemToAPIWeaponType(weapon) == WeaponTypes.Bow))
                    hitChanceMod = Mathf.CeilToInt((float)hitChanceMod * Instance.LastPower);
            }

            if (Instance.powerMessage)
                Debug.Log("TOME OF BATTLE - ACCURACY SCALING - Adjusted accuracy is " + hitChanceMod.ToString() + "%!");
        }

        return hitChanceMod;
    }

    public static int AdjustWeaponAttackDamage(DaggerfallEntity attacker, DaggerfallEntity target, int damage, int weaponAnimTime, DaggerfallUnityItem weapon)
    {

        if (attacker == GameManager.Instance.PlayerEntity)
        {
            if (Instance.powerMessage)
                Debug.Log("TOME OF BATTLE - DAMAGE SCALING - Base damage is " + damage.ToString() + "!");

            if (Instance.archery > 0)
            {
                //if weapon is bow, add TOB damage modifier if any
                if (DaggerfallUnity.Instance.ItemHelper.ConvertItemToAPIWeaponType(weapon) == WeaponTypes.Bow)
                    damage += Instance.archeryBonusDamage;
            } 

            //if Power Attacks module is enabled
            if (Instance.powerAttacks)
            {
                //If Damage is set to Random Range With Scaling, scale total damage to Last Power
                if (Instance.powerDamage == 1)
                    damage = Mathf.CeilToInt((float)damage * Instance.LastPower);
            }

            if (damage < 0)
                damage = 0;

            if (Instance.powerMessage)
                Debug.Log("TOME OF BATTLE - DAMAGE SCALING - Adjusted damage is " + damage.ToString() + "!");
        }

        return damage;
    }

    int CalculateWeaponAttackDamage(DaggerfallEntity attacker, DaggerfallEntity target, int damageModifier, int weaponAnimTime, DaggerfallUnityItem weapon)
    {
        int damage = UnityEngine.Random.Range(weapon.GetBaseDamageMin(), weapon.GetBaseDamageMax() + 1) + damageModifier;

        //Power Attack
        if (Instance.powerAttacks && Instance.powerDamage == 2 && attacker == GameManager.Instance.PlayerEntity)
        {
            //If attacker is player
            //Lerp between Min and Max damage using Last Power
            damage = Mathf.FloorToInt(Mathf.Lerp(weapon.GetBaseDamageMin(), weapon.GetBaseDamageMax(), Instance.LastPower));
            if (Instance.powerMessage)
                DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("Your base damage was " + damage.ToString() + " at " + (Instance.LastPower * 100).ToString("0") + "% Power!");
            damage += damageModifier;
        }


        if (target != GameManager.Instance.PlayerEntity)
        {
            if ((target as EnemyEntity).CareerIndex == (int)MonsterCareers.SkeletalWarrior)
            {
                // Apply edged-weapon damage modifier for Skeletal Warrior
                if ((weapon.flags & 0x10) == 0)
                    damage /= 2;

                // Apply silver weapon damage modifier for Skeletal Warrior
                // Arena applies a silver weapon damage bonus for undead enemies, which is probably where this comes from.
                if (weapon.NativeMaterialValue == (int)WeaponMaterialTypes.Silver)
                    damage *= 2;
            }
        }
        // TODO: Apply strength bonus from Mace of Molag Bal

        // Apply strength modifier
        damage += FormulaHelper.DamageModifier(attacker.Stats.LiveStrength);

        // Apply material modifier.
        // The in-game display in Daggerfall of weapon damages with material modifiers is incorrect. The material modifier is half of what the display suggests.
        damage += weapon.GetWeaponMaterialModifier();
        if (damage < 1)
            damage = 0;

        damage += FormulaHelper.GetBonusOrPenaltyByEnemyType(attacker, target);

        // Mod hook for adjusting final weapon damage. (no-op in DFU)
        damage = AdjustWeaponAttackDamage(attacker, target, damage, weaponAnimTime, weapon);

        return damage;
    }

    int GetPickedCombatSkills()
    {
        int count = 0;

        List<DFCareer.Skills> skills = GameManager.Instance.PlayerEntity.GetPrimarySkills();
        foreach (DFCareer.Skills skill in skills)
        {
            if (skill == DFCareer.Skills.Archery ||
                skill == DFCareer.Skills.Axe ||
                skill == DFCareer.Skills.BluntWeapon ||
                skill == DFCareer.Skills.LongBlade ||
                skill == DFCareer.Skills.ShortBlade
                )
                count++;
        }

        skills = GameManager.Instance.PlayerEntity.GetMajorSkills();
        foreach (DFCareer.Skills skill in skills)
        {
            if (skill == DFCareer.Skills.Archery ||
                skill == DFCareer.Skills.Axe ||
                skill == DFCareer.Skills.BluntWeapon ||
                skill == DFCareer.Skills.LongBlade ||
                skill == DFCareer.Skills.ShortBlade
                )
                count++;
        }

        skills = GameManager.Instance.PlayerEntity.GetMinorSkills();
        foreach (DFCareer.Skills skill in skills)
        {
            if (skill == DFCareer.Skills.Archery ||
                skill == DFCareer.Skills.Axe ||
                skill == DFCareer.Skills.BluntWeapon ||
                skill == DFCareer.Skills.LongBlade ||
                skill == DFCareer.Skills.ShortBlade
                )
                count++;
        }

        return count;
    }

    int GetSphincterVisionMinDamageMod(DaggerfallUnityItem weapon)
    {
        int minDamageMod = -weapon.GetBaseDamageMax() + weapon.GetBaseDamageMin();

        if (weapon != null)
        {
            if (GameManager.Instance.RightHandWeapon.WeaponState == WeaponStates.StrikeDownLeft)
                minDamageMod += 2;

            if (GameManager.Instance.RightHandWeapon.WeaponState == WeaponStates.StrikeDown)
                minDamageMod += 4;
        }

        return minDamageMod;
    }

    int GetSphincterVisionMaxDamageMod(DaggerfallUnityItem weapon)
    {
        //Prowess is (combat skills (6) / 150) for max damage bonus.
        int maxDmgMod = (GetPickedCombatSkills() * 6) / 150;

        if (weapon != null)
        {
            DFCareer.Skills weaponSkill = weapon.GetWeaponSkillID();

            //Bows, long blades, and short blades gain accuracy and damage from Agility - up to +10 to hit, and +5 to max damage.
            if (weaponSkill == DFCareer.Skills.Archery || weaponSkill == DFCareer.Skills.LongBlade || weaponSkill == DFCareer.Skills.ShortBlade)
                maxDmgMod += playerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Agility) / 20;

            //Axes gain a large max damage bonus from strength, up to +10 max damage.
            if (weaponSkill == DFCareer.Skills.Axe)
                maxDmgMod += playerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Strength) / 10;

            //Blunt weapons gain up to a +5 max damage bonus from strength.
            if (weaponSkill == DFCareer.Skills.BluntWeapon)
                maxDmgMod += playerEntity.Stats.GetLiveStatValue(DFCareer.Stats.Strength) / 20;

            if (GameManager.Instance.RightHandWeapon.WeaponState == WeaponStates.StrikeUp)
                maxDmgMod -= 4;

            if (GameManager.Instance.RightHandWeapon.WeaponState == WeaponStates.StrikeDownRight)
                maxDmgMod -= 2;

            if (GameManager.Instance.RightHandWeapon.WeaponState == WeaponStates.StrikeDownLeft)
                maxDmgMod += 2;

            if (GameManager.Instance.RightHandWeapon.WeaponState == WeaponStates.StrikeDown)
                maxDmgMod += 4;
        }

        return maxDmgMod;
    }

    public static int CalculateWeaponAttackDamage_SphincterVision(DaggerfallEntity attacker, DaggerfallEntity target, int damageModifier, int weaponAnimTime, DaggerfallUnityItem weapon)
    {
        //derived variables that don't exist in the original formula
        //I don't know what the values are supposed to be at this point
        //these are just educated guesses taken from the mod description
        int maxDmgMod = Instance.GetSphincterVisionMaxDamageMod(weapon);
        int minDmgMod = Instance.GetSphincterVisionMinDamageMod(weapon);

        int damage = 0;
        int minDmg = 0;
        int maxDmg = 0;

        maxDmgMod += FormulaHelper.DamageModifier(attacker.Stats.LiveStrength);
        if (ItemEquipTable.GetItemHands(weapon) != ItemHands.Both)
        {
            maxDmgMod -= FormulaHelper.DamageModifier(attacker.Stats.LiveStrength) / 2;
        }

        minDmg = (weapon.GetBaseDamageMax() + minDmgMod); //mainly for maxing out a critical hit
        if (minDmg < 0) //min dmg is zero
        {
            minDmg = 0;
        }

        maxDmg = (weapon.GetBaseDamageMax() + maxDmgMod);

        if (minDmg > maxDmg)
        {
            minDmg = maxDmg;
        }

        damage = UnityEngine.Random.Range(minDmg, maxDmg + 1);

        if (attacker == GameManager.Instance.PlayerEntity)
        {
            if (Instance.powerAttacks && Instance.powerDamage == 2)
            {
                //If attacker is player
                //Lerp between Min and Max damage using Last Power
                damage = Mathf.FloorToInt(Mathf.Lerp(minDmg, maxDmg, Instance.LastPower));
                if (Instance.powerMessage)
                    DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("Your base damage was " + damage.ToString() + " at " + (Instance.LastPower * 100).ToString("0") + "% Power!");
            }
        }

        damage += damageModifier;

        if ((target != GameManager.Instance.PlayerEntity) && ((target as EnemyEntity).EntityType != EntityTypes.EnemyClass)) //need this or npcs with same enum are affected
        {
            if ((target as EnemyEntity).CareerIndex == (int)MonsterCareers.SkeletalWarrior)
            {
                // Apply edged-weapon damage modifier for Skeletal Warrior
                if (weapon.GetWeaponSkillIDAsShort() != 32) //blunt
                {
                    damage /= 2;
                    if (attacker == GameManager.Instance.PlayerEntity)
                    {
                        DaggerfallUI.Instance.PopupMessage("The weapon you are using is less effective against the Skeletal Warrior.");
                    }
                }
            }
            if ((target as EnemyEntity).GetEnemyGroup() == DFCareer.EnemyGroups.Undead)
            {
                // Apply silver weapon damage modifier for Undead
                // Arena applies a silver weapon damage bonus for undead enemies, which is probably where this comes from.
                if (weapon.NativeMaterialValue == (int)WeaponMaterialTypes.Silver)
                {
                    damage *= 2;
                }
            }
            if ((target as EnemyEntity).CareerIndex == (int)MonsterCareers.Werewolf || (target as EnemyEntity).CareerIndex == (int)MonsterCareers.Wereboar)
            {
                // Apply silver weapon damage modifier for furries
                if (weapon.NativeMaterialValue == (int)WeaponMaterialTypes.Silver)
                {
                    damage *= 2;
                }
            }
            // Spriggans are mentioned to be impervious to most weapons in in-game lore.
            if ((target as EnemyEntity).CareerIndex == (int)MonsterCareers.Spriggan)
            {
                if (weapon.GetWeaponSkillIDAsShort() != 31) //axe
                {
                    damage /= 5;
                    if (attacker == GameManager.Instance.PlayerEntity)
                    {
                        DaggerfallUI.Instance.PopupMessage("The weapon you are using is less effective against the Spriggan.");
                    }
                }
            }
        }

        return damage;
    }

    public static int CalculateHandToHandAttackDamage(DaggerfallEntity attacker, DaggerfallEntity target, int damageModifier, bool player)
    {
        int minBaseDamage = FormulaHelper.CalculateHandToHandMinDamage(attacker.Skills.GetLiveSkillValue(DFCareer.Skills.HandToHand));
        int maxBaseDamage = FormulaHelper.CalculateHandToHandMaxDamage(attacker.Skills.GetLiveSkillValue(DFCareer.Skills.HandToHand));
        int damage = UnityEngine.Random.Range(minBaseDamage, maxBaseDamage + 1);

        if (Instance.powerAttacks && attacker == GameManager.Instance.PlayerEntity)
        {
            //If attacker is player
            //Lerp between Min and Max damage using Last Power
            damage = Mathf.FloorToInt(Mathf.Lerp(minBaseDamage, maxBaseDamage, Instance.LastPower));
            DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("You dealt " + damage.ToString() + " damage at " + (Instance.LastPower * 100).ToString("0") + "% Power!");
        }

        // Apply damage modifiers.
        damage += damageModifier;

        // Apply strength modifier for players. It is not applied in classic despite what the in-game description for the Strength attribute says.
        if (player)
            damage += FormulaHelper.DamageModifier(attacker.Stats.LiveStrength);

        damage += FormulaHelper.GetBonusOrPenaltyByEnemyType(attacker, target);

        return damage;
    }
    public static int CalculateHandToHandAttackDamage_SphincterVision(DaggerfallEntity attacker, DaggerfallEntity target, int damageModifier, bool player)
    {
        // if hand to hand is forbidden, you aren't hitting jack
        if (((int)attacker.Career.ForbiddenProficiencies & (int)DFCareer.ProficiencyFlags.HandToHand) != 0)
            return 0;

        int minBaseDamage = FormulaHelper.CalculateHandToHandMinDamage(attacker.Skills.GetLiveSkillValue(DFCareer.Skills.HandToHand));
        int maxBaseDamage = FormulaHelper.CalculateHandToHandMaxDamage(attacker.Skills.GetLiveSkillValue(DFCareer.Skills.HandToHand));
        // Apply strength modifier for players. It is not applied in classic despite what the in-game description for the Strength attribute says.
        if (player || ((attacker as EnemyEntity).EntityType == EntityTypes.EnemyClass))
        {
            maxBaseDamage += (FormulaHelper.DamageModifier(attacker.Stats.LiveStrength) / 2);
        }

        if (minBaseDamage < 0)
        {
            minBaseDamage = 0;
        }
        else if (minBaseDamage > maxBaseDamage)
        {
            minBaseDamage = maxBaseDamage;
        }

        int damage = UnityEngine.Random.Range(minBaseDamage, maxBaseDamage + 1);

        if (Instance.powerAttacks && attacker == GameManager.Instance.PlayerEntity)
        {
            //If attacker is player
            //Lerp between Min and Max damage using Last Power
            damage = Mathf.FloorToInt(Mathf.Lerp(minBaseDamage, maxBaseDamage, Instance.LastPower));
            DaggerfallUI.Instance.DaggerfallHUD.SetMidScreenText("You dealt " + damage.ToString() + " damage at " + (Instance.LastPower * 100).ToString("0") + "% Power!");
        }

        // Apply damage modifiers.
        damage += damageModifier;

        damage += FormulaHelper.GetBonusOrPenaltyByEnemyType(attacker, target);

        return damage;
    }
    public static FormulaHelper.ToHitAndDamageMods CalculateSwingModifiers(FPSWeapon onscreenWeapon)
    {
        FormulaHelper.ToHitAndDamageMods mods = new FormulaHelper.ToHitAndDamageMods();
        if (onscreenWeapon != null)
        {
            //Universal ruleset
            //Horizontal swings are easiest to hit with but may not hit any vulnerable parts
            if (onscreenWeapon.WeaponState == WeaponStates.StrikeRight || onscreenWeapon.WeaponState == WeaponStates.StrikeLeft)
            {
                mods.toHitMod = 10;
                mods.damageMod = -2;
            }
            //Diagonal swings are neutral
            if (onscreenWeapon.WeaponState == WeaponStates.StrikeDownRight || onscreenWeapon.WeaponState == WeaponStates.StrikeDownLeft)
            {
                mods.toHitMod = 0;
                mods.damageMod = 0;
            }
            //Chops are clumsy but are devastating when landed
            if (onscreenWeapon.WeaponState == WeaponStates.StrikeDown)
            {
                mods.toHitMod = -5;
                mods.damageMod = 4;
            }
            //Thrusts have extra reach but are tricky to land
            if (onscreenWeapon.WeaponState == WeaponStates.StrikeUp)
            {
                mods.toHitMod = -10;
                mods.damageMod = 2;
            }

            //Weapon-specific
            if (onscreenWeapon.WeaponType == WeaponTypes.Melee)
            {
                //Left, Down and Up are kicks
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.WeaponType == WeaponTypes.Werecreature)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Broadsword)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Claymore)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Dai_Katana)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Katana)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Longsword)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Saber)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Dagger)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = -1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = -1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = -1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Shortsword)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Tanto)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Wakazashi)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Battle_Axe)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 2; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 2; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 2; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -6; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.War_Axe)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Flail)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Mace)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Staff)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == (int)Weapons.Warhammer)
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 0; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = 0; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == 513)    //Archer's Axe
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 2; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 2; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 2; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -6; break;
                }
            }
            else if (onscreenWeapon.SpecificWeapon.TemplateIndex == 514)    //Light Flail
            {
                switch (onscreenWeapon.WeaponState)
                {
                    case WeaponStates.StrikeRight:
                    case WeaponStates.StrikeLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDownRight:
                    case WeaponStates.StrikeDownLeft:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeDown:
                        mods.damageMod = 1; break;
                    case WeaponStates.StrikeUp:
                        mods.damageMod = -3; break;
                }
            }
        }
        return mods;
    }

}
