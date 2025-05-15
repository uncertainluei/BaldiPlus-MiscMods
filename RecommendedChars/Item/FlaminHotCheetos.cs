using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;

using System.Collections;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    class ITM_FlaminHotCheetos : Item
    {
        public SoundObject audEat;
        public Sprite gaugeSprite;

        private HudGauge timerGauge;

        private RaycastHit raycastHit;

        private readonly ValueModifier walkSpeedModifier = new ValueModifier(1.5f);
        private readonly ValueModifier runSpeedModifier = new ValueModifier(1.6f);
        private readonly ValueModifier staminaDropModifier = new ValueModifier(1.5f);
        private readonly ValueModifier staminaRiseModifier = new ValueModifier(0f);

        public override bool Use(PlayerManager pm)
        {
		    CoreGameManager.Instance.audMan.PlaySingle(audEat);
            StartCoroutine(Timer(pm, 15f));
            return true;
        }

        private IEnumerator Timer(PlayerManager pm, float setTime)
        {
            timerGauge = CoreGameManager.Instance.GetHud(pm.playerNumber).gaugeManager.ActivateNewGauge(gaugeSprite, setTime);
            PlayerMovementStatModifier statMod = pm.GetMovementStatModifier();
            statMod.AddModifier("walkSpeed", walkSpeedModifier);
            statMod.AddModifier("runSpeed", runSpeedModifier);
            statMod.AddModifier("staminaDrop", staminaDropModifier);
            statMod.AddModifier("staminaRise", staminaRiseModifier);

            float time = setTime;

            while (time > 0f)
            {
                time -= Time.deltaTime * pm.PlayerTimeScale;
                timerGauge.SetValue(setTime, time);

                if (pm.plm.running && pm.plm.RealVelocity > pm.plm.cc.minMoveDistance && Physics.Raycast(pm.transform.position, pm.transform.forward, out raycastHit, 5f, 2097152, QueryTriggerInteraction.Collide) && raycastHit.transform.CompareTag("Window"))
                    raycastHit.transform.GetComponent<Window>().Break(true);
                yield return null;
            }

            statMod.RemoveModifier(walkSpeedModifier);
            statMod.RemoveModifier(runSpeedModifier);
            statMod.RemoveModifier(staminaDropModifier);
            statMod.RemoveModifier(staminaRiseModifier);
            timerGauge.Deactivate();
            Destroy(gameObject);
            yield break;
        }
    }
}
