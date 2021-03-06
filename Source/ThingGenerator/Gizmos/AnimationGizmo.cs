using System;
using AAM.Data;
using AAM.Grappling;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace AAM.Gizmos
{
    public class AnimationGizmo : Gizmo
    {
        private Pawn pawn;
        private PawnMeleeData data;

        public AnimationGizmo(Pawn pawn)
        {
            this.pawn = pawn;
            this.data = Current.Game.GetComponent<GameComp>().GetOrCreateData(pawn); // TODO optimize.
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var butRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            try
            {
                Text.Font = GameFont.Tiny;

                // Mouseover and highlight.
                MouseoverSounds.DoRegion(butRect, SoundDefOf.Mouseover_Command);
                if (parms.highLight)
                    QuickSearchWidget.DrawStrongHighlight(butRect.ExpandedBy(12f));

                bool mouseOver = Mouse.IsOver(butRect);

                Widgets.DrawBoxSolidWithOutline(butRect, new Color32(21, 25, 29, 255), Color.white * 0.75f);

                Rect left = butRect.ExpandedBy(-2).LeftPartPixels(34);
                Rect tl = left.TopPartPixels(34);
                Rect bl = left.BottomPartPixels(34);

                Rect right = butRect.ExpandedBy(-2).RightPartPixels(34);
                Rect tr = right.TopPartPixels(34);
                Rect br = right.BottomPartPixels(34);

                Rect middle = butRect.ExpandedBy(-2).ExpandedBy(-36, 0);

                Rect tm = middle.TopPartPixels(34).ExpandedBy(-2, 0);
                Rect bm = middle.BottomPartPixels(34).ExpandedBy(-2, 0);

                DrawAutoExecute(tl);
                DrawAutoGrapple(bl);
                DrawInfo(tr);
                DrawSkill(br);
                DrawExecuteTarget(tm);
                DrawLassoTarget(bm);

                string pawnName = null;
                if (Find.Selector.SelectedPawns.Count > 1)
                    pawnName = pawn.NameShortColored;
                string labelCap = $"Advanced Melee{(pawnName != null ? $" ({pawnName})" : "")}";

                if (!labelCap.NullOrEmpty())
                {
                    Text.Font = GameFont.Tiny;
                    float num = Text.CalcHeight(labelCap, butRect.width);
                    Rect rect2 = new Rect(butRect.x, butRect.yMax - num + 12f, butRect.width, num);
                    GUI.DrawTexture(rect2, TexUI.GrayTextBG);
                    Text.Anchor = TextAnchor.UpperCenter;
                    Widgets.Label(rect2, labelCap);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }

                GUI.color = Color.white;

                bool HasLos(IntVec3 c)
                {
                    return GenSight.LineOfSight(pawn.Position, c, pawn.Map);
                }

                if (Event.current.type == EventType.Repaint && Find.Targeter.IsTargeting && Find.Targeter.IsPawnTargeting(pawn))
                {
                    pawn.GetAnimManager().AddPostDraw(() =>
                    {
                        GenDraw.DrawRadiusRing(pawn.Position, 10, Color.green, HasLos);
                    });
                }

                var state = mouseOver ? GizmoState.Mouseover : GizmoState.Clear;
                return new GizmoResult(state, null);
            }
            catch (Exception e)
            {
                Text.Font = GameFont.Tiny;
                Widgets.DrawBoxSolid(butRect, Color.black);
                Widgets.Label(butRect, $"<color=red>Exception drawing AdvancedMelee gizmo:\n{e}</color>");
                Text.Font = GameFont.Small;
                Core.Error($"Exception drawing gizmo for {pawn.NameFullColored}!", e);
                return default;
            }
        }

        private void DrawIcon(Rect rect, Texture icon, Color color)
        {
            GUI.DrawTexture(rect, Content.IconBG);

            GUI.color = color;
            GUI.DrawTexture(rect, icon);
            GUI.color = Color.white;

            Widgets.DrawHighlightIfMouseover(rect);
        }

        private void DrawAutoExecute(Rect rect)
        {
            var mode = data.AutoExecute;
            (string status, Color color) = mode switch
            {
                AutoOption.Default => ($"Default ({(Core.Settings.AutoExecute ? AutoOption.Enabled : AutoOption.Disabled)})", Color.grey),
                AutoOption.Enabled => ("Enabled", Color.green),
                AutoOption.Disabled => ("Disabled", Color.red),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (mode == AutoOption.Default)
                mode = Core.Settings.AutoExecute ? AutoOption.Enabled : AutoOption.Disabled;
            string tint = mode == AutoOption.Enabled ? "green" : "red";
            status = $"<b><color={tint}>{status}</color></b>";

            DrawIcon(rect, Content.IconExecute, color);

            bool lasso = data.AutoGrapple switch
            {
                AutoOption.Default => Core.Settings.AutoGrapple,
                AutoOption.Enabled => true,
                AutoOption.Disabled => false,
                _ => throw new ArgumentOutOfRangeException()
            };
            string grappleStatus = lasso
                ? "Since <i>Auto Lasso</i> is <b><color=green>enabled</color></b>, this pawn may automatically grab distant enemies to drag them in and execute them."
                : "Since <i>Auto Lasso</i> is <b><color=red>disabled</color></b>, this pawn will only automatically execute enemies when they are standing right next to them (horizontally).";
            TooltipHandler.TipRegion(rect, $"<b>Auto Execute</b>: {status}\n\nAllows this pawn to automatically perform executions on enemies.{(mode == AutoOption.Enabled ? $"\n\n{grappleStatus}" : "")}");

            if (Widgets.ButtonInvisible(rect))
                data.AutoExecute = (AutoOption)(((int)data.AutoExecute + 1) % 3);
        }

        private void DrawAutoGrapple(Rect rect)
        {
            var mode = data.AutoGrapple;
            (string status, Color color) = mode switch
            {
                AutoOption.Default => ($"Default ({(Core.Settings.AutoGrapple ? AutoOption.Enabled : AutoOption.Disabled)})", Color.grey),
                AutoOption.Enabled => ("Enabled", Color.green),
                AutoOption.Disabled => ("Disabled", Color.red),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (mode == AutoOption.Default)
                mode = Core.Settings.AutoExecute ? AutoOption.Enabled : AutoOption.Disabled;
            string tint = mode == AutoOption.Enabled ? "green" : "red";
            status = $"<b><color={tint}>{status}</color></b>";

            DrawIcon(rect, Content.IconGrapple, color);

            TooltipHandler.TipRegion(rect, $"<b>Auto Lasso</b>: {status}\n\nAllows this pawn to automatically use their lasso to pull in distant enemies into melee range.");

            if (Widgets.ButtonInvisible(rect))
                data.AutoGrapple = (AutoOption)(((int)data.AutoGrapple + 1) % 3);
        }

        private void DrawInfo(Rect rect)
        {
            DrawIcon(rect, Content.IconInfo, Color.cyan);

            var weapon = pawn.GetFirstMeleeWeapon();
            // yes the " 1" is intentional: Rimworld doesn't account for RichText in text size calculation.
            string msg = $"Weapon: {(weapon == null ? "<i>None</i>" : weapon.LabelCap)}";
            TooltipHandler.TipRegion(rect, msg);
        }

        private void DrawSkill(Rect rect)
        {
            DrawIcon(rect, Content.IconSkill, Color.grey);

            TooltipHandler.TipRegion(rect, "<b>Skills:</b>\n\nThis weapon has no special skills available.\n\n<color=yellow>(Not implemented yet!)</color>");
        }

        private void DrawExecuteTarget(Rect rect)
        {
            GUI.DrawTexture(rect, Content.IconLongBG);

            bool isOffCooldown = data.IsExecutionOffCooldown(5f);
            if (!isOffCooldown)
            {
                float pct = data.GetExecuteCooldownPct(5f);
                Widgets.FillableBar(rect.BottomPartPixels(14).ExpandedBy(-4, -3), pct);
            }

            Widgets.DrawHighlightIfMouseover(rect);
            if (Widgets.ButtonInvisible(rect) && !Find.Targeter.IsTargeting)
            {
                if(!isOffCooldown)
                {
                    string name = pawn.Name.ToStringShort;
                    Messages.Message($"{name}'s execution is on cooldown!", MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                    var args = new TargetingParameters()
                    {
                        canTargetSelf = false,
                        canTargetBuildings = false,
                        validator = LassoTargetValidator
                    };
                    Find.Targeter.BeginTargeting(args, OnSelectedExecutionTarget, null, null, pawn);
                }
                
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Execute Target...");
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(rect, "Use melee weapon to stylishly kill an enemy.\n\nWill use a lasso to pull the victim into striking distance, if possible.\nOtherwise, this pawn will walk towards the target to perform the execution.");
        }

        private void DrawLassoTarget(Rect rect)
        {
            GUI.DrawTexture(rect, Content.IconLongBG);

            bool isOffCooldown = data.IsGrappleOffCooldown(5f);
            if (!isOffCooldown)
            {
                float pct = data.GetGrappleCooldownPct(5f);
                Widgets.FillableBar(rect.BottomPartPixels(14).ExpandedBy(-4, -3), pct);
            }

            Widgets.DrawHighlightIfMouseover(rect);

            // TODO check grappler conditions.

            if (Widgets.ButtonInvisible(rect) && !Find.Targeter.IsTargeting)
            {
                if(!isOffCooldown)
                {
                    string name = pawn.Name.ToStringShort;
                    Messages.Message($"{name}'s lasso is on cooldown!", MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                    var args = new TargetingParameters()
                    {
                        canTargetSelf = false,
                        canTargetBuildings = false,
                        validator = LassoTargetValidator
                    };
                    Find.Targeter.BeginTargeting(args, OnSelectedLassoTarget, null, null, pawn);
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "Lasso Target...");
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(rect, "Use a lasso to drag in a pawn.\nYou can lasso enemies or friendlies, even if they are downed.\nLassoing does not harm the target.");
        }

        private bool LassoTargetValidator(TargetInfo info)
        {
            return info.HasThing && info.Thing is Pawn { Dead: false, Spawned: true } p && p != pawn && (Core.Settings.AnimalsCanBeExecuted || !p.RaceProps.Animal);
        }

        private void OnSelectedLassoTarget(LocalTargetInfo info)
        {
            if (info.Thing is not Pawn target || target.Dead)
                return;
            var targetPos = target.Position;

            string lastReason = $"No valid position to drag {target.NameShortColored} to.";

            foreach (var pos in GrabUtility.GetIdealGrappleSpots(pawn, target, false))
            {
                if (targetPos == pos)
                    return; // Yes, return, not continue. If the target is already in a good spot, there is nothing to do.

                if (GrabUtility.CanStartGrapple(pawn, target, pos, out lastReason))
                {
                    if (JobDriver_GrapplePawn.GiveJob(pawn, target, pos, true))
                        data.TimeSinceGrappled = 0;
                    return;
                }
            }

            Messages.Message($"Cannot lasso target: {lastReason}", MessageTypeDefOf.RejectInput, false);
        }

        private void OnSelectedExecutionTarget(LocalTargetInfo info)
        {
            // TODO walk to execute.

            if (info.Thing is not Pawn target || target.Dead)
                return;

            bool canGrapple = data.IsGrappleOffCooldown(5f);
            var targetPos = target.Position;
            string lastReason = canGrapple ? $"No valid position to drag {target.NameShortColored} to." : "Lasoo is on cooldown, so the target cannot be dragged in for an execution.";

            var startParams = new AnimationStartParameters(AnimDef.GetExecutionAnimationsForWeapon(pawn.GetFirstMeleeWeapon().def).RandomElement(), pawn, target);
            // TODO start params need to change based on space available.

            foreach (var pos in GrabUtility.GetIdealGrappleSpots(pawn, target, true))
            {
                if (pos == targetPos)
                {
                    // Start immediately.
                    if (startParams.TryTrigger())
                        data.TimeSinceExecuted = 0;
                    return;
                }

                // Grapple, then execute.
                if (canGrapple && GrabUtility.CanStartGrapple(pawn, target, pos, out lastReason))
                {

                    if (JobDriver_GrapplePawn.GiveJob(pawn, target, pos, true, startParams))
                        data.TimeSinceGrappled = 0;
                    return;
                }
            }

            Messages.Message($"Cannot execute target: {lastReason}", MessageTypeDefOf.RejectInput, false);
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(180, maxWidth);
        }
    }
}
