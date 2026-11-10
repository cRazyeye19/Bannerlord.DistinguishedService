using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DistinguishedService;

internal class DSBattleBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, FindBattle);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, PromotionManager.AddDialogs);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void FindBattle(IMission misson)
	{
		if (((Mission)misson).CombatType <= Mission.MissionCombatType.Combat && Mission.Current.Scene != null && !Mission.Current.HasMissionBehavior<DSBattleLogic>())
		{
			Mission.Current.AddMissionBehavior(new DSBattleLogic());
		}
	}
}
