using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using BepInEx.Logging;
using Il2CppSystem.Collections.Generic; // 操作游戏原生字典

using LC2;
using LC2.CampData;

namespace LostCastle2Unlocker
{
    [BepInPlugin("lostcastle2.unlockAllSkin", "Lost Castle 2 Unlock All Skin Mod", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public static ManualLogSource? InstanceLog;

        public override void Load()
        {
            InstanceLog = Log;
            InstanceLog.LogInfo("使用方式为自己到哥布林商人那把全部的饰品看一遍然后返回主菜单，就已经保存好了");
            InstanceLog.LogInfo("不要强制退出，这样无法保存数据");
            InstanceLog.LogInfo("失落城堡2【雷达捕捉 + 即时物理灌录】插件正在注入...");

            try
            {
                var harmony = new Harmony("com.peer.lostcastle2.unlocker");

                // 仅拦截百分之百会触发的核心查询函数
                harmony.PatchAll(typeof(GoblinMerchantSkinPatch));

                InstanceLog.LogInfo("注入完成！即时持久化拦截网已全面覆盖。");
            }
            catch (Exception ex)
            {
                InstanceLog.LogError($"注入过程中发生错误: {ex.Message}");
            }
        }

        // ==================== 核心拦截区域 ====================
        [HarmonyPatch(typeof(CampMgr), nameof(CampMgr.GoblinMerchant_GetSkinIsUnlock), new Type[] { typeof(string) })]
        public static class GoblinMerchantSkinPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(string skin, ref bool __result)
            {
                // 1. 强制返回已解锁，确保 UI 能亮起并可随意穿戴
                __result = true;

                // 2. 核心实时灌录逻辑：只要名字有效，就在这个瞬间强行写入持久化存档！
                if (!string.IsNullOrEmpty(skin))
                {
                    // 顺藤摸瓜：通过 CampMgr 的静态原生单例 Instance 直接去抓游戏当前的存档
                    if (CampMgr.Instance != null && CampMgr.Instance.CampSaveData != null)
                    {
                        var dict = CampMgr.Instance.CampSaveData.mGoblinMerchantUnlockedDic;
                        if (dict != null)
                        {
                            // 检查字典里是否已经拥有这个饰品
                            if (!dict.ContainsKey(skin))
                            {
                                dict.Add(skin, 1); // 1 代表已购买/永久拥有
                                Plugin.InstanceLog?.LogWarning($"[物理灌录] 发现新饰品: {skin} -> 已在此时此刻【真正写入】原生存档字典中！");
                            }
                            else if (dict[skin] != 1)
                            {
                                dict[skin] = 1; // 确保数值为 1 代表拥有
                            }
                        }
                    }
                }

                return false; // 拦截原函数，不让游戏执行原版的查账逻辑
            }
        }
    }
}
