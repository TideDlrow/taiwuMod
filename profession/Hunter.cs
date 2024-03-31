using GameData.Domains.Combat;
using GameData.Domains.Mod;
using GameData.Domains.Taiwu.Profession.SkillsData;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace Profession
{
    [PluginConfig("HunterPlugin", "rhonin", "0.4.0")]
    public class HunterPlugin : TaiwuRemakePlugin
    {
        Harmony? harmony;
        //驱使动物攻击次数无限
        static bool animalAttackUnlimited = false;
        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
        public override void OnModSettingUpdate()
        {
            ModDomain modDomain = new ModDomain();
            modDomain.GetSetting(ModIdStr, "animalAttackUnlimited", ref animalAttackUnlimited);
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(HunterPlugin));
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CombatCharacter), nameof(CombatCharacter.GetAnimalAttackCount))]
        public static void CombatCharacter_GetAnimalAttackCount_Post(ref sbyte __result)
        {
            if (animalAttackUnlimited)
            {
                //把获取动物剩余攻击次数的返回值改为每月可驱使的最大值，以达到无限驱使动物的目的
                __result = HunterSkillsData.CarrierAnimalAttackCountPerMonth;
            }
        }
    }
}
