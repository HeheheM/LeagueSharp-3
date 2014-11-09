using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using xSLx_Orbwalker;

namespace Ultimate_Carry_Prevolution.Plugin
{
	internal class Lucian : Champion
	{
		private const int QMaxRange = 1100;
		private bool PassiveUp;
		private int PassivTimer;
		public Lucian()
		{
			SetSpells();
			LoadMenu();
		}

		private void SetSpells()
		{
			Q = new Spell(SpellSlot.Q, 675);
			Q.SetTargetted(500, float.MaxValue);

			W = new Spell(SpellSlot.W, 1000);
			W.SetSkillshot(300, 80, 1600, true, SkillshotType.SkillshotCircle);

			E = new Spell(SpellSlot.E, 475);
			E.SetSkillshot(250, 1, float.MaxValue, false, SkillshotType.SkillshotLine);

			R = new Spell(SpellSlot.R, 1400);
			R.SetSkillshot(100, 110, 2800, true, SkillshotType.SkillshotLine);
		}

		private void LoadMenu()
		{
			var champMenu = new Menu("Lucian Plugin", "Lucian");
			{
				var comboMenu = new Menu("Combo", "Combo");
				{
					AddSpelltoMenu(comboMenu, "Q", true);
					AddSpelltoMenu(comboMenu, "W", true);
					AddSpelltoMenu(comboMenu, "E", true);
					champMenu.AddSubMenu(comboMenu);
				}
				var harassMenu = new Menu("Harass", "Harass");
				{
					AddSpelltoMenu(harassMenu, "Q", true);
					AddManaManagertoMenu(harassMenu, 30);
					champMenu.AddSubMenu(harassMenu);
				}
				var laneClearMenu = new Menu("LaneClear", "LaneClear");
				{

					AddManaManagertoMenu(laneClearMenu, 20);
					champMenu.AddSubMenu(laneClearMenu);
				}

				var miscMenu = new Menu("Misc", "Misc");
				{
					champMenu.AddSubMenu(miscMenu);
				}
				var drawMenu = new Menu("Drawing", "Drawing");
				{
					drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
					drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
					drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
					drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
					drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));

					var drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
					drawMenu.AddItem(drawComboDamageMenu);
					Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
					Utility.HpBarDamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
					drawComboDamageMenu.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
					{
						Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
					};

					champMenu.AddSubMenu(drawMenu);
				}
			}

