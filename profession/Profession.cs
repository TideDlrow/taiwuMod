﻿using GameData.Domains.Taiwu.Profession;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.Mod;
using GameData.Domains.Extra;
using GameData.Utilities;
using GameData.Domains.Taiwu.Profession.SkillsData;
using GameData.Domains.Combat;
namespace Profession
{
    [PluginConfig("ProfessionBackend", "rhonin", "0.4.0")]
    public class ProfessionSeniorityBackendPlugin : TaiwuRemakePlugin
    {
        Harmony? harmony;
        //是否直接满进度
        static bool fullPercentage = true;
        //增长的倍数
        static int increaseTimes = 1;
        //切换无冷却
        static bool noCoolTime = false;
        //使用技能无冷却
        static bool professionSkillNoCooldown = false;
        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(ProfessionSeniorityBackendPlugin));
        }
        public override void OnModSettingUpdate()
        {
            ModDomain modDomain = new ModDomain();
            modDomain.GetSetting(ModIdStr, "fullPercentage", ref fullPercentage);
            modDomain.GetSetting(ModIdStr, "increaseTimes", ref increaseTimes);
            modDomain.GetSetting(ModIdStr, "NoCoolTime", ref noCoolTime);
            modDomain.GetSetting(ModIdStr, "professionSkillNoCooldown", ref professionSkillNoCooldown);
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(ExtraDomain), "ChangeProfessionSeniority")]
        public static bool ExtraDomain_ChangeProfessionSeniority(int professionId, ref int baseDelta)
        {
            ProfessionData professionData;
            bool flag = !DomainManager.Extra.TryGetElement_TaiwuProfessions(professionId, out professionData);
            if (!flag)
            {

                if (fullPercentage)
                {
                    //把志向的进度改为最大值
                    professionData.Seniority = ProfessionRelatedConstants.MaxSeniority;
                }
                else
                {
                    //把志向增长速度改为指定倍数
                    baseDelta = baseDelta * increaseTimes;
                }
                //AdaptableLog.Info("把id为：" + professionId + "的进度改成了：" + ProfessionRelatedConstants.MaxSeniority);
            }
            return true;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ExtraDomain), nameof(ExtraDomain.ChangeProfession))]
        public static void ExtraDomain_ChangeProfession_Post(Dictionary<int, ProfessionData> ____taiwuProfessions)
        {
            if (noCoolTime)
            {
                //把所有的志向冷却时间改为0
                foreach (KeyValuePair<int, ProfessionData> keyValuePair in ____taiwuProfessions)
                {
                    ProfessionData professionData = keyValuePair.Value;
                    professionData.ProfessionOffCooldownDate = 0;
                }
            }
        }
        [HarmonyPrefix,HarmonyPatch(typeof(ProfessionData),nameof(ProfessionData.OfflineSkillCooldown))]
        public static bool ProfessionData_OfflineSkillCooldown_Pre()
        {
            //直接跳过加冷却时间的方法，以达到技能无冷却的目的
            return !professionSkillNoCooldown;
        }
    }
}