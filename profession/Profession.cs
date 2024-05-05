using GameData.Domains.Taiwu.Profession;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.Mod;
using GameData.Domains.Extra;
using GameData.Utilities;
using GameData.Domains.Taiwu.Profession.SkillsData;
using GameData.Domains.Combat;
using System.Reflection.Emit;
using Config;
using GameData.Common;
using System;
using System.Security.Cryptography;
using System.Linq;
namespace Profession
{
    [PluginConfig("ProfessionBackend", "rhonin", "0.5.0")]
    public class ProfessionSeniorityBackendPlugin : TaiwuRemakePlugin
    {
        Harmony? harmony;
        //是否直接满进度
        static bool fullPercentage = true;
        //增长的倍数
        static int increaseTimes = 1;
        //切换无冷却
        //static bool noCoolTime = false;
        //使用技能无冷却
        static bool professionSkillNoCooldown = false;
        //驱使动物攻击次数无限
        static bool animalAttackUnlimited = false;
        //云游道易天改命优先交换负面特质
        static bool travelingTaoistMonk = false;
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
            //modDomain.GetSetting(ModIdStr, "NoCoolTime", ref noCoolTime);
            modDomain.GetSetting(ModIdStr, "professionSkillNoCooldown", ref professionSkillNoCooldown);
            modDomain.GetSetting(ModIdStr, "animalAttackUnlimited", ref animalAttackUnlimited);
            modDomain.GetSetting(ModIdStr, "travelingTaoistMonk", ref travelingTaoistMonk);

