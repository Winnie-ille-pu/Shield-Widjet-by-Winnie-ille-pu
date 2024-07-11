using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.UserInterface;

public class AutoSight : MonoBehaviour
{
    const int nativeScreenWidth = 320;
    const int nativeScreenHeight = 200;
    Rect screenRect;
    Rect screenRectLast;
    float HUDHeight;
    float HUDHeightLast;

    Camera eye;
    Transform body;
    PlayerMouseLook mouseLook;


    public LayerMask enemyLayerMask;
    public LayerMask playerLayerMask;
    public LayerMask obstacleLayerMask;

    int activationCondition = 0;    //0 = On SwingWeapon Input, 1 = On Input Toggle
    bool activation;
    float activationInputThresholdUpper = 0.8f;
    float activationInputThresholdLower = 0.2f;
    float activationInputLook;

    public float viewSoftzone = 15;
    public int viewSpeed = 1;
    float viewSpeedFinal;

    public int targetingRange = 100;
    public float targetingRadius = 45;
    float targetingTime = 0.25f;
    float targetingTimer;

    public bool feedbackOnTarget;
    public int feedbackIndicator;
    public float feedbackWidgetOffsetX;
    public float feedbackWidgetOffsetY;

    public Vector3 vectorCurrent;
    Vector3 offset;

    DaggerfallEntityBehaviour target;
    CharacterController targetController;
    DaggerfallMobileUnit targetMobile;

    Texture2D[] eyeTextures;
    Texture2D eyeTexture;
    Rect eyeRect;
    int eyeFrameCurrent;
    float eyeFrameTime = 0.25f;
    float eyeFrameTimer;

