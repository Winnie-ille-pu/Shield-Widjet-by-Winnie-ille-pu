using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

public class HandheldTorchesProjectile : MonoBehaviour
{
    int templateIndex;
    float time;

    Vector3 dirStart;

    float speedStart = 25;
    float speed = 25;
    float gravityDrag;
    float gravityAccel = -9.81f;
    Vector3 currentGravity;

    float bounce = 0.5f;

    //prevent audio from playing too fast
    float lastAudio;

    Renderer renderer;

    public void Initialize(int newTemplateIndex, float newTime, Vector3 pos, Vector3 dir)
    {
        templateIndex = newTemplateIndex;
        time = newTime;

        dirStart = Quaternion.AngleAxis(-15, GameManager.Instance.MainCameraObject.transform.right) * dir;

        /*dirStart = new Vector3(dir.x,0,dir.z);
        currentGravity = new Vector3(0,dir.y,0);*/

        speedStart = 25 * (GameManager.Instance.PlayerEntity.Stats.LiveStrength / 100f) * HandheldTorches.Instance.throwStrength;
        speed = speedStart;
        gravityDrag = 0.05f * HandheldTorches.Instance.throwGravity;
        bounce = HandheldTorches.Instance.throwBounce;

        renderer = gameObject.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.IsGamePaused && dirStart != Vector3.zero)
        {
            currentGravity += Vector3.up * gravityAccel * gravityDrag * Time.deltaTime;

            Vector3 delta = (dirStart * speed * Time.deltaTime) + currentGravity;

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sin(Time.time*20);

            transform.localScale = scale;

            Ray ray = new Ray(transform.position, delta);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, delta.magnitude))
            {
                DaggerfallEntityBehaviour enemy = hit.collider.gameObject.GetComponent<DaggerfallEntityBehaviour>();
                //if (HandheldTorches.Instance.hasBloodfall && enemy != null && enemy != GameManager.Instance.PlayerEntityBehaviour && FormulaHelper.CalculateSuccessfulHit(GameManager.Instance.PlayerEntity, enemy.Entity, HandheldTorches.Instance.fireAccuracy, FormulaHelper.CalculateStruckBodyPart()))
                if (HandheldTorches.Instance.fire && enemy != null && enemy != GameManager.Instance.PlayerEntityBehaviour && FormulaHelper.CalculateSuccessfulHit(GameManager.Instance.PlayerEntity, enemy.Entity, HandheldTorches.Instance.fireAccuracy, FormulaHelper.CalculateStruckBodyPart()))
                {
                    //we hit an entity and successfully roll for player accuracy
                    EntityEffectManager manager = enemy.GetComponent<EntityEffectManager>();
                    EffectSettings settings = BaseEntityEffect.DefaultEffectSettings();
                    settings.ChanceBase = HandheldTorches.Instance.fireChance;
                    settings.DurationBase = HandheldTorches.Instance.fireDuration;
                    settings.MagnitudeBaseMin = HandheldTorches.Instance.fireDamageRange.x;
                    settings.MagnitudeBaseMax = HandheldTorches.Instance.fireDamageRange.y;

                    EntityEffectBundle bundle = manager.CreateSpellBundle(ContinuousDamageHealth.EffectKey, ElementTypes.Fire, settings);
                    manager.AssignBundle(bundle);

                    if (HandheldTorches.Instance.fireLight && !HandheldTorches.Instance.hasBloodfall)
                    {
                        EffectSettings settingsLight = BaseEntityEffect.DefaultEffectSettings();
                        settingsLight.DurationBase = settings.DurationBase;
                        EntityEffectBundle bundleLight = manager.CreateSpellBundle(HandheldTorchesEnemyLight.EffectKey, ElementTypes.Magic, settingsLight);
                        manager.AssignBundle(bundleLight);
                    }

                    HandheldTorches.Instance.audioSourceOneShot.PlayClipAtPoint((int)DaggerfallWorkshop.SoundClips.Ignite, hit.point, 1);
                    Destroy(gameObject);
                }
                else if (speed < speedStart * 0.2f)
                {
                    //spawn torch in hit location and destroy own projectile
                    HandheldTorches.Instance.SpawnLightSource(templateIndex, hit.point, time);
                    if (Time.time - lastAudio > 0.2f)
                    {
                        HandheldTorches.Instance.audioSourceOneShot.PlayClipAtPoint(380, hit.point, 1);
                        lastAudio = Time.time;
                    }
                    Destroy(gameObject);
                }
                else
                {
                    //Vector3 dirNew = Vector3.Reflect(dirStart, hit.normal);
                    Vector3 dirNew = Vector3.Reflect(new Vector3(dirStart.x, currentGravity.y, dirStart.z), hit.normal);
                    dirStart = dirNew;

                    speed *= bounce;
                    currentGravity = Vector3.zero;
                    if (Time.time - lastAudio > 0.2f)
                    {
                        HandheldTorches.Instance.audioSourceOneShot.PlayClipAtPoint(380, hit.point, 1);
                        lastAudio = Time.time;
                    }
                }
                transform.position = hit.point+(hit.normal * (renderer.material.mainTexture.height * 0.016f));
            }
            else
                transform.position += delta;
        }
    }
}
