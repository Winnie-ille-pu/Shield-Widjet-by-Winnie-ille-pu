using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallConnect;

public class HandheldTorchesEnemyParticleEmitter : MonoBehaviour
{
    public Color color;
    public float interval = 0.5f;

    float timer;

    GameObject puffObject;

    // Update is called once per frame
    void Update()
    {
        /*if (timer > interval)
        {
            DoPuff(transform.position);
            timer = 0;
        }
        else
            timer += Time.deltaTime;*/

        if (puffObject == null)
            DoPuff(transform.position);
    }

    public void DoPuff(Vector3 point)
    {
        //Create oneshot animated billboard for puff effect
        Camera eye = GameManager.Instance.MainCamera;
        GameObject go = GameObjectHelper.CreateDaggerfallBillboardGameObject(375, 0, transform);
        go.name = "Puff";
        Billboard c = go.GetComponent<Billboard>();
        go.transform.position = point;
        go.transform.localScale = new Vector3(go.transform.localScale.x,-go.transform.localScale.y, go.transform.localScale.z);
        //c.OneShot = true;
        c.FramesPerSecond = 15;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.material.SetColor("_Color", Color.white);
        renderer.material.EnableKeyword(KeyWords.Emission);

        puffObject = go;
    }
}
