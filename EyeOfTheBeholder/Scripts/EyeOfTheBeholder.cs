using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Items;

public class EyeOfTheBeholder : MonoBehaviour
{
    public static EyeOfTheBeholder Instance;

    Camera eye;
    Transform body;

    Transform torch = null;
    Vector3 torchPosLocalDefault;

    float speed = 10f;
    float dampen = 1;

    KeyCode offsetKeyCode = KeyCode.KeypadEnter;
    bool offset = true;
    bool offsetDefault = true;

    KeyCode mirrorKeyCode = KeyCode.Tab;
    bool mirror;
    bool mirrorOriginal;
    bool mirrorAuto;
    float mirrorTime = 3;
    float mirrorTimer = 0;

    bool hideWeapon;
    bool hideHorse;
    bool offsetAttacks;

    Vector3 posLocalDefault;
    Vector3 pivotLocal
    {
        get
        {
            return posLocalDefault + Vector3.forward;
        }
    }

    public float offsetX;
    public float offsetY;
    public float offsetZ;
    Vector3 posOffset {
        get
        {
            if (mirror)
                return new Vector3(-offsetX, offsetY + (offsetY * offsetRidingMod), offsetZ + (offsetZ * offsetRidingMod));
            else
                return new Vector3(offsetX, offsetY + (offsetY * offsetRidingMod), offsetZ + (offsetZ * offsetRidingMod));
        }
    }
    Vector3 posCurrent;
    Vector3 posTarget;
    float offsetMinZ;
    float offsetRiding;
    float offsetRidingMod
    {
        get
        {
            if (GameManager.Instance.PlayerMotor.IsRiding)
                return offsetRiding;
            else
                return 0;
        }
    }

    bool billboard;
    bool firstpersonShadow;
    PlayerBillboard billboardPlayer;
    GameObject billboardObject;

    LayerMask layerMask;

    float eyeRadius = 0.25f;
    float boundsX = 2;
    float boundsY = 2;
    float boundsZ = 2;

    //billboard settings
    int indexFoot;
    int indexHorse;
    float size;
    float sizeOffset;
    int readyStance;
    int turnToView;
    int combo;
    float comboTime;
    int comboOffset;
    int torchFollow;

    int playerLayerMask = 0;
    float missileTimer;
    public bool HasSwungWeapon;
    public bool HasFiredMissile;

    static Mod mod;
    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<EyeOfTheBeholder>();
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        eye = GameManager.Instance.MainCamera;
        posLocalDefault = eye.transform.localPosition;

        body = GameManager.Instance.MainCamera.transform.parent;

        torch = GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.transform;
        torchPosLocalDefault = torch.localPosition;

        posTarget = eye.transform.position;
        posCurrent = posTarget;

