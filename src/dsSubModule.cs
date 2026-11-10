using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace DistinguishedService;

public class dsSubModule : MBSubModuleBase
{
	public static dsSubModule instance;

	public bool gamestarted = false;

	private static PromotionManager _pm;

	private Settings CurrentSettings { get; set; }

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		if (!(Game.Current.GameType is Campaign))
		{
			return;
		}
		try
		{
			string filename = Path.Combine(ModuleHelper.GetModuleFullPath("DistinguishedService110"), "Settings.xml");
			if (File.Exists(Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", "Settings.xml")))
			{
				filename = Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", "Settings.xml");
			}
			DeserializeObject(filename);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("Could not serialize Settings.xml: " + ex.Message.ToString() + " Using default values!", Color.FromUint(4282569842u)));
			CurrentSettings = new Settings();
		}
		try
		{
			if (PromotionManager.__instance == null)
			{
				_pm = new PromotionManager();
			}
			else
			{
				_pm = PromotionManager.__instance;
			}
			((CampaignGameStarter)gameStarterObject).AddBehavior(new DSBattleBehavior());
			if (CurrentSettings.ai_promotion_chance > 0f)
			{
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("AI Companions have been disabled for now!", Color.FromUint(4282569842u)));
				}
				catch (Exception)
				{
					InformationManager.DisplayMessage(new InformationMessage("Serialized Listener for MapEventEnded could not be created. AI Promotion chance reset to 0.", Color.FromUint(4282569842u)));
					CurrentSettings.ai_promotion_chance = 0f;
				}
			}
			if (CurrentSettings.upgrade_to_hero)
			{
				CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, _pm.upgrade_to_hero);
				CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, _pm.recruit_to_hero);
			}
			InformationManager.DisplayMessage(new InformationMessage("Distinguished Service loaded successfully", Colors.Blue));
		}
		catch (Exception ex3)
		{
			InformationManager.DisplayMessage(new InformationMessage("There was a problem:\n" + ex3.ToString(), Colors.Blue));
		}
		gamestarted = true;
	}

	public override void OnGameLoaded(Game game, object initializerObject)
	{
		if (!(game.GameType is Campaign))
		{
			return;
		}
		bool flag = false;
		try
		{
			string filename = Path.Combine(ModuleHelper.GetModuleFullPath("DistinguishedService110"), "Settings.xml");
			if (File.Exists(Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", "Settings.xml")))
			{
				filename = Path.Combine(BasePath.Name, "Modules", "DistinguishedService110", "Settings.xml");
			}
			DeserializeObject(filename);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("Could not serialize Settings.xml: " + ex.Message.ToString() + " Using default values!", Color.FromUint(4282569842u)));
			CurrentSettings = new Settings();
		}
		try
		{
			if (PromotionManager.__instance == null)
			{
				_pm = new PromotionManager();
			}
			else
			{
				_pm = PromotionManager.__instance;
				flag = true;
			}
			if (!gamestarted)
			{
				((CampaignGameStarter)initializerObject).AddBehavior(new DSBattleBehavior());
			}
			if (CurrentSettings.ai_promotion_chance > 0f)
			{
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("AI Companions have been disabled for now!", Color.FromUint(4282569842u)));
				}
				catch (Exception)
				{
					InformationManager.DisplayMessage(new InformationMessage("Serialized Listener for MapEventEnded could not be created. AI Promotion chance reset to 0.", Color.FromUint(4282569842u)));
					CurrentSettings.ai_promotion_chance = 0f;
				}
			}
			if (CurrentSettings.upgrade_to_hero)
			{
				CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, _pm.upgrade_to_hero);
				CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, _pm.recruit_to_hero);
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage("Distinguished Service loaded successfully", Colors.Blue));
			}
		}
		catch (Exception ex3)
		{
			InformationManager.DisplayMessage(new InformationMessage("There was a problem:\n" + ex3.ToString(), Colors.Blue));
		}
		gamestarted = false;
	}

	public override void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		PromotionManager.MyLittleWarbandLoaded = CheckMLWBLoaded();
		gamestarted = false;
	}

	public bool CheckMLWBLoaded()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.FullName.Contains("MyLittleWarband"))
			{
				return true;
			}
		}
		return false;
	}

	private void DeserializeObject(string filename)
	{
		Settings currentSettings;
		using (Stream stream = new FileStream(filename, FileMode.Open))
		{
			currentSettings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
		}
		CurrentSettings = currentSettings;
	}

	private void SerializeObject(string filename)
	{
		Console.WriteLine("Writing With XmlTextWriter");
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(Settings));
		Settings o = new Settings();
		XmlWriter xmlWriter = XmlWriter.Create(new FileStream(filename, FileMode.Create), new XmlWriterSettings
		{
			Indent = true,
			IndentChars = "\t",
			OmitXmlDeclaration = true
		});
		xmlSerializer.Serialize(xmlWriter, o);
		xmlWriter.Close();
	}
}
