using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

public class HandheldTorches : MonoBehaviour
{
    public static HandheldTorches Instance;

    Light playerLightSource;
    DaggerfallUnityItem lastLightSource;

    DaggerfallAudioSource audioSourceOneShot;
    DaggerfallAudioSource audioSourceLoop;

    LayerMask layerMaskPlayer;

    public List<GameObject> billboardsObject = new List<GameObject>();
    public List<float> billboardsTime = new List<float>();
    public List<int> billboardsItemTemplateIndex = new List<int>();

    bool showSprite = true;
    bool mirrorSprite = false;
    bool tintSprite = true;
    bool playAudio = true;
    float sfxVolume = 0.5f;
    KeyCode dropKeyCode = KeyCode.Tab;
    int onStow = 0; //0 = unequip, 1 = drop
    int onPick = 0; //0 = inventory, 1 = equip
    bool stowOnSpellcasting;
    bool stowOnClimbing;
    bool stowOnSwimming;

    string messageDrop = "You drop the ";
    string messageDropTorchless = "You don't have a light source to drop";
    string messagePickupEquip = "You pick up the ";
    string messagePickupStore = "You stow the ";
    string messageNoFreeHand = "You can't hold a light source right now";
    string messageExamine = "You see a ";

    bool sheathed;
    bool attacked;

    bool attacking;
    bool spellcasting;
    bool climbing;
    bool swimming;

    public bool handLeft;
    public bool handRight;

    public bool HasFreeHand
    {
        get
        {
            if (handLeft || handRight)
                return true;
            else
                return false;
        }
    }

    public int GetFreeHand
    {
        get
        {
            if (HasFreeHand)
            {
                if (handLeft)
                    return 1;
                else
                    return 2;
            }
            else
                return 0;
        }
    }

    const int nativeScreenWidth = 320;
    const int nativeScreenHeight = 200;

    bool flipped;
    bool flippedCurrent;
    bool flippedLast;

    public Texture2D currentTexture;
    public Texture2D[] textures;

    int currentFrame;
    float animationTime = 0.0625f;
    float animationTimer;

    Rect positionCurrent;
    Rect positionTarget;
    Rect curAnimRect;

    Rect screenRect;
    Rect screenRectLast;

    float weaponScaleX;
    float weaponScaleY;
    float weaponOffsetHeight;
    float weaponOffsetHeightLast;

    bool lockAspectRatio;

    bool stepTransforms;
    int stepLength = 16;
    int stepCondition = 1;

    float offsetX = 2.0f;           //distance from the texture's left edge the center of the screen
    float offsetY = 0.75f;           //distance from the texture's bottom edge to the bottom of the screen
    float scale = 1f;
    float offsetSpeed = 1000f;
    int whenSheathed;
    int whenCasting;

    bool bob;
    float bobLength = 1.0f;
    float bobOffset = 0.5f;
    float bobSizeXMod = 1f;      //RECOMMEND RANGE OF 0.5-1.5
    float bobSizeYMod = 4f;      //RECOMMEND RANGE OF 0.5-1.5
    float moveSmoothSpeed = 4;        //RECOMMEND RANGE OF 1-3
    float bobSmoothSpeed = 500;         //RECOMMEND RANGE OF UP TO 1-3
    float bobShape = 1;
    bool bobWhileIdle;

    bool inertia;
    float inertiaScale = 500;
    float inertiaSpeed = 500;
    float inertiaSpeedMod = 1;
    float inertiaForwardSpeed = 0.2f;
    float inertiaForwardScale = 0.2f;

    //PERSISTENT VARIABLES FOR THE SMOOTHING
    float moveSmooth = 0;
    Vector2 bobSmooth = Vector2.zero;

    Vector2 inertiaCurrent = Vector2.zero;
    Vector2 inertiaTarget;

    Vector2 inertiaForwardCurrent = Vector2.zero;
    Vector2 inertiaForwardTarget;

    public Vector2 Position { get; set; } = Vector2.zero;
    public Vector2 Offset { get; set; } = Vector2.zero;
    public Vector2 Scale { get; set; } = Vector2.one;

    public ulong skipTimeStart;
    IUserInterfaceWindow LastUIWindow;

    //EOTB compatibility
    bool eyeOfTheBeholder;
    bool isInThirdPerson;

    static Mod mod;
    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        mod.SaveDataInterface = new HandheldTorchesSaveData();
        var go = new GameObject(mod.Title);
        go.AddComponent<HandheldTorches>();

        PlayerEnterExit.OnTransitionInterior += DestroyLightSources_OnTransition;
        PlayerEnterExit.OnTransitionExterior += DestroyLightSources_OnTransition;
        PlayerEnterExit.OnTransitionDungeonInterior += DestroyLightSources_OnTransition;
        PlayerEnterExit.OnTransitionDungeonExterior += DestroyLightSources_OnTransition;
        PlayerActivate.RegisterCustomActivation(mod, 4000, 0, PickUpLightSource, 3.2f);
        PlayerActivate.RegisterCustomActivation(mod, 4000, 10, PickUpLightSource, 3.2f);
        PlayerActivate.RegisterCustomActivation(mod, 4000, 1, PickUpLightSource, 3.2f);
        PlayerActivate.RegisterCustomActivation(mod, 4000, 11, PickUpLightSource, 3.2f);
        PlayerActivate.RegisterCustomActivation(mod, 4000, 2, PickUpLightSource, 3.2f);
        PlayerActivate.RegisterCustomActivation(mod, 4000, 12, PickUpLightSource, 3.2f);