            //AdaptableLog.Info($"志向修改MOD--当前值：满进度flag:{fullPercentage},增长倍数increaseTimes:{increaseTimes},技能无冷却professionSkillNoCooldown:{professionSkillNoCooldown},动物攻击次数不限flag:{animalAttackUnlimited}");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ExtraDomain), "ChangeProfessionSeniority")]
        public static bool ExtraDomain_ChangeProfessionSeniority(int professionId, ref int baseDelta)
        {
            //AdaptableLog.Info($"志向修改MOD--执行了改变进度的方法，full值为{fullPercentage}，times值为:{increaseTimes}");
            ProfessionData professionData;
            bool flag = !DomainManager.Extra.TryGetElement_TaiwuProfessions(professionId, out professionData);
            if (!flag)
            {

                if (fullPercentage)
                {
                    //AdaptableLog.Info("志向修改MOD--把进度改为了最大值");
                    //把志向的进度改为最大值
                    //professionData.Seniority = ProfessionRelatedConstants.MaxSeniority;
                    baseDelta = ProfessionRelatedConstants.MaxSeniority;
                }
                else
                {

                    //AdaptableLog.Info("志向修改MOD--把进度按倍数进行了修改");
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
            //AdaptableLog.Info($"志向修改MOD--执行了使用志向技能无冷却的方法，flag值为{professionSkillNoCooldown}");
            //直接跳过加冷却时间的方法，以达到技能无冷却的目的
            return !professionSkillNoCooldown;
        }
        [HarmonyPatch(typeof(ExtraDomain), nameof(ExtraDomain.ChangeProfession))]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ////AdaptableLog.Info($"执行了使用切换志向无冷却的方法，flag值为{noCoolTime}");
            //AdaptableLog.Info($"志向修改MOD--对切换志向的函数进行替换，以实现切换无冷却");
            //if (!noCoolTime)
            //{
            //    return instructions;
            //}
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
                    sbyte operand = (sbyte)instruction.operand;
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
            //AdaptableLog.Info($"志向修改MOD--执行了驱使动物不限的方法，flag值为{animalAttackUnlimited}");

            if (animalAttackUnlimited)
            {
                //AdaptableLog.Info("志向修改MOD--驱使动物次数改为了3");
                //把获取动物剩余攻击次数的返回值改为每月可驱使的最大值，以达到无限驱使动物的目的
                __result = HunterSkillsData.CarrierAnimalAttackCountPerMonth;
            }
        }
        //[HarmonyPrefix, HarmonyPatch(typeof(TravelingBuddhistMonkSkillsData), nameof(TravelingBuddhistMonkSkillsData.OfflineCreateTemple))]
        //public static bool TravelingBuddhistMonkSkills_StateHasTemple_Pre(bool[] ____stateTempleVisited, Location[] ____stateTempleLocation, Location location)
        //{
        //    //AdaptableLog.Info($"志向修改MOD--区域是否存在寺庙,参数location:{location}");
        //    sbyte stateId = DomainManager.Map.GetStateIdByAreaId(location.AreaId);
        //    //AdaptableLog.Info($"志向修改MOD--stateId:{stateId}");
        //    string visitStr = string.Join(" ,", ____stateTempleVisited);
        //    //AdaptableLog.Info($"志向修改MOD--{visitStr}");
        //    string tempLocations = string.Join(" ,", ____stateTempleLocation);
        //    //AdaptableLog.Info($"志向修改MOD--{tempLocations}");
        //    return true;
        //}
        [HarmonyPrefix, HarmonyPatch(typeof(ProfessionSkillHandle), nameof(ProfessionSkillHandle.TravelingTaoistMonkSkill_ReallocateFeatures))]
        public static bool ProfessionSkillHandle_TravelingTaoistMonkSkill_ReallocateFeatures_Pre(DataContext context, GameData.Domains.Character.Character targetChar)
        {
            if (!travelingTaoistMonk)
            {
                return true;
            }
            GameData.Domains.Character.Character taiwuChar = DomainManager.Taiwu.GetTaiwu();
            List<short> taiwuFeatureIds = taiwuChar.GetFeatureIds();
            int taiwuFeatureCount = taiwuFeatureIds.Count;
            List<short> badFeatureIds = new List<short>();
            for (int index = taiwuFeatureCount - 1; index >= 0; index--)
            {
                short featureId = taiwuFeatureIds[index];
                CharacterFeatureItem featureCfg = CharacterFeature.Instance[featureId];
                //寻找可以交换的负面特性
                if (featureCfg.CanBeExchanged && featureCfg.Level < 0)
                {
                    badFeatureIds.Add(featureId);
                    //AdaptableLog.Info($"可交换的负面特性有：{featureCfg.Name}-{featureId}");
                }
            }

            if (badFeatureIds.Count <= 0)
            {
                //如果不存在可交换的负面特性，则随机交换，即执行原本的交换特性方法
                return true;
            }

            List<short> goodFeatureIds = new List<short>();
            List<short> targetFeatureIds = targetChar.GetFeatureIds();
            for (int index = targetFeatureIds.Count - 1; index >= 0; index--)
            {
                short featureId = targetFeatureIds[index];
                CharacterFeatureItem featureCfg = CharacterFeature.Instance[featureId];
                //寻找可以交换的正面特性
                if (featureCfg.CanBeExchanged && featureCfg.Level > 0)
                {
                    
                    goodFeatureIds.Add(featureId);
                    //AdaptableLog.Info($"可交换的正面特性有：{featureCfg.Name}-{featureId}");
                }
            }
            if(goodFeatureIds.Count <= 0)
            {
                return true;
            }

            HashSet<short> idAndMutexId = new HashSet<short>();
            foreach (short taiwuFeatureId in taiwuFeatureIds)
            {
                CharacterFeatureItem feature = CharacterFeature.Instance[taiwuFeatureId];
                idAndMutexId.Add(feature.MutexGroupId);
                //AdaptableLog.Info($"{feature.Name} 的互斥ID组为{feature.MutexGroupId}");
            }
            //AdaptableLog.Info($"1互斥的ID有：{string.Join(",",idAndMutexId.ToList())}");
            foreach (short bFId in badFeatureIds)
            {
                //从太吾的负面特性中挑一个
                //1.判断目标身上的正面特性是否和太吾身上除featureId外的特性冲突，如果存在不冲突的特性，则该正面特性直接和featureId交换
                //判断是否冲突的方法：
                //（1）把太吾身上除featureId外的特性Id和对应的MutexId放到Set中
                //（2）依次判断gooFeatureIds里是否有Set中的Id，有则表示冲突，没有则不冲突
                
                CharacterFeatureItem feature = CharacterFeature.Instance[bFId];
                idAndMutexId.Remove(feature.MutexGroupId);
                foreach(short gFId in goodFeatureIds)
                {
                    //AdaptableLog.Info($"2互斥的ID有：{string.Join(",", idAndMutexId.ToList())}");
                    CharacterFeatureItem gFeature = CharacterFeature.Instance[gFId];
                    //AdaptableLog.Info($"判断是否冲突的id:{gFeature.MutexGroupId}");
                    if (!idAndMutexId.Contains(gFeature.MutexGroupId))
                    {

                        taiwuChar.RemoveFeature(context, bFId);
                        targetChar.RemoveFeature(context, gFId);

                        taiwuChar.AddFeature(context, gFId);
                        targetChar.AddFeature(context,bFId);
                        //AdaptableLog.Info($"把太吾身上的--{feature.Name}和目标身上的--{gFeature.Name}进行了交换");
                        return false;
                    }
                }
                idAndMutexId.Add(feature.MutexGroupId);

                //2.如果所有正面特性都与太吾身上的特性冲突，则继续下一个循环


            }
            //AdaptableLog.Info($"没找到可交换的特性，进行随机交换");
            //如果无法将太吾的坏特性和目标的好特性进行交换，则随机交换
            return true;


        }
        public static void printfeatureGroups(Dictionary<short, List<short>> featureGroups)
        {
            string featureGroupsString = string.Join("。", featureGroups.Select(key =>
            {
                CharacterFeatureItem feature1 = CharacterFeature.Instance[key.Key];
                string ff = string.Join(",", key.Value.Select(value =>
                {
                    CharacterFeatureItem feature2 = CharacterFeature.Instance[value];
                    return $"{feature2.Name}({feature2.Level})";
                }));
                return $"{feature1.Name}({feature1.Level})--{ff}";
            }));
            //AdaptableLog.Info(featureGroupsString);
        }
        //[HarmonyPrefix, HarmonyPatch(typeof(ProfessionSkillHandle), nameof(ProfessionSkillHandle.TravelingTaoistMonkSkill_ReallocateFeatures))]
        public static bool ProfessionSkillHandle_TravelingTaoistMonkSkill_ReallocateFeatures_Pre1(DataContext context, GameData.Domains.Character.Character targetChar)
        {
            GameData.Domains.Character.Character taiwuChar = DomainManager.Taiwu.GetTaiwu();
            List<short> featureIds = new List<short>();
            Dictionary<short, List<short>> featureGroups = new Dictionary<short, List<short>>();
            List<short> taiwuFeatureIds = taiwuChar.GetFeatureIds();
            int taiwuFeatureCount = taiwuFeatureIds.Count;
            for (int index = taiwuFeatureCount - 1; index >= 0; index--)
            {
                short featureId = taiwuFeatureIds[index];
                CharacterFeatureItem featureCfg = CharacterFeature.Instance[featureId];
                //AdaptableLog.Info($"云游道交换特质Log：太无--index:{index},name:{featureCfg.Name},id:{featureId},level:{featureCfg.Level}");
                if (featureCfg.CanBeExchanged)
                {
                    //AdaptableLog.Info($"云游道交换特质Log：id:{featureId}可被交换");
                    featureIds.Add(featureId);
                    taiwuFeatureIds.RemoveAt(index);
                    bool flag2 = !featureGroups.ContainsKey(featureCfg.MutexGroupId);
                    if (flag2)
                    {
                        featureGroups.Add(featureCfg.MutexGroupId, new List<short>());
                    }
                    featureGroups[featureCfg.MutexGroupId].Add(featureId);
                }
            }
            taiwuChar.SetFeatureIds(taiwuFeatureIds, context);

            //string featureGroupsString = string.Join("。", featureGroups.Select(key => $"{key.Key}--{string.Join(",", key.Value)}"));
            ////AdaptableLog.Info($"taiwu - featureGroups:{featureGroupsString}");
            printfeatureGroups(featureGroups);
            List<short> targetFeatureIds = targetChar.GetFeatureIds();
            int targetFeatureCount = targetFeatureIds.Count;
            for (int index2 = targetFeatureCount - 1; index2 >= 0; index2--)
            {
                short featureId2 = targetFeatureIds[index2];
                CharacterFeatureItem featureCfg2 = CharacterFeature.Instance[featureId2];
                //AdaptableLog.Info($"云游道交换特质Log：目标--index:{index2},name:{featureCfg2.Name},id:{featureId2},level:{featureCfg2.Level}");
                if (featureCfg2.CanBeExchanged)
                {
                    //AdaptableLog.Info($"云游道交换特质Log：id:{featureId2}可被交换");
                    featureIds.Add(featureId2);
                    targetFeatureIds.RemoveAt(index2);
                    bool flag4 = !featureGroups.ContainsKey(featureCfg2.MutexGroupId);
                    if (flag4)
                    {
                        featureGroups.Add(featureCfg2.MutexGroupId, new List<short>());
                    }
                    featureGroups[featureCfg2.MutexGroupId].Add(featureId2);
                }
            }
            //string featureGroupsString2 = string.Join("。", featureGroups.Select(key => $"{key.Key}--{string.Join(",", key.Value)}"));
            ////AdaptableLog.Info($"target2 - featureGroups:{featureGroupsString2}");
            printfeatureGroups(featureGroups);
            //AdaptableLog.Info($"featureIds--{string.Join(",", featureIds)}");

            targetChar.SetFeatureIds(targetFeatureIds, context);
            bool taiwuFeatureAdded = false;
            bool targetFeatureAdded = false;
            foreach (KeyValuePair<short, List<short>> pair in featureGroups)
            {
                //AdaptableLog.Info($"key:{pair.Key}--value:{string.Join(",", pair.Value)}");
                bool flag5 = pair.Value.Count <= 1;
                if (!flag5)
                {
                    bool flag6 = context.Random.NextBool();
                    if (flag6)
                    {
                        //AdaptableLog.Info("flag6为true");
                        taiwuChar.AddFeature(context, pair.Value[0], false);
                        targetChar.AddFeature(context, pair.Value[1], false);
                    }
                    else
                    {
                        taiwuChar.AddFeature(context, pair.Value[1], false);
                        targetChar.AddFeature(context, pair.Value[0], false);
                    }
                    taiwuFeatureAdded = true;
                    targetFeatureAdded = true;
                    featureIds.Remove(pair.Value[0]);
                    featureIds.Remove(pair.Value[1]);
                }
            }
            CollectionUtils.Shuffle<short>(context.Random, featureIds);
            //AdaptableLog.Info($"featureIds--{string.Join(",", featureIds)}");
            foreach (short featureId3 in featureIds)
            {
                bool flag7 = taiwuFeatureIds.Count < taiwuFeatureCount && taiwuChar.AddFeature(context, featureId3, false);
                if (flag7)
                {
                    taiwuFeatureAdded = true;
                }
                else
                {
                    bool flag8 = targetFeatureIds.Count < targetFeatureCount && targetChar.AddFeature(context, featureId3, false);
                    if (flag8)
                    {
                        targetFeatureAdded = true;
                    }
                }
            }
            bool flag9 = !taiwuFeatureAdded;
            if (flag9)
            {
                DomainManager.Extra.GenerateCharTeammateCommands(context, taiwuChar.GetId(), false);
            }
            bool flag10 = !targetFeatureAdded;
            if (flag10)
            {
                DomainManager.Extra.GenerateCharTeammateCommands(context, targetChar.GetId(), false);
            }
            return false;
        }
    }
}
