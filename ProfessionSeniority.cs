using GameData.Domains.Taiwu.Profession;
using GameData.Domains;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains.Mod;
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
        }
        //把志向的进度改为最大值
        [HarmonyPrefix, HarmonyPatch(typeof(GameData.Domains.Extra.ExtraDomain), "ChangeProfessionSeniority")]
        public static bool ExtraDomain_ChangeProfessionSeniority(int professionId, ref int baseDelta)
        {
            ProfessionData professionData;
            bool flag = !DomainManager.Extra.TryGetElement_TaiwuProfessions(professionId, out professionData);
            if (!flag)
            {
                
                if (fullPercentage)
                {
                    professionData.Seniority = ProfessionRelatedConstants.MaxSeniority;
                }
                else
                {
                    baseDelta = baseDelta * increaseTimes;
                }
                //AdaptableLog.Info("把id为：" + professionId + "的进度改成了：" + ProfessionRelatedConstants.MaxSeniority);
            }
            return true;
        }
    }
}
