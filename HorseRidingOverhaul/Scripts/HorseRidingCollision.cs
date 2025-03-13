using System.Collections;
using System.Collections.Generic;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using UnityEngine;

public class HorseRidingCollision : MonoBehaviour
{
    GameObject lastHitGameObject;

    float distance = 0.1f;
    float cooldown = 1f;

    List<GameObject> hitGameObjects = new List<GameObject>();
    List<float> hitTime = new List<float>();

    private void LateUpdate()
    {
        if (GameManager.IsGamePaused)
            return;

        if (HorseRidingOverhaul.Instance.trample > 0)
            UpdateHitGameObject();
    }

    void UpdateHitGameObject()
    {
        if (hitGameObjects.Count < 1)
            return;

        for (int i = 0; i < hitGameObjects.Count; i++)
        {
            if (Time.time - hitTime[i] > cooldown)
            {
                hitGameObjects[i] = null;
                hitTime[i] = -1;
            }
        }

        hitGameObjects.RemoveAll(x => x == null);
        hitTime.RemoveAll(x => x == -1);
    }

    //Detect trample
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (HorseRidingOverhaul.Instance.trample < 1)
            return;

        //check if its an object that we've hit before and can't be trampled
        if (lastHitGameObject != hit.gameObject && !hitGameObjects.Contains(hit.gameObject) && hit.moveLength > distance)
        {
            DaggerfallEntityBehaviour hitEntityBehaviour = hit.gameObject.GetComponent<DaggerfallEntityBehaviour>();
            PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
            if (playerMotor.IsRiding && playerMotor.IsRunning && hitEntityBehaviour)
            {
                hitGameObjects.Add(hit.gameObject);
                hitTime.Add(Time.time);
                HorseRidingOverhaul.Instance.AttemptTrample(hit.gameObject, hitEntityBehaviour, hit.moveDirection);
            }
            else
                lastHitGameObject = hit.gameObject;
        }
    }
}
