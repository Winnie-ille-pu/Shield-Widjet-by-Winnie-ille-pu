
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace DramaticDeaths
{
    public class DramaticDeaths : MonoBehaviour
    {
        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<DramaticDeaths>();
        }

        public static float knockbackMinScale;

        public static int deaths;

        void Awake()
        {
            GameManager.OnEnemySpawn += OnEnemySpawn;

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();

            mod.IsReady = true;
        }
        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            if (change.HasChanged("Main"))
            {
                knockbackMinScale = settings.GetValue<float>("Main", "MinimumKnockbackScale");
            }
        }

        void OnEnemySpawn(GameObject enemy)
        {
            enemy.AddComponent<DramaticDeathsDelayer>();
        }

    }
}