    static Mod mod;
    [Invoke(StateManager.StateTypes.Start, 0)]

    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        var go = new GameObject(mod.Title);
        go.AddComponent<AutoSight>();
    }

    void Awake()
    {

        eye = GameManager.Instance.MainCamera;
        body = GameManager.Instance.PlayerObject.transform;
        mouseLook = GameManager.Instance.PlayerMouseLook;
        obstacleLayerMask = ~(1 << LayerMask.NameToLayer("Player"));

        mod.LoadSettingsCallback = LoadSettings;
        mod.LoadSettings();

        InitializeTextures();

        mod.IsReady = true;
    }

    private void LoadSettings(ModSettings settings, ModSettingsChange change)
    {
        if (change.HasChanged("Activation"))
        {
            activationCondition = settings.GetValue<int>("Activation", "Condition");
            activationInputThresholdLower = settings.GetValue<float>("Activation", "InputTargetThreshold");
            activationInputThresholdUpper = settings.GetValue<float>("Activation", "InputBreakThreshold");
        }
        if (change.HasChanged("View"))
        {
            viewSoftzone = settings.GetValue<int>("View", "Softzone") * 0.01f * eye.fieldOfView;
            viewSpeed = settings.GetValue<int>("View", "Speed");
        }
        if (change.HasChanged("Targeting"))
        {
            targetingRange = settings.GetValue<int>("Targeting", "Range");
            targetingRadius = settings.GetValue<int>("Targeting", "Radius") * 0.01f * eye.fieldOfView;
            targetingTime = settings.GetValue<int>("Targeting", "Interval")*0.0625f;
        }
        if (change.HasChanged("Feedback"))
        {
            feedbackOnTarget = settings.GetValue<bool>("Feedback", "OnTargetAcquired");
            feedbackIndicator = settings.GetValue<int>("Feedback", "Indicator");
            feedbackWidgetOffsetX = settings.GetValue<float>("Feedback", "WidgetOffsetX");
            feedbackWidgetOffsetY = 1-settings.GetValue<float>("Feedback", "WidgetOffsetY");
            GetEyeRect();
        }
    }

    void InitializeTextures()
    {
        eyeTextures = new Texture2D[5];
        int archive = 500;
        int record = 0;
        int frame = 0;
        for (int i = 0; i < 5; i++)
        {
            Texture2D texture;
            DaggerfallWorkshop.Utility.AssetInjection.TextureReplacement.TryImportTexture(archive, record, frame, out texture);
            eyeTextures[i] = texture;
            frame++;
        }

        eyeFrameCurrent = 0;
    }

    void RefreshWidget()
    {
        if (feedbackIndicator == 2)
        {
            if (activation && DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair)
                DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = false;
            else if (!activation && !DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair)
                DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = true;
        }
        else
        {
            if (!DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair)
                DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = true;
        }
    }

    void GetEyeRect()
    {
        if (eyeTexture == null)
            return;

        Vector2 size = new Vector2(eyeTexture.width * ((float)screenRect.width / (float)nativeScreenWidth), eyeTexture.height * ((float)screenRect.height / (float)nativeScreenHeight))*0.5f;
        Vector2 position = Vector2.zero;

        switch (feedbackIndicator)
        {
            case 1: //Widget
                position = new Vector2(screenRect.x + (screenRect.width * feedbackWidgetOffsetX) - (size.x * 0.5f), screenRect.y + (screenRect.height * feedbackWidgetOffsetY) - (size.y * 0.5f) - (HUDHeight * 0.5f));
                break;
            case 2: //Crosshair
                position = new Vector2(screenRect.x + screenRect.width * 0.5f - (size.x * 0.5f), screenRect.y + screenRect.height * 0.5f - (size.y * 0.5f) - (HUDHeight * 0.5f));
                break;
        }

        eyeRect = new Rect(position, size);
    }

    private void OnGUI()
    {
        GUI.depth = 0;

        if (eyeTexture == null || GameManager.Instance.PlayerEntity == null || feedbackIndicator == 0)
            return;

        if (activationCondition > 0)
        {
            if (!activation)
                return;
        }
        else
        {
            if (!InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon))
                return;
        }

        //get eye texture rect

        DaggerfallUI.DrawTexture(eyeRect, eyeTexture);
    }

    private void LateUpdate()
    {
        activationInputLook = new Vector2(InputManager.Instance.TouchJoyLookX, InputManager.Instance.TouchJoyLookX).magnitude;

        if (feedbackIndicator > 0)
        { 
            if (DaggerfallUI.Instance.CustomScreenRect != null)
                screenRect = DaggerfallUI.Instance.CustomScreenRect.Value;
            else
                screenRect = new Rect(0, 0, Screen.width, Screen.height);

            HUDHeight = 0;
            if (DaggerfallUI.Instance.DaggerfallHUD != null &&
                DaggerfallUnity.Settings.LargeHUD &&
                (DaggerfallUnity.Settings.LargeHUDUndockedOffsetWeapon || DaggerfallUnity.Settings.LargeHUDDocked))
            {
                HUDHeight = (int)DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
            }

            if (screenRect != screenRectLast || HUDHeight != HUDHeightLast)
                GetEyeRect();

            screenRectLast = screenRect;
            HUDHeightLast = HUDHeight;

            RefreshWidget();
        }

        if (activationCondition > 0)
        {
            if (!activation && target != null)
                target = null;
        }
        else
        {
            if (InputManager.Instance.ActionComplete(InputManager.Actions.SwingWeapon))
            {
                target = null;
                DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = true;
            }
        }

        if (activationCondition > 0)
        {
            if (InputManager.Instance.ActionStarted(InputManager.Actions.CenterView))
            {
                activation = !activation;

                if (activation)
                    DaggerfallUI.Instance.PopupMessage("Auto-sight enabled");
                else
                    DaggerfallUI.Instance.PopupMessage("Auto-sight disabled");
            }

            if (activation)
            {
                if (target == null)
                    TrySeekTarget();
            }
        }
        else
        {
            if (InputManager.Instance.HasAction(InputManager.Actions.SwingWeapon))
            {
                if (target == null)
                    TrySeekTarget();
                if (feedbackIndicator == 2)
                    DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = false;
            }
        }

        if (target == null)
        {
            if (eyeFrameTimer > eyeFrameTime)
            {
                if (eyeFrameCurrent >= 3)
                    eyeFrameCurrent = 0;
                else
                    eyeFrameCurrent++;
                eyeFrameTimer = 0;
            }
            else
                eyeFrameTimer += Time.deltaTime;
            eyeTexture = eyeTextures[eyeFrameCurrent];
            return;
        }

        if (activationCondition > 0)
        {
            if (activationInputLook > activationInputThresholdUpper)
            {
                target = null;
                return;
            }
        }

        eyeFrameCurrent = 4;
        eyeTexture = eyeTextures[eyeFrameCurrent];

        if (!target.gameObject.activeSelf)
        {
            target = null;
            return;
        }

        UpdateView();
    }

    void UpdateView()
    {
        float offsetY = 0.25f;
        switch (targetMobile.Summary.Enemy.ID)
        {
            case (int)MobileTypes.Rat:
            case (int)MobileTypes.GiantBat:
            case (int)MobileTypes.Imp:
            case (int)MobileTypes.Slaughterfish:
            case (int)MobileTypes.Spider:
            case (int)MobileTypes.GiantScorpion:
            case (int)MobileTypes.Dragonling:
            case (int)MobileTypes.Dragonling_Alternate:
            case (int)MobileTypes.Lamia:
                offsetY = 0f;
                break;
            case (int)MobileTypes.Harpy:
                offsetY = -0.125f;
                break;
        }

        Vector3 vectorTarget = (((target.transform.position-targetController.center) + (Vector3.up * (targetController.height * offsetY))) - eye.transform.position).normalized;
        viewSpeedFinal = viewSpeed * Mathf.Clamp01(Vector3.Angle(vectorCurrent, vectorTarget) / viewSoftzone);
        vectorCurrent = Vector3.MoveTowards(vectorCurrent, vectorTarget, Time.deltaTime * viewSpeedFinal);
        offset = Quaternion.LookRotation(vectorCurrent, Vector3.up).eulerAngles;

        if (!mouseLook.cursorActive)
        {
            if (offset.x > 180)
                offset.x -= 360;
            mouseLook.SetFacing(offset.y, offset.x);
        }
    }

    void TrySeekTarget()
    {
        if (activationCondition > 0)
        {
            if (activationInputLook > activationInputThresholdLower)
                return;
        }

        if (targetingTimer > targetingTime)
        {
            SeekTarget();
            targetingTimer = 0;
        }
        else
            targetingTimer += Time.deltaTime;
    }

    void SeekTarget()
    {
        if (target != null)
            return;

        DaggerfallEntityBehaviour closestValidTarget = null;
        float closestViewAngle = Mathf.Infinity;

        foreach (DaggerfallEntityBehaviour entityBehaviour in ActiveGameObjectDatabase.GetActiveEnemyBehaviours())
        {
            if (entityBehaviour.EntityType == EntityTypes.EnemyMonster || entityBehaviour.EntityType == EntityTypes.EnemyClass)
            {
                if (IsValidTarget(entityBehaviour))
                {
                    Vector3 targetDir = (entityBehaviour.transform.position - eye.transform.position).normalized;
                    float viewAngle = Vector3.Angle(eye.transform.forward, targetDir);
                    if (viewAngle < closestViewAngle)
                    {
                        closestValidTarget = entityBehaviour;
                        closestViewAngle = viewAngle;
                    }
                }
            }
        }

        if (closestValidTarget != null)
        {
            if (feedbackOnTarget)
                DaggerfallUI.Instance.PopupMessage("Auto-sighting on " + TextManager.Instance.GetLocalizedEnemyName(closestValidTarget.GetComponent<DaggerfallEnemy>().MobileUnit.Enemy.ID));
            SetTarget(closestValidTarget);
            vectorCurrent = eye.transform.forward;
        }
    }

    void SetTarget(DaggerfallEntityBehaviour newTarget)
    {
        target = newTarget;
        targetController = target.GetComponent<CharacterController>();
        targetMobile = target.GetComponentInChildren<DaggerfallMobileUnit>();
    }

    bool IsValidTarget(DaggerfallEntityBehaviour target)
    {
        Vector3 targetVector = target.transform.position - eye.transform.position;

        float distance = targetVector.magnitude;
        if (distance > targetingRange)
            return false;

        float angle = Vector3.Angle(eye.transform.forward,targetVector.normalized);
        if (angle > targetingRadius)
            return false;

        RaycastHit hit;
        Ray ray = new Ray(eye.transform.position + ((-targetVector).normalized*2), targetVector);
        //if (Physics.SphereCast(ray, 2f, out hit, distance, obstacleLayerMask))
        if (Physics.Raycast(ray, out hit, distance, obstacleLayerMask))
        {
            if (hit.transform != target.transform)
                return false;
        }

            return true;
    }

}
