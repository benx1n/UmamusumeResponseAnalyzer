﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmamusumeResponseAnalyzer.Entities
{
    public class SkillData
    {
        /// <summary>
        /// 此技能的上位技能,不计算hint及适性,需调用Apply单独应用
        /// </summary>
        public SkillData Superior => Database.Skills[(GroupId, Rarity, Rate + 1)] ?? Database.Skills[(GroupId, Rarity + 1, Rate + 1)];
        /// <summary>
        /// 此技能的下位技能,不计算hint及适性,需调用Apply单独应用
        /// </summary>
        public SkillData Inferior => Database.Skills[(GroupId, Rarity, Rate - 1)] ?? Database.Skills[(GroupId, Rarity - 1, Rate - 1)];
        /// <summary>
        /// backing field
        /// </summary>
        private int _totalCost = 0;
        /// <summary>
        /// 学会该技能所需的总技能点（包括下位技能，不计算折扣）
        /// </summary>
        public int TotalCost
        {
            get
            {
                if (_totalCost != 0) return _totalCost;
                var total = Cost;
                var inferior = Inferior;
                while (inferior != null && inferior.Rate > 0)
                {
                    total += inferior.Cost;
                    inferior = inferior.Inferior;
                }
                return total;
            }
            set => _totalCost = value;
        }
        public string Name;
        public int Id;
        public int GroupId;
        public int Rarity;
        public int Rate;
        public int Grade;
        public int Cost;
        public int DisplayOrder;
        public GroundType Ground;
        public DistanceType Distance;
        public StyleType Style;

        /// <summary>
        /// 根据马的属性应用折扣及相性加成
        /// </summary>
        /// <param name="chara_info">@event.data.chara_info</param>
        /// <param name="level">该技能的折扣等级</param>
        /// <returns></returns>
        public SkillData Apply(Gallop.SingleModeChara chara_info, int level = int.MinValue)
        {
            if (level == int.MinValue)
            {
                level = chara_info.skill_tips_array.FirstOrDefault(x => x.group_id == GroupId && x.rarity == Rarity)?.level ?? 0;
            }
            var instance = Clone();
            instance.ApplyHint(chara_info, level);
            instance.ApplyProper(chara_info);
            return instance;
        }
        private void ApplyHint(Gallop.SingleModeChara chara_info, int level)
        {
            var cutted = chara_info.chara_effect_id_array.Contains(7) ? 10 : 0; //切者
            var off = level switch //打折等级
            {
                0 => 0,
                1 => 10,
                2 => 20,
                3 => 30,
                4 => 35,
                5 => 40
            };
            Cost = Cost * (100 - off - cutted) / 100;
        }
        private void ApplyProper(Gallop.SingleModeChara chara_info)
        {
            switch (Ground)
            {
                case GroundType.Dirt:
                    Grade = applyProperLevel(Grade, chara_info.proper_ground_dirt);
                    break;
                case GroundType.Turf:
                    Grade = applyProperLevel(Grade, chara_info.proper_ground_turf);
                    break;
            }
            switch (Distance)
            {
                case DistanceType.Short:
                    Grade = applyProperLevel(Grade, chara_info.proper_distance_short);
                    break;
                case DistanceType.Mile:
                    Grade = applyProperLevel(Grade, chara_info.proper_distance_mile);
                    break;
                case DistanceType.Middle:
                    Grade = applyProperLevel(Grade, chara_info.proper_distance_middle);
                    break;
                case DistanceType.Long:
                    Grade = applyProperLevel(Grade, chara_info.proper_distance_long);
                    break;
            }
            switch (Style)
            {
                case StyleType.Nige:
                    Grade = applyProperLevel(Grade, chara_info.proper_running_style_nige);
                    break;
                case StyleType.Senko:
                    Grade = applyProperLevel(Grade, chara_info.proper_running_style_senko);
                    break;
                case StyleType.Sashi:
                    Grade = applyProperLevel(Grade, chara_info.proper_running_style_sashi);
                    break;
                case StyleType.Oikomi:
                    Grade = applyProperLevel(Grade, chara_info.proper_running_style_oikomi);
                    break;
            }

            static int applyProperLevel(int grade, int level) => level switch
            {
                8 or 7 => (int)Math.Round(grade * 1.1), //S,A
                6 or 5 => (int)Math.Round(grade * 0.9), //B,C
                4 or 3 or 2 => (int)Math.Round(grade * 0.8), //D,E,F
                1 => (int)Math.Round(grade * 0.7), //G
                _ => 0,
            };

        }
        public (int GroupId, int Rarity, int Rate) Deconstruction() => (GroupId, Rarity, Rate);
        public SkillData Clone()
            => new()
            {
                Name = Name,
                Id = Id,
                GroupId = GroupId,
                Rarity = Rarity,
                Rate = Rate,
                Grade = Grade,
                Cost = Cost,
                DisplayOrder = DisplayOrder,
                Ground = Ground,
                Distance = Distance,
                Style = Style
            };
        public enum GroundType
        {
            None,
            /// <summary>
            /// 芝
            /// </summary>
            Turf,
            /// <summary>
            /// 泥
            /// </summary>
            Dirt
        }
        public enum DistanceType
        {
            None,
            /// <summary>
            /// 短
            /// </summary>
            Short,
            /// <summary>
            /// 英
            /// </summary>
            Mile,
            /// <summary>
            /// 中
            /// </summary>
            Middle,
            /// <summary>
            /// 长
            /// </summary>
            Long
        }
        public enum StyleType
        {
            None,
            /// <summary>
            /// 逃
            /// </summary>
            Nige,
            /// <summary>
            /// 先
            /// </summary>
            Senko,
            /// <summary>
            /// 差
            /// </summary>
            Sashi,
            /// <summary>
            /// 追
            /// </summary>
            Oikomi
        }
    }
    public class TalentSkillData
    {
        public int SkillId { get; set; }
        public int Rank { get; set; }
    }
}