        DaggerfallUI.UIManager.OnWindowChange += OnRestWindowOpen;
        DaggerfallUI.UIManager.OnWindowChange += OnRestWindowClose;

        SaveLoadManager.OnStartLoad += DestroyLightSources_OnStartLoad;
    }

    public static void DestroyLightSources_OnStartLoad(SaveData_v1 saveData)
    {
        DestroyLightSources();
    }

    public static void OnRestWindowOpen(object sender, EventArgs e)
    {
        if (!GameManager.Instance.StateManager.GameInProgress)
            return;

        if (GameManager.Instance.StateManager.CurrentState == StateManager.StateTypes.Game)
        {
            if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow)
            {
                Debug.Log("ON REST WINDOW OPENED! RECORDING START TIME!");
                Instance.skipTimeStart = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds();
            }
        }
    }
    public static void OnRestWindowClose(object sender, EventArgs e)
    {
        if (!GameManager.Instance.StateManager.GameInProgress)
            return;

        if (GameManager.Instance.StateManager.LastState == StateManager.StateTypes.Game || GameManager.Instance.StateManager.LastState == StateManager.StateTypes.UI)
        {
            if (DaggerfallUI.UIManager.WindowCount > 0)
                Instance.LastUIWindow = DaggerfallUI.Instance.UserInterfaceManager.TopWindow;

            if (DaggerfallUI.UIManager.WindowCount == 0 && Instance.LastUIWindow is DaggerfallRestWindow)
            {
                Debug.Log("ON REST WINDOW CLOSED! CALCULATING DURATION!");
                ulong skipGameTimeDuration = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToSeconds()-Instance.skipTimeStart;

                if (skipGameTimeDuration > 0 && Instance.billboardsTime.Count > 0)
                {
                    Debug.Log("TIME SKIP DETECTED, UPDATING DROPPED TORCHES");
                    float skipRealTimeDuration = skipGameTimeDuration / 12;
                    for (int i  = 0; i < Instance.billboardsTime.Count; i++)
                    {
                        Instance.billboardsTime[i] -= skipRealTimeDuration;

                        if (Instance.billboardsTime[i] < 0)
                        {
                            Instance.billboardsItemTemplateIndex[i] = 0;
                            Destroy(Instance.billboardsObject[i]);
                        }
                    }
                }
                Instance.LastUIWindow = null;
            }
        }
    }

    void Awake()
    {
        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

        ModCompatibilityChecking();

        Instance = this;

        playerLightSource = GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.GetComponent<Light>();

        audioSourceOneShot = gameObject.AddComponent<DaggerfallAudioSource>();

        GameObject audioSourcePlayerObject = new GameObject(mod.Title + " Player Audio");
        audioSourcePlayerObject.transform.SetParent(GameManager.Instance.PlayerObject.transform,false);
        audioSourcePlayerObject.transform.localPosition = Vector3.zero;
        audioSourcePlayerObject.transform.localRotation = Quaternion.identity;
        audioSourceLoop = audioSourcePlayerObject.AddComponent<DaggerfallAudioSource>();
        audioSourceLoop.AudioSource.dopplerLevel = 0;
        audioSourceLoop.AudioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSourceLoop.AudioSource.maxDistance = 5f;
        audioSourceLoop.AudioSource.volume = sfxVolume;
        audioSourceLoop.SetSound(SoundClips.Burning, AudioPresets.LoopOnDemand);

        layerMaskPlayer = ~(1 << LayerMask.NameToLayer("Player"));

        if (DaggerfallUI.Instance.CustomScreenRect != null)
            screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
        else
            screenRect = new Rect(0, 0, Screen.width, Screen.height);

        screenRectLast = screenRect;

        if (DaggerfallUnity.Settings.Handedness == 1)
            flipped = true;

        if (flipped)
            curAnimRect = new Rect(1, 0, -1, 1);
        else
            curAnimRect = new Rect(0, 0, 1, 1);

        InitializeTextures();

        RefreshSprite();


        mod.MessageReceiver = MessageReceiver;
        mod.IsReady = true;
    }

    void MessageReceiver(string message, object data, DFModMessageCallback callBack)
    {
        switch (message)
        {
            case "onToggleOffset":
                isInThirdPerson = (bool)data;
                break;

            default:
                Debug.LogErrorFormat("{0}: unknown message received ({1}).", this, message);
                break;
        }
    }

    private void ModCompatibilityChecking()
    {
        //Eye Of The Beholder
        Mod eotb = ModManager.Instance.GetModFromGUID("2942ea8c-dbd4-42af-bdf9-8199d2f4a0aa");
        //eyeOfTheBeholder = eotb != null ? true : false;
        if (eotb != null)
            ModManager.Instance.SendModMessage("Eye Of The Beholder", "onToggleOffset", "Handheld Torches", null);
    }

    public void OnToggleOffset(bool offsetCurrent)
    {
        isInThirdPerson = offsetCurrent;
    }

    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Handling"))
        {
            dropKeyCode = SetKeyFromText(settings.GetString("Handling", "ManualDropInput"));
            onStow = settings.GetValue<int>("Handling", "OnStow");
            onPick = settings.GetValue<int>("Handling", "OnPick");
            stowOnSpellcasting = settings.GetValue<bool>("Handling", "StowWhenSpellcasting");
            stowOnClimbing = settings.GetValue<bool>("Handling", "StowWhenClimbing");
            stowOnSwimming = settings.GetValue<bool>("Handling", "StowWhenSwimming");
        }
        if (change.HasChanged("Presentation"))
        {
            showSprite = settings.GetValue<bool>("Presentation", "ShowFirstPersonTorch");
            mirrorSprite = settings.GetValue<bool>("Presentation", "Ambidexterity");
            tintSprite = settings.GetValue<bool>("Presentation", "Tint");
            playAudio = settings.GetValue<bool>("Presentation", "PlayerTorchAudio");
            sfxVolume = settings.GetValue<float>("Presentation", "AudioVolume");
            offsetX = settings.GetTupleFloat("Presentation", "Offset").First;
            offsetY = settings.GetTupleFloat("Presentation", "Offset").Second;
            scale = settings.GetValue<float>("Presentation", "Scale");
            offsetSpeed = settings.GetValue<float>("Presentation", "Speed") * 1000;
            lockAspectRatio = settings.GetValue<bool>("Presentation", "LockAspectRatio");
            if (audioSourceLoop != null)
                audioSourceLoop.AudioSource.volume = sfxVolume;
        }
        if (change.HasChanged("Bob"))
        {
            bob = settings.GetValue<bool>("Bob", "EnableBob");
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
            inertia = settings.GetValue<bool>("Inertia", "EnableInertia");
            inertiaScale = settings.GetValue<float>("Inertia", "Scale") * 500;
            inertiaSpeed = settings.GetValue<float>("Inertia", "Speed") * 500;
            inertiaForwardScale = settings.GetValue<float>("Inertia", "ForwardDepth") * 0.2f;
            inertiaForwardSpeed = settings.GetValue<float>("Inertia", "ForwardSpeed") * 0.2f;
        }
        if (change.HasChanged("Step"))
        {
            stepTransforms = settings.GetValue<bool>("Step", "EnableStep");
            stepLength = settings.GetValue<int>("Step", "Length") * 16;
            stepCondition = settings.GetValue<int>("Step", "Condition");
        }

        if (showSprite && currentTexture != null)
            RefreshSprite();
    }
    void InitializeTextures()
    {
        textures = new Texture2D[4];
        int archive = 4001;
        int record = 0;
        int frame = 0;
        for (int i = 0; i < 4; i++)
        {
            Texture2D texture;
            bool valid = DaggerfallWorkshop.Utility.AssetInjection.TextureReplacement.TryImportTexture(archive, record, frame, out texture);
            if (valid)
            {
                Debug.Log("Handheld Torches - Found a texture! " + archive.ToString() + "_" + record.ToString() + "-" + frame.ToString());
                textures[i] = texture;
                frame++;
            } else
            {
                Debug.Log("Handheld Torches - Didn't find a texture! " + archive.ToString() + "_" + record.ToString() + "-" + frame.ToString());
                frame = 0;
                record++;
            }
        }
        currentTexture = textures[0];
    }

    private void OnGUI()
    {
        GUI.depth = 0;

        if (!showSprite || GameManager.IsGamePaused || isInThirdPerson)
            return;

        //if a light source is equipped
        DaggerfallUnityItem itemLightSource = GameManager.Instance.PlayerEntity.LightSource;
        if (itemLightSource != null)
        {
            if (itemLightSource.TemplateIndex != 247 || !HasFreeHand)
                return;

            if (tintSprite)
                DaggerfallUI.DrawTextureWithTexCoords(GetSpriteRect(), currentTexture, curAnimRect, true, GameManager.Instance.WeaponManager.ScreenWeapon.Tint);
            else
                DaggerfallUI.DrawTextureWithTexCoords(GetSpriteRect(), currentTexture, curAnimRect, true, Color.white);
        }
    }

    private void FixedUpdate()
    {
        if (eyeOfTheBeholder)
            isInThirdPerson = CheckForThirdPerson();
    }

    bool CheckForThirdPerson()
    {
        bool thirdPerson = false;

        ModManager.Instance.SendModMessage("Eye Of The Beholder", "isInThirdPerson", null, (string message, object data) =>
        {
            thirdPerson = (bool)data;
        });

        return thirdPerson;
    }

    public Rect GetSpriteRect()
    {
        if (Position != Vector2.zero || Scale != Vector2.zero || Offset != Vector2.zero)
        {
            Rect positionOffset = positionCurrent;

            if (stepTransforms)
            {
                Position = new Vector2(
                    Snapping.Snap(Position.x, stepLength),
                    Snapping.Snap(Position.y, stepLength)
                    );
            }

            positionOffset.x += Position.x - (positionOffset.width * Scale.x);
            positionOffset.y += Position.y - (positionOffset.height * Scale.y);

            positionOffset.width += positionOffset.width * Scale.x;
            positionOffset.height += positionOffset.height * Scale.y;

            if (stepTransforms && stepCondition > 0)
            {
                Offset = new Vector2(
                    Snapping.Snap(Offset.x, stepLength * 0.01f),
                    Snapping.Snap(Offset.y, stepLength * 0.01f)
                    );
            }

            positionOffset.x += positionOffset.width * Offset.x;
            positionOffset.y += positionOffset.height * Offset.y;

            //stop the texture from going higher than its bottom edge
            positionOffset.y = Mathf.Clamp(positionOffset.y, screenRect.height - positionOffset.height, screenRect.height);

            return positionOffset;

        }
        else if (stepTransforms)
        {
            Rect positionOffset = positionCurrent;

            if (stepTransforms)
            {
                positionOffset.x = Snapping.Snap(positionOffset.x, stepLength);
                positionOffset.y = Snapping.Snap(positionOffset.y, stepLength);
            }

            return positionOffset;
        }
        else
            return positionCurrent;
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.IsGamePaused)
            return;

        DaggerfallUnityItem lightSource = GameManager.Instance.PlayerEntity.LightSource;

        UpdateFreeHand();

        if (playAudio)
        {
            if (lightSource != null)
            {
                if (lightSource.TemplateIndex == 247 && !audioSourceLoop.AudioSource.isPlaying)
                    audioSourceLoop.AudioSource.Play();
                else if (lightSource.TemplateIndex != 247 && audioSourceLoop.AudioSource.isPlaying)
                    audioSourceLoop.AudioSource.Stop();
            }
            else if (lightSource == null && audioSourceLoop.AudioSource.isPlaying)
                audioSourceLoop.AudioSource.Stop();
        } else
        {
            if (audioSourceLoop.AudioSource.isPlaying)
                audioSourceLoop.AudioSource.Stop();
        }

        if (!GameManager.Instance.WeaponManager.Sheathed)
        {
            if (!HasFreeHand && lightSource != null && lightSource.TemplateIndex != 248)
            {
                if (onStow > 0 && (sheathed || attacking))
                    DropLightSource(lightSource);
                else
                {
                    if (!sheathed)
                        DaggerfallUI.Instance.PopupMessage(messageNoFreeHand);
                    lastLightSource = lightSource;
                    GameManager.Instance.PlayerEntity.LightSource = null;
                }
            }
            else if (HasFreeHand && lastLightSource != null)
            {
                GameManager.Instance.PlayerEntity.LightSource = lastLightSource;
                lastLightSource = null;
            }

            if (sheathed)
                sheathed = false;
        }
        else if (GameManager.Instance.WeaponManager.Sheathed)
        {
            if (!HasFreeHand && lightSource != null && lightSource.TemplateIndex != 248)
            {
                if (onStow > 0)
                    DropLightSource(lightSource);
                else
                {
                    lastLightSource = lightSource;
                    GameManager.Instance.PlayerEntity.LightSource = null;
                }
            }
            else if (lastLightSource != null && HasFreeHand)
            {
                GameManager.Instance.PlayerEntity.LightSource = lastLightSource;
                lastLightSource = null;
            }

            if (!sheathed)
                sheathed = true;
        }

        //animate sprite
        if (showSprite)
        {
            if (animationTimer > animationTime)
            {
                animationTimer = 0;

                if (currentFrame < 3)
                    currentFrame++;
                else
                    currentFrame = 0;

                currentTexture = textures[currentFrame];
            }
            else
            {
                animationTimer += Time.deltaTime;
            }
        }

        //Manually drop a lightsource
        if (InputManager.Instance.GetKeyDown(dropKeyCode))
        {
            if (!sheathed)
            {
                if (HasFreeHand)
                    DropLightSourceAction(lightSource);
                else
                    DaggerfallUI.Instance.PopupMessage(messageNoFreeHand);
            }
            else
                DropLightSourceAction(lightSource);
        }

        //update light sources
        if (billboardsObject.Count > 0 || billboardsTime.Count > 0)
        {
            CleanUpLightSources();

            for (int i = 0; i < billboardsTime.Count; i++)
            {
                billboardsTime[i] -= (DaggerfallUnity.Instance.WorldTime.TimeScale/12) * Time.deltaTime;
            }

            for (int i = 0; i < billboardsObject.Count; i++)
            {
                if (billboardsObject[i] == null)
                    continue;

                if (billboardsTime.Count <= i)
                    break;

                if (billboardsTime[i] <= 0)
                {
                    billboardsItemTemplateIndex[i] = 0;
                    Destroy(billboardsObject[i]);
                }
                else
                {
                    Light light = billboardsObject[i].GetComponentInChildren<Light>();
                    if (light != null)
                    {
                        float factor = billboardsTime[i] / (GameManager.Instance.ItemHelper.GetItemTemplate(billboardsItemTemplateIndex[i]).hitPoints*20);
                        if (light.color != playerLightSource.color)
                            light.color = playerLightSource.color;
                        light.intensity = (1.25f + (Mathf.Sin(Time.time*2)*0.1f)) * DaggerfallUnity.Settings.PlayerTorchLightScale;
                        light.range = 1 + GameManager.Instance.ItemHelper.GetItemTemplate(billboardsItemTemplateIndex[i]).capacityOrTarget * factor * DaggerfallUnity.Settings.PlayerTorchLightScale;
                        //billboardsObject[i].GetComponent
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        Position = Vector2.zero;
        Offset = Vector2.zero;
        Scale = Vector2.zero;

        attacking = GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking();

        if (GameManager.Instance.PlayerSpellCasting.IsPlayingAnim || GameManager.Instance.PlayerEffectManager.HasReadySpell)
            spellcasting = true;
        else
            spellcasting = false;

        climbing = GameManager.Instance.PlayerMotor.IsClimbing;
        swimming = GameManager.Instance.PlayerMotor.IsSwimming;

        //if off-hand is shield
        DaggerfallUnityItem itemLightSource = GameManager.Instance.PlayerEntity.LightSource;
        if (itemLightSource != null)
        {
            /*if (itemLightSource.TemplateIndex != 247)
                return;*/

            if (DaggerfallUI.Instance.CustomScreenRect != null)
                screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
            else
                screenRect = new Rect(0, 0, Screen.width, Screen.height);

            weaponOffsetHeight = 0;
            if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                DaggerfallUnity.Settings.LargeHUD &&
                (DaggerfallUnity.Settings.LargeHUDUndockedOffsetWeapon || DaggerfallUnity.Settings.LargeHUDDocked))
            {
                weaponOffsetHeight = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
            }

            if (screenRect != screenRectLast || weaponOffsetHeight != weaponOffsetHeightLast)
                RefreshSprite();

            screenRectLast = screenRect;
            weaponOffsetHeightLast = weaponOffsetHeight;

            /*//adjust shield position when sheathed
            if (GameManager.Instance.WeaponManager.Sheathed)
            {
                if (!sheathed)
                {
                    //sheathed = true;
                    if (whenSheathed == 1)
                        SetAttack();
                    else
                        SetGuard();
                }
            }
            else
            {
                if (sheathed)
                {
                    //sheathed = false;
                    RefreshSprite();
                }
            }*/

            /*//adjust shield position when spellcasting
            if (GameManager.Instance.PlayerSpellCasting.IsPlayingAnim || GameManager.Instance.PlayerEffectManager.HasReadySpell)
            {
                if (!spelled)
                {
                    spelled = true;
                    if (whenCasting == 1)
                        SetAttack();
                    else
                        SetGuard();
                }
            }
            else
            {
                if (spelled)
                {
                    spelled = false;
                    RefreshShield();
                }
            }*/

            //adjust shield position when attacking
            if (attacking)
            {
                if (!attacked)
                {
                    attacked = true;
                    SetAttack();
                }
            }
            else
            {
                if (attacked)
                {
                    attacked = false;
                    RefreshSprite();
                }
            }

            Vector3 current = new Vector3(positionCurrent.x, positionCurrent.y, 0);
            Vector3 target = new Vector3(positionTarget.x, positionTarget.y, 0);
            current = Vector3.MoveTowards(current, target, Time.deltaTime * offsetSpeed);
            positionCurrent = new Rect(current.x, current.y, positionTarget.width, positionTarget.height);

            //SCALE  TO SPEED AND MOVEMENT
            float currentSpeed = GameManager.Instance.PlayerMotor.Speed;
            float baseSpeed = GameManager.Instance.SpeedChanger.GetBaseSpeed();
            float speed = currentSpeed / baseSpeed;

            //start of weapon bob code
            if (bob && !attacking && positionCurrent == positionTarget)
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
                float screenOffsetX = 1f;
                float screenOffsetY = 1f;
                //REVERSE OFFSET IF LEFT-HANDED
                if (flipped)
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
                if (attacking || positionCurrent != positionTarget)
                {
                    inertiaCurrent = Vector2.zero;
                    inertiaTarget = Vector2.zero;
                    inertiaForwardCurrent = Vector2.zero;
                    inertiaForwardTarget = Vector2.zero;
                }
                else
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
                    Offset += inertiaForwardCurrent;
                }
            }
        }
    }

    public void RefreshSprite()
    {
        if (GameManager.Instance.WeaponManager.ScreenWeapon.IsAttacking())
            SetAttack();
        else
            SetGuard();
    }

    public void SetGuard()
    {
        weaponScaleX = (float)screenRect.width / (float)nativeScreenWidth;
        if (lockAspectRatio)
            weaponScaleY = weaponScaleX;
        else
            weaponScaleY = (float)screenRect.height / (float)nativeScreenHeight;

        if (flipped)
        {
            positionTarget = new Rect(
                screenRect.x + screenRect.width - ((currentTexture.width * scale) * (1-offsetX) * weaponScaleX),
                screenRect.y + screenRect.height - ((currentTexture.height * scale) * (0.5f + offsetY) * weaponScaleY - weaponOffsetHeight),
                currentTexture.width * scale * weaponScaleX,
                currentTexture.height * scale * weaponScaleY
                );
        }
        else
        {
            positionTarget = new Rect(
                screenRect.x - ((currentTexture.width * scale) * offsetX * weaponScaleX),
                screenRect.y + screenRect.height - ((currentTexture.height * scale) * (0.5f + offsetY) * weaponScaleY - weaponOffsetHeight),
                currentTexture.width * scale * weaponScaleX,
                currentTexture.height * scale * weaponScaleY
                );
        }
    }

    public void SetAttack()
    {
        weaponScaleX = (float)screenRect.width / (float)nativeScreenWidth;
        if (lockAspectRatio)
            weaponScaleY = weaponScaleX;
        else
            weaponScaleY = (float)screenRect.height / (float)nativeScreenHeight;

        if (flipped)
        {
            positionTarget = new Rect(
                screenRect.x + screenRect.width - ((currentTexture.width * 0.5f * scale) * weaponScaleX),
                screenRect.y + screenRect.height - ((currentTexture.height * 0.5f * scale) * weaponScaleY - weaponOffsetHeight),
                currentTexture.width * scale * weaponScaleX,
                currentTexture.height * scale * weaponScaleY
            );
        }
        else
        {
            positionTarget = new Rect(
                screenRect.x - ((currentTexture.width * 0.5f * scale) * weaponScaleX),
                screenRect.y + screenRect.height - ((currentTexture.height * 0.5f * scale) * weaponScaleY - weaponOffsetHeight),
                currentTexture.width * scale * weaponScaleX,
                currentTexture.height * scale * weaponScaleY
            );
        }
    }


    public void UpdateFreeHand()
    {
        handRight = true;
        handLeft = true;

        DaggerfallUnityItem itemLeftHand = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.LeftHand);
        DaggerfallUnityItem itemRightHand = GameManager.Instance.PlayerEntity.ItemEquipTable.GetItem(EquipSlots.RightHand);

        if (GameManager.Instance.WeaponManager.Sheathed)
        {
            //check if wielding a shield or bow in left hand
            if (itemLeftHand != null)
            {
                if (itemLeftHand.IsShield || GameManager.Instance.ItemHelper.ConvertItemToAPIWeaponType(itemLeftHand) == WeaponTypes.Bow)
                    handLeft = false;
            }
        }
        else
        {
            //check if wielding anything in left hand
            if (itemLeftHand != null)
            {
                handLeft = false;
                if (ItemEquipTable.GetItemHands(itemLeftHand) == ItemHands.Both || GameManager.Instance.ItemHelper.ConvertItemToAPIWeaponType(itemLeftHand) == WeaponTypes.Bow)
                    handRight = false;
            }
            else if (!GameManager.Instance.WeaponManager.UsingRightHand) //if left hand is free and used for punching, disallow torch
            {
                handLeft = false;
            }

            //check if wielding anything in right hand
            if (itemRightHand != null)
            {
                handRight = false;
                if (ItemEquipTable.GetItemHands(itemRightHand) == ItemHands.Both) //if right hand item is two-handed, occupy the other hand even if free
                {
                    if (GameManager.Instance.ItemHelper.ConvertItemToAPIWeaponType(itemRightHand) == WeaponTypes.Bow || attacking)
                        handLeft = false;
                }
            }
            else if (GameManager.Instance.WeaponManager.UsingRightHand) //if right hand is free and used for punching, disallow torch
            {
                handRight = false;
            }
        }

        //occupy both hands if spellcasting
        if (stowOnSpellcasting && spellcasting)
        {
            handRight = false;
            handLeft = false;
        }

        //occupy both hands if climbing
        if (stowOnClimbing && climbing)
        {
            handRight = false;
            handLeft = false;
        }
        //occupy both hands if swimming
        if (stowOnSwimming && swimming)
        {
            handRight = false;
            handLeft = false;
        }

        if (mirrorSprite)
        {
            if (DaggerfallUnity.Settings.Handedness == 1)
                flippedCurrent = true;
            else
                flippedCurrent = false;

            if (!handLeft && handRight)
                flippedCurrent = !flippedCurrent;

            if (flippedCurrent != flippedLast)
            {
                flipped = flippedCurrent;
                flippedLast = flippedCurrent;

                if (flipped)
                    curAnimRect = new Rect(1, 0, -1, 1);
                else
                    curAnimRect = new Rect(0, 0, 1, 1);

                RefreshSprite();

                if (GameManager.Instance.PlayerEntity.LightSource != null)
                {
                    float x = 1;
                    if (flipped)
                        x = -1;
                    if (GameManager.Instance.PlayerEntity.LightSource.TemplateIndex == 248)
                        GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.transform.localPosition = new Vector3(-0.26f, 0, 0.25f);
                    else
                        GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.transform.localPosition = new Vector3(-0.34f * x, 0.9f, 0.25f);
                }

                positionCurrent = positionTarget;
            }
        }
        else
        {
            if (DaggerfallUnity.Settings.Handedness == 1)
                flipped = true;
            else
                flipped = false;
        }
    }

    void DropLightSource(DaggerfallUnityItem item)
    {
        Vector3 positionCurrent;
        float offset = 0;
        if (item.ItemTemplate.index == 247) //is torch
            offset = 0.45f;
        else if (item.ItemTemplate.index == 253) //is candle
            offset = 0.3f;
        else if (item.ItemTemplate.index == 269) //is holy candle
            offset = 0.3f;

        //get forward bounds
        Ray ray1 = new Ray(GameManager.Instance.PlayerObject.transform.position, GameManager.Instance.PlayerObject.transform.forward);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray1, out hit, 1.45f, layerMaskPlayer))
        {
            positionCurrent = hit.point - (GameManager.Instance.PlayerObject.transform.forward * offset);
        }
        else
            positionCurrent = ray1.origin + ray1.direction;

        //get lower bounds
        Ray ray2 = new Ray(positionCurrent, Vector3.down);
        if (Physics.Raycast(ray2, out hit, 145f, layerMaskPlayer))
        {
            positionCurrent = hit.point + (Vector3.up * offset);
        }
        else
            positionCurrent = ray2.origin + ray2.direction;

        //Spawn a torch given the player's position and the torch's condition
        SpawnLightSource(item.ItemTemplate.index, positionCurrent, item.currentCondition * 20f);

        //remove from inventory
        if (item == GameManager.Instance.PlayerEntity.LightSource)
            GameManager.Instance.PlayerEntity.LightSource = null;
        GameManager.Instance.PlayerEntity.Items.RemoveItem(item);

        DaggerfallUI.Instance.PopupMessage(Instance.messageDrop + item.GetMacroDataSource().Condition().ToLower() + " " + item.LongName.ToLower());

        Instance.audioSourceOneShot.PlayClipAtPoint(380, positionCurrent, 1);
    }

    void DropLightSourceAction(DaggerfallUnityItem item)
    {
        if (item != null && item.TemplateIndex != 248)
            DropLightSource(item);
        else
        {
            if (GameManager.Instance.PlayerEntity.Items.Contains(ItemGroups.UselessItems2, (int)UselessItems2.Torch))
            {
                //Drop first torch
                DropLightSource(GameManager.Instance.PlayerEntity.Items.GetItem(ItemGroups.UselessItems2, (int)UselessItems2.Torch));
            }
            else if (GameManager.Instance.PlayerEntity.Items.Contains(ItemGroups.UselessItems2, (int)UselessItems2.Candle))
            {
                //Drop first candle
                DropLightSource(GameManager.Instance.PlayerEntity.Items.GetItem(ItemGroups.UselessItems2, (int)UselessItems2.Candle));
            }
            else if (GameManager.Instance.PlayerEntity.Items.Contains(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_candle))
            {
                //Drop first holy candle
                DropLightSource(GameManager.Instance.PlayerEntity.Items.GetItem(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_candle));
            }
            else
                DaggerfallUI.Instance.PopupMessage(Instance.messageDropTorchless);
        }
    }

    public void SpawnLightSource(int itemTemplateIndex, Vector3 position, float time)
    {
        //check if spawn point is below water level
        int indexOffset = 0;
        if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon && GameManager.Instance.PlayerEnterExit.blockWaterLevel != 10000 && position.y + (50 * MeshReader.GlobalScale) < GameManager.Instance.PlayerEnterExit.blockWaterLevel * -1 * MeshReader.GlobalScale)
            indexOffset = 10;

        //spawn billboard object
        GameObject billboardObject = null;
        //DaggerfallUnityItem item = ItemHelper.GetItem;
        if (itemTemplateIndex == 247) //is torch
            billboardObject = GameObjectHelper.CreateDaggerfallBillboardGameObject(4000, 0+indexOffset, null);
        else if (itemTemplateIndex == 253) //is candle
            billboardObject = GameObjectHelper.CreateDaggerfallBillboardGameObject(4000, 1+indexOffset, null);
        else if (itemTemplateIndex == 269) //is holy candle
            billboardObject = GameObjectHelper.CreateDaggerfallBillboardGameObject(4000, 2+indexOffset, null);
        billboardObject.transform.position = position;
        billboardObject.layer = 2;
        billboardsObject.Add(billboardObject);

        //set condition
        billboardsTime.Add(time);

        //set item
        billboardsItemTemplateIndex.Add(itemTemplateIndex);

        //add light and audio if not doused
        if (indexOffset == 0)
        {
            //add light
            GameObject lightSource = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_InteriorLightPrefab.gameObject, string.Empty, billboardObject.transform, billboardObject.transform.position + (Vector3.up * 0.5f));
            Light light = lightSource.GetComponent<Light>();
            if (light != null)
            {
                light.color = playerLightSource.color;
                light.intensity = 1.25f * DaggerfallUnity.Settings.PlayerTorchLightScale;
                light.range = DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(itemTemplateIndex).capacityOrTarget;
                light.type = playerLightSource.type;
                light.shadows = playerLightSource.shadows;
            }

            //if torch, add audio
            if (itemTemplateIndex == 247)
            {
                DaggerfallAudioSource audioSource = billboardObject.AddComponent<DaggerfallAudioSource>();
                audioSource.AudioSource.dopplerLevel = 0;
                audioSource.AudioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.AudioSource.maxDistance = 5f;
                audioSource.AudioSource.volume = sfxVolume;
                audioSource.SetSound(SoundClips.Burning, AudioPresets.LoopIfPlayerNear);
            }
        }
    }

    public static void PickUpLightSource(RaycastHit hit)
    {
        if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Grab || GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Steal)
        {
            for (int i = 0; i < Instance.billboardsObject.Count; i++)
            {
                if (hit.transform.gameObject.GetInstanceID() == Instance.billboardsObject[i].GetInstanceID())
                {
                    PickupLightSource(i);
                    break;
                }
            }
        }
        else if (GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Info || GameManager.Instance.PlayerActivate.CurrentMode == PlayerActivateModes.Talk)
        {
            for (int i = 0; i < Instance.billboardsObject.Count; i++)
            {
                if (hit.transform.gameObject.GetInstanceID() == Instance.billboardsObject[i].GetInstanceID())
                {
                    DaggerfallUI.Instance.PopupMessage(Instance.messageExamine + DaggerfallUnity.Instance.ItemHelper.GetItemTemplate(Instance.billboardsItemTemplateIndex[i]).name.ToLower());
                    break;
                }
            }
        }
    }

    public static void PickupLightSource(int index)
    {
        //add item to player inventory
        DaggerfallUnityItem item = null;
        if (Instance.billboardsItemTemplateIndex[index] == 247) //is torch
            item = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Torch);
        else if (Instance.billboardsItemTemplateIndex[index] == 253) //is candle
            item = ItemBuilder.CreateItem(ItemGroups.UselessItems2, (int)UselessItems2.Candle);
        else if (Instance.billboardsItemTemplateIndex[index] == 269) //is holy candle
            item = ItemBuilder.CreateItem(ItemGroups.ReligiousItems, (int)ReligiousItems.Holy_candle);

        GameManager.Instance.PlayerEntity.Items.AddItem(item);

        //set item condition
        item.currentCondition = Mathf.CeilToInt(Instance.billboardsTime[index] / 20);

        //destroy world object
        Instance.billboardsItemTemplateIndex[index] = 0;
        Destroy(Instance.billboardsObject[index]);
        Instance.billboardsTime[index] = -1;

        if (Instance.onPick > 0 && GameManager.Instance.PlayerEntity.LightSource == null)
        {
            if (Instance.HasFreeHand)
            {
                GameManager.Instance.PlayerEntity.LightSource = item;
                DaggerfallUI.Instance.PopupMessage(Instance.messagePickupEquip + item.GetMacroDataSource().Condition().ToLower() + " " + item.LongName.ToLower());
            }
            else
            {
                DaggerfallUI.Instance.PopupMessage(Instance.messagePickupStore + item.GetMacroDataSource().Condition().ToLower() + " " + item.LongName.ToLower());
                if (Instance.lastLightSource == null)
                    Instance.lastLightSource = item;
            }
        }
        else
        {
            DaggerfallUI.Instance.PopupMessage(Instance.messagePickupStore + item.GetMacroDataSource().Condition().ToLower() + " " + item.LongName.ToLower());
        }

       Instance.audioSourceOneShot.PlayOneShot(417, 0, 1);
    }

    public void CleanUpLightSources()
    {
        billboardsObject.RemoveAll(x => x == null);
        billboardsTime.RemoveAll(y => y < 0);
        billboardsItemTemplateIndex.RemoveAll(z => z == 0);
    }

    public static void DestroyLightSources()
    {
        if (Instance.billboardsObject.Count < 1)
            return;

        foreach (GameObject billboard in Instance.billboardsObject)
        {
            Destroy(billboard);
        }

        Instance.billboardsObject.Clear();
        Instance.billboardsTime.Clear();
        Instance.billboardsItemTemplateIndex.Clear();
    }

    public static void DestroyLightSources_OnTransition(PlayerEnterExit.TransitionEventArgs args)
    {
        DestroyLightSources();
    }

    private KeyCode SetKeyFromText(string text)
    {
        Debug.Log("Setting Key");
        if (System.Enum.TryParse(text, out KeyCode result))
        {
            Debug.Log("Key set to " + result.ToString());
            return result;
        }
        else
        {
            Debug.Log("Detected an invalid key code. Setting to default.");
            return KeyCode.Tab;
        }
    }

    [FullSerializer.fsObject("v1")]
    public class DroppedTorchData
    {
        public Vector3 position;
        public float time;
        public int itemTemplateIndex;
    }

    public class HandheldTorchesSaveData : IHasModSaveData
    {
        public List<DroppedTorchData> DroppedTorches;

        public Type SaveDataType
        {
            get
            {
                return typeof(HandheldTorchesSaveData);
            }
        }

        public object NewSaveData()
        {
            HandheldTorchesSaveData emptyData = new HandheldTorchesSaveData();
            emptyData.DroppedTorches = new List<DroppedTorchData>();
            return emptyData;
        }
        public object GetSaveData()
        {
            List<DroppedTorchData> dropEntries = new List<DroppedTorchData>();

            for (int i = 0; i < Instance.billboardsObject.Count; i++)
            {
                DroppedTorchData newData = new DroppedTorchData();
                newData.position = Instance.billboardsObject[i].transform.position;
                newData.time = Instance.billboardsTime[i];
                newData.itemTemplateIndex = Instance.billboardsItemTemplateIndex[i];
                dropEntries.Add(newData);
            }

            HandheldTorchesSaveData data = new HandheldTorchesSaveData();
            data.DroppedTorches = dropEntries;
            return data;
        }

        public void RestoreSaveData(object dataIn)
        {
            HandheldTorchesSaveData data = (HandheldTorchesSaveData)dataIn;
            List<DroppedTorchData> droppedTorches = data.DroppedTorches;

            foreach (DroppedTorchData torch in droppedTorches)
            {
                Instance.SpawnLightSource(torch.itemTemplateIndex, torch.position, torch.time);
            }
        }
    }
}
