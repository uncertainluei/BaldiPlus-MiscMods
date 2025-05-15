using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;

namespace UncertainLuei.BaldiPlus.RecommendedChars.Patches
{
    [ConditionalPatchConfig(RecommendedCharsPlugin.ModGuid, "Modules", "CaAprilFools")]
    [HarmonyPatch(typeof(Baldi), "CaughtPlayer")]
    class BaldiTakeUltimateApplePatch
    {
        private static bool Prefix(Baldi __instance, PlayerManager player)
        {
            if (player.itm.Has(Baldi_UltimateApple.ultiAppleEnum))
            {
                player.itm.Remove(Baldi_UltimateApple.ultiAppleEnum);
                __instance.behaviorStateMachine.ChangeState(new Baldi_UltimateApple(__instance, __instance.behaviorStateMachine.CurrentState));
                __instance.StopAllCoroutines();
                __instance.navigator.SetSpeed(0f);
                __instance.audMan.FlushQueue(true);
                __instance.audMan.PlaySingle(__instance.audAppleThanks);
                __instance.volumeAnimator.enabled = false;
                return false;
            }
            return true;
        }
    }
}

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public class Baldi_UltimateApple : Baldi_Apple
    {
        public static Sprite[] ultiAppleSprites;
        public static Items ultiAppleEnum;

        private bool loopAnim = false;
        private bool frame = false;
        private float frameTime = 1/60f;

        public Baldi_UltimateApple(Baldi baldi, NpcState previous) : base(baldi,baldi,previous)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            time = 60f;
        }

        public override void Enter()
        {
            base.Enter();
            baldi.spriteRenderer[0].sprite = ultiAppleSprites[0];
            baldi.animator.enabled = false;
        }

        public override void Exit()
        {
            base.Exit();
            baldi.animator.enabled = true;
            baldi.GetExtraAnger(12);
        }

        public override void Update()
        {
            base.Update();

            if (!loopAnim)
            {
                if (!baldi.audMan.AnyAudioIsPlaying)
                    loopAnim = true;

                return;
            }

            frameTime -= Time.deltaTime;
            if (frameTime <= 0f)
            {
                frameTime = 1/60f;
                frame = !frame;
                baldi.spriteRenderer[0].sprite = ultiAppleSprites[frame?1:0];
                if (frame)
                    baldi.EatSound();
            }
        }
    }
}
