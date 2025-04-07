using BepInEx;
using BepInEx.Configuration;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;

using System.Collections;
using System.IO;
using System.Linq;
using TMPro;

using UnityEngine;
using UnityEngine.Events;

namespace UncertainLuei.BaldiPlus.RestartLevelButton
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    class RestartButtonPlugin : BaseUnityPlugin
    {
        public const string ModName = "Restart Level Button";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.restartlevelbutton";
        public const string ModVersion = "1.0";

        // CONFIGURATION
        internal static ConfigEntry<bool> config_requireConfirm;

        private void Awake()
        {
            config_requireConfirm = Config.Bind(
                "General",
                "RequireConfirmation",
                true,
                "Will display a confirmation screen just like the exit button if true.");

            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Lang_En.json"), Language.English);

            LoadingEvents.RegisterOnAssetsLoaded(Info, CreateRestartButton(), false);
        }

        private IEnumerator CreateRestartButton()
        {
            yield return 1;
            yield return "Modifying pause screen";

            CoreGameManager cgm = Resources.FindObjectsOfTypeAll<CoreGameManager>().First(x => x.GetInstanceID() > 0);
            Transform pause = cgm.pauseScreens[0].transform;

            // Workaround for creating events (unity why do you gotta make persistent events an ass)
            RestartPauseMenu pauseMenuFunction = pause.gameObject.AddComponent<RestartPauseMenu>();

            // Future-proofing
            Transform pauseMain = pause.Find("Main");
            pauseMenuFunction.canvasMain = pauseMain.gameObject;
            StandardMenuButton quitButton = pauseMain.Find("QuitButton").GetComponent<StandardMenuButton>();
            pauseMain.Find("OptionsButton").transform.localPosition = new Vector3(-96f, -64f);

            Transform pauseQuitConfirm = pause.Find("QuitConfirm");
            pauseMenuFunction.canvasConfirm = pauseQuitConfirm.gameObject;
            TMP_Text quitConfirmText = pauseQuitConfirm.GetChild(0).GetComponent<TMP_Text>();
            Destroy(quitConfirmText.GetComponent<TextLocalizer>());
            pauseMenuFunction.confirmText = quitConfirmText;

            StandardMenuButton yesButton = pauseQuitConfirm.GetChild(1).GetComponent<StandardMenuButton>();
            StandardMenuButton restartYesButton = Instantiate(yesButton);
            restartYesButton.transform.SetParent(pauseQuitConfirm.transform, false);

            pauseMenuFunction.confirmYesQuit = yesButton.gameObject;
            pauseMenuFunction.confirmYesRestart = restartYesButton;


            StandardMenuButton restartButton = Instantiate(quitButton);
            restartButton.transform.SetParent(pauseMain, false);
            restartButton.name = "RestartButton";
            restartButton.transform.localPosition = new Vector3(96f, -64f);

            TextLocalizer localizer = restartButton.GetComponent<TextLocalizer>();
            localizer.key = "But_Restart";

            pauseMenuFunction.restartLabel = restartButton.text;
            pauseMenuFunction.restartLabel.text = "Restart";

            pauseMenuFunction.colorDefault = pauseMenuFunction.restartLabel.color;
            pauseMenuFunction.colorDisabled = pauseMenuFunction.colorDefault * Color.gray;

            pauseMenuFunction.restartButton = restartButton;
            pauseMenuFunction.quitButton = quitButton;
            yield break;
        }

        public static void RestartLevel()
        {
            if (CoreGameManager.Instance.paused)
                CoreGameManager.Instance.Pause(false);

            if (CoreGameManager.Instance.lives > 0)
                CoreGameManager.Instance.lives--;
            else
                CoreGameManager.Instance.extraLives--;

            BaseGameManager.Instance.RestartLevel();
        }
    }

    // Persistent calls are awful to create at runtime so this is the best I've come up with
    class RestartPauseMenu : MonoBehaviour
    {
        public GameObject canvasMain;
        public GameObject canvasConfirm;

        public GameObject confirmYesQuit;
        public StandardMenuButton confirmYesRestart;
        public TMP_Text confirmText;

        public StandardMenuButton quitButton;

        public StandardMenuButton restartButton;
        public TMP_Text restartLabel;
        private bool restartActive;

        public Color colorDefault;
        public Color colorDisabled;

        private Vector2 confirmQuitSize = new Vector2(200f, 50f);
        private Vector2 confirmRestartSize = new Vector2(320f, 100f);

        void Start()
        {
            confirmYesRestart.OnPress = new UnityEvent();
            confirmYesRestart.OnPress.AddListener(() => RestartButtonPlugin.RestartLevel());

            restartButton.OnPress = new UnityEvent();
            restartButton.OnPress.AddListener(() =>
            {
                if (!restartActive) return;

                if (!RestartButtonPlugin.config_requireConfirm.Value)
                {
                    RestartButtonPlugin.RestartLevel();
                    return;
                }

                canvasConfirm.SetActive(true);
                canvasMain.SetActive(false);

                confirmYesRestart.gameObject.SetActive(true);
                confirmYesQuit.SetActive(false);
                confirmText.text = LocalizationManager.Instance.GetLocalizedText("Men_RestartConfirm");
                confirmText.rectTransform.sizeDelta = confirmRestartSize;
            });

            quitButton.OnPress.AddListener(() =>
            {
                confirmYesRestart.gameObject.SetActive(false);
                confirmYesQuit.SetActive(true);
                confirmText.text = LocalizationManager.Instance.GetLocalizedText("Men_QuitConfirm");
                confirmText.rectTransform.sizeDelta = confirmQuitSize;
            });
        }

        void OnEnable()
        {
            restartActive = false;

            // If Hide'n'Seek (not Explorer Mode) and it isn't the player's last power tube
            if (BaseGameManager.Instance is MainGameManager &&
                CoreGameManager.Instance.currentMode == Mode.Main &&
                CoreGameManager.Instance.lives > 0 || CoreGameManager.Instance.extraLives > 0)
            {
                // Activate restart button
                restartActive = true;
                restartLabel.color = colorDefault;
                restartButton.transitionOnPress = true;
                return;
            }

            restartButton.transitionOnPress = false;
            restartLabel.color = colorDisabled;
        }
    }
}