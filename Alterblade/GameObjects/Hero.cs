﻿using Alterblade.GameObjects.Statuses;
using Alterblade.Modes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alterblade.GameObjects
{
	internal class Hero
	{

		#region Constants

		readonly static Hero none = new Hero("Unknown", "???", new Dictionary<Stats, int>(), new List<Skill>(), null);

		#endregion

		#region Fields

		readonly string name;
		readonly string title;

		readonly List<Skill> skills;
		readonly List<Hero> team;
		readonly Dictionary<Stats, int> baseStats = new Dictionary<Stats, int>();
		readonly Dictionary<Stats, int> currentStats = new Dictionary<Stats, int>();
		readonly List<Status> statuses = new List<Status>();
		bool isAlive = true;
		bool isSupressed = false;

		#endregion

		#region Properties
		public string Name => name;
		public string Title => title;
		public List<Skill> Skills => skills;
		public Dictionary<Stats, int> BaseStats => baseStats;
		public Dictionary<Stats, int> CurrentStats => currentStats;
		public List<Status> Statuses => statuses;
		public List<Hero> Team => team;
		public Skill LastSkillUsed { get; set; }
		public Skill LastSkillHit { get; set; }
		public Hero PriorityTarget { get; set; }
		public Hero LastHeroAttacker { get; set; }
		public bool IsAlive => isAlive;
		public bool IsSupressed => isSupressed;
		public static Hero None => none;

		#endregion

		#region Constructor

		public Hero(string name, string title, Dictionary<Stats, int> stats, List<Skill> skills, List<Hero>? team)
		{
			this.name = name;
			this.title = title;
			baseStats = new Dictionary<Stats, int>(stats);
			currentStats = new Dictionary<Stats, int>(stats);

			// Cloning skills
			this.skills = new List<Skill>();
			for (int i = 0; i < skills.Count; i++)
				this.skills.Add(new Skill(skills[i]));

			this.team = team is null ? new List<Hero>() : team;

			LastHeroAttacker = none;
			PriorityTarget = none;
			LastSkillHit = Skill.None;
			LastSkillUsed = Skill.None;
		}

		#endregion

		#region Methods

		public void TakeDamage(int trueAmount, bool showText, bool isCrit = false)
		{
			if (!isAlive) {
				Utils.Error("Damage to a dead hero detected.");
				return;
			}

			currentStats[Stats.HP] = Math.Clamp(currentStats[Stats.HP] - trueAmount, 0, baseStats[Stats.HP]);
			if (showText)
			{
				string output = new StringBuilder().AppendFormat("{0} takes {1} damage! {2}", name, trueAmount, isCrit ? "[red]It's a critical hit![/red]" : "").ToString();
				Utils.WriteEmbeddedColorLine(output);
			}
			CheckAliveCondition();
		}

		public void TakeDamage(int baseDamage, int attackerAttack, bool showText, bool isCrit)
		{
			int damage = CalculateDamage(baseDamage, isCrit, attackerAttack, currentStats[Stats.DEFENSE], baseStats[Stats.DEFENSE]);
			TakeDamage(damage, showText, isCrit);
		}

		public void TakeDamage(int baseDamage, int attackerAttack, bool showText)
		{
			TakeDamage(baseDamage, attackerAttack, showText, Utils.RollBoolean(currentStats[Stats.CRIT_CHANCE] * 0.01F));
		}

		public void TakeDamage(float percent, bool showText)
		{
			TakeDamage(Convert.ToInt32(baseStats[Stats.HP] * percent), showText);
		}

		public void Heal(int amount, bool showText)
		{
			currentStats[Stats.HP] = Math.Clamp(currentStats[Stats.HP] + amount, 0, 1000);
			if (showText) {
				Utils.WriteEmbeddedColorLine(new StringBuilder().AppendFormat("{0} regained [yellow]{1}[/yellow] HP!", name, amount).ToString());
			}
			CheckAliveCondition();
		}

		public void DoSkill(Battle battle)
		{
			Skill skill;
			DisplaySkills();
			while (true)
			{
				skill = skills[Utils.GetInteger(1, 4, "[yellow]Skill:[/yellow] ") - 1];
				if (skill.Activate(this, battle)) break;
			}
			LastSkillUsed = skill;
		}

		public bool ModifyStats(Stats stat, int amount)
		{
			if (amount == 0) { return false; }

			if (Convert.ToInt32(stat) < 1 && Convert.ToInt32(stat) > 3)
			{
				Utils.Error("Such Stat cannot be modified.");
				return false;
			}

			if ( stat == Stats.RANDOM ) { stat = (Stats)Utils.Random.Next(1, 4); }

			amount = Math.Clamp(amount, -3, 3);
			int delta = Convert.ToInt32(baseStats[stat] * 0.25F);
			string prefix = amount < 0 ? "fell" : "rose";
			string adverb = "";

			if (Math.Abs(amount) > 1) {  adverb = (Math.Abs(amount) == 2 ? "sharply" : "drastically") + " "; }

			StringBuilder output = new StringBuilder();

			if (currentStats[stat] <= baseStats[stat] - (delta * 3) || currentStats[stat] >= baseStats[stat] + (delta * 6))
			{
				prefix = amount < 0 ? "lowered" : "raised";
				output.AppendFormat("{0}'s {1} cannot be {2} anymore!", name, stat.ToString(), prefix); 
				Utils.Error(output.ToString());
				return false;
			}
			else
			{
				currentStats[stat] = currentStats[stat] + (delta * amount);
				output.AppendFormat("{0}'s {1} {2}{3}!", name, stat.ToString(), adverb, prefix);
				Utils.WriteEmbeddedColorLine(output.ToString());
				return true;
			}
		}

		public void UpdateStatuses(UpdateType updateType)
		{
			for (int i = 0; i < statuses.Count; i++)
			{
				if (statuses[i].UpdateType == updateType)
					statuses[i].Update();
			}
		}

		public bool AddStatus(Status status)
		{
			for (int i = 0; i < statuses.Count; i++)
			{
				if (status.Name == statuses[i].Name)
				{
					Utils.Error("Status already exists!");
					return false;
				}
			}
			statuses.Add(status);
			return true;
		}

		public bool RemoveStatus(Status status)
		{
			return statuses.Remove(status);
		}

		void CheckAliveCondition()
		{
			if (currentStats[Stats.HP] < 1)
			{
				isAlive = false;
				Utils.WriteEmbeddedColorLine(new StringBuilder().AppendFormat("{0} had fallen in battle!", name).ToString());
				team.Remove(this);
			}
		}

		public void DisplayStats()
		{
			StringBuilder output = new StringBuilder();
			output.Append("\n[yellow]HP[/yellow]: ");
			int spaceCount = (int)Math.Ceiling(20F * currentStats[Stats.HP] / baseStats[Stats.HP]);
			output.Append("[green]~");
			for (int i = 0; i < spaceCount; i++)
				output.Append(' ');
			output.Append("[/green]");
			output.Append("[magenta]~");
			for (int i = 0; i < 20 - spaceCount; i++)
				output.Append(' ');
			output.Append("[/magenta]");
			output.AppendFormat(" {0}/{1}\n", currentStats[Stats.HP], baseStats[Stats.HP]);
			output.AppendFormat(
				"[yellow]ATK[/yellow]: {0, -7} [yellow]DEF[/yellow]: {1, -7} [yellow]SPE[/yellow]: {2, -7} [yellow]CRI[/yellow]: {3}%\n",
			 	new StringBuilder().AppendFormat("{0, 3}/{1, -3}", currentStats[Stats.ATTACK], baseStats[Stats.ATTACK]).ToString().Trim(),
				new StringBuilder().AppendFormat("{0, 3}/{1, -3}", currentStats[Stats.DEFENSE], baseStats[Stats.DEFENSE]).ToString().Trim(),
				new StringBuilder().AppendFormat("{0, 3}/{1, -3}", currentStats[Stats.SPEED], baseStats[Stats.SPEED]).ToString().Trim(),
				currentStats[Stats.CRIT_CHANCE]
			);
			output.Append("[yellow]STATUS[/yellow]: ");
			for (int i = 0; i < statuses.Count; i++)
			{
				output.AppendFormat("[cyan]{0} ({1})[/cyan]", statuses[i].Name, statuses[i].Duration);
				if (i != statuses.Count - 1)
					output.Append(", ");
			}
			output.AppendLine();
			Utils.WriteEmbeddedColor(output.ToString());
		}

		public void DisplaySkills()
		{
			StringBuilder output = new StringBuilder();
			for (int i = 0; i < skills.Count; i++)
			{
				output.AppendFormat(
					"  [cyan]{0, -20}[/cyan] | {1, -3} | {2, -4}| {3}\n",
					skills[i].Name,
					skills[i].BaseDamage < 1 ? "-" : skills[i].BaseDamage,
					skills[i].Accuracy <= 0 ? '-' : new StringBuilder().Append(Convert.ToInt32(skills[i].Accuracy * 100)).Append('%'),
					skills[i].SkillPoint
				);
			}
			Utils.WriteEmbeddedColorLine(output.ToString());
		}

		#endregion

		#region Statics

		public static int CalculateDamage(int baseDamage, Hero source, Hero target, bool isCrit)
		{
			return CalculateDamage(baseDamage, isCrit, source.CurrentStats[Stats.ATTACK], target.CurrentStats[Stats.DEFENSE], target.BaseStats[Stats.DEFENSE]);
		}

		public static int CalculateDamage(int baseDamage, bool isCrit, int attackerAttack, int targetDefense, int targetBaseDefense)
		{
			if (isCrit)
				targetDefense = Math.Clamp(targetDefense, 0, targetBaseDefense);
			float multiplier = isCrit ? 1.5F : 1F;
			float staple = (30F * baseDamage * attackerAttack / (targetDefense * 40F)) + 15F;
			float bonusMultiplier = 0.9F + Convert.ToSingle(0.2F * Utils.Random.NextDouble());
			return Convert.ToInt32(staple * bonusMultiplier * multiplier);
		}

		#endregion

	}
}