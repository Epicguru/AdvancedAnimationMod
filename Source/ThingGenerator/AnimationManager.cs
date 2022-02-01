﻿using AAM.Tweaks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AAM
{
    public class AnimationManager : MapComponent
    {
        public static Texture2D HandTexture;

        private static int lastTickedFrame = -1;

        public static void Init()
        {
            HandTexture = ContentFinder<Texture2D>.Get("AAM/Hand");
        }

        private List<(Pawn pawn, Vector2 position)> labels = new List<(Pawn pawn, Vector2 position)>();

        public AnimationManager(Map map) : base(map)
        {

        }

        /// <summary>
        /// Attempts to start an animation given an animation def, and a position (<paramref name="rootTransform"/>).
        /// You may also optionally supply one or more pawns to participate in the animation, if the animation requires them.
        /// The caller should pass in as many pawns as the animation is designed for. Passing in too few or too many pawns may lead
        /// to undefined behaviour.
        /// Passing in an 'invalid' pawn (one that is dead, downed, or otherwise inadequate for the animation) will result in the animation
        /// being immediately cancelled and never started. Check the <see cref="AnimRenderer.IsDestroyed"/> field of the return value to check that
        /// the animation started successfully.
        /// </summary>
        /// <returns>The created animation renderer, or null if the <paramref name="def"/> was null.</returns>
        public AnimRenderer StartAnimation(AnimDef def, Matrix4x4 rootTransform, params Pawn[] pawns)
        {
            return StartAnimation(def, rootTransform, false, false, pawns);
        }

        /// <summary>
        /// Attempts to start an animation given an animation def, and a position (<paramref name="rootTransform"/>).
        /// You may also optionally supply one or more pawns to participate in the animation, if the animation requires them.
        /// The caller should pass in as many pawns as the animation is designed for. Passing in too few or too many pawns may lead
        /// to undefined behaviour.
        /// Passing in an 'invalid' pawn (one that is dead, downed, or otherwise inadequate for the animation) will result in the animation
        /// being immediately cancelled and never started. Check the <see cref="AnimRenderer.IsDestroyed"/> field of the return value to check that
        /// the animation started successfully.
        /// </summary>
        /// <returns>The created animation renderer, or null if the <paramref name="def"/> was null.</returns>
        public AnimRenderer StartAnimation(AnimDef def, Matrix4x4 rootTransform, bool mirrorX, params Pawn[] pawns)
        {
            return StartAnimation(def, rootTransform, mirrorX, false, pawns);
        }

        /// <summary>
        /// Attempts to start an animation given an animation def, and a position (<paramref name="rootTransform"/>).
        /// You may also optionally supply one or more pawns to participate in the animation, if the animation requires them.
        /// The caller should pass in as many pawns as the animation is designed for. Passing in too few or too many pawns may lead
        /// to undefined behaviour.
        /// Passing in an 'invalid' pawn (one that is dead, downed, or otherwise inadequate for the animation) will result in the animation
        /// being immediately cancelled and never started. Check the <see cref="AnimRenderer.IsDestroyed"/> field of the return value to check that
        /// the animation started successfully.
        /// </summary>
        /// <returns>The created animation renderer, or null if the <paramref name="def"/> was null.</returns>
        public virtual AnimRenderer StartAnimation(AnimDef def, Matrix4x4 rootTransform, bool mirrorX, bool mirrorY, params Pawn[] pawns)
        {
            if (def?.Data == null)
                return null;

            var renderer = new AnimRenderer(def, map);
            renderer.RootTransform = rootTransform;
            renderer.MirrorHorizontal = mirrorX;
            renderer.MirrorVertical = mirrorY;

            foreach(var pawn in pawns)            
                renderer.AddPawn(pawn);            

            renderer.Register();

            return renderer;
        }

        /// <summary>
        /// Stops the animation that the pawn is currently part of.
        /// If the pawn is not part of any animation, this does nothing.
        /// </summary>
        public void StopAnimation(Pawn pawn)
        {
            if (pawn == null)
                return;

            var renderer = AnimRenderer.TryGetAnimator(pawn);
            StopAnimation(renderer);
        }

        private void StopAnimation(AnimRenderer renderer)
        {
            if (renderer != null && !renderer.IsDestroyed)
                renderer.Destroy();
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            float dt = Time.deltaTime * Find.TickManager.TickRateMultiplier * (Input.GetKey(KeyCode.LeftShift) ? 0.1f : 1f);
            Draw(dt);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (GenTicks.TicksAbs == lastTickedFrame)
                return;
            lastTickedFrame = GenTicks.TicksAbs;

            AnimRenderer.UpdateAll();
        }

        public void Draw(float deltaTime)
        {
            labels.Clear();
            AnimRenderer.DrawAll(deltaTime, map, DrawLabel);
        }

        private void DrawLabel(Pawn pawn, Vector2 position)
        {
            labels.Add((pawn, position));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Core.Warn("MAP EXPOSE");
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            Core.Warn("MAP REMOVED");
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Core.Warn("FINISH INIT");
        }

        public override void MapGenerated()
        {
            base.MapGenerated();
            Core.Warn("GENERATED");
        }
    }
}
