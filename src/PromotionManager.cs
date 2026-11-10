using TaleWorlds.Core.ImageIdentifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace DistinguishedService;

internal class PromotionManager
{
	private Random rand;

	public static PromotionManager __instance;

	public List<CharacterObject> nominations;

	public List<int> killcounts;

	public static bool MyLittleWarbandLoaded;

	public bool using_extern_namelist { get; set; }

	public string extern_namelist { get; set; }

	public int max_nominations { get; set; }

	public int tier_threshold { get; set; }

	public int inf_kill_threshold { get; set; }

	public int cav_kill_threshold { get; set; }

	public int ran_kill_threshold { get; set; }

	public bool fill_perks { get; set; }

	public float outperform_percentile { get; set; }

	public int up_front_cost { get; set; }

	public bool respect_companion_limit { get; set; }

	public bool ignore_cautions { get; set; }

	private int base_additional_skill_points { get; set; }

	private int leadership_points_per_50_extra_skill_points { get; set; }

	private int skp_per_excess_kill { get; set; }

	public bool select_skills_randomly { get; set; }

	public int num_skill_rounds { get; set; }

	public int num_skill_bonuses { get; set; }

	public float ai_promotion_chance { get; set; }

	public int max_ai_comp_per_party { get; set; }

	public PromotionManager()
	{
		string path = Path.Combine(ModuleHelper.GetModuleFullPath("DistinguishedService110"), "Settings.xml");
		if (File.Exists(Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", "Settings.xml")))
		{
			InformationManager.DisplayMessage(new InformationMessage("Using Modules/DistinguishedService110 Settings", Color.FromUint(4282569842u)));
			path = Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", "Settings.xml");
		}
		Settings settings;
		using (Stream stream = new FileStream(path, FileMode.Open))
		{
			settings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
		}
		tier_threshold = settings.tier_threshold;
		max_nominations = settings.max_nominations;
		inf_kill_threshold = settings.inf_kill_threshold;
		cav_kill_threshold = settings.cav_kill_threshold;
		ran_kill_threshold = settings.ran_kill_threshold;
		outperform_percentile = settings.outperform_percentile;
		skp_per_excess_kill = settings.skillpoints_per_excess_kill;
		up_front_cost = settings.up_front_cost;
		fill_perks = settings.fill_in_perks;
		respect_companion_limit = settings.respect_companion_limit;
		base_additional_skill_points = settings.base_additional_skill_points;
		leadership_points_per_50_extra_skill_points = settings.leadership_points_per_50_extra_skill_points;
		num_skill_bonuses = settings.number_of_skill_bonuses;
		num_skill_rounds = settings.number_of_skill_rounds;
		select_skills_randomly = settings.select_skills_randomly;
		ai_promotion_chance = settings.ai_promotion_chance;
		max_ai_comp_per_party = settings.max_ai_companions_per_party;
		rand = new Random();
		nominations = new List<CharacterObject>();
		killcounts = new List<int>();
		using_extern_namelist = settings.NAMES_FROM_EXTERNAL_FILE;
		extern_namelist = Path.Combine(ModuleHelper.GetModuleFullPath("DistinguishedService110"), settings.EXTERNAL_NAME_FILE);
		if (File.Exists(Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", settings.EXTERNAL_NAME_FILE)))
		{
			extern_namelist = Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", settings.EXTERNAL_NAME_FILE);
		}
		if (using_extern_namelist)
		{
			InformationManager.DisplayMessage(new InformationMessage("USING EXTERNAL NAMELIST FILE!\nThis file will be written back to/edited to knock out used names", Color.FromUint(4282569842u)));
		}
		fill_perks = settings.fill_in_perks;
		InformationManager.DisplayMessage(new InformationMessage("Max nominations = " + max_nominations + "\nTier Thresh = " + tier_threshold + "\nKill Thresh:\nInf = " + inf_kill_threshold + " cav = " + cav_kill_threshold + " ran = " + ran_kill_threshold + "\nPerformance Thresh = " + outperform_percentile, Color.FromUint(4282569842u)));
		__instance = this;
		if (settings.upgrade_to_hero)
		{
			InformationManager.DisplayMessage(new InformationMessage("CAUTION: Troops will auto-promote to heros at tier " + tier_threshold + "\nChange settings if this is unintended.", Colors.Yellow));
		}
		if (fill_perks)
		{
			InformationManager.DisplayMessage(new InformationMessage("CAUTION: New hero perks will be assigned automatically.\nChange settings if this is unintended.", Colors.Yellow));
		}
		if (!respect_companion_limit)
		{
			InformationManager.DisplayMessage(new InformationMessage("CAUTION: Ignoring companion limit.", Colors.Yellow));
		}
	}

	public void OnPCBattleEndedResults()
	{
		if (respect_companion_limit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
		{
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("At maximum allowed companions. No nominations possible.").ToString(), Colors.Blue));
			return;
		}
		List<CharacterObject> _stripped_noms = new List<CharacterObject>();
		List<int> _stripped_kcs = new List<int>();
		if (nominations.Count > 0 && killcounts.Count > 0)
		{
			for (int i = 0; i < nominations.Count; i++)
			{
				if (MobileParty.MainParty.MemberRoster.Contains(nominations[i]) && nominations[i] != null && nominations[i].HitPoints > 0)
				{
					_stripped_noms.Add(nominations[i]);
					_stripped_kcs.Add(killcounts[i]);
				}
			}
		}
		int num = max_nominations;
		double num2 = rand.NextDouble();
		if (_stripped_noms.Count > 0)
		{
			List<CharacterObject> cos = new List<CharacterObject>(_stripped_noms).OrderBy((CharacterObject o) => _stripped_kcs[_stripped_noms.IndexOf(o)]).Reverse().ToList();
			_stripped_kcs = new List<int>(_stripped_kcs).OrderBy((int o) => _stripped_kcs[_stripped_kcs.IndexOf(o)]).Reverse().ToList();
			num = ((!respect_companion_limit || max_nominations + Clan.PlayerClan.Companions.Count <= Clan.PlayerClan.CompanionLimit) ? max_nominations : (Clan.PlayerClan.CompanionLimit - Clan.PlayerClan.Companions.Count));
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Distinguished Soldiers", "Several soldiers made names for themselves in this battle. You can choose up to " + num + " (or none, by exiting) to fight at your side as a companion.", GenInquiryelements(cos, _stripped_kcs), isExitShown: true, 0, num, "DONE", "RANDOM", OnNomineeSelect, null), pauseGameActiveState: true);
		}
	}

	public List<InquiryElement> GenInquiryelements(List<CharacterObject> _cos, List<int> _kills)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		for (int i = 0; i < _cos.Count; i++)
		{
			if (MobileParty.MainParty.MemberRoster.Contains(_cos[i]))
			{
				list.Add(new InquiryElement(_cos[i], _cos[i].Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(_cos[i])), isEnabled: true, _kills[i] + " kills"));
			}
		}
		return list;
	}

	public void OnNomineeSelect(List<InquiryElement> ies)
	{
		foreach (InquiryElement ie in ies)
		{
			CharacterObject characterObject = (CharacterObject)ie.Identifier;
			int result = 0;
			string s = ie.Hint.Split(' ')[0];
			if (int.TryParse(s, out result))
			{
				InformationManager.DisplayMessage(new InformationMessage(characterObject.Name?.ToString() + " got " + result, Colors.Red));
			}
			else
			{
				result = -1;
			}
			PromoteUnit(characterObject, result);
			if (MobileParty.MainParty.MemberRoster.Contains(characterObject))
			{
				MobileParty.MainParty.MemberRoster.RemoveTroop(characterObject);
			}
		}
	}

	public static bool IsSoldierQualified(CharacterObject co)
	{
		if (co == null)
		{
			return false;
		}
		if (__instance.tier_threshold < 0)
		{
			if (co.UpgradeTargets == null || co.UpgradeTargets.Length == 0)
			{
				return true;
			}
		}
		else if (co.Tier >= __instance.tier_threshold)
		{
			return true;
		}
		return false;
	}

	public string GetNameFromExternalFile()
	{
		string text = "ABORT";
		if (File.Exists(extern_namelist))
		{
			try
			{
				string[] array = File.ReadAllLines(extern_namelist);
				if (array.Length == 0)
				{
					return "ABORT";
				}
				int num = MBRandom.RandomInt(0, array.Length);
				text = array[num];
				string text2 = "";
				for (int i = 0; i < array.Length; i++)
				{
					if (i != num)
					{
						text2 = text2 + array[i] + "\n";
					}
				}
				File.WriteAllText(extern_namelist, text2);
				return text;
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage(ex.Message, Colors.Red));
				return "ABORT";
			}
		}
		return "ABORT";
	}

