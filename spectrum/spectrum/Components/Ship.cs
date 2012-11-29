﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Spectrum.Library.Graphics;
using Spectrum.Library.Paths;
using Spectrum.Library.Collisions;

namespace Spectrum.Components
{
    public class Ship : Sprite, PathAware
    {
        public Ship(PlayerIndex playerIndex, string label) : base("ship")
        {
            HealthBar = new HealthBar(this, label);
            CurrentHealthPoints = MaxHealthPoints = 200;
            Origin = new Vector2(Width / 2, Height / 2);
            BoundingArea = new Sphere(Position, 0.2f * (Height / 2), 0.2f * (Width / 2));
            Scale = 0.2f;
            Layer = Layers.Player + 0.001f * (float)playerIndex;
            SetTint(Color.Black);
            PlayerIndex = playerIndex;
            LaserFireRateCounter = 0.0f;
            LaserCharge = 0.0f;
            FeedbackTime = 0.0f;
        }

        public void PathPosition(Vector2 position)
        {
            Position = position;
        }

        public void PathDirection(float angle)
        {
            Rotation = angle;
            BoundingArea.Shape.Rotation = angle;
        }

        public override void Draw(GameTime gameTime, SpriteBatch targetSpriteBatch)
        {
            base.Draw(gameTime, targetSpriteBatch);
            HealthBar.Draw(gameTime, targetSpriteBatch);
        }

        public override void ProcessHit(Laser laser)
        {
            Color oldTint = Tint;
            LoseTint(laser.Tint);
            if (oldTint == Tint)
            {
                base.ProcessHit(laser);
            }
        }

        public Explosion GetExplosion(double destroyedTime)
        {
            return new Explosion(this, destroyedTime);
        }

        public Path Path;
        public HealthBar HealthBar;
        public float LaserFireRateCounter, LaserCharge, FeedbackTime;
        public PlayerIndex PlayerIndex { get; protected set; }
    }
}
