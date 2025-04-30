using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using Wenzil.Console;

namespace BasicFrameMod
{
    public class BasicFrame : MonoBehaviour
    {
        Texture2D frame43;
        Texture2D frame1610;

        int depthPlay = -1;
        int depthPause = 1;

        ConsoleController console;

        private static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<BasicFrame>();

            mod.IsReady = true;
        }

        private void Awake()
        {
            DaggerfallWorkshop.Utility.AssetInjection.TextureReplacement.TryImportTexture(15000, 0, 0, out frame43);
            DaggerfallWorkshop.Utility.AssetInjection.TextureReplacement.TryImportTexture(15000, 0, 1, out frame1610);

            console = GameObject.Find("Console").GetComponent<ConsoleController>();

            mod.LoadSettingsCallback = LoadSettings;
            mod.LoadSettings();
        }
        private void LoadSettings(ModSettings settings, ModSettingsChange change)
        {
            if (change.HasChanged("Main"))
            {
                depthPlay = settings.GetValue<int>("Main", "PlayDepth");
                depthPause = settings.GetValue<int>("Main", "PauseDepth");
            }
        }

        private void OnGUI()
        {
            if (DaggerfallUnity.Settings.RetroRenderingMode != 0 &&
                DaggerfallUnity.Settings.RetroModeAspectCorrection != (int)RetroModeAspects.Off &&
                Event.current.type == EventType.Repaint &&
                !console.ui.isConsoleOpen)
            {
                GUI.depth = depthPlay;
                if (GameManager.IsGamePaused)
                    GUI.depth = depthPause;

                Texture2D frameTexture = frame43;
                if (DaggerfallUnity.Settings.RetroModeAspectCorrection == (int)RetroModeAspects.SixteenTen)
                    frameTexture = frame1610;

                Vector2 frameSize = new Vector2(Screen.height * 3, Screen.height);
                Vector2 framePosition = new Vector2((Screen.width/2)-(frameSize.x/2),0);
                Rect frameRect = new Rect(framePosition, frameSize);

                DaggerfallUI.DrawTexture(frameRect, frameTexture, ScaleMode.StretchToFill, true, Color.white);
            }
        }
    }
}