	public string GetNameSuffix(CharacterObject co)
	{
		List<string> list = new List<string>();
		if (co.IsRanged)
		{
			list.AppendList(NameList.archer_suff);
		}
		else if (!co.FirstBattleEquipment[EquipmentIndex.ArmorItemEndSlot].IsEmpty)
		{
			list.AppendList(NameList.cavalry_suff);
		}
		else
		{
			list.AppendList(NameList.infantry_suff);
		}
		switch (DSCompat.CultureKey(co.Culture))
		{
		case "aserai":
			list.AppendList(NameList.aserai_suff);
			break;
		case "battania":
			list.AppendList(NameList.battanian_suff);
			break;
		case "khuzait":
			list.AppendList(NameList.khuzait_suff);
			break;
		case "sturgia":
			list.AppendList(NameList.sturgian_suff);
			break;
		case "nord":
			list.AppendList(NameList.sturgian_suff);
			break;
		case "vakken":
			list.AppendList(NameList.sturgian_suff);
			break;
		case "darshi":
			list.AppendList(NameList.aserai_suff);
			break;
		case "vlandia":
			list.AppendList(NameList.vlandian_suff);
			break;
		}
		if (co.Culture.StringId == "criminals")
		{
			list.Clear();
			list.AppendList(NameList.criminal_suff);
		}
		else if (co.Culture.StringId == "criminals2")
		{
			list.Clear();
			list.AppendList(NameList.criminal2_suff);
		}
		else if (co.Culture.StringId == "criminals3")
		{
			list.Clear();
			list.AppendList(NameList.criminal3_suff);
		}
		else if (co.Culture.StringId == "pirates")
		{
			list.Clear();
			list.AppendList(NameList.pirates_suff);
		}
		else if (co.Culture.StringId == "shift_sand")
		{
			list.Clear();
			list.AppendList(NameList.shiftingsands_suff);
		}
		return list[MBRandom.RandomInt(list.Count)];
	}