        layerMask |= (1 << LayerMask.NameToLayer("Default"));
        playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));

        if (billboardObject == null)
            SpawnBillboard();

        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

        offset = offsetDefault;
        ToggleOffset(offset);

        mod.IsReady = true;
    }

    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Camera"))
        {
            offsetDefault = settings.GetValue<bool>("Camera", "StartInThirdPerson");
            offsetKeyCode = SetKeyFromText(settings.GetString("Camera", "TogglePerspective"));
            offsetX = settings.GetTupleFloat("Camera", "FrontalPlaneOffset").First;
            offsetY = settings.GetTupleFloat("Camera", "FrontalPlaneOffset").Second;
            offsetZ = settings.GetValue<float>("Camera", "LongitudinalDistance")*-1;
            offsetMinZ = settings.GetValue<float>("Camera", "MinimumDistance");
            offsetRiding = settings.GetValue<float>("Camera", "RidingOffset");
            speed = settings.GetValue<float>("Camera", "Speed");
            dampen = settings.GetValue<float>("Camera", "Dampen");
            mirrorKeyCode = SetKeyFromText(settings.GetString("Camera", "SwitchShoulder"));
            mirrorAuto = settings.GetValue<bool>("Camera", "Auto-Switch");
            mirrorTime = settings.GetValue<float>("Camera", "SwitchResetTime");
        }
        if (change.HasChanged("Graphics"))
        {
            billboard = settings.GetValue<bool>("Graphics", "Enable");
            indexFoot = settings.GetValue<int>("Graphics", "OnFoot");
            indexHorse = settings.GetValue<int>("Graphics", "OnHorse");
            readyStance = settings.GetValue<int>("Graphics", "ReadyStance");
            turnToView = settings.GetValue<int>("Graphics", "TurnToView");
            combo = settings.GetValue<int>("Graphics", "AttackStrings");
            comboTime = settings.GetValue<float>("Graphics", "MirrorTime");
            comboOffset = settings.GetValue<int>("Graphics", "PingPongOffset");
            firstpersonShadow = settings.GetValue<bool>("Graphics", "FirstPersonShadow");
            torchFollow = settings.GetValue<int>("Graphics", "TorchOffset");
        }
        if (change.HasChanged("Animation"))
        {
            if (billboardPlayer != null)
            {
                billboardPlayer.lengthsChanged = true;
                billboardPlayer.StatesIdleLength = settings.GetValue<int>("Animation", "FootIdleLength");
                billboardPlayer.StatesReadyMeleeLength = settings.GetValue<int>("Animation", "FootReadyMeleeLength");
                billboardPlayer.StatesReadyRangedLength = settings.GetValue<int>("Animation", "FootReadyRangedLength");
                billboardPlayer.StatesReadySpellLength = settings.GetValue<int>("Animation", "FootReadySpellLength");
                billboardPlayer.StatesMoveLength = settings.GetValue<int>("Animation", "FootMoveLength");
                billboardPlayer.StatesAttackMeleeLength = settings.GetValue<int>("Animation", "FootAttackMeleeLength");
                billboardPlayer.StatesAttackRangedLength = settings.GetValue<int>("Animation", "FootAttackRangedLength");
                billboardPlayer.StatesAttackSpellLength = settings.GetValue<int>("Animation", "FootAttackSpellLength");
                billboardPlayer.StatesDeathLength = settings.GetValue<int>("Animation", "FootDeathLength");
                billboardPlayer.StatesIdleHorseLength = settings.GetValue<int>("Animation", "HorseIdleLength");
                billboardPlayer.StatesMoveHorseLength = settings.GetValue<int>("Animation", "HorseMoveLength");
                billboardPlayer.StatesIdleLycanLength = settings.GetValue<int>("Animation", "LycanIdleLength");
                billboardPlayer.StatesReadyLycanLength = settings.GetValue<int>("Animation", "LycanReadyLength");
                billboardPlayer.StatesMoveLycanLength = settings.GetValue<int>("Animation", "LycanMoveLength");
                billboardPlayer.StatesAttackLycanLength = settings.GetValue<int>("Animation", "LycanAttackLength");
                billboardPlayer.StatesDeathLycanLength = settings.GetValue<int>("Animation", "LycanDeathLength");

                billboardPlayer.StatesIdleOffset = new Vector2(settings.GetTupleFloat("Animation", "FootIdleOffset").First, settings.GetTupleFloat("Animation", "FootIdleOffset").Second);
                billboardPlayer.StatesMoveOffset = new Vector2(settings.GetTupleFloat("Animation", "FootMoveOffset").First, settings.GetTupleFloat("Animation", "FootMoveOffset").Second);
                billboardPlayer.StatesAttackMeleeOffset = new Vector2(settings.GetTupleFloat("Animation", "FootAttackMeleeOffset").First, settings.GetTupleFloat("Animation", "FootAttackMeleeOffset").Second);
                billboardPlayer.StatesAttackRangedOffset = new Vector2(settings.GetTupleFloat("Animation", "FootAttackRangedOffset").First, settings.GetTupleFloat("Animation", "FootAttackRangedOffset").Second);
                billboardPlayer.StatesAttackSpellOffset = new Vector2(settings.GetTupleFloat("Animation", "FootAttackSpellOffset").First, settings.GetTupleFloat("Animation", "FootAttackSpellOffset").Second);
                billboardPlayer.StatesDeathOffset = new Vector2(settings.GetTupleFloat("Animation", "FootDeathOffset").First, settings.GetTupleFloat("Animation", "FootDeathOffset").Second);
                billboardPlayer.StatesIdleHorseOffset = new Vector2(settings.GetTupleFloat("Animation", "HorseIdleOffset").First, settings.GetTupleFloat("Animation", "HorseIdleOffset").Second);
                billboardPlayer.StatesMoveHorseOffset = new Vector2(settings.GetTupleFloat("Animation", "HorseMoveOffset").First, settings.GetTupleFloat("Animation", "HorseMoveOffset").Second);
                billboardPlayer.StatesIdleLycanOffset = new Vector2(settings.GetTupleFloat("Animation", "LycanIdleOffset").First, settings.GetTupleFloat("Animation", "LycanIdleOffset").Second);
                billboardPlayer.StatesMoveLycanOffset = new Vector2(settings.GetTupleFloat("Animation", "LycanMoveOffset").First, settings.GetTupleFloat("Animation", "LycanMoveOffset").Second);
                billboardPlayer.StatesAttackLycanOffset = new Vector2(settings.GetTupleFloat("Animation", "LycanAttackOffset").First, settings.GetTupleFloat("Animation", "LycanAttackOffset").Second);
                billboardPlayer.StatesDeathLycanOffset = new Vector2(settings.GetTupleFloat("Animation", "LycanDeathOffset").First, settings.GetTupleFloat("Animation", "LycanDeathOffset").Second);
                sizeOffset = settings.GetValue<float>("Animation", "GlobalOffsetScale");
                size = settings.GetValue<float>("Animation", "BillboardScale");
            }
        }
        if (change.HasChanged("Compatibility"))
        {
            hideWeapon = settings.GetValue<bool>("Compatibility", "Don'tHideWeapon");
            hideHorse = settings.GetValue<bool>("Compatibility", "Don'tHideHorse");
            offsetAttacks = settings.GetValue<bool>("Compatibility", "Don'tOffsetAttacks");
        }

        if (change.HasChanged("Camera") || change.HasChanged("Graphics") || change.HasChanged("Animation") || change.HasChanged("Compatibility"))
        {
            ToggleOffset(offset);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.SaveLoadManager.LoadInProgress && offset != offsetDefault)
            ToggleOffset(offsetDefault);

        if (GameManager.IsGamePaused)
            return;

        if (InputManager.Instance.GetKeyUp(offsetKeyCode))
            ToggleOffset(!offset);

        if (!offset)
            return;

        if (Vector3.Distance(body.transform.position, posCurrent) > 15)
            posCurrent = eye.transform.position;

        //eye.transform.localPosition = posLocalDefault;

        if (!offsetAttacks)
        {
            if (HasSwungWeapon && GameManager.Instance.WeaponManager.ScreenWeapon.GetCurrentFrame() == GameManager.Instance.WeaponManager.ScreenWeapon.GetHitFrame())
            {
                bool hitEnemy = false;
                MeleeDamage(GameManager.Instance.WeaponManager.ScreenWeapon, out hitEnemy);
                HasSwungWeapon = false;
            }

            if (HasFiredMissile)
            {
                if (missileTimer > 3)
                {
                    Debug.Log("MISSILE - DURATION EXCEEDED - RESETTING");
                    HasFiredMissile = false;
                    missileTimer = 0;
                }
                else
                {
                    Vector3 origin = body.TransformPoint(posLocalDefault);
                    Debug.Log("MISSILE - SEARCHING FOR A MISSILE");
                    DaggerfallMissile myMissile = null;
                    DaggerfallMissile[] missiles = FindObjectsOfType<DaggerfallMissile>();
                    float closestDistance = 5;
                    foreach (DaggerfallMissile missile in missiles)
                    {
                        if (missile.Caster == GameManager.Instance.PlayerEntityBehaviour)
                        {
                            float distance = (origin - missile.transform.position).sqrMagnitude;
                            if (distance < closestDistance)
                            {
                                myMissile = missile;
                                closestDistance = distance;
                            }
                        }
                    }

                    if (myMissile != null)
                    {
                        Debug.Log("MISSILE - MISSILE FOUND! RELOCATING!");
                        HasFiredMissile = false;
                        myMissile.CustomAimPosition = origin;
                        myMissile.transform.position = origin;
                        missileTimer = 0;
                    }
                    else
                    {
                        Debug.Log("MISSILE - NO MISSILE YET!");
                        missileTimer += Time.deltaTime;
                    }
                }
            }
            else
                missileTimer = 0;
        }

        //if offsetX is not zero, allow mirroring
        if (offsetX != 0)
        {
            if (InputManager.Instance.GetKeyUp(mirrorKeyCode))
            {
                mirror = !mirror;
                mirrorOriginal = mirror;
            }

            if (mirror != mirrorOriginal && mirrorTime > 0)
            {
                if (mirrorTimer > mirrorTime)
                {
                    Vector3 posLocal = body.InverseTransformPoint(posTarget);
                    Vector3 originX = posLocal;
                    originX.x = posLocalDefault.x;
                    originX = body.TransformPoint(originX);

                    Vector3 dir = -body.transform.right;
                    if (posOffset.x < 0)
                        dir = body.transform.right;

                    Ray ray = new Ray(originX, dir);
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(ray, out hit, Mathf.Abs(posOffset.x * 2f) + eyeRadius, layerMask))
                    {
                        if (hit.distance < (Mathf.Abs(posOffset.x) / 2) + eyeRadius)
                            mirrorTimer = 0;
                        else
                            mirror = mirrorOriginal;
                    }
                    else
                        mirror = mirrorOriginal;
                }
                else
                {
                    mirrorTimer += Time.deltaTime;
                }
            }
        }

        CheckBounds();

        float pitch = GameManager.Instance.PlayerMouseLook.Pitch / 90;
        Vector3 posOffsetPitched = posOffset;
        posOffsetPitched.y = posOffset.y - (posOffset.z * pitch);
        posOffsetPitched.z = posOffset.z - (posOffset.z * pitch);
        posTarget = body.transform.position + body.transform.TransformVector(posLocalDefault + posOffsetPitched);

        posTarget = SetVectorBounds(posTarget);

        float speedFinal = speed;
        if (dampen != 0)
            speedFinal = speed * (Vector3.Distance(posCurrent, posTarget) / dampen);

        posCurrent = Vector3.MoveTowards(posCurrent, posTarget, Time.deltaTime * speedFinal);

        //clamp z offset but respect terrain collision
        if (offsetMinZ != 0)
        {
            float distance = (posOffset.z * offsetMinZ) * (1 - pitch);
            if (boundsZ > Mathf.Abs(distance))
            {
                Vector3 posCurrentLocal = body.transform.InverseTransformPoint(posCurrent);
                if (posCurrentLocal.z > distance)
                    posCurrentLocal.z = distance;
                posCurrent = body.transform.TransformPoint(posCurrentLocal);
            }
        }

    }

    private void LateUpdate()
    {
        if (!offset)
            return;

        eye.transform.position = posCurrent;

        if (!hideWeapon)
            GameManager.Instance.WeaponManager.ScreenWeapon.ShowWeapon = false;
    }

    void SpawnBillboard()
    {
        billboardObject = Instantiate(new GameObject("PlayerBillboard"));
        billboardObject.AddComponent<MeshRenderer>();
        billboardObject.AddComponent<MeshFilter>();
        billboardObject.transform.SetParent(GameManager.Instance.PlayerObject.transform.GetChild(0));
        billboardObject.transform.localPosition = Vector3.zero;
        billboardObject.transform.localRotation = Quaternion.identity;

        billboardPlayer = billboardObject.AddComponent<PlayerBillboard>();
    }

    public void ToggleOffset(bool state)
    {
        if (state)
        {
            posCurrent = eye.transform.position;
            ToggleBillboard(billboard);
            if (firstpersonShadow)
                billboardPlayer.FP = false;
        }
        else
        {
            torch.localPosition = torchPosLocalDefault;
            eye.transform.localPosition = posLocalDefault;
            if (firstpersonShadow)
            {
                billboardPlayer.FP = true;
                ToggleBillboard(billboard);
            }
            else
                ToggleBillboard(false);
        }

        if (!hideHorse)
            GameManager.Instance.TransportManager.DrawHorse = !state;

        offset = state;
    }

    void ToggleBillboard(bool state)
    {
        Debug.Log("Toggling billboard to " + state.ToString());

        billboardObject.SetActive(state);

        if (state)
            billboardPlayer.Initialize(indexFoot, indexHorse, size, sizeOffset, readyStance, turnToView, combo, comboTime, comboOffset, torchFollow);
    }

    void CheckBounds()
    {
        Vector3 posLocal = body.InverseTransformPoint(posTarget);
        Vector3 originX = posLocal;
        originX.x = posLocalDefault.x;
        originX = body.TransformPoint(originX);
        Vector3 originY = posLocal;
        originY.y = posLocalDefault.y;
        originY = body.TransformPoint(originY);
        Vector3 originZ = posLocal;
        originZ.z = posLocalDefault.z;
        originZ = body.TransformPoint(originZ);

        if (posOffset.x != 0)    //raycast sideways
        {
            Vector3 dir = body.transform.right;

            if (posOffset.x < 0)
                dir = -body.transform.right;

            Ray ray = new Ray(originX, dir);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, Mathf.Abs(posOffset.x * 2f) + eyeRadius, layerMask))
                boundsX = hit.distance;
            else
                boundsX = 10 + eyeRadius;

            if (mirrorAuto)
            {
                if (Mathf.Abs(boundsX) < (Mathf.Abs(posOffset.x) / 2) + eyeRadius)
                {
                    mirror = !mirror;
                    if (mirrorTime > 0)
                        mirrorTimer = 0;
                }
            }
        }

        if (posOffset.y != 0)    //raycast vertically
        {
            Vector3 dir = body.transform.up;

            if (posOffset.y < 0)
                dir = -body.transform.up;

            Ray ray = new Ray(originY, dir);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, Mathf.Abs(posOffset.y*2f) + eyeRadius, layerMask))
                boundsY = hit.distance;
            else
                boundsY = 10 + eyeRadius;
        }

        if (posOffset.z != 0)    //raycast forward/backward
        {
            Vector3 dir = body.transform.forward;

            if (posOffset.z < 0)
                dir = -body.transform.forward;

            Ray ray = new Ray(originZ, dir);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, Mathf.Abs(posOffset.z * 2f) + eyeRadius, layerMask))
                boundsZ = hit.distance;
            else
                boundsZ = 10 + eyeRadius;
        }
    }

    Vector3 SetVectorBounds(Vector3 vector)
    {
        Vector3 posLocal = body.InverseTransformPoint(vector);

        if (posOffset.x != 0)    //constrain sideways
        {
            if (posOffset.x < 0 && posLocal.x < -boundsX)
                posLocal.x = -boundsX;
            else if (posLocal.x > boundsX)
                posLocal.x = boundsX;
        }

        if (posOffset.y != 0)    //constrain vertically
        {
            if (posOffset.y < 0 && posLocal.y < -boundsY)
                posLocal.y = -boundsY;
            else if (posLocal.y > boundsY)
                posLocal.y = boundsY;
        }

        if (posOffset.z != 0)    //constrain sagitally
        {
            if (posOffset.z < 0 && posLocal.z < -boundsZ)
                posLocal.z = -boundsZ;
            else if (posLocal.z > boundsZ)
                posLocal.z = boundsZ;
        }

        Vector3 newVector = body.TransformPoint(posLocal);

        return newVector;
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

    private void MeleeDamage(FPSWeapon weapon, out bool hitEnemy)
    {
        hitEnemy = false;

        if (!weapon)
            return;

        // Fire ray along player facing using weapon range
        RaycastHit hit;
        Ray ray = new Ray(body.TransformPoint(posLocalDefault), eye.transform.forward);
        Debug.DrawLine(ray.origin, ray.origin + (ray.direction * weapon.Reach), Color.green, 1f, false);
        if (Physics.SphereCast(ray, 0.25f, out hit, weapon.Reach, playerLayerMask))
        {
            DaggerfallUnityItem strikingWeapon = GameManager.Instance.WeaponManager.ScreenWeapon.SpecificWeapon;
            if (!GameManager.Instance.WeaponManager.WeaponEnvDamage(strikingWeapon, hit)
               // Fall back to simple ray for narrow cages https://forums.dfworkshop.net/viewtopic.php?f=5&t=2195#p39524
               || Physics.Raycast(ray, out hit, weapon.Reach, playerLayerMask))
            {
                hitEnemy = GameManager.Instance.WeaponManager.WeaponDamage(strikingWeapon, false, false, hit.transform, hit.point, ray.direction);
            }
        }
    }
}
