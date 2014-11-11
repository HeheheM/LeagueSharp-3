using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using xSLx_Orbwalker;
using Color = System.Drawing.Color;

namespace Ultimate_Carry_Prevolution.Plugin
{
    class Syndraz : Champion
    {
        public Syndraz()
        {
            SetSpells();
            LoadMenu();
        }

        private void SetSpells()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W, 1500);
            W.SetSkillshot(0.6f, 60f, float.MaxValue, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(0.7f, 120f, 1750f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 25000f);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);
        }

        private void LoadMenu()
        {
            var champMenu = new Menu("Jinx Plugin", "Jinx");
            {
                var SpellMenu = new Menu("SpellMenu", "SpellMenu");
                {
                    var qMenu = new Menu("QMenu", "QMenu");
                    {
                        qMenu.AddItem(new MenuItem("Q_Auto_Immobile", "Auto Q on Immobile").SetValue(true));
                        SpellMenu.AddSubMenu(qMenu);
                    }

                    var wMenu = new Menu("WMenu", "WMenu");
                    {
                        wMenu.AddItem(new MenuItem("Auto_W_Immobile", "Auto W Immobile").SetValue(false));
                        wMenu.AddItem(new MenuItem("W_Only_Orb", "Only Pick Up Orb").SetValue(false));
                        SpellMenu.AddSubMenu(wMenu);
                    }

                    var eMenu = new Menu("EMenu", "EMenu");
                    {
                        eMenu.AddItem(new MenuItem("E_Enemy_Into_ball", "If ball is behind target").SetValue(true));
                        SpellMenu.AddSubMenu(eMenu);
                    }

                    var rMenu = new Menu("RMenu", "RMenu");
                    {
                        rMenu.AddItem(new MenuItem("R_Overkill_Check", "Overkill Check").SetValue(true));

                        rMenu.AddSubMenu(new Menu("Don't use R on", "Dont_R"));
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != MyHero.Team)
                        )
                            rMenu.SubMenu("Dont_R")
                                .AddItem(new MenuItem("Dont_R" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

                        SpellMenu.AddSubMenu(rMenu);
                    }

                    champMenu.AddSubMenu(SpellMenu);
                }

                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddSpelltoMenu(comboMenu, "Q", true);
                    AddSpelltoMenu(comboMenu, "QE", true, "Use QE");
                    AddSpelltoMenu(comboMenu, "W", true);
                    AddSpelltoMenu(comboMenu, "E", true);
                    AddSpelltoMenu(comboMenu, "R", true);
                    champMenu.AddSubMenu(comboMenu);
                }

                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddSpelltoMenu(harassMenu, "Q", true);
                    AddSpelltoMenu(harassMenu, "QE", true, "Use QE");
                    AddSpelltoMenu(harassMenu, "W", true);
                    AddSpelltoMenu(harassMenu, "E", true);
                    AddManaManagertoMenu(harassMenu, 30);
                    champMenu.AddSubMenu(harassMenu);
                }

                var laneClearMenu = new Menu("LaneClear", "LaneClear");
                {
                    AddSpelltoMenu(laneClearMenu, "Q", true);
                    AddSpelltoMenu(laneClearMenu, "W", true);
                    AddSpelltoMenu(laneClearMenu, "E", true);
                    AddManaManagertoMenu(harassMenu, 30);
                    champMenu.AddSubMenu(laneClearMenu);
                }

                var miscMenu = new Menu("Misc", "Misc");
                {
                    miscMenu.AddItem(new MenuItem("QE_Gap_Closer", "Use QE to Interrupt").SetValue(true));
                    miscMenu.AddItem(new MenuItem("E_Gap_Closer", "Use E On Gap Closer").SetValue(true));
                    champMenu.AddSubMenu(miscMenu);
                }

                var drawMenu = new Menu("Drawing", "Drawing");
                {
                    drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                    drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                    drawMenu.AddItem(new MenuItem("Draw_R_Killable", "Draw R Mark on Killable").SetValue(true));

                    MenuItem drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                    drawMenu.AddItem(drawComboDamageMenu);
                    Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                    Utility.HpBarDamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                    drawComboDamageMenu.ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs eventArgs)
                        {
                            Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                        }; 

                    champMenu.AddSubMenu(drawMenu);
                }
            }

            Menu.AddSubMenu(champMenu);
            Menu.AddToMainMenu();
        }

        private IEnumerable<SpellSlot> GetSpellCombo()
        {
            var spellCombo = new List<SpellSlot>();
            if (Q.IsReady())
                spellCombo.Add(SpellSlot.Q);
            if (W.IsReady())
                spellCombo.Add(SpellSlot.W);
            if (E.IsReady())
                spellCombo.Add(SpellSlot.E);
            if (R.IsReady())
                spellCombo.Add(SpellSlot.R);
            return spellCombo;
        }

        private float GetComboDamage(Obj_AI_Base target)
        {
            double comboDamage = (float)ObjectManager.Player.GetComboDamage(target, GetSpellCombo());
            return (float)(comboDamage + ObjectManager.Player.GetAutoAttackDamage(target));
        }


        public override void OnDraw()
        {
            if (Menu.Item("Draw_Disabled").GetValue<bool>())
            {
                xSLxOrbwalker.DisableDrawing();
                return;
            }
            xSLxOrbwalker.EnableDrawing();

            if (Menu.Item("Draw_W").GetValue<bool>())
                if (W.Level > 0)
                    Utility.DrawCircle(MyHero.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);

            if (Menu.Item("Draw_E").GetValue<bool>())
                if (E.Level > 0)
                    Utility.DrawCircle(MyHero.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (Menu.Item("Draw_R").GetValue<bool>())
                if (R.Level > 0)
                    Utility.DrawCircle(MyHero.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);

            if (Menu.Item("Draw_R_Killable").GetValue<bool>() && R.IsReady())
            {
                foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(R.Range) && !x.IsDead && x.IsEnemy).OrderBy(x => x.Health))
                {
                    var health = unit.Health + unit.HPRegenRate + 10;
                    if (ObjectManager.Player.GetSpellDamage(unit, SpellSlot.R) > health)
                    {
                        Vector2 wts = Drawing.WorldToScreen(unit.Position);
                        Drawing.DrawText(wts[0] - 20, wts[1], Color.White, "KILL!!!");
                    }
                }
            }
        }

        public override void OnPassive()
        {
            var Q_Target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (Menu.Item("Q_Auto_Immobile").GetValue<bool>() && Q_Target != null)
                if (Q.GetPrediction(Q_Target).Hitchance == HitChance.Immobile)
                    Q.Cast(Q_Target);

            //todo
            var W_Target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            if (Menu.Item("Auto_W_Immobile").GetValue<bool>() && W_Target != null)
                if (W.GetPrediction(W_Target).Hitchance == HitChance.Immobile)
                    W.Cast(Q_Target);
        }

        public override void OnCombo()
        {
            if (IsSpellActive("Q"))
                Cast_Q();
            if (IsSpellActive("W"))
                Cast_W();
            if (IsSpellActive("E"))
                Cast_E();
            if (IsSpellActive("R"))
                Cast_R();
            if (IsSpellActive("QE"))
                Cast_QE();
        }

        private void Cast_Q()
        {
            var Q_Target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (Q_Target == null)
                return;

            var Q_Pred = Q.GetPrediction(Q_Target);
            if (Q_Pred.Hitchance >= HitChance.High)
                Q.Cast(Q_Pred.CastPosition, UsePackets());
        }

        private void Cast_W()
        {
            
        }

        private void Cast_E()
        {
            
        }

        private void Cast_R()
        {
            
        }
        private void Cast_QE()
        {

        }
    }
}
