﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spectrum.Library.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spectrum.Library.Collisions;

namespace Spectrum.Components
{
    public class PowerCore : Sprite
    {
        private static readonly int TEXTURE_HEIGHT = 129;
        private static readonly int TEXTURE_WIDTH = 129;

        /// <summary>
        /// The Power Core Health at the beggining of the game
        /// </summary>
        private static int INITIAL_HEALTH = 1000;

        /// <summary>
        /// The interval at which the Core Regains Health (in ms).
        /// </summary>
        private static readonly int REGEN_INTERVAL = 100;

        /// <summary>
        /// The amount of HP the Core Regains at each Health Regeneration
        /// </summary>
        private static readonly int REGEN_RATE = 1;

        private static readonly int FORCEFIELD_RECHARGE_INTERVAL = 10; // sec

        private enum State {Normal, Destroyed}

        public PowerCore(Random RNG) : base("powercore")
        {
            this.RNG = RNG;
            Health = INITIAL_HEALTH;
            TimeElapsedSinceLastRegen = new TimeSpan(0);
            ForcefieldRechargeTime = new TimeSpan(0);
            CurrentState = State.Normal;

            // Place the PowerCore at the center of the ViewPort
            Viewport viewPort = Application.Instance.GraphicsDevice.Viewport;
            Position = new Vector2(viewPort.Width/2, viewPort.Height/2);
            Origin = new Vector2(TEXTURE_WIDTH / 2, TEXTURE_HEIGHT / 2);

            BoundingSphere = new Sphere(Position, CalculateCurrentRadius());
            CreateRandomForcefield();
        }
        
        public void Update(GameTime gameTime)        
        {
            switch (CurrentState) 
            {
                case State.Normal:
                    TimeElapsedSinceLastRegen = TimeElapsedSinceLastRegen.Add(gameTime.ElapsedGameTime);
                    if (TimeElapsedSinceLastRegen.TotalMilliseconds > REGEN_INTERVAL) 
                    {
                        RegainHealth();
                        TimeElapsedSinceLastRegen = new TimeSpan(0);
                    }
                    Scale = CalculateCurrentScale();
                    float radius = CalculateCurrentRadius();

                    if (Forcefield != null)
                    {
                        Forcefield.Scale = 0.2f * Scale;
                        radius *= 1.2f;
                    }
                    else
                    {
                        ForcefieldRechargeTime = ForcefieldRechargeTime.Add(gameTime.ElapsedGameTime);
                        if (ForcefieldRechargeTime.TotalSeconds > FORCEFIELD_RECHARGE_INTERVAL)
                        {
                            CreateRandomForcefield();
                            ForcefieldRechargeTime = new TimeSpan(0);
                        }
                    }

                    BoundingSphere.Circle.ChangeRadius(radius, radius);
                    break;

                case State.Destroyed:
                    break;
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch targetSpriteBatch)
        {
            switch (CurrentState)
            {
                case State.Normal:
                    base.Draw(gameTime, targetSpriteBatch);
                    if (Forcefield != null) Forcefield.Draw(gameTime, targetSpriteBatch);
                    break;

                case State.Destroyed:
                    break;
            }
        }

        public Sphere GetBoundingSphere() 
        {
            return BoundingSphere;
        }

        /// <summary>
        /// Returns the current Health of the Power Core (in HP)
        /// </summary>
        public int GetHealth() 
        {
            return Health;
        }

        /// <summary>
        /// Decrease the Core's health by the passed Damage.
        /// The health will always be >= 0;
        /// </summary>
        /// <param name="Damage">The Damage to decrease the health by (in HP).</param>
        public void ProcessHit(Color laserColor, int damage) 
        {
            damage = (int)MathHelper.Clamp(damage, 0, Health);

            if (Forcefield != null)
            {
                if (Forcefield.Tint == laserColor)
                {
                    Forcefield.Health -= damage;
                    if (Forcefield.Health <= 0)
                    {
                        Application.Instance.Drawables.Remove(Forcefield);
                        Forcefield = null;
                    }
                }
            }
            else
            {
                Health -= damage;
            }
            if (Health <= 0) 
            {
                DestroyCore();
                if (Observer != null)
                {
                    Observer.OnPowerCoreHealthReachedZero();
                }
            }
        }

        private void DestroyCore() 
        {
            CurrentState = State.Destroyed;
            BoundingSphere = new Sphere(Position, 0);
            Texture = null;
        }

        /// <summary>
        /// Returns the new radius of the Core's Bounding Sphere.
        /// </summary>
        /// <returns></returns>
        private float CalculateCurrentRadius()
        {
            return TEXTURE_HEIGHT / 2 * CalculateCurrentScale();
        }

        /// <summary>
        /// Returns the scale Factor to apply on the core so that it reflects its health.
        /// </summary>
        /// <returns></returns>
        private float CalculateCurrentScale()
        {
            return Health/1000f + 0.5f;
        }

        /// <summary>
        /// Increase the Power Core's health
        /// </summary>
        private void RegainHealth() 
        {
            Health += REGEN_RATE;
        }

        public void CreateRandomForcefield()
        {
            Color color = Color.Black;
            switch (RNG.Next(7))
            {
                case 0: color = Color.Red; break;
                case 1: color = Color.Lime; break;
                case 2: color = Color.Blue; break;
                case 3: color = Color.Cyan; break;
                case 4: color = Color.Magenta; break;
                case 5: color = Color.Yellow; break;
                case 6: color = Color.White; break;
            }
            Forcefield = new Forcefield(Position, color);
            Forcefield.Health = 100;
            Application.Instance.Drawables.Add(Forcefield);
        }

        /// <summary>
        /// This object's observer
        /// </summary>
        public EventObservers.PowerCoreObserver Observer;

        private Random RNG;
        private int Health;
        private State CurrentState;
        private Sphere BoundingSphere;
        private TimeSpan TimeElapsedSinceLastRegen, ForcefieldRechargeTime;
        private Forcefield Forcefield;
    }
}
