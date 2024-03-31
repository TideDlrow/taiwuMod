using GameData.Domains.Taiwu.Profession;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.Mod;
using GameData.Domains.Extra;
using GameData.Utilities;
using GameData.Domains.Taiwu.Profession.SkillsData;
using GameData.Domains.Combat;
using Config;
using GameData.Common;
using System.Reflection.Emit;
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
        //驱使动物攻击次数无限
        static bool animalAttackUnlimited = false;
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
            modDomain.GetSetting(ModIdStr, "animalAttackUnlimited", ref animalAttackUnlimited);

            AdaptableLog.Info($"当前值：满进度flag:{fullPercentage},增长倍数increaseTimes:{increaseTimes},切换无冷却noCoolTime:{noCoolTime},技能无冷却professionSkillNoCooldown:{professionSkillNoCooldown},动物攻击次数不限flag:{animalAttackUnlimited}");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ExtraDomain), "ChangeProfessionSeniority")]
        public static bool ExtraDomain_ChangeProfessionSeniority(int professionId, ref int baseDelta)
        {
            AdaptableLog.Info($"执行了改变进度的方法，full值为{fullPercentage}，times值为:{increaseTimes}");
            ProfessionData professionData;
            bool flag = !DomainManager.Extra.TryGetElement_TaiwuProfessions(professionId, out professionData);
            if (!flag)
            {

                if (fullPercentage)
                {
                    //把志向的进度改为最大值
                    //professionData.Seniority = ProfessionRelatedConstants.MaxSeniority;
                    baseDelta = ProfessionRelatedConstants.MaxSeniority;
                }
                else
                {
                    //把志向增长速度改为指定倍数
                    baseDelta = baseDelta * increaseTimes;
                }
            }
            return true;
        }
        //[HarmonyPostfix, HarmonyPatch(typeof(ExtraDomain), nameof(ExtraDomain.ChangeProfession))]
        //public static void ExtraDomain_ChangeProfession_Post(Dictionary<int, ProfessionData> ____taiwuProfessions)
        //{
        //    if (noCoolTime)
        //    {
        //        //把所有的志向冷却时间改为0
        //        foreach (KeyValuePair<int, ProfessionData> keyValuePair in ____taiwuProfessions)
        //        {
        //            ProfessionData professionData = keyValuePair.Value;
        //            professionData.ProfessionOffCooldownDate = 0;
        //        }
        //    }
        //}
        [HarmonyPrefix, HarmonyPatch(typeof(ProfessionData), nameof(ProfessionData.OfflineSkillCooldown))]
        public static bool ProfessionData_OfflineSkillCooldown_Pre()
        {
            AdaptableLog.Info($"执行了使用志向技能无冷却的方法，flag值为{professionSkillNoCooldown}");
            //直接跳过加冷却时间的方法，以达到技能无冷却的目的
            return !professionSkillNoCooldown;
        }
        [HarmonyPatch(typeof(ExtraDomain), nameof(ExtraDomain.ChangeProfession))]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            AdaptableLog.Info($"执行了使用切换志向无冷却的方法，flag值为{noCoolTime}");

            if (!noCoolTime)
            {
                return instructions;
            }
            //把切换志向+3,+6,+12个月的冷却时间，改为0，以实现无冷却
            List<CodeInstruction> result = new List<CodeInstruction>();
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_3 || instruction.opcode == OpCodes.Ldc_I4_6)
                {
                    // 创建新的指令，将操作数替换为0  
                    instruction.opcode = OpCodes.Ldc_I4_0; // ldc.i4.0 加载整数0到栈上  
                    instruction.operand = null; // 对于 ldc.i4.0，operand 不需要设置，因为它是固定的0  
                }

                // 检查是否是 ldc.i4.s 指令  
                if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand != null)
                {
                    // 检查操作数是否是我们要替换的特定值  
                    byte operand = (byte)instruction.operand;
                    if (operand == 12)
                    {
                        instruction.opcode = OpCodes.Ldc_I4_0;
                        instruction.operand = null;
                    }
                }
                // 将原始或修改后的指令添加到新列表中  
                result.Add(instruction);
            }
            return result.AsEnumerable();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CombatCharacter), nameof(CombatCharacter.GetAnimalAttackCount))]
        public static void CombatCharacter_GetAnimalAttackCount_Post(ref sbyte __result)
        {
            AdaptableLog.Info($"执行了驱使动物不限的方法，flag值为{animalAttackUnlimited}");

            if (animalAttackUnlimited)
            {
                //把获取动物剩余攻击次数的返回值改为每月可驱使的最大值，以达到无限驱使动物的目的
                __result = HunterSkillsData.CarrierAnimalAttackCountPerMonth;
            }
        }
    }
}
