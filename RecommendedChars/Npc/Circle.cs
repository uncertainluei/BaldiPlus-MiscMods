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
            StartCoroutine(RopeTimerNew());
        }

        private void RopeDownNew()
        {
            ropeDelay = 0f;

            if (height > jumpBuffer)
            {
                jumps++;

                if (jumps < 10)
                    playtime.Count(jumps);

                countTmp.text = $"{jumps}/{LocalizationManager.Instance.GetLocalizedText(continueKey)}";
            }
            else
            {
                playtime.ec.MakeNoise(playtime.transform.position, noiseValue);
                jumps = 0;
                ropeDelay = 2f;
                playtime.JumpropeHit();

                totalPoints += penaltyVal;
                if (totalPoints < 0)
                {
                    totalPoints = 0;
                }

                countTmp.text = $"{jumps}/{LocalizationManager.Instance.GetLocalizedText(failKey)}";
            }
        }

        private IEnumerator RopeTimerNew()
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

                RopeDownNew();
            }

            while (height > 0f)
            {
                yield return null;
            }

            End(success: true);
        }
    }
}