	public void AdjustEquipment(Hero _h)
	{
		Equipment battleEquipment = _h.BattleEquipment;
		ItemModifier itemModifier = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
		ItemModifier itemModifier2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
		ItemModifier itemModifier3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			EquipmentElement equipmentElement = battleEquipment[equipmentIndex];
			if (equipmentElement.Item != null)
			{
				if (equipmentElement.Item.ArmorComponent != null)
				{
					battleEquipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, itemModifier);
				}
				else if (equipmentElement.Item.HorseComponent != null)
				{
					battleEquipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, itemModifier3);
				}
				else if (equipmentElement.Item.WeaponComponent != null)
				{
					battleEquipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, itemModifier2);
				}
			}
		}
	}

	public void AddTraitVariance(Hero hero)
	{
		foreach (TraitObject item in TraitObject.All)
		{
			if (item != DefaultTraits.Honor && item != DefaultTraits.Mercy && item != DefaultTraits.Generosity && item != DefaultTraits.Valor && item != DefaultTraits.Calculating)
			{
				continue;
			}
			int num = hero.CharacterObject.GetTraitLevel(item);
			float num2 = MBRandom.RandomFloat;
			if ((double)Hero.MainHero.GetTraitLevel(item) >= 0.9)
			{
				num2 *= 1.2f;
			}
			if ((double)num2 < 0.1)
			{
				num--;
				if (num < -1)
				{
					num = -1;
				}
			}
			if ((double)num2 > 0.9)
			{
				num++;
				if (num > 1)
				{
					num = 1;
				}
			}
			int value = MBMath.ClampInt(num, item.MinValue, item.MaxValue);
			hero.SetTraitLevel(item, value);
		}
	}

	public void PromoteUnit(CharacterObject co, int kills = -1, bool pick_skills = true)
	{
		CharacterObject characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>(co.StringId);
		co = characterObject;
		if (co == null)
		{
			return;
		}
		CharacterObject randomElementWithPredicate = DSCompat.GetRandomWanderer(co.Culture, (CharacterObject x) => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null);
		if (randomElementWithPredicate == null)
		{
			if (!ignore_cautions)
			{
				InformationManager.DisplayMessage(new InformationMessage("CAUTION: No wanderer template with culture " + co.Culture.Name?.ToString() + " available.\nChoosing randomly instead.", Colors.Yellow));
			}
			randomElementWithPredicate = DSCompat.GetRandomWanderer(CharacterObject.PlayerCharacter.Culture, (CharacterObject x) => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null);
		}
		if (randomElementWithPredicate == null)
		{
			if (!ignore_cautions)
			{
				InformationManager.DisplayMessage(new InformationMessage("WARNING: SOMETHING ACTUALLY WENT WRONG WITH WANDERER TEMPLATES\nPICKING COMPLETELY RANDOMLY", Colors.Red));
			}
			randomElementWithPredicate = DSCompat.GetRandomWanderer(CharacterObject.PlayerCharacter.Culture, (CharacterObject x) => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale);
		}
		if (randomElementWithPredicate == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("WARNING: Could not find valid wanderer template. You broke something.", Colors.Red));
			return;
		}
		Hero hero = HeroCreator.CreateSpecialHero(randomElementWithPredicate, null, null, null, rand.Next(20, 50));
		bool flag = false;
		string text = "ABORT";
		if (using_extern_namelist)
		{
			text = GetNameFromExternalFile();
			if (text.Equals("ABORT"))
			{
				flag = false;
			}
			else
			{
				flag = true;
				hero.SetName(new TextObject(text + GetNameSuffix(co)), new TextObject(text));
			}
		}
		if (!using_extern_namelist || !flag)
		{
			hero.SetName(new TextObject(hero.FirstName.ToString() + GetNameSuffix(co)), hero.FirstName);
		}
		hero.Culture = co.Culture;
		hero.CharacterObject.DefaultFormationGroup = co.DefaultFormationGroup;
		hero.ChangeState(Hero.CharacterStates.Active);
		AddCompanionAction.Apply(Clan.PlayerClan, hero);
		AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
		CampaignEventDispatcher.Instance.OnHeroCreated(hero);
		AddTraitVariance(hero);
		float num = up_front_cost;
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.GreatInvestor))
		{
			num *= 0.7f;
		}
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise))
		{
			num *= 0.75f;
		}
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, hero, (int)num);
		hero.SetHasMet();
		try
		{
			if (MyLittleWarbandLoaded)
			{
				InformationManager.DisplayMessage(new InformationMessage("MLBW compatibility mode:\nUsing first equipment sets", Colors.Yellow));
				hero.BattleEquipment.FillFrom(co.FirstBattleEquipment);
				hero.CivilianEquipment.FillFrom(co.FirstCivilianEquipment);
			}
			else
			{
				hero.BattleEquipment.FillFrom(co.RandomBattleEquipment);
				hero.CivilianEquipment.FillFrom(co.RandomCivilianEquipment);
			}
			AdjustEquipment(hero);
		}
		catch (Exception ex)
		{
			if (!ignore_cautions)
			{
				InformationManager.DisplayMessage(new InformationMessage("CAUTION: Something went wrong with this unit's equipment.\nAborting process, not everything might be added properly.", Colors.Yellow));
				Debug.Print("Equipment format issue, providing default equipment instead! Exception details:\n" + ex.Message);
			}
		}
		hero.HeroDeveloper.SetInitialLevel(co.Level);
		Dictionary<SkillObject, int> dictionary = new Dictionary<SkillObject, int>();
		foreach (SkillObject item in Skills.All)
		{
			dictionary[item] = Math.Min(co.GetSkillValue(item), 300);
		}
		int num2 = 0;
		foreach (SkillObject item2 in Skills.All)
		{
			num2 = hero.GetSkillValue(item2);
			hero.HeroDeveloper.ChangeSkillLevel(item2, dictionary[item2] - num2);
		}
		int num3 = base_additional_skill_points + 50 * Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) / leadership_points_per_50_extra_skill_points;
		if (kills > 0)
		{
			if (co.IsMounted)
			{
				num3 += __instance.skp_per_excess_kill * (kills - __instance.cav_kill_threshold);
			}
			else
			{
				num3 = ((!co.IsRanged) ? (num3 + __instance.skp_per_excess_kill * (kills - __instance.inf_kill_threshold)) : (num3 + __instance.skp_per_excess_kill * (kills - __instance.ran_kill_threshold)));
			}
		}
		if (!select_skills_randomly)
		{
			for (int num4 = 1; num4 <= num_skill_rounds; num4++)
			{
				AssignSkills(hero, num3 / num4, num_skill_bonuses, "Round " + num4, co.Name.ToString());
			}
		}
		else
		{
			for (int num5 = 1; num5 <= num_skill_rounds; num5++)
			{
				AssignSkillsRandomly(hero, num3 / num5, num_skill_bonuses);
			}
		}
		int unspentAttributePoints = hero.HeroDeveloper.UnspentAttributePoints;
		hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2, checkUnspentPoints: false);
		hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 2, checkUnspentPoints: false);
		hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, 2, checkUnspentPoints: false);
		hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 2, checkUnspentPoints: false);
		hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 2, checkUnspentPoints: false);
		hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, 2, checkUnspentPoints: false);
		unspentAttributePoints -= 12;
		int num6 = 0;
		if (unspentAttributePoints > 0)
		{
			if (co.IsMounted)
			{
				num6 = rand.Next(3);
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, num6, checkUnspentPoints: false);
				unspentAttributePoints -= num6;
			}
			else if (co.IsRanged)
			{
				num6 = rand.Next(3);
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, num6, checkUnspentPoints: false);
				unspentAttributePoints -= num6;
			}
			else
			{
				num6 = rand.Next(3);
				hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, num6, checkUnspentPoints: false);
				unspentAttributePoints -= num6;
			}
			List<CharacterAttribute> list = new List<CharacterAttribute>(Attributes.All);
			Shuffle(list);
			foreach (CharacterAttribute item3 in list)
			{
				num6 = rand.Next(2);
				hero.HeroDeveloper.AddAttribute(item3, num6, checkUnspentPoints: false);
				unspentAttributePoints -= num6;
				if (unspentAttributePoints <= 0)
				{
					break;
				}
			}
		}
		if (fill_perks)
		{
			CharacterDevelopmentCampaignBehavior campaignBehavior = CampaignBehaviorBase.GetCampaignBehavior<CharacterDevelopmentCampaignBehavior>();
			if (campaignBehavior != null)
			{
				hero.HeroDeveloper.DevelopCharacterStats();
			}
		}
		hero.HeroDeveloper.UnspentAttributePoints = 0;
	}

	public void AssignSkills(Hero specialHero, int skill_points_to_assign, int num_skills_to_select, string title_prefix, string prev = " basic soldier")
	{
		List<InquiryElement> list = new List<InquiryElement>();
		if (specialHero.GetSkillValue(DefaultSkills.Scouting) < 300)
		{
			list.Add(new InquiryElement("scout_bonus", "Ranged with the Scouts", null, isEnabled: true, "+" + skill_points_to_assign + " scouting"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Crafting) < 300)
		{
			list.Add(new InquiryElement("smithing_bonus", "Repaired the party's weapons", null, isEnabled: true, "+" + skill_points_to_assign + " smithing"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Athletics) < 300)
		{
			list.Add(new InquiryElement("athletics_bonus", "Trained for combat", null, isEnabled: true, "+" + skill_points_to_assign + " athletics"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Riding) < 300)
		{
			list.Add(new InquiryElement("riding_bonus", "Rode horses", null, isEnabled: true, "+" + skill_points_to_assign + " riding"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Tactics) < 300)
		{
			list.Add(new InquiryElement("tactics_bonus", "Studied past battles", null, isEnabled: true, "+" + skill_points_to_assign + " tactics"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Roguery) < 300)
		{
			list.Add(new InquiryElement("roguery_bonus", "Sold loot in the black market", null, isEnabled: true, "+" + skill_points_to_assign + " roguery"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Charm) < 300)
		{
			list.Add(new InquiryElement("charm_bonus", "Chatted everyone up", null, isEnabled: true, "+" + skill_points_to_assign + " charm"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Leadership) < 300)
		{
			list.Add(new InquiryElement("leadership_bonus", "Organized duties for the party", null, isEnabled: true, "+" + skill_points_to_assign + " leadership"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Trade) < 300)
		{
			list.Add(new InquiryElement("trade_bonus", "Bought and sold items from towns you visited", null, isEnabled: true, "+" + skill_points_to_assign + " trade"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Steward) < 300)
		{
			list.Add(new InquiryElement("steward_bonus", "Helped handle the party's accounts", null, isEnabled: true, "+" + skill_points_to_assign + " stewardship"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Medicine) < 300)
		{
			list.Add(new InquiryElement("medicine_bonus", "Helped as a medic", null, isEnabled: true, "+" + skill_points_to_assign + " medicine"));
		}
		if (specialHero.GetSkillValue(DefaultSkills.Engineering) < 300)
		{
			list.Add(new InquiryElement("engineering_bonus", "Helped construct and take down the camp", null, isEnabled: true, "+" + skill_points_to_assign + " engineering"));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("Select " + title_prefix + " Skill Focuses", specialHero.Name?.ToString() + " (previously a " + prev + ") has finally found glory on the battlefield. Before this, and besides training for battle they...", list, isExitShown: true, 0, num_skills_to_select, "Accept", "Refuse", (List<InquiryElement> ies) =>
		{
			int num = 0;
			foreach (InquiryElement ie in ies)
			{
				switch ((string)ie.Identifier)
				{
				case "scout_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Scouting);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Scouting, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "smithing_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Crafting);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Crafting, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "athletics_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Athletics);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Athletics, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "riding_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Riding);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Riding, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "tactics_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Tactics);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Tactics, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "roguery_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Roguery);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Roguery, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "charm_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Charm);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Charm, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "leadership_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Leadership);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Leadership, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "trade_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Trade);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Trade, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "steward_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Steward);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Steward, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "medicine_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Medicine);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Medicine, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				case "engineering_bonus":
					try
					{
						num = 300 - specialHero.GetSkillValue(DefaultSkills.Engineering);
						specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Engineering, Math.Min(skill_points_to_assign, num));
					}
					catch
					{
					}
					break;
				}
				try
				{
				}
				catch (Exception)
				{
					if (!ignore_cautions)
					{
						InformationManager.DisplayMessage(new InformationMessage("CAUTION: Reflection call to CheckInitialLevel failed. Potential version issue.", Colors.Yellow));
					}
				}
			}
		}, (List<InquiryElement> list2) =>
		{
			InformationManager.DisplayMessage(new InformationMessage("Negative action triggered.", Colors.Red));
		});
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	public void AssignSkillsRandomly(Hero specialHero, int skill_points_to_assign, int num_skills_to_select)
	{
		for (int i = 0; i < num_skills_to_select; i++)
		{
			switch ((int)(MBRandom.RandomFloat * 12f))
			{
			case 0:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Scouting, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 1:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Crafting, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 2:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Athletics, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 3:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Riding, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 4:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Tactics, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 5:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Roguery, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 6:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Charm, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 7:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Leadership, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 8:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Trade, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 9:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Steward, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 10:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Medicine, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			case 11:
				try
				{
					specialHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Engineering, skill_points_to_assign);
				}
				catch
				{
				}
				break;
			}
		}
		int num = 0;
		foreach (SkillObject item in Skills.All)
		{
			num = 300 - specialHero.GetSkillValue(item);
			if (num < 0)
			{
				specialHero.HeroDeveloper.ChangeSkillLevel(item, num);
			}
		}
		try
		{
		}
		catch (Exception)
		{
			if (!ignore_cautions)
			{
				InformationManager.DisplayMessage(new InformationMessage("CAUTION: Reflection call to CheckInitialLevel failed. Potential version issue.", Colors.Yellow));
			}
		}
	}

	public void Shuffle<T>(IList<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index = rand.Next(num + 1);
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}

	public void recruit_to_hero(CharacterObject troop, int amount)
	{
		if (!IsSoldierQualified(troop) || !MobileParty.MainParty.MemberRoster.Contains(troop))
		{
			return;
		}
		for (int i = 0; i < amount; i++)
		{
			if (respect_companion_limit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
			{
				MobileParty.MainParty.MemberRoster.RemoveTroop(troop, i);
				return;
			}
			__instance.PromoteUnit(troop);
		}
		MobileParty.MainParty.MemberRoster.RemoveTroop(troop, amount);
	}

	public void upgrade_to_hero(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
	{
		if (!IsSoldierQualified(upgradeToTroop))
		{
			return;
		}
		for (int i = 0; i < number; i++)
		{
			if (respect_companion_limit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
			{
				MobileParty.MainParty.MemberRoster.RemoveTroop(upgradeToTroop, i);
				return;
			}
			__instance.PromoteUnit(upgradeToTroop);
		}
		MobileParty.MainParty.MemberRoster.RemoveTroop(upgradeToTroop, number);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("uplift_soldier", "dservice")]
	public static string NewGuyCheat(List<string> strings)
	{
		int result = -1;
		if (!CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
		{
			return "Usage: uplift_soldier [tier threshold = 0]";
		}
		if (!int.TryParse(strings[0], out result))
		{
			result = 0;
		}
		List<CharacterObject> list = new List<CharacterObject>();
		List<int> list2 = new List<int>();
		foreach (CharacterObject troop in MobileParty.MainParty.MemberRoster.ToFlattenedRoster().Troops)
		{
			if (!troop.IsHero && troop.Tier >= result)
			{
				list.Add(troop);
				list2.Add(1337);
			}
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Console Command", "Pick a soldier to uplift!", __instance.GenInquiryelements(list, list2), isExitShown: true, 0, 1, "DONE", "RANDOM", __instance.OnNomineeSelect, null), pauseGameActiveState: true);
		return "Dialog Generated";
	}

	public void MapEventEnded(MapEvent me)
	{
		try
		{
			if (!me.HasWinner)
			{
				return;
			}
			Random random = new Random();
			foreach (MapEventParty item in me.PartiesOnSide(me.WinningSide))
			{
				if (item == null || item.Party == PartyBase.MainParty || item.Party.LeaderHero?.Clan == Clan.PlayerClan || item.Party.LeaderHero == null || random.NextDouble() > (double)ai_promotion_chance)
				{
					continue;
				}
				List<CharacterObject> list = item.Troops.Troops.ToList();
				if (list == null)
				{
					continue;
				}
				Shuffle(list);
				List<CharacterObject> list2 = new List<CharacterObject>();
				int num = 0;
				foreach (CharacterObject item2 in list)
				{
					if (item2.IsHero && item2.HeroObject.Occupation == Occupation.Wanderer)
					{
						num++;
					}
					if (item2 != null && !item2.IsHero && item2.IsSoldier && IsSoldierQualified(item2))
					{
						list2.Add(item2);
					}
				}
				if (num < max_ai_comp_per_party && list2.Count > 0)
				{
					PromoteToParty(list2[0], item.Party.MobileParty);
				}
			}
		}
		catch
		{
		}
	}

	public void PromoteToParty(CharacterObject co, MobileParty party)
	{
		if (co == null || party == null)
		{
			return;
		}
		Hero hero = party?.LeaderHero;
		if (hero == null)
		{
			return;
		}
		CharacterObject randomElementWithPredicate = DSCompat.GetRandomWanderer(co.Culture, (CharacterObject x) => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null);
		if (randomElementWithPredicate == null)
		{
			randomElementWithPredicate = DSCompat.GetRandomWanderer(CharacterObject.PlayerCharacter.Culture, (CharacterObject x) => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null);
		}
		if (randomElementWithPredicate == null)
		{
			return;
		}
		Hero hero2 = HeroCreator.CreateSpecialHero(randomElementWithPredicate, null, null, null, rand.Next(20, 50));
		hero2.SetName(new TextObject(hero2.FirstName.ToString() + GetNameSuffix(co)), hero2.FirstName);
		hero2.Culture = co.Culture;
		hero2.ChangeState(Hero.CharacterStates.Active);
		AddHeroToPartyAction.Apply(hero2, party);
		CampaignEventDispatcher.Instance.OnHeroCreated(hero2);
		AddTraitVariance(hero2);
		GiveGoldAction.ApplyBetweenCharacters(hero, hero2, up_front_cost, disableNotification: true);
		try
		{
			hero2.BattleEquipment.FillFrom(co.FirstBattleEquipment);
			hero2.CivilianEquipment.FillFrom(co.FirstCivilianEquipment);
			AdjustEquipment(hero2);
		}
		catch (Exception ex)
		{
			if (!ignore_cautions)
			{
				InformationManager.DisplayMessage(new InformationMessage("CAUTION: Something went wrong with this unit's equipment.\nAborting process, not everything might be added properly.", Colors.Yellow));
				Debug.Print("Equipment format issue, providing default equipment instead! Exception details:\n" + ex.Message);
			}
		}
		if (co.IsMounted)
		{
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2 + rand.Next(2), checkUnspentPoints: false);
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 1 + rand.Next(2), checkUnspentPoints: false);
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 4 + rand.Next(3), checkUnspentPoints: false);
		}
		else if (co.IsRanged)
		{
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2 + rand.Next(2), checkUnspentPoints: false);
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 4 + rand.Next(3), checkUnspentPoints: false);
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 1 + rand.Next(2), checkUnspentPoints: false);
		}
		else
		{
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 3 + rand.Next(3), checkUnspentPoints: false);
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 2 + rand.Next(2), checkUnspentPoints: false);
			hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 2 + rand.Next(2), checkUnspentPoints: false);
		}
		hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, 1 + rand.Next(3), checkUnspentPoints: false);
		hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, 1 + rand.Next(3), checkUnspentPoints: false);
		hero2.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 1 + rand.Next(3), checkUnspentPoints: false);
		foreach (SkillObject item in Skills.All)
		{
			hero2.HeroDeveloper.ChangeSkillLevel(item, co.GetSkillValue(item), shouldNotify: false);
		}
		List<SkillObject> list = new List<SkillObject>(Skills.All);
		Shuffle(list);
		int num = base_additional_skill_points + 50 * hero.GetSkillValue(DefaultSkills.Leadership) / leadership_points_per_50_extra_skill_points;
		int num2 = 0;
		foreach (SkillObject item2 in list)
		{
			num2 = ((item2 != DefaultSkills.OneHanded && item2 != DefaultSkills.TwoHanded && item2 != DefaultSkills.Polearm && item2 != DefaultSkills.Bow && item2 != DefaultSkills.Crossbow && item2 != DefaultSkills.Throwing) ? (rand.Next(10) + rand.Next(15) + rand.Next(25)) : (rand.Next(10) + rand.Next(15)));
			num -= num2;
			if (num < 0)
			{
				num2 += num;
			}
			try
			{
				hero2.HeroDeveloper.ChangeSkillLevel(item2, num2, shouldNotify: false);
			}
			catch (Exception)
			{
			}
			if (num <= 0)
			{
				break;
			}
		}
		try
		{
		}
		catch (Exception)
		{
		}
	}

	public static void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddPlayerLine("companion_change_name_start", "hero_main_options", "companion_change_name_confirm", "I want you to be known by another name...", GetNamechangecondition, GetNamechanceconsequence);
		campaignGameStarter.AddDialogLine("companion_change_name_confirm", "companion_change_name_confirm", "hero_main_options", "That is all.", null, null);
		campaignGameStarter.AddPlayerLine("companion_transfer_start", "hero_main_options", "companion_transfer_confirm", "Take these companions into your party...", GetGiveCompToClanPartyCondition, GetGiveCompToClanPartyConsequence);
		campaignGameStarter.AddDialogLine("companion_transfer_confirm", "companion_transfer_confirm", "hero_main_options", "That is all.", null, null);
		campaignGameStarter.AddPlayerLine("companion_takeback_start", "hero_main_options", "companion_takeback_confirm", "I wish to reassign heroes in your party...", GetTakeCompFromClanPartyCondition, GetTakeCompFromClanPartyConsequence);
		campaignGameStarter.AddDialogLine("companion_takeback_confirm", "companion_takeback_confirm", "hero_main_options", "That is all.", null, null);
		campaignGameStarter.AddPlayerLine("enemy_comp_recruit_1", "defeated_lord_answer", "companion_poach_confirm", "After this defeat, are you sure you wouldn't rather work for me?", GetCapturedAIWandererCondition, null);
		campaignGameStarter.AddDialogLine("enemy_comp_recruit_2", "companion_poach_confirm", "close_window", "{RECRUIT_RESPONSE}", null, null);
	}

	private static bool GetNamechangecondition()
	{
		return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.IsPlayerCompanion;
	}

	private static void GetNamechanceconsequence()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData("Create a new name: ", string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_done").ToString(), null, ChangeHeroName, null));
	}

	private static void ChangeHeroName(string s)
	{
		Hero.OneToOneConversationHero.SetName(new TextObject(s), new TextObject(s));
	}

	private static bool GetGiveCompToClanPartyCondition()
	{
		return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.IsPartyLeader;
	}

	private static void GetGiveCompToClanPartyConsequence()
	{
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Transfer Heroes", "You can select who to assign to " + Hero.OneToOneConversationHero.Name?.ToString() + "'s party.", GenTransferList(GetPlayerPartyHeroCOs()), isExitShown: true, 0, PartyBase.MainParty.MemberRoster.Count, "DONE", "NOBODY", TransferCompsToConversationParty, null), pauseGameActiveState: true);
	}

	private static void TransferCompsToConversationParty(List<InquiryElement> ies)
	{
		MobileParty partyBelongedTo = Hero.OneToOneConversationHero.PartyBelongedTo;
		foreach (InquiryElement ie in ies)
		{
			CharacterObject characterObject = (CharacterObject)ie.Identifier;
			Hero heroObject = characterObject.HeroObject;
			AddHeroToPartyAction.Apply(heroObject, partyBelongedTo);
		}
	}

	private static List<CharacterObject> GetPlayerPartyHeroCOs()
	{
		List<CharacterObject> list = new List<CharacterObject>();
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character.IsHero && !character.IsPlayerCharacter)
			{
				list.Add(character);
			}
		}
		return list;
	}

	private static bool GetTakeCompFromClanPartyCondition()
	{
		return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.IsPartyLeader;
	}

	private static void GetTakeCompFromClanPartyConsequence()
	{
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Transfer Heroes", "You can select who to take from " + Hero.OneToOneConversationHero.Name?.ToString() + "'s party.", GenTransferList(GetConversationPartyHeros(), require_mainparty: false), isExitShown: true, 0, PartyBase.MainParty.MemberRoster.Count, "DONE", "NOBODY", TransferCompsFromConversationParty, null), pauseGameActiveState: true);
	}

	private static void TransferCompsFromConversationParty(List<InquiryElement> ies)
	{
		foreach (InquiryElement ie in ies)
		{
			CharacterObject characterObject = (CharacterObject)ie.Identifier;
			Hero heroObject = characterObject.HeroObject;
			AddHeroToPartyAction.Apply(heroObject, MobileParty.MainParty);
		}
	}

	public static List<InquiryElement> GenTransferList(List<CharacterObject> _cos, bool require_mainparty = true)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (CharacterObject _co in _cos)
		{
			if (!require_mainparty || (require_mainparty && MobileParty.MainParty.MemberRoster.Contains(_co)))
			{
				list.Add(new InquiryElement(_co, _co.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(_co)), isEnabled: true, " kills"));
			}
		}
		return list;
	}

	private static List<CharacterObject> GetConversationPartyHeros()
	{
		PartyBase party = Hero.OneToOneConversationHero.PartyBelongedTo.Party;
		List<CharacterObject> list = new List<CharacterObject>();
		foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character.IsHero && character.HeroObject != Hero.OneToOneConversationHero)
			{
				list.Add(character);
			}
		}
		return list;
	}

	private static bool GetCapturedAIWandererCondition()
	{
		if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan != Clan.PlayerClan && Hero.OneToOneConversationHero.Occupation == Occupation.Wanderer)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			int num4 = 0;
			int num5 = 0;
			float num6 = 0f;
			num6 = MathF.Max(0.25f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * (float)Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Calculating), 0f);
			if (num6 > num2)
			{
				num4 = 1;
			}
			else if (num6 < num3)
			{
				num5 = 1;
			}
			num6 = MathF.Max(-0.25f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * (float)Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Calculating), 0f);
			if (num6 > num2)
			{
				num4 = 5;
			}
			else if (num6 < num3)
			{
				num5 = 5;
			}
			num += num6;
			num6 = -0.5f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * (float)Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Honor);
			if (0f - num6 > num2)
			{
				num4 = 2;
			}
			else if (num6 < num3)
			{
				num5 = 2;
			}
			num += num6;
			num6 = 0.5f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * (float)Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Trader);
			if (num6 > num2)
			{
				num4 = 3;
			}
			else if (num6 < num3)
			{
				num5 = 3;
			}
			num += num6;
			num6 = 0.25f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * (float)Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Valor);
			if (num6 > num2)
			{
				num4 = 4;
			}
			else if (num6 < num3)
			{
				num5 = 4;
			}
			num += num6;
			if (MBRandom.RandomFloat < num)
			{
				switch (num4)
				{
				default:
					MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("Well, uh, I guess it beats prison..."));
					break;
				case 1:
					MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("If it means I don't get chained up, it sounds good to me!"));
					break;
				case 2:
					MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("Deal - I never liked the other guy anyway."));
					break;
				case 3:
					MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("Deal. Working with you seems like the safest bet."));
					break;
				case 4:
					MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("I like your proposition! We shall face more glorious battle together!"));
					break;
				case 5:
					MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("Uh... Sure, why not."));
					break;
				}
				Hero.OneToOneConversationHero.ChangeState(Hero.CharacterStates.Active);
				AddCompanionAction.Apply(Clan.PlayerClan, Hero.OneToOneConversationHero);
				AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty);
				return true;
			}
			switch (num4)
			{
			default:
				MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("I just can't accept!"));
				break;
			case 1:
				MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("I can't just join with whoever's most convenient! The whole system would fall apart!"));
				break;
			case 2:
				MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("I would never betray my commander like that!"));
				break;
			case 3:
				MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("No - I won't throw my life away for somebody who hasn't earned it."));
				break;
			case 4:
				MBTextManager.SetTextVariable("RECRUIT_RESPONSE", new TextObject("I just want to retire after this! No deal!"));
				break;
			}
			return true;
		}
		return false;
	}
}

