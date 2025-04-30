using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace CameraTransformerMod
{
    public class CameraTransformer : MonoBehaviour
    {
        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<CameraTransformer>();

            mod.IsReady = true;
        }

        public static CameraTransformer Instance;

        GameObject body;
        Camera eye;
        PlayerMotor motor;
        PlayerMouseLook mouseLook;

        Camera skyEye;

        Vector3 currentMoveDirection;
        float speedFactor;

        Vector3 eyeLocalPosDefault;

        Vector3 bobVector;
        Vector3 bobCurrent;
        Vector3 bobTarget;
        float bobSpeed = 1;
        float bobSize = 1;

        bool tiltInvertWhileRiding = true;
        Vector3 tiltTarget;
        Vector3 tiltCurrent;
        float rollMovementSpeed = 10;
        float rollMovementSize = 0.5f;

        Vector3 groundVector;

        float rollSwimSize = 3;
        float rollSwimSpeed = 3;
        float rollShipSize;
        float rollShipSpeed;

        float recoilStrength = 10;
        float recoilSpeed = 2;
        float recoilSize = 1;
        float currentRecoil;
        Vector3 recoilVector;

        bool roll;

        Mod ceh;

        private void Awake()
        {
            Instance = this;

            body = GameManager.Instance.PlayerObject;
            eye = GameManager.Instance.MainCamera;
            eyeLocalPosDefault = eye.transform.localPosition;

            skyEye = GameManager.Instance.SkyRig.SkyCamera;

            motor = GameManager.Instance.PlayerMotor;

            mouseLook = GameManager.Instance.PlayerMouseLook;

            mod.LoadSettingsCallback = LoadSettings;

            StartGameBehaviour.OnStartMenu += OnStartMenu;
        }
        public static void OnStartMenu(object sender, EventArgs e)
        {
            Instance.ModCompatibilityChecking();

            mod.LoadSettings();
        }

        private void ModCompatibilityChecking()
        {
            if (ceh == null)
            {
                //listen to Combat Event Handler for attacks
                ceh = ModManager.Instance.GetModFromGUID("fb086c76-38e7-4d83-91dc-f29e6f1bb17e");
                if (ceh != null)
                    ModManager.Instance.SendModMessage(ceh.Title, "onAttackDamageCalculated", (Action<DaggerfallEntity, DaggerfallEntity, DaggerfallUnityItem, int, int>)OnAttackDamageCalculated);
            }
        }

        void OnAttackDamageCalculated(DaggerfallEntity attacker, DaggerfallEntity target, DaggerfallUnityItem weapon, int bodyPart, int damage)
        {
            //if target is not the player or recoil is disabled, do nothing
            if (target != GameManager.Instance.PlayerEntity || damage < 1 || recoilStrength == 0 || currentRecoil > 0.5f)
                return;

            recoilSize = ((float)damage / (float)target.MaxHealth) * recoilStrength;

            currentRecoil = 1;
        }

        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            if (change.HasChanged("Bob"))
            {
                bobSpeed = settings.GetValue<float>("Bob", "Speed") * 6;
                bobSize = settings.GetValue<float>("Bob", "Scale") * 0.1f;
            }
            if (change.HasChanged("Roll"))
            {
                roll = settings.GetValue<bool>("Roll", "Enable");
                tiltInvertWhileRiding = settings.GetValue<bool>("Roll", "InvertWhileRiding");
                rollMovementSpeed = settings.GetValue<float>("Roll", "MovementSpeed") * 10;
                rollMovementSize = settings.GetValue<float>("Roll", "MovementScale") * 0.5f;
                rollSwimSpeed = settings.GetValue<float>("Roll", "SwimSpeed") * 3;
                rollSwimSize = settings.GetValue<float>("Roll", "SwimScale") * 3;
                rollShipSpeed = settings.GetValue<float>("Roll", "ShipSpeed") * 0.1f;
                rollShipSize = settings.GetValue<float>("Roll", "ShipScale") * 1;
            }
            if (change.HasChanged("Recoil"))
            {
                recoilStrength = settings.GetValue<float>("Recoil", "Strength") * 100;
            }
        }

        private void LateUpdate()
        {
            bobVector = Vector3.zero;
            tiltTarget = Vector3.zero;
            groundVector = Vector3.zero;

            if (!GameManager.IsGamePaused && !GameManager.Instance.PlayerDeath.DeathInProgress)
            {
                Vector3 playerCameraLocalEulerAngles = eye.transform.localEulerAngles;
                Vector3 skyCameraLocalEulerAngles = skyEye.transform.localEulerAngles;

                currentMoveDirection = body.transform.InverseTransformDirection(new Vector3(motor.MoveDirection.x, 0, motor.MoveDirection.z));
                speedFactor = currentMoveDirection.magnitude / GameManager.Instance.SpeedChanger.GetWalkSpeed(GameManager.Instance.PlayerEntity);

                tiltTarget = Vector3.back * rollMovementSize * currentMoveDirection.x;

                float ridingOffset = 0;
                if (motor.IsRiding)
                {
                    speedFactor *= 0.5f;
                    ridingOffset = 1;
                    if (tiltInvertWhileRiding)
                        tiltTarget *= -1;
                }

                if (currentMoveDirection.sqrMagnitude > 0 && motor.IsGrounded)
                    bobTarget = new Vector3(Mathf.Sin(Time.time * bobSpeed) * (bobSize), Mathf.Sin(ridingOffset + Time.time * bobSpeed * 2) * (bobSize), 0) * speedFactor;
                else
                    bobTarget = Vector3.zero;

                float sinkHeight = 0;
                if (motor.IsSwimming || motor.OnExteriorWater == PlayerMotor.OnExteriorWaterMethod.Swimming)
                {
                    if (!motor.IsSwimming)
                        sinkHeight = 0.6f;

                    bobVector *= 0.1f;
                    if (rollSwimSize > 0)
                        tiltTarget += new Vector3(Mathf.Sin(Time.time * rollSwimSpeed * 2) * rollSwimSize, Mathf.Sin(1 + Time.time * rollSwimSpeed) * rollSwimSize, Mathf.Sin(Time.time * rollSwimSpeed) * rollSwimSize);
                }
                else if (GameManager.Instance.TransportManager.IsOnShip())
                {
                    if (rollShipSize > 0)
                    {
                        float weatherMod = 1;
                        if ((int)GameManager.Instance.WeatherManager.PlayerWeather.WeatherType != 6)
                            weatherMod += (float)GameManager.Instance.WeatherManager.PlayerWeather.WeatherType;

                        if (DaggerfallWorkshop.Game.Banking.DaggerfallBankManager.OwnedShip == DaggerfallWorkshop.Game.Banking.ShipType.Small)
                            groundVector += new Vector3(Mathf.Sin(Time.time * rollShipSpeed * 2) * rollShipSize * 2, Mathf.Sin(Time.time * rollShipSpeed) * rollShipSize, Mathf.Sin(Time.time * rollShipSpeed * 3) * rollShipSize * 3);
                        else
                            groundVector += new Vector3(Mathf.Sin(Time.time * rollShipSpeed * 3) * rollShipSize * 3, Mathf.Sin(Time.time * rollShipSpeed) * rollShipSize, Mathf.Sin(Time.time * rollShipSpeed * 2) * rollShipSize * 2);

                        groundVector *= weatherMod;
                    }
                }

                if (recoilStrength > 0)
                {
                    recoilVector = new Vector3(Mathf.Sin(Time.time * recoilSpeed) * recoilSize, Mathf.Sin(Time.time * recoilSpeed * 2) * recoilSize * 2, 0) * currentRecoil;

                    //increment current recoil to 0
                    currentRecoil = Mathf.Lerp(currentRecoil, 0, Time.deltaTime * 2.5f);
                }

                bobCurrent = Vector3.MoveTowards(bobCurrent, bobTarget, Time.deltaTime);
                tiltCurrent = Vector3.MoveTowards(tiltCurrent, tiltTarget, Time.deltaTime * rollMovementSpeed);

                eye.transform.localPosition = eyeLocalPosDefault + (Vector3.down * sinkHeight) + (bobCurrent);

                if (roll)
                {
                    eye.transform.localEulerAngles = playerCameraLocalEulerAngles + tiltCurrent + recoilVector + eye.transform.InverseTransformVector(groundVector);
                    skyEye.transform.localEulerAngles = skyCameraLocalEulerAngles + tiltCurrent + recoilVector;
                }
            }
        }
    }
}
