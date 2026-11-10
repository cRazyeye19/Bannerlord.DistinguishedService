namespace DistinguishedService;

public class Settings
{
	public int up_front_cost { get; set; }

	public int tier_threshold { get; set; }

	public int base_additional_skill_points { get; set; }

	public int leadership_points_per_50_extra_skill_points { get; set; }

	public int inf_kill_threshold { get; set; }

	public int cav_kill_threshold { get; set; }

	public int ran_kill_threshold { get; set; }

	public float outperform_percentile { get; set; }

	public int max_nominations { get; set; }

	public bool upgrade_to_hero { get; set; }

	public bool fill_in_perks { get; set; }

	public bool respect_companion_limit { get; set; }

	public int bonus_companion_slots_base { get; set; }

	public int bonus_companion_slots_per_clan_tier { get; set; }

	public float companion_extra_lethality { get; set; }

	public float ai_promotion_chance { get; set; }

	public int number_of_skill_bonuses { get; set; }

	public int number_of_skill_rounds { get; set; }

	public bool select_skills_randomly { get; set; }

	public bool remove_tavern_companions { get; set; }

	public bool cull_ai_companions_on_defeat { get; set; }

	public bool disable_caution_text { get; set; }

	public int skillpoints_per_excess_kill { get; set; }

	public int max_ai_companions_per_party { get; set; }

	public bool NAMES_FROM_EXTERNAL_FILE { get; set; }

	public string EXTERNAL_NAME_FILE { get; set; }

	public Settings()
	{
		up_front_cost = 0;
		tier_threshold = 4;
		base_additional_skill_points = 150;
		leadership_points_per_50_extra_skill_points = 100;
		number_of_skill_bonuses = 3;
		inf_kill_threshold = 5;
		cav_kill_threshold = 5;
		ran_kill_threshold = 5;
		outperform_percentile = 0.68f;
		max_nominations = 1;
		upgrade_to_hero = false;
		fill_in_perks = false;
		respect_companion_limit = false;
		bonus_companion_slots_base = 3;
		bonus_companion_slots_per_clan_tier = 2;
		companion_extra_lethality = 0f;
		ai_promotion_chance = 0.001f;
		remove_tavern_companions = true;
		cull_ai_companions_on_defeat = true;
		disable_caution_text = false;
		skillpoints_per_excess_kill = 25;
		NAMES_FROM_EXTERNAL_FILE = false;
		EXTERNAL_NAME_FILE = "external_namelist.txt";
		max_ai_companions_per_party = 1;
		number_of_skill_rounds = 2;
		select_skills_randomly = false;
	}
}
