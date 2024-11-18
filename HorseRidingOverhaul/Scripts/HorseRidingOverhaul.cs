using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

public class HorseRidingOverhaul : MonoBehaviour
{
    public static HorseRidingOverhaul Instance;

    bool wasRiding;

    Camera playerCamera;
    GameObject playerObject;
    PlayerMotor playerMotor;
    PlayerMouseLook playerMouseLook;
    TransportManager transportManager;

    HorseRidingWidget widget;

    TransportModes lastTransportMode;

    public float targetYaw;
    public float currentYaw;
    public float Yaw;
    public float targetThrottle;
    public float currentThrottle;
    public float lastThrottle;
    public Vector3 moveVector;
    public Vector3 lastMoveVector;
    public Vector3 strafeVector;

    public float angleYaw;
    public float anglePitch;

    public int speedIndex = 1;
    float[] speeds = new float[] { -0.25f, 0, 0.5f, 1.0f };

    //horse characteristics
    bool acceleration;
    float moveAcceleration = 1;
    float moveBraking = 1;
    float turnSpeed = 90;
    float turnAcceleration = 3;
    float turnBraking = 3;
    bool colliding;

    //horse sprite
    public float ScaleFactorX = 0.8f;
    public int nativeScreenWidth = 320;
    public int nativeScreenHeight = 200;
    public Rect screenRect;
    Rect horseRect;

    ImageData ridingTexture;
    ImageData[] ridingTexures = new ImageData[4];
    float lastFrameTime = 0;
    int frameIndex = 0;
    public Color Tint { get; set; } = Color.white;

    const string horseTextureName = "MRED00I0.CFA";
    const string cartTextureName = "MRED01I0.CFA";
    const float animFrameTimeBase = 0.125f;  // Time between animation frames in seconds.
    float animFrameTime;
    string lastTextureName;
    bool mirror;

    //horse sounds
    GameObject audioObject;
    const SoundClips horseSound = SoundClips.AnimalHorse;
    const SoundClips horseRidingSound1 = SoundClips.HorseClop;
    const SoundClips horseRidingSound2 = SoundClips.HorseClop2;
    const SoundClips cartRidingSound = SoundClips.HorseAndCart;
    DaggerfallAudioSource dfAudioSource;
    AudioSource ridingAudioSource;
    AudioClip neighClip;
    float neighTime = 0;

    public bool customAudio;
    float customAudioVolume;
    IEnumerator audioRiding;
    AudioClip[] audioHorseStepWalk;
    AudioClip[] audioHorseStepSprint;
    AudioClip[] audioHorseNoise;
    AudioClip[] audioHorseMount;
    AudioClip[] audioHorseDismount;
    AudioClip[] audioHorseCollision;
    public bool[] gait1 = new bool[] { false, false, false, false, true };
    public bool[] gait2 = new bool[] { false, false, false, true };
    public bool[] gait3 = new bool[] { false, false, true };
    public bool[] gait4 = new bool[] { false, false, false, true, true, true };
    public bool[] gait5 = new bool[] { false, false, true, true, true, true };
    public bool[] currentGait;
    bool sprint;

    float lastCollision;

    public int steering;   //0 = default, 1 = mouse steering
    public int throttle;    //0 = default, 1 = hold
    public int view;       //0 = free, 1 = locked
    KeyCode centerViewKeyCode = KeyCode.X;
    KeyCode centerHorseKeyCode = KeyCode.C;
    KeyCode steeringKeyCode = KeyCode.Tab;
    KeyCode throttleKeyCode = KeyCode.KeypadEnter;
    KeyCode viewKeyCode = KeyCode.KeypadPlus;

    bool galloping;

    bool slopeFollow;
    float targetSlopeAngle;
    float currentSlopeAngle;
    float slopeFollowSpeed;
    float slopeFollowStrength;

    bool inertia;
    float targetInertia;
    float currentInertia;
    float currentGravityOffset;
    float targetGravityOffset;
    float inertiaSpeed = 1;
    float inertiaStrength = 1;

    //EOTB compatibility
    bool isInThirdPerson;

    //Travel Options compatibility
    bool hasTravelOptions;
    public bool isTravelling;
    public bool wasTravelling;

