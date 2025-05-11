using HarmonyLib;

using MTM101BaldAPI;

using System.Linq;
using System.Reflection;

using TMPro;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.MathMachineTweaks
{
    [HarmonyPatch(typeof(MathMachine))]
    [HarmonyPatch("Start")]
    public class IntializerPatch
    {
        static void Prefix(ref int ___totalProblems)
        {
            ___totalProblems = Mathf.Max(___totalProblems, Mathf.Min(10, MathMachineTweaksPlugin.config_mathProblemCount.Value));
        }
    }

    [HarmonyPatch(typeof(MathMachine))]
    [HarmonyPatch("NumberDropped")]
    public class FixMultiQuestionBalloons
    {
        static void Prefix(MathMachine __instance, ref bool[] ___playerIsHolding, ref int[] ___playerHolding, int player)
        {
            if (player >= ___playerIsHolding.Length || !___playerIsHolding[player] || player >= ___playerHolding.Length) return;
            if (___playerHolding[player] >= __instance.currentNumbers.Count) return;

            MathMachineNumber num = __instance.currentNumbers[___playerHolding[player]];
            num.trackPlayer = false;
            num.ClickableUnsighted(player);

            num.sprite.gameObject.layer = num.initialSpriteLayer;
        }
    }

    [HarmonyPatch(typeof(MathMachine))]
    [HarmonyPatch("ReInit")]
    public class ChangeTotalTmpInit
    {
        public static void UpdateTotalText(TMP_Text text, int answered, int total)
        {
            // Don't do it if text isn't enabled or the total is less than 2 digits
            if (total < 10 || !text.isActiveAndEnabled) return;

            string newText = "";
            answered.ToString().ToCharArray().Do((x) => newText += $"<sprite={x}>");
            newText = newText + "<sprite=10>";
            total.ToString().ToCharArray().Do((x) => newText += $"<sprite={x}>");

            text.text = newText;
        }

        static void Postfix(TMP_Text ___totalTmp, int ___answeredProblems, int ___totalProblems)
        {
            if (___totalTmp.isActiveAndEnabled)
            {
                // Do not wrap into separate lines
                ___totalTmp.enableWordWrapping = false;
                UpdateTotalText(___totalTmp, ___answeredProblems, ___totalProblems);
            }
        }
    }

    [HarmonyPatch(typeof(MathMachine))]
    [HarmonyPatch("NewProblem")]
    public class MathMachineNewProblem
    {
        static void Prefix(TMP_Text ___totalTmp, int ___answeredProblems, int ___totalProblems)
        {
            ChangeTotalTmpInit.UpdateTotalText(___totalTmp, ___answeredProblems, ___totalProblems);
        }
    }

    [HarmonyPatch(typeof(MathMachine))]
    [HarmonyPatch("Completed")]
    public class MathMachineCompletion
    {
        static int bonusWins = 0;
        static int bonusRequirement = -1;

        static void Prefix(TMP_Text ___totalTmp, int ___answeredProblems, int ___totalProblems)
        {
            ChangeTotalTmpInit.UpdateTotalText(___totalTmp, ___answeredProblems, ___totalProblems);
        }

        static void Postfix(MathMachine __instance, TMP_Text ___val1Text, TMP_Text ___val2Text, TMP_Text ___signText, TMP_Text ___answerText, Notebook ___notebook)
        {
            if (!MathMachineTweaksPlugin.config_mathBonusAward.Value) return;

            if (!__instance.InBonusMode)
            {
                bonusWins = 0;
                bonusRequirement = -1;
                return;
            }

            bonusWins++;
            if (bonusRequirement == -1)
                bonusRequirement = Singleton<BaseGameManager>.Instance.Ec.activities.Where((x) => x is MathMachine).Count() - 1;

            if (bonusRequirement < MathMachineTweaksPlugin.config_mathBonusAwardMinimum.Value) return;

            ___val1Text.text = bonusWins.ToString();

            ___val1Text.enableAutoSizing = true;
            ___val1Text.fontSizeMin = 12;
            ___val1Text.fontSizeMax = ___val1Text.fontSize;

            ___val2Text.text = bonusRequirement.ToString();

            ___val2Text.enableAutoSizing = true;
            ___val2Text.fontSizeMin = 12;
            ___val2Text.fontSizeMax = ___val2Text.fontSize;

            ___signText.text = "/";
            ___answerText.text = "!";

            if (bonusWins >= bonusRequirement)
            {
                // Placeholder code
                Singleton<BaseGameManager>.Instance.Ec.CreateItem(__instance.room,
                    MTM101BaldiDevAPI.itemMetadata.FindByEnum(Items.PortalPoster).value,
                    ___notebook.transform.position);
            }
        }
    }
}
