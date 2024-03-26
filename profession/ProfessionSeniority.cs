using GameData.Domains.Taiwu.Profession;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.Mod;
using GameData.Domains.Extra;
using GameData.Utilities;
namespace Profession
{
    [PluginConfig("ProfessionSeniorityBackend", "rhonin", "0.1.0")]
    public class ProfessionSeniorityBackendPlugin : TaiwuRemakePlugin
    {
        Harmony? harmony;
        //是否直接满进度
        static bool fullPercentage = true;
        //增长的倍数
        static int increaseTimes = 1;
        //无冷却
        static bool noCoolTime = false;
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
            //AdaptableLog.Info("这是一个后端mod,Hello 太吾 backend");
        }
        public override void OnModSettingUpdate()
        {
            ModDomain modDomain = new ModDomain();
            modDomain.GetSetting(ModIdStr, "fullPercentage", ref fullPercentage);
            modDomain.GetSetting(ModIdStr, "increaseTimes", ref increaseTimes);
            modDomain.GetSetting(ModIdStr, "NoCoolTime", ref noCoolTime);
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
    }
}
