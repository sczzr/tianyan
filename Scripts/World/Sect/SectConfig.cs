namespace TianYanShop.World.Sect
{
    /// <summary>
    /// 宗门生成配置
    /// </summary>
    public class SectConfig
    {
        public static SectConfig Default => new SectConfig();

        #region 数量配置

        /// <summary>
        /// 各级宗门数量计算系数（每N格1个）
        /// </summary>
        public int TopSectRatio { get; set; } = 8000;
        public int LargeSectRatio { get; set; } = 2500;
        public int SmallSectRatio { get; set; } = 800;

        #endregion

        #region 距离限制

        /// <summary>
        /// 距离限制（用于中心点距离检查）
        /// </summary>
        public int MinDistanceTopToTop { get; set; } = 50;
        public int MinDistanceLargeToLarge { get; set; } = 25;
        public int MinDistanceLargeToTop { get; set; } = 15;
        public int MinDistanceSmallToAdvanced { get; set; } = 8;

        #endregion

        #region 势力范围

        /// <summary>
        /// 势力范围半径
        /// </summary>
        public (int min, int max) TopInfluenceRange { get; set; } = (25, 40);
        public (int min, int max) LargeInfluenceRange { get; set; } = (6, 12);
        public (int min, int max) SmallInfluenceRange { get; set; } = (1, 3);

        #endregion

        #region 灵力阈值

        /// <summary>
        /// 灵力阈值
        /// </summary>
        public float TopSectSpiritThreshold { get; set; } = 0.85f;
        public float LargeSectSpiritThreshold { get; set; } = 0.7f;
        public float SmallSectSpiritThreshold { get; set; } = 0.5f;

        #endregion

        #region 双专精概率

        /// <summary>
        /// 双专精概率
        /// </summary>
        public float TopDualSpecializationChance { get; set; } = 0.5f;
        public float LargeDualSpecializationChance { get; set; } = 0.2f;

        #endregion

        #region 声望范围

        /// <summary>
        /// 声望范围
        /// </summary>
        public (int min, int max) TopReputationRange { get; set; } = (80, 100);
        public (int min, int max) LargeReputationRange { get; set; } = (50, 80);
        public (int min, int max) SmallReputationRange { get; set; } = (20, 50);

        #endregion

        #region 弟子数量

        /// <summary>
        /// 弟子数量
        /// </summary>
        public (int min, int max) TopMemberRange { get; set; } = (1000, 5000);
        public (int min, int max) LargeMemberRange { get; set; } = (200, 1000);
        public (int min, int max) SmallMemberRange { get; set; } = (20, 200);

        #endregion

        #region 灵石收入

        /// <summary>
        /// 灵石收入
        /// </summary>
        public (int min, int max) TopSpiritStoneIncomeRange { get; set; } = (10000, 50000);
        public (int min, int max) LargeSpiritStoneIncomeRange { get; set; } = (2000, 10000);
        public (int min, int max) SmallSpiritStoneIncomeRange { get; set; } = (100, 2000);

        #endregion
    }
}