			Menu.AddSubMenu(champMenu);
			Menu.AddToMainMenu();

		}

		private float GetComboDamage(Obj_AI_Base target)
		{
			var comboDamage = 0d;
			if (Q.IsReady())
			{
				comboDamage += MyHero.GetSpellDamage(target, SpellSlot.Q);
				comboDamage += MyHero.GetAutoAttackDamage(target)*1.5;
			}
			if (W.IsReady())
			{
				comboDamage += MyHero.GetSpellDamage(target, SpellSlot.W);
				comboDamage += MyHero.GetAutoAttackDamage(target)*1.5;
			}
			if (E.IsReady())
			{
				comboDamage += MyHero.GetAutoAttackDamage(target)*1.5;
			}
			if (R.IsReady())
			{
				comboDamage += MyHero.GetSpellDamage(target, SpellSlot.R)*0.3;
				comboDamage += MyHero.GetAutoAttackDamage(target)*1.5;
			}

			return (float) (comboDamage + MyHero.GetAutoAttackDamage(target));
		}
		public override void OnDraw()
		{
			if(Menu.Item("Draw_Disabled").GetValue<bool>())
			{
				xSLxOrbwalker.DisableDrawing();
				return;
			}
			xSLxOrbwalker.EnableDrawing();

			if(Menu.Item("Draw_Q").GetValue<bool>())
				if(Q.Level > 0)
				{
					Utility.DrawCircle(MyHero.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
					Utility.DrawCircle(MyHero.Position, QMaxRange, Q.IsReady() ? Color.Green : Color.Red);
				}

			if(Menu.Item("Draw_E").GetValue<bool>())
				if(E.Level > 0)
					Utility.DrawCircle(MyHero.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

			if(Menu.Item("Draw_R").GetValue<bool>())
				if(R.Level > 0)
					Utility.DrawCircle(MyHero.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
		}
		public override void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
		{
			if (unit.IsMe)
			{
				if(spell.SData.Name == "LucianQ" || spell.SData.Name == "LucianW" || spell.SData.Name == "LucianE" || spell.SData.Name == "LucianR")
				{
					PassiveUp = true;
					PassivTimer = Environment.TickCount;
					return;
				}
				if(spell.SData.Name.Contains( "Attack"))
				{
					PassiveUp = false;
				}
			}
		}

		public override void OnPassive()
		{
			if (Environment.TickCount - PassivTimer > 7000 && PassiveUp)
				PassiveUp = false;
		}

		public override void OnCombo()
		{
			if (IsSpellActive("Q"))
				Cast_Q(true);
			if(IsSpellActive("W"))
				Cast_W(true);
			if(IsSpellActive("E"))
				Cast_E(true);
		}

		public override void OnHarass()
		{
			if(IsSpellActive("Q") && ManaManagerAllowCast())
				Cast_Q(true);
			if(IsSpellActive("W") && ManaManagerAllowCast())
				Cast_W(true);
		}

		public override void OnLaneClear()
		{

			if(IsSpellActive("Q") && ManaManagerAllowCast())
				Cast_Q(false);
			if (IsSpellActive("W") && ManaManagerAllowCast() && !PassiveUp && Environment.TickCount - PassivTimer > 250)
				Cast_BasicSkillshot_AOE_Farm(W,220);
			if(IsSpellActive("E"))
				Cast_E(false);

		}

		private void Cast_Q(bool mode)
		{
			if(!Q.IsReady() || PassiveUp || Environment.TickCount -PassivTimer <250 )
				return;
			if (mode)
			{
				var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
				if(target != null)
				{
					PassivTimer = Environment.TickCount;
					Q.CastOnUnit(target, UsePackets());
					return;
				}
				target = SimpleTs.GetTarget(QMaxRange, SimpleTs.DamageType.Physical);
				if(target == null)
					return;
				foreach(var obj in ObjectManager.Get<Obj_AI_Base>().Where(obj => obj.IsValidTarget(Q.Range) && (obj.ServerPosition.To2D().Distance(MyHero.ServerPosition.To2D(), Q.GetPrediction(target).UnitPosition.To2D(), true) < 50)))
				{
					PassivTimer = Environment.TickCount;
					Q.CastOnUnit(obj, UsePackets());
					return;
				}
			}
			else
			{
				var allMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
				var minion = allMinions.FirstOrDefault(minionn => minionn.Distance(MyHero) <= Q.Range && HealthPrediction.LaneClearHealthPrediction(minionn, 500) > 0);
				if(minion == null)
					return;
				PassivTimer = Environment.TickCount;
				Q.CastOnUnit(minion, UsePackets());
			}
		}

		private void Cast_W(bool mode)
		{
			if(!W.IsReady() || PassiveUp || Environment.TickCount - PassivTimer < 250)
				return;
			var target = SimpleTs.GetTarget(W.Range + 150, SimpleTs.DamageType.Physical);
			if(target.IsValidTarget(W.Range + 150) && W.GetPrediction(target).Hitchance >= HitChance.High)
			{
				W.UpdateSourcePosition();
				PassivTimer = Environment.TickCount;
				W.Cast(target, UsePackets());
			}

		}
		private void Cast_E(bool mode)
		{
			if (mode)
			{
				if(!E.IsReady() || PassiveUp || Environment.TickCount - PassivTimer < 250)
					return;
				var target = SimpleTs.GetTarget(1100, SimpleTs.DamageType.Physical);
				if (target == null)
					return;
				PassivTimer = Environment.TickCount;
				E.Cast(Game.CursorPos, UsePackets());
			}
			else
			{
				var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, 1100, MinionTypes.All,
						MinionTeam.NotAlly);
				if(!allMinions.Where(minion => minion != null).Any(minion => minion.IsValidTarget(1100) && E.IsReady()))
					return;
				PassivTimer = Environment.TickCount;
				E.Cast(Game.CursorPos, UsePackets());
			}

		}

	}
}

