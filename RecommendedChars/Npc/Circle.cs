using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    class CircleNpc : Playtime
    {
        public SpriteRenderer sprite;
        public Sprite sprNormal;
        public Sprite sprSad;

        internal static Character charEnum = (Character)(-1);
    }

    class CircleJumprope : Jumprope
    {
        private const string startKey = "RecChars_JumpRope_Start";
        private const string continueKey = "RecChars_JumpRope_Continue";
        private const string failKey = "RecChars_JumpRope_Fail";

        internal static Dictionary<string, Sprite[]> ropeAnimation;
        public CustomSpriteAnimator ropeAnimator;

        private new void Start()
        {
            base.Start();

            // Stop the RopeTimer routine
            StopAllCoroutines();

            animator.enabled = false;

            ropeDelay = 0f;
            ropeAnimator.PopulateAnimations(ropeAnimation, 15);

            countTmp.text = $"{jumps}/{LocalizationManager.Instance.GetLocalizedText(startKey)}";
            StartCoroutine(CircleRopeTimer());
        }

        private void CircleRopeDown()
        {
            ropeDelay = 0f;

            if (height > jumpBuffer)
            {
                jumps++;

                if (jumps < 10)
                    playtime.Count(jumps);

                countTmp.text = $"{jumps}/{LocalizationManager.Instance.GetLocalizedText(continueKey)}";
                return;
            }

            playtime.ec.MakeNoise(playtime.transform.position, noiseValue);
            jumps = 0;
            ropeDelay = 2f;
            playtime.JumpropeHit();

            totalPoints += penaltyVal;
            if (totalPoints < 20)
                totalPoints = 20;

            countTmp.text = $"{jumps}/{LocalizationManager.Instance.GetLocalizedText(failKey)}";
        }

        private IEnumerator CircleRopeTimer()
        {
            while (jumps < maxJumps)
            {
                float delay = ropeDelay;
                while (delay > 0f)
                {
                    delay -= Time.deltaTime;
                    yield return null;
                }

                ropeAnimator.Play("JumpRope", 1F/ropeTime);
                float hitTime = ropeTime;
                while (hitTime > 0f)
                {
                    hitTime -= Time.deltaTime;
                    yield return null;
                }

                CircleRopeDown();
            }

            while (height > 0f)
            {
                yield return null;
            }

            End(success: true);
        }
    }
}

namespace UncertainLuei.BaldiPlus.RecommendedChars.Patches
{
    [ConditionalPatchConfig(RecommendedCharsPlugin.ModGuid, "Modules", "Circle")]
    [HarmonyPatch]
    class CirclePolymorphismPatches
    {
        [HarmonyPatch(typeof(Playtime), "EndJumprope")]
        [HarmonyPostfix]
        private static void EndJumprope(Playtime __instance, bool won)
        {
            if (__instance.Character != CircleNpc.charEnum) return;

            CircleNpc circle = (CircleNpc)__instance;

            if (!won)
            {
                // Re-disable the animator for good measure
                __instance.animator.enabled = false;
                circle.sprite.sprite = circle.sprSad;
                return;
            }
            // If you win the jump rope game, then his cooldown is added by 200%
            if (circle.behaviorStateMachine.currentState is Playtime_Cooldown cooldown)
                cooldown.time = circle.initialCooldown * 3;
        }

        [HarmonyPatch(typeof(Playtime), "EndCooldown")]
        [HarmonyPostfix]
        private static void EndCooldown(Playtime __instance)
        {
            if (__instance.Character == CircleNpc.charEnum)
            {
                // Re-disable the animator for good measure
                __instance.animator.enabled = false;

                CircleNpc circle = (CircleNpc)__instance;
                circle.sprite.sprite = circle.sprNormal;
            }
        }
    }
}
