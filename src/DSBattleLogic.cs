using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DistinguishedService;

internal class DSBattleLogic : MissionLogic
{
	public DSBattleLogic()
	{
		if (PromotionManager.__instance != null)
		{
			PromotionManager.__instance.nominations = new List<CharacterObject>();
			PromotionManager.__instance.killcounts = new List<int>();
		}
	}

	private int GetTotalKillCount()
	{
		int num = 0;
		foreach (Agent activeAgent in Mission.Current.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsHero)
			{
				num += activeAgent.KillCount;
			}
		}
		return num;
	}

	private List<float> GetKillCounts()
	{
		List<float> list = new List<float>();
		foreach (Agent activeAgent in Mission.Current.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsHero)
			{
				list.Add(activeAgent.KillCount);
			}
		}
		return list;
	}

	public static double GetPercentile(IEnumerable<float> seq, double percentile)
	{
		float[] array = seq.ToArray();
		Array.Sort(array);
		double num = percentile * (double)(array.Length - 1);
		int num2 = (int)num;
		double num3 = num - (double)num2;
		if (num2 + 1 < array.Length)
		{
			return (double)array[num2] * (1.0 - num3) + (double)array[num2 + 1] * num3;
		}
		return array[num2];
	}

	public override void ShowBattleResults()
	{
		if (PromotionManager.__instance == null)
		{
			return;
		}
		PromotionManager.__instance.nominations.Clear();
		PromotionManager.__instance.killcounts.Clear();
		if (Mission.Current.Mode == MissionMode.Conversation || Mission.Current.Mode == MissionMode.StartUp || Mission.Current.CombatType == Mission.MissionCombatType.ArenaCombat)
		{
			return;
		}
		int totalKillCount = GetTotalKillCount();
		if (totalKillCount <= 0)
		{
			return;
		}
		float f = (float)GetPercentile(GetKillCounts(), PromotionManager.__instance.outperform_percentile);
		int num = 0;
		foreach (Agent activeAgent in Mission.Current.PlayerTeam.ActiveAgents)
		{
			if (activeAgent.IsHero || activeAgent.Origin == null || (PartyBase)activeAgent.Origin.BattleCombatant == null || ((PartyBase)activeAgent.Origin.BattleCombatant).MobileParty == null || !((PartyBase)activeAgent.Origin.BattleCombatant).MobileParty.IsMainParty)
			{
				continue;
			}
			CharacterObject characterObject = CharacterObject.Find(activeAgent.Character.StringId);
			if (PartyBase.MainParty.MemberRoster.Contains(characterObject) && MobileParty.MainParty.MemberRoster.Contains(characterObject))
			{
				if (characterObject.IsRanged)
				{
					num = PromotionManager.__instance.ran_kill_threshold;
				}
				else
				{
					num = ((!characterObject.IsMounted) ? PromotionManager.__instance.inf_kill_threshold : PromotionManager.__instance.cav_kill_threshold);
				}
				if ((PromotionManager.__instance.outperform_percentile <= 0f || activeAgent.KillCount > MathF.Ceiling(f)) && activeAgent.KillCount >= num && PromotionManager.IsSoldierQualified(characterObject))
				{
					PromotionManager.__instance.nominations.Add(characterObject);
					PromotionManager.__instance.killcounts.Add(activeAgent.KillCount);
				}
			}
		}
		PromotionManager.__instance.OnPCBattleEndedResults();
	}
}