    static Mod mod;
    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<HorseRidingOverhaul>();
    }
    public bool CanRunUnlessRidingCart()
    {
        return !((GameManager.Instance.TransportManager.TransportMode == TransportModes.Cart || !galloping) && playerMotor.IsRiding);
    }

    void Awake()
    {
        Instance = this;

        playerCamera = GameManager.Instance.MainCamera;
        playerObject = GameManager.Instance.PlayerObject;
        playerMouseLook = GameManager.Instance.PlayerMouseLook;
        playerMotor = GameManager.Instance.PlayerMotor;
        transportManager = GameManager.Instance.TransportManager;
        widget = gameObject.AddComponent<HorseRidingWidget>();

        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

        ModCompatibilityChecking();

        GameManager.Instance.SpeedChanger.CanRun = CanRunUnlessRidingCart;

        //do the sounds
        audioObject = new GameObject("Free Rein - Audio");
        audioObject.transform.SetParent(playerObject.transform, false);
        audioObject.transform.localPosition = Vector3.zero;
        audioObject.transform.localRotation = Quaternion.identity;

        dfAudioSource = playerObject.GetComponent<DaggerfallAudioSource>();

        // Use custom audio source as we don't want to affect other sounds while riding.
        ridingAudioSource = audioObject.AddComponent<AudioSource>();
        ridingAudioSource.hideFlags = HideFlags.HideInInspector;
        ridingAudioSource.playOnAwake = false;
        ridingAudioSource.loop = false;
        ridingAudioSource.dopplerLevel = 0f;
        ridingAudioSource.spatialBlend = 1.0f;
        ridingAudioSource.volume = 0.5f * DaggerfallUnity.Settings.SoundVolume;
        neighClip = dfAudioSource.GetAudioClip((int)horseSound);

        transportManager.RidingVolumeScale = 0;

        SaveLoadManager.OnLoad += OnLoad;

        audioHorseStepWalk = new AudioClip[]
        {
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_front_01"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_back_01"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_front_02"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_back_02"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_front_03"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_back_03"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_front_04"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_back_04"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_front_05"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_back_05"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_front_06"),
            mod.GetAsset<AudioClip>("npc_horse_foot_walk_back_06"),
        };

        audioHorseStepSprint = new AudioClip[]
        {
            mod.GetAsset<AudioClip>("fst_horse_sprint_front_01"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_back_01"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_front_02"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_back_02"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_front_03"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_back_03"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_front_04"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_back_04"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_front_05"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_back_05"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_front_06"),
            mod.GetAsset<AudioClip>("fst_horse_sprint_back_06"),
        };

        audioHorseNoise = new AudioClip[]
        {
            mod.GetAsset<AudioClip>("Horse Snort 1"),
            mod.GetAsset<AudioClip>("Horse Snort 2"),
            mod.GetAsset<AudioClip>("Horse Snort 3"),
            mod.GetAsset<AudioClip>("Horse Snort 4"),
            mod.GetAsset<AudioClip>("Horse Snort 5"),
            mod.GetAsset<AudioClip>("Horse Snort 6"),
            mod.GetAsset<AudioClip>("Horse Snort 7"),
            mod.GetAsset<AudioClip>("Horse Snort 8"),
            mod.GetAsset<AudioClip>("Horse Snort 9"),
            mod.GetAsset<AudioClip>("Horse Snort 10"),
        };

        audioHorseMount = new AudioClip[]
        {
            mod.GetAsset<AudioClip>("Horse Mount 1"),
            mod.GetAsset<AudioClip>("Horse Mount 2"),
        };

        audioHorseDismount = new AudioClip[]
        {
            mod.GetAsset<AudioClip>("Horse Dismount 1"),
            mod.GetAsset<AudioClip>("Horse Dismount 2"),
        };

        audioHorseCollision = new AudioClip[]
        {
            mod.GetAsset<AudioClip>("Horse Rearup 1"),
            mod.GetAsset<AudioClip>("Horse Rearup 2"),
            mod.GetAsset<AudioClip>("Horse Rearup 3"),
        };

        mod.IsReady = true;
    }

    private void ModCompatibilityChecking()
    {
        //listen to Eye Of The Beholder for changes in POV
        Mod eotb = ModManager.Instance.GetModFromGUID("2942ea8c-dbd4-42af-bdf9-8199d2f4a0aa");
        if (eotb != null)
        {
            //subscribe to EOTB's OnToggleOffset event
            ModManager.Instance.SendModMessage(eotb.Title, "onToggleOffset", (Action<bool>)(toggleState => {
                isInThirdPerson = toggleState;
            }));
        }


        //Check for Travel Options mod
        Mod travelOptions = ModManager.Instance.GetModFromGUID("93f3ad1c-83cc-40ac-b762-96d2f47f2e05");
        hasTravelOptions = travelOptions != null ? true : false;
    }

    public static void OnLoad(SaveData_v1 saveData)
    {
        Instance.ResetVariables();
    }

    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Controls"))
        {
            steering = settings.GetValue<int>("Controls", "DefaultSteeringType");
            throttle = settings.GetValue<int>("Controls", "DefaultThrottleType");
            view = settings.GetValue<int>("Controls", "DefaultViewType");
            steeringKeyCode = SetKeyFromText(settings.GetString("Controls", "CycleSteeringTypeInput"));
            throttleKeyCode = SetKeyFromText(settings.GetString("Controls", "CycleThrottleTypeInput"));
            viewKeyCode = SetKeyFromText(settings.GetString("Controls", "CycleViewTypeInput"));
            centerHorseKeyCode = SetKeyFromText(settings.GetString("Controls", "AlignHorseToViewInput"));
            centerViewKeyCode = SetKeyFromText(settings.GetString("Controls", "AlignViewToHorseInput"));
        }
        if (change.HasChanged("Handling"))
        {
            galloping = settings.GetValue<bool>("Handling", "Galloping");
            acceleration = settings.GetValue<bool>("Handling", "Acceleration");
            moveAcceleration = settings.GetValue<float>("Handling", "MoveAcceleration");
            moveBraking = settings.GetValue<float>("Handling", "MoveBraking");
            turnSpeed = settings.GetValue<float>("Handling", "TurnSpeed");
            turnAcceleration = settings.GetValue<float>("Handling", "TurnAcceleration");
            turnBraking = settings.GetValue<float>("Handling", "TurnBraking");
        }
        if (change.HasChanged("CustomAudio"))
        {
            customAudio = settings.GetValue<bool>("CustomAudio", "Enable");
            customAudioVolume = settings.GetValue<float>("CustomAudio", "Volume");
            if (customAudio && ridingAudioSource != null)
                ridingAudioSource.pitch = 1;
        }
        if (change.HasChanged("Inertia"))
        {
            inertia = settings.GetValue<bool>("Inertia", "Enable");
            inertiaSpeed = settings.GetValue<float>("Inertia", "Speed");
            inertiaStrength = settings.GetValue<float>("Inertia", "Strength");
        }
        if (change.HasChanged("SlopeFollowing"))
        {
            slopeFollow = settings.GetValue<bool>("SlopeFollowing", "Enable");
            slopeFollowSpeed = settings.GetValue<float>("SlopeFollowing", "Speed");
            slopeFollowStrength = settings.GetValue<float>("SlopeFollowing", "Strength");
        }
        if (change.HasChanged("Widget"))
        {
            widget.scaleToScreen = settings.GetValue<int>("Widget", "Scaling");
            widget.speed = settings.GetValue<bool>("Widget", "Speed");
            widget.speedOffset = new Vector2(settings.GetTupleFloat("Widget", "SpeedPosition").First, settings.GetTupleFloat("Widget", "SpeedPosition").Second);
            widget.speedScale = settings.GetValue<float>("Widget", "SpeedScale");
            widget.speedColor = settings.GetColor("Widget", "SpeedColor");
            widget.heading = settings.GetValue<bool>("Widget", "Heading");
            widget.headingOffset = new Vector2(settings.GetTupleFloat("Widget", "HeadingPosition").First, settings.GetTupleFloat("Widget", "HeadingPosition").Second);
            widget.headingScale = settings.GetValue<float>("Widget", "HeadingScale");
            widget.headingColor = settings.GetColor("Widget", "HeadingColor");
            widget.headingIntervalIndex = settings.GetValue<int>("Widget", "HeadingInterval");
            widget.Initialize();

        }
    }

    void ResetVariables()
    {
        speedIndex = 1;

        //Set Yaw to player facing
        Yaw = Vector3.SignedAngle(Vector3.forward,playerObject.transform.forward,Vector3.up);

        targetYaw = 0;
        currentYaw = 0;
        targetThrottle = 0;
        currentThrottle = 0;
        lastThrottle = 0;

        currentInertia = 1;

        moveVector = (Quaternion.AngleAxis(Yaw, Vector3.up) * Vector3.forward) * 1;
        lastMoveVector = moveVector;
    }

    private void FixedUpdate()
    {
        if (GameManager.IsGamePaused)
            return;

        if (playerMotor.IsRiding && hasTravelOptions)
        {
            ModManager.Instance.SendModMessage("TravelOptions", "isTravelActive", null, (string message, object data) =>
            {
                isTravelling = (bool)data;
            });

            //just started travelling
            if (isTravelling && !wasTravelling)
            {
                //do thing
                wasTravelling = isTravelling;
            }

            //just stopped travelling
            if (!isTravelling && wasTravelling)
            {
                ResetVariables();
                //do thing
                wasTravelling = isTravelling;
            }
        }
    }

    private void Update()
    {
        if (!GameManager.IsGamePaused)
        {
            if (playerMotor.IsRiding)
            {
                bool sneaking = GameManager.Instance.SpeedChanger.isSneaking;
                bool running = GameManager.Instance.SpeedChanger.isRunning;

                Tint = transportManager.Tint;

                //if transport mode changed or just started riding
                if (lastTransportMode != transportManager.TransportMode || !wasRiding)
                {
                    //Just started riding
                    if (!wasRiding)
                    {
                        ResetVariables();
                        playerMotor.limitDiagonalSpeed = false;
                        wasRiding = true;
                    }

                    // Setup appropriate riding textures.
                    string textureName = (transportManager.TransportMode == TransportModes.Horse) ? horseTextureName : cartTextureName;
                    if (textureName != lastTextureName)
                    {
                        for (int i = 0; i < 4; i++)
                            ridingTexures[i] = ImageReader.GetImageData(textureName, 0, i, true, true);
                        ridingTexture = ridingTexures[0];
                        lastTextureName = textureName;
                    }

                    // Setup appropriate riding sounds.
                    SoundClips sound = (transportManager.TransportMode == TransportModes.Horse) ? horseRidingSound2 : cartRidingSound;
                    ridingAudioSource.clip = dfAudioSource.GetAudioClip((int)sound);
                    lastTransportMode = transportManager.TransportMode;

                    if (customAudio)
                        dfAudioSource.AudioSource.PlayOneShot(audioHorseMount[UnityEngine.Random.Range(0, audioHorseMount.Length - 1)], 0.25f * DaggerfallUnity.Settings.SoundVolume * customAudioVolume);
                }

                //slope following
                if (slopeFollow)
                {
                    //check slope
                    if (Physics.Raycast(playerObject.transform.position, Vector3.down, out RaycastHit hit))
                        targetSlopeAngle = Vector3.SignedAngle(Vector3.up, hit.normal, Quaternion.AngleAxis(90, Vector3.up) * lastMoveVector) * slopeFollowStrength;
                    else
                        targetSlopeAngle = 0;

                    //move current slope angle to target slope angle
                    //currentSlopeAngle = Mathf.MoveTowards(currentSlopeAngle, targetSlopeAngle, 45 * slopeFollowSpeed * Time.deltaTime);
                    currentSlopeAngle = Mathf.Lerp(currentSlopeAngle, targetSlopeAngle, 1 * slopeFollowSpeed * Time.deltaTime);
                }
                else
                    currentSlopeAngle = 0;

                GameManager.Instance.PlayerMouseLook.PitchMaxLimit = 45f + currentSlopeAngle + currentGravityOffset;

                bool isRidingCart = transportManager.TransportMode == TransportModes.Cart ? true : false;
                float cartMod = 1;
                if (isRidingCart)
                    cartMod = 0.5f;

                transportManager.DrawHorse = false;

                //cycle modes
                if (InputManager.Instance.GetKeyUp(steeringKeyCode))
                {
                    if (steering == 1)
                        steering = 0;
                    else
                        steering = 1;

                    if (steering == 1)
                        DaggerfallUI.Instance.PopupMessage("You grasp the reins");
                    else
                        DaggerfallUI.Instance.PopupMessage("You let go of the reins");
                }

                if (InputManager.Instance.GetKeyUp(throttleKeyCode))
                {
                    if (throttle == 1)
                        throttle = 0;
                    else
                        throttle = 1;

                    if (throttle == 1)
                    {
                        speedIndex = 1;
                        DaggerfallUI.Instance.PopupMessage("Hold throttle");
                    }
                    else
                        DaggerfallUI.Instance.PopupMessage("Press throttle");
                }

                if (InputManager.Instance.GetKeyUp(viewKeyCode))
                {
                    if (view == 1)
                        view = 0;
                    else
                        view = 1;

                    if (view == 1)
                        DaggerfallUI.Instance.PopupMessage("Locked view");
                    else
                        DaggerfallUI.Instance.PopupMessage("Free view");
                }

                //Autorun input funcitonality
                if (InputManager.Instance.ActionStarted(InputManager.Actions.AutoRun))
                {
                    if (throttle == 1)
                    {
                        //do something?
                    }
                    else
                    {
                        if ((speedIndex == speeds.Length - 1 && galloping && running) || (speedIndex == speeds.Length - 1 && !galloping) || speedIndex == 0)
                            speedIndex = 1;    //at full speed and sprinting or in reverse, stop
                        else
                            speedIndex = speeds.Length - 1;    //otherwise, go full speed
                    }
                }

                strafeVector = Vector3.zero;

                //change yaw input
                if (steering == 1 || isTravelling)
                {
                    //Horizontal controls
                    if ((InputManager.Instance.HasAction(InputManager.Actions.MoveLeft) || InputManager.Instance.HasAction(InputManager.Actions.MoveRight)))
                    {
                        //strafe if stationary and not riding a cart
                        //if moving, slightly yaw the horse
                        if (((throttle == 0 && speedIndex == 1) || (throttle == 1 && currentThrottle == 0)) && !isRidingCart)
                        {
                            if (Mathf.Abs(angleYaw) > 1)
                                targetYaw = -90 * turnSpeed * cartMod * Mathf.Clamp(angleYaw / 30, -1, 1) * Time.deltaTime;
                            else
                                targetYaw = 0;

                            if (InputManager.Instance.HasAction(InputManager.Actions.MoveLeft))
                                strafeVector = -playerObject.transform.right * 0.25f;

                            if (InputManager.Instance.HasAction(InputManager.Actions.MoveRight))
                                strafeVector = playerObject.transform.right * 0.25f;
                        }
                        else
                        {
                            if (Mathf.Abs(angleYaw) > 31)
                                targetYaw = -90 * turnSpeed * cartMod * Mathf.Clamp(angleYaw / 30, -1, 1) * Time.deltaTime;
                            else
                                targetYaw = 0;

                            if (InputManager.Instance.HasAction(InputManager.Actions.MoveLeft) && angleYaw > -30)
                                targetYaw = -45 * turnSpeed * cartMod * Time.deltaTime;

                            if (InputManager.Instance.HasAction(InputManager.Actions.MoveRight) && angleYaw < 30)
                                targetYaw = 45 * turnSpeed * cartMod * Time.deltaTime;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(angleYaw) > 1)
                            targetYaw = -90 * turnSpeed * cartMod * Mathf.Clamp(angleYaw / 30, -1, 1) * Time.deltaTime;
                        else
                            targetYaw = 0;
                    }

                    Yaw += targetYaw;
                }
                else
                {
                    //center view to horse if input pressed
                    if (InputManager.Instance.GetKeyDown(centerViewKeyCode))
                    {
                        playerMouseLook.SetHorizontalFacing(moveVector);
                    }

                    float turnForce = turnBraking;

                    if (InputManager.Instance.HasAction(InputManager.Actions.MoveLeft) || InputManager.Instance.HasAction(InputManager.Actions.MoveRight))
                    {
                        if (InputManager.Instance.HasAction(InputManager.Actions.MoveLeft))
                            targetYaw = -90 * turnSpeed * cartMod * Time.deltaTime;

                        if (InputManager.Instance.HasAction(InputManager.Actions.MoveRight))
                            targetYaw = 90 * turnSpeed * cartMod * Time.deltaTime;

                        turnForce = turnAcceleration;
                    }
                    else
                        targetYaw = 0;

                    //center horse to view while input held
                    if (InputManager.Instance.GetKey(centerHorseKeyCode))
                        targetYaw = -90 * turnSpeed * Mathf.Clamp(angleYaw / 30, -1, 1) * Time.deltaTime;

                    //move currentYaw to targetYaw
                    currentYaw = Mathf.MoveTowards(currentYaw, targetYaw, 3 * turnForce * cartMod * Time.deltaTime);

                    if (acceleration && !isTravelling)
                        Yaw += currentYaw;
                    else
                        Yaw += targetYaw;

                    //if view is locked, add horse horizontal movement to MouseLook Yaw
                    if (view == 1 && currentYaw != 0)
                    {
                        if (acceleration)
                            playerMouseLook.Yaw += currentYaw + InputManager.Instance.LookX;
                        else
                            playerMouseLook.Yaw += targetYaw + InputManager.Instance.LookX;
                        playerMouseLook.Pitch -= InputManager.Instance.LookY;
                        playerMouseLook.SetFacing(playerMouseLook.Yaw, playerMouseLook.Pitch);
                    }
                }

                //change speed input
                if (throttle == 1)
                {
                    if (InputManager.Instance.ToggleAutorun)
                    {
                        targetThrottle = speeds[speeds.Length - 1];
                    }
                    else
                    {
                        if (InputManager.Instance.HasAction(InputManager.Actions.MoveForwards) || InputManager.Instance.HasAction(InputManager.Actions.MoveBackwards))
                        {
                            if (InputManager.Instance.HasAction(InputManager.Actions.MoveBackwards) && currentThrottle > speeds[0])
                                targetThrottle = speeds[0];
                            else if (InputManager.Instance.HasAction(InputManager.Actions.MoveForwards) && currentThrottle < speeds[speeds.Length - 1])
                                targetThrottle = speeds[speeds.Length - 1];
                        }
                        else
                            targetThrottle = 0;
                    }

                    //prevent cart from reversing
                    if (isRidingCart && targetThrottle < 0)
                        targetThrottle = 0;
                }
                else
                {
                    if (InputManager.Instance.ActionStarted(InputManager.Actions.MoveBackwards) && speedIndex > 0)
                        speedIndex--;

                    if (InputManager.Instance.ActionStarted(InputManager.Actions.MoveForwards) && speedIndex < speeds.Length - 1)
                        speedIndex++;

                    //prevent cart from reversing
                    if (isRidingCart && speedIndex < 1)
                        speedIndex = 1;
                }

                //collision
                colliding = false;
                if ((throttle == 0 && speedIndex != 1) || (throttle == 1 && currentThrottle != 0))
                {
                    Ray ray = new Ray(playerObject.transform.position + (Vector3.down * 0.5f), (moveVector * targetThrottle).normalized);
                    RaycastHit hit = new RaycastHit();
                    Debug.DrawRay(ray.origin, ray.direction * 3, Color.red, 0.1f, false);
                    if (Physics.Raycast(ray, out hit, 3))
                    {
                        //hit something
                        //if hit object is a mesh or non-mobile billboard (eg, building or decoration), stop
                        DaggerfallMesh dfMesh = hit.collider.gameObject.GetComponent<DaggerfallMesh>();
                        DaggerfallBillboard dfBillboard = hit.collider.gameObject.GetComponent<DaggerfallBillboard>();
                        if (dfMesh != null || (dfBillboard != null && !dfBillboard.Summary.IsMobile))
                        {
                            //play rear up noise
                            if (customAudio && Time.time - lastCollision > 2)
                            {
                                dfAudioSource.AudioSource.PlayOneShot(audioHorseCollision[UnityEngine.Random.Range(0, audioHorseCollision.Length - 1)], 0.25f * DaggerfallUnity.Settings.SoundVolume * customAudioVolume);
                                lastCollision = Time.time;
                            }

                            if (throttle == 1)
                                targetThrottle = 0;
                            else
                                speedIndex = 1;
                            colliding = true;
                        }
                    }
                }

                //set variables
                if (throttle == 1)
                {
                    if (acceleration)
                    {
                        float moveForce = moveBraking;
                        if ((InputManager.Instance.HasAction(InputManager.Actions.MoveForwards) || InputManager.Instance.HasAction(InputManager.Actions.MoveBackwards) || InputManager.Instance.ToggleAutorun) && !colliding)
                            moveForce = moveAcceleration;

                        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, moveForce * Time.deltaTime);
                    }
                    else
                        currentThrottle = targetThrottle;

                    /*if (InputManager.Instance.ToggleAutorun)
                        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, moveAcceleration * Time.deltaTime);
                    else if (!InputManager.Instance.HasAction(InputManager.Actions.MoveForwards) && !InputManager.Instance.HasAction(InputManager.Actions.MoveBackwards) && !InputManager.Instance.ToggleAutorun)
                        currentThrottle = Mathf.MoveTowards(currentThrottle, 0, moveBraking * Time.deltaTime);*/
                }
                else
                {
                    targetThrottle = speeds[speedIndex];
                    if (acceleration && !isTravelling)
                    {
                        float moveForce = moveBraking;
                        if (speedIndex != 1)
                            moveForce = moveAcceleration;

                        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, moveForce * Time.deltaTime);
                    }
                    else
                        currentThrottle = targetThrottle;
                }

                moveVector = (Quaternion.AngleAxis(Yaw, Vector3.up) * Vector3.forward).normalized;


                angleYaw = Vector3.SignedAngle(playerObject.transform.forward, moveVector, Vector3.up);

                //attempted to make it work with G&G's Free Look
                //angleYaw = Vector3.SignedAngle(playerCamera.transform.forward, moveVector, Vector3.up);

                anglePitch = Vector3.SignedAngle(playerCamera.transform.forward, playerObject.transform.forward, playerObject.transform.right);

                Vector3 moveVectorLocal = playerObject.transform.InverseTransformDirection(moveVector * currentThrottle + strafeVector);

                InputManager.Instance.ApplyHorizontalForce(moveVectorLocal.x);
                InputManager.Instance.ApplyVerticalForce(moveVectorLocal.z);
            }
            else
            {
                //Just stopped riding
                if (wasRiding)
                {
                    transportManager.DrawHorse = true;
                    GameManager.Instance.PlayerMouseLook.PitchMaxLimit = 90f;
                    playerMotor.limitDiagonalSpeed = true;

                    if (customAudio)
                    {
                        dfAudioSource.AudioSource.PlayOneShot(audioHorseDismount[UnityEngine.Random.Range(0, audioHorseDismount.Length - 1)], 0.25f * DaggerfallUnity.Settings.SoundVolume * customAudioVolume);

                        if (audioRiding != null)
                            StopRidingAudio();

                        if (ridingAudioSource.isPlaying)
                            StartCoroutine(StopRiding());
                    }
                    else
                    {
                        if (ridingAudioSource.isPlaying)
                            StartCoroutine(StopRiding());
                    }

                    wasRiding = false;
                }
            }
        }

        // Handle horse & cart riding animation & sounds.
        if (transportManager.TransportMode == TransportModes.Horse || transportManager.TransportMode == TransportModes.Cart)
        {
            if ((playerMotor.IsStandingStill && targetYaw == 0) || !playerMotor.IsGrounded || GameManager.IsGamePaused || SaveLoadManager.Instance.LoadInProgress)
            {   // Stop animation frames and sound playing.
                lastFrameTime = 0;
                frameIndex = 0;
                ridingTexture = ridingTexures[0];

                if (customAudio)
                {
                    if (audioRiding != null)
                        StopRidingAudio();
                }

                if (ridingAudioSource.isPlaying)
                    StartCoroutine(StopRiding());
            }
            else
            {
                bool sneaking = GameManager.Instance.SpeedChanger.isSneaking;
                bool running = GameManager.Instance.SpeedChanger.isRunning;

                float sneakMod = sneaking ? 0.8f : 1;
                float runMod = running ? 1.2f : 1;

                if (customAudio && currentGait != null)
                {
                    //if sprinting, stretch animation over two strides
                    float mod = sprint ? 0.025f : 0.05f;
                    animFrameTime = currentGait.Length * mod;
                }
                else
                    animFrameTime = animFrameTimeBase * (2 - ((currentThrottle * runMod * sneakMod) / 1));

                // Update Animation frame?
                if (lastFrameTime == 0)
                {
                    lastFrameTime = Time.unscaledTime;
                }
                else if (Time.unscaledTime > lastFrameTime + animFrameTime)
                {
                    lastFrameTime = Time.unscaledTime;
                    frameIndex = (frameIndex == 3) ? 0 : frameIndex + 1;
                    ridingTexture = ridingTexures[frameIndex];
                }

                if (transportManager.RidingVolumeScale > 0)
                    transportManager.RidingVolumeScale = 0;

                if (customAudio)
                {
                    // refresh audio volume to reflect global changes
                    float volumeScale = 0.5f * sneakMod;
                    ridingAudioSource.volume = volumeScale * DaggerfallUnity.Settings.SoundVolume * customAudioVolume;

                    //play cart sounds
                    if (transportManager.TransportMode == TransportModes.Cart && !ridingAudioSource.isPlaying)
                    {
                        ridingAudioSource.loop = true;
                        ridingAudioSource.Play();
                    }

                    //Set gait depending on throttle type and speed
                    bool[] gait = currentGait;
                    if (currentGait == null)
                        gait = gait1;
                    sprint = false;

                    if (transportManager.TransportMode == TransportModes.Cart)
                    {
                        //Carts can't gallop or reverse
                        if ((throttle == 0 && speedIndex == 3 && !running && !sneaking) ||
                            (throttle == 1 && currentThrottle >= 1 && !running && !sneaking)) //fast
                        {
                            gait = gait3;
                            sprint = false;
                        }
                        else if ((throttle == 0 && ((speedIndex == 3 && sneaking) || (speedIndex == 2 && !running && !sneaking))) || 
                            (throttle == 1 && ((currentThrottle >= 1 && sneaking) || (currentThrottle >= 0.5f && !running && !sneaking)))) //fast+sneak or slow
                        {
                            gait = gait2;
                            sprint = false;
                        }
                        else if ((throttle == 0 && (speedIndex == 2 && sneaking)) ||
                            (throttle == 1 && currentThrottle >= 0.5f && sneaking)) //slow+sneak
                        {
                            gait = gait1;
                            sprint = false;
                        }
                        else if ((throttle == 0 && (speedIndex == 1)) ||
                            (throttle == 1 && currentThrottle == 0f)) //stopped
                        {
                            gait = gait1;
                            sprint = false;
                        }
                    }
                    else
                    {
                        if ((throttle == 0 && (speedIndex == 3 && running)) ||
                            (throttle == 1 && (currentThrottle >= 1 && running))) //fast+run
                        {
                            gait = gait5;
                            sprint = true;
                        }
                        else if ((throttle == 0 && ((speedIndex == 3 && !running && !sneaking) || (speedIndex == 2 && running))) ||
                            (throttle == 1 && ((currentThrottle >= 1 && !running && !sneaking) || (currentThrottle >= 0.5f && running)))) //fast or slow+run
                        {
                            gait = gait4;
                            sprint = true;
                        }
                        else if ((throttle == 0 && ((speedIndex == 3 && sneaking) || (speedIndex == 2 && !running && !sneaking) || (speedIndex == 0 && running))) ||
                            (throttle == 1 && ((currentThrottle >= 1 && sneaking) || (currentThrottle >= 0.5f && !running && !sneaking) || (currentThrottle < 0 && running)))) //fast+sneak or slow or reverse+run
                        {
                            gait = gait3;
                            sprint = false;
                        }
                        else if ((throttle == 0 && ((speedIndex == 2 && sneaking) || (speedIndex == 0 && !running && !sneaking))) ||
                            (throttle == 1 && ((currentThrottle >= 0.5f && sneaking) || (currentThrottle < 0 && !running && !sneaking)))) //slow+sneak or reverse
                        {
                            gait = gait2;
                            sprint = false;
                        }
                        else if ((throttle == 0 && ((speedIndex == 0 && sneaking) || currentYaw != 0 || strafeVector.sqrMagnitude > 0)) ||
                            (throttle == 1 && ((currentThrottle < 0 && sneaking) || currentYaw != 0 || strafeVector.sqrMagnitude > 0))) //reverse+sneak or turning or strafing
                        {
                            gait = gait1;
                            sprint = false;
                        }
                    }

                    if (audioRiding == null || gait != currentGait)
                        StartRidingAudio(gait);
                }
                else
                {
                    // Get appropriate hoof sound for horse
                    if (transportManager.TransportMode == TransportModes.Horse)
                    {
                        //pendingStopRidingAudio = false;

                        if (currentThrottle * runMod * sneakMod < 0.6f && sprint)
                        {
                            ridingAudioSource.clip = dfAudioSource.GetAudioClip((int)horseRidingSound1);
                            sprint = false;
                        }
                        else if (currentThrottle * runMod * sneakMod >= 0.6f && !sprint)
                        {
                            ridingAudioSource.clip = dfAudioSource.GetAudioClip((int)horseRidingSound2);
                            sprint = true;
                        }

                        if (sprint)
                            ridingAudioSource.pitch = ((Mathf.Abs(currentThrottle) * sneakMod * runMod) / 1f);
                        else
                            ridingAudioSource.pitch = ((Mathf.Abs(currentThrottle) * sneakMod * runMod) / 0.5f);
                    }
                    else if (transportManager.TransportMode == TransportModes.Cart)
                    {
                        sprint = true;
                        ridingAudioSource.pitch = Mathf.Clamp(((Mathf.Abs(currentThrottle) * sneakMod * runMod) / 1f), 0.8f, 1);
                    }

                    if (playerMotor.IsStandingStill || strafeVector.sqrMagnitude > 0)
                        ridingAudioSource.pitch = 0.5f;

                    // refresh audio volume to reflect global changes
                    float volumeScale = 0.5f * sneakMod;

                    ridingAudioSource.volume = volumeScale * DaggerfallUnity.Settings.SoundVolume;

                    if (!ridingAudioSource.isPlaying)
                    {
                        ridingAudioSource.Play();
                    }
                }
            }
            // Time for a whinney?
            if (neighTime < Time.time && dfAudioSource.AudioSource.enabled && !isTravelling)
            {
                if (customAudio)
                {
                    dfAudioSource.AudioSource.PlayOneShot(audioHorseNoise[UnityEngine.Random.Range(0,audioHorseNoise.Length-1)], 0.25f * DaggerfallUnity.Settings.SoundVolume * customAudioVolume);
                    neighTime = Time.time + UnityEngine.Random.Range(2, 40);
                }
                else
                {
                    dfAudioSource.AudioSource.PlayOneShot(neighClip, 0.5f * DaggerfallUnity.Settings.SoundVolume);
                    neighTime = Time.time + UnityEngine.Random.Range(2, 40);
                }
            }
        }

        if (inertia)
        {
            float isRunning = GameManager.Instance.PlayerMotor.IsRunning ? 0.2f : 0f;
            float throttleDiff = currentThrottle - targetThrottle;
            if (throttleDiff < 0)
            {
                targetInertia = 1 - (0.2f - isRunning * inertiaStrength);
                currentInertia = Mathf.Lerp(currentInertia, targetInertia, 3f * inertiaSpeed * Time.deltaTime);
            }
            else if (throttleDiff > 0)
            {
                targetInertia = 1 + (0.2f - isRunning * inertiaStrength);
                currentInertia = Mathf.Lerp(currentInertia, targetInertia, 3f * inertiaSpeed * Time.deltaTime);
            }
            else
            {
                targetInertia = 1 - (isRunning * inertiaStrength);
                currentInertia = Mathf.Lerp(currentInertia, targetInertia, 1.5f * inertiaSpeed * Time.deltaTime);
            }

            if (GameManager.Instance.AcrobatMotor.Falling)
            {
                //move gravity towards value
                targetGravityOffset = 5f * inertiaStrength * -playerMotor.MoveDirection.y;
            }
            else
            {
                //move gravity towards 0
                targetGravityOffset = 0;
            }
            currentGravityOffset = Mathf.Lerp(currentGravityOffset, targetGravityOffset, 10 * inertiaSpeed * Time.deltaTime);
        }
        else
        {
            currentInertia = 1;
            currentGravityOffset = 0;
        }

        if (currentThrottle != 0)
            lastThrottle = currentThrottle;
        if (moveVector.sqrMagnitude > 0)
            lastMoveVector = moveVector;
    }

    IEnumerator StopRiding()
    {
        //pendingStopRidingAudio = true;
        yield return new WaitForSecondsRealtime(0.2f);
        ridingAudioSource.Stop();
        ridingAudioSource.loop = false;
        //pendingStopRidingAudio = false;
    }

    void OnGUI()
    {
        if (DaggerfallUI.Instance.CustomScreenRect != null)
            screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
        else
            screenRect = new Rect(0, 0, Screen.width, Screen.height);

        if (Event.current.type.Equals(EventType.Repaint) && !GameManager.IsGamePaused)
        {
            if ((transportManager.TransportMode == TransportModes.Horse || transportManager.TransportMode == TransportModes.Cart) && ridingTexture.texture != null)
            {
                // Draw horse texture behind other HUD elements & weapons.
                GUI.depth = 0;
                // Get horse texture scaling factor. (base on height to avoid aspect ratio issues like fat horses)
                float horseScaleY = (float)screenRect.height / (float)nativeScreenHeight;
                float horseScaleX = horseScaleY * ScaleFactorX;

                // Allow horse to be offset when large HUD enabled
                // This is enabled by default to match classic but can be toggled for either docked/undocked large HUD
                float horseOffsetHeight = 0;
                if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                    DaggerfallUnity.Settings.LargeHUD &&
                    DaggerfallUnity.Settings.LargeHUDOffsetHorse)
                {
                    horseOffsetHeight = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
                }

                if (!isInThirdPerson)
                {
                    float fov = DaggerfallUnity.Settings.FieldOfView;

                    Vector2 ridingTextureScaled = new Vector2(ridingTexture.width * horseScaleX, ridingTexture.height * horseScaleY) * currentInertia;

                    float offsetX = ridingTextureScaled.x * (angleYaw / (fov * 0.5f));

                    if (mirror)
                    {
                        //if horse texture is on the right
                        if (offsetX > ridingTextureScaled.x * 0.25f)
                            mirror = false;
                    }
                    else
                    {
                        //if horse texture is on the left
                        if (offsetX < ridingTextureScaled.x * -0.25f)
                            mirror = true;
                    }

                    //adjust with mouse look pitch and slope angle as well as gravity
                    //mid is 0.5f
                    float offsetY = ridingTextureScaled.y * (0.5f + ((anglePitch / 90))) + ((currentSlopeAngle+currentGravityOffset) * horseScaleY);

                    //if horse is on the right of the screen, flip the image

                    // Calculate position for horse texture and draw it.
                    if (mirror)
                    {
                        horseRect = new Rect(
                                        (screenRect.x + screenRect.width / 2f + ridingTextureScaled.x / 2f) + offsetX,
                                        (screenRect.y + screenRect.height - ridingTextureScaled.y - horseOffsetHeight) + offsetY,
                                        ridingTextureScaled.x * -1,
                                        ridingTextureScaled.y);
                    }
                    else
                    {
                        horseRect = new Rect(
                                        (screenRect.x + screenRect.width / 2f - ridingTextureScaled.x / 2f) + offsetX,
                                        (screenRect.y + screenRect.height - ridingTextureScaled.y - horseOffsetHeight) + offsetY,
                                        ridingTextureScaled.x,
                                        ridingTextureScaled.y);
                    }
                    DaggerfallUI.DrawTexture(horseRect, ridingTexture.texture, ScaleMode.StretchToFill, true, Tint);
                }
            }
        }
    }
    public Vector2 RotateVector(Vector2 v, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        float _x = v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian);
        float _y = v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian);
        return new Vector2(_x, _y);
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

    void StartRidingAudio(bool[] gait, float interval = 0.1f)
    {
        if (audioRiding != null)
            StopCoroutine(audioRiding);

        currentGait = gait;

        audioRiding = PlayRidingAudio(gait, interval);

        StartCoroutine(audioRiding);
    }

    void StopRidingAudio()
    {
        if (audioRiding != null)
            StopCoroutine(audioRiding);

        /*if (transportManager.TransportMode == TransportModes.Cart && ridingAudioSource.isPlaying)
        {
            ridingAudioSource.loop = false;
            //ridingAudioSource.Play();
        }*/

        audioRiding = null;
    }

    IEnumerator PlayRidingAudio(bool[] gait, float interval)
    {
        int i = 0;
        while (true)
        {
            if (hasTravelOptions && isTravelling)
            {
                //do nothing while in accelerated travel
                yield return new WaitForSeconds(interval);
            }
            else
            {
                foreach (bool step in gait)
                {
                    if (step)
                    {
                        AudioClip[] pace = audioHorseStepWalk;
                        if (sprint)
                            pace = audioHorseStepSprint;

                        ridingAudioSource.PlayOneShot(pace[i], 0.5f * DaggerfallUnity.Settings.SoundVolume * customAudioVolume);

                        if (i < pace.Length - 1)
                            i++;
                        else
                            i = 0;
                    }

                    yield return new WaitForSeconds(interval);
                }
            }
        }
    }
}
