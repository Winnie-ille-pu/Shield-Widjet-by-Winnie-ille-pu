
using UnityEngine;
using System.Collections;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DramaticDeaths
{
    public class DramaticDeathsCorpseGrounder : MonoBehaviour
    {
        bool grounded;

        /// <summary>
        /// Attempts to find the ground position below enemy, even if player is flying/falling
        /// </summary>
        /// <param name="distance">Distance to fire ray.</param>
        /// <returns>Hit point on surface below enemy, or enemy position if hit not found in distance.</returns>
        Vector3 FindGroundPosition(float distance = 32)
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out hit, distance))
            {
                grounded = true;
                return hit.point;
            }

            return transform.position;
        }

        private void OnEnable()
        {
            //Ground me
            if (!grounded)
            {
                DaggerfallBillboard dfBillboard = gameObject.GetComponent<DaggerfallBillboard>();
                Vector3 position = FindGroundPosition();

                position += Vector3.up * (dfBillboard.Summary.Size.y / 2f);

                transform.position = position;
            }
        }
    }
}
