﻿using Microsoft.Xna.Framework;
using Spectrum.Components;
using Spectrum.Components.EnemyTypes;
using Spectrum.Library.States;
using Spectrum.Library.Paths;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System;
using Microsoft.Xna.Framework.Graphics;
using Spectrum.Components.EventObservers;
using Spectrum.Library.Graphics;

namespace Spectrum.States
{
    public class Game : State, PowerCoreObserver
    {
        public const float SPEED_PLAYER = 450f;
        public const float SPEED_LASER = 600f;
        public const float FIRE_RATE = 7f; // shots/sec
        public const float LASER_MAX_CHARGE_TIME = 2f; // sec
        public const float COLLISION_DISTANCE = 30f; // pixels
        public const float RECOIL_DISTANCE = 30f;
        public const float DAMAGE_FEEDBACK_TIME = 0.25f; // numbers of seconds to vibrate the controller when hurt

        public Game()
            : this(1, 0)
        {
        }

        public Game(int level, int score)
        {
            Level = level;
            EnemyWaveSize = 3 + level;
            EnemyWaveSpawnTime = 10 - level / 2f;

            RNG = new Random();
            Viewport = Application.Instance.GraphicsDevice.Viewport;
            Player = new Ship();
            Player.Position = new Vector2(Viewport.Width / 2, Viewport.Height * 4/5);
            Player.Path = new User(Player, new Rectangle(0, 0, Viewport.Width, Viewport.Height));
            Crosshair = new Crosshair();
            mBackground = new Background(2000, RNG);
            Core = new PowerCore(level, RNG);
            Core.Observer = this;
            ScoreKeeper = new ScoreKeeper(level, Player);
            Score = score;
            feedbackTime = 0f;
            EnemySpawnCounter = EnemyWaveSpawnTime;

            Application.Instance.Drawables.Add(mBackground);
            Application.Instance.Drawables.Add(Core);
            Application.Instance.Drawables.Add(Player);
            Application.Instance.Drawables.Add(Crosshair);
            Application.Instance.Drawables.Add(ScoreKeeper);

            Lasers = new List<Laser>();
            LasersToRemove = new List<Laser>();
            Enemies = new List<Enemy>();
            EnemiesToRemove = new List<Enemy>();
            Powerups = new List<Powerup>();
            PowerupsToRemove = new List<Powerup>();
            Explosions = new List<Explosion>();

            SoundPlayer.PlayMainGameSong();
        }

        public override void Destroy()
        {
            Application.Instance.Drawables.Remove(mBackground);
            Application.Instance.Drawables.Remove(ScoreKeeper);
            Application.Instance.Drawables.Remove(Core);
            Application.Instance.Drawables.Remove(Player);
            Application.Instance.Drawables.Remove(Crosshair);

            Lasers.ForEach(delegate(Laser laser) { Application.Instance.Drawables.Remove(laser); });
            LasersToRemove.ForEach(delegate(Laser laser) { Application.Instance.Drawables.Remove(laser); });

            Enemies.ForEach(delegate(Enemy enemy) { Application.Instance.Drawables.Remove(enemy); });
            EnemiesToRemove.ForEach(delegate(Enemy enemy) { Application.Instance.Drawables.Remove(enemy); });

            Powerups.ForEach(delegate(Powerup powerup) { Application.Instance.Drawables.Remove(powerup); });
            PowerupsToRemove.ForEach(delegate(Powerup powerup) { Application.Instance.Drawables.Remove(powerup); });

            Explosions.ForEach(explosion => Application.Instance.Drawables.Remove(explosion));
            Explosions.Clear();
        }

        public override bool Transition()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamepadState = GamePad.GetState(PlayerIndex.One);

            if (keyboardState.IsKeyDown(Keys.Escape) || gamepadState.Buttons.Start == ButtonState.Pressed)
            {
                GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
                SoundPlayer.ReduceMainGameSongVolume();
                SoundPlayer.PlayEffect(SoundEffectType.PauseTriggered);

                return Application.Instance.StateMachine.SetState(new States.Pause(this));
            }

            if (Player.CurrentHealthPoints <= 0)
            {
                GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f); 
                return Application.Instance.StateMachine.SetState(new States.Lost(this));
            }

            if (Core.Health <= 0)
            {
                GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f); 
                return Application.Instance.StateMachine.SetState(new States.Won(this));
            }

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            HandleForceFeedback(gameTime);
            Player.Path.Move((float)(SPEED_PLAYER * gameTime.ElapsedGameTime.TotalSeconds * (Player.IsSlowed ? Entity2D.SLOW_SPEED_MULTIPLIER : 1f)));
            Player.UpdateStatusEffects(gameTime);
            Player.HealthBar.Update(gameTime);
            ShootLaser(gameTime);
            MoveLasers(gameTime);
            Collisions(gameTime);
            UpdatePowerups(gameTime);
            SpawnRandomEnemyWave(gameTime);
            MoveEnemies(gameTime);
            EnemyAttacks(gameTime);
            Core.Update(gameTime);
            Explosions.ForEach(explosion => explosion.Update(gameTime));
        }

        public void OnPowerCoreHealthReachedZero()
        {
            // Switch to player Wins state.
        }

        public void OnPowerCoreHealthReduced(int Damage)
        {
            ScoreKeeper.AddPoints(Damage * ScoreKeeper.POWERCORE_HIT_SCORE_VALUE * Level);
        }

        private void ShootLaser(GameTime gameTime)
        {
            LaserFireRateCounter += (float)gameTime.ElapsedGameTime.TotalSeconds;
            MouseState mouseState = Mouse.GetState();
            GamePadState gamepadState = GamePad.GetState(PlayerIndex.One);
            Vector2 direction = Vector2.Zero;

            if (gamepadState.IsConnected)
            {
                Crosshair.Position = new Vector2(-1, -1);

                if (gamepadState.ThumbSticks.Right.LengthSquared() != 0)
                {
                    direction = gamepadState.ThumbSticks.Right;
                    direction.Y *= -1;
                    Player.PathDirection((float)Math.Atan2(direction.X, -direction.Y));
                }
                if (gamepadState.Triggers.Right != 0)
                    LaserCharge += (float)(gameTime.ElapsedGameTime.TotalSeconds / LASER_MAX_CHARGE_TIME);
                else
                    LaserCharge = 0;
            }
            else
            {
                int mouseX = (int)MathHelper.Clamp(mouseState.X, 1, Viewport.Width-1);
                int mouseY = (int)MathHelper.Clamp(mouseState.Y, 1, Viewport.Height-1);

                Mouse.SetPosition(mouseX, mouseY);
                Crosshair.Position = new Vector2(mouseX, mouseY);

                direction = new Vector2(mouseX, mouseY) - Player.Position;
                Player.PathDirection((float)Math.Atan2(direction.X, -direction.Y));
                if (mouseState.LeftButton == ButtonState.Pressed)
                    LaserCharge += (float)(gameTime.ElapsedGameTime.TotalSeconds / LASER_MAX_CHARGE_TIME);
                else
                    LaserCharge = 0;
            }

            LaserCharge = MathHelper.Clamp(LaserCharge, 0f, 1f);

            // more laser charge -> slower fire rate
            if (direction.LengthSquared() != 0 && LaserFireRateCounter >= (1 + LaserCharge * 3) / FIRE_RATE)
            {
                Laser laser = new Laser(Player.Tint, LaserCharge, Player.Position, direction, SPEED_LASER, LaserAlignment.Player);
                laser.Path.Move(COLLISION_DISTANCE);
                Lasers.Add(laser);
                Application.Instance.Drawables.Add(laser);
                LaserFireRateCounter = 0f;

                SoundPlayer.PlayEffect(SoundEffectType.PlayerShoots);
            }
        }

        private void EnemyAttacks(GameTime gameTime)
        {
            foreach (Enemy enemy in Enemies)
            {
                Laser laser = enemy.Attack((float)gameTime.ElapsedGameTime.TotalSeconds);
                if (laser != null)
                {
                    Lasers.Add(laser);
                    Application.Instance.Drawables.Add(laser);
                }
            }
        }

        private void MoveLasers(GameTime gameTime)
        {
            foreach (Laser laser in Lasers)
            {
                laser.Path.Move((float)(laser.Speed * gameTime.ElapsedGameTime.TotalSeconds));
                if (!laser.IsVisible(Viewport))
                {
                    LasersToRemove.Add(laser);
                }
            }
            foreach (Laser laser in LasersToRemove)
            {
                Lasers.Remove(laser);
                Application.Instance.Drawables.Remove(laser);
            }
            LasersToRemove.Clear();
        }

        private void MoveEnemies(GameTime gameTime)
        {
            foreach (Enemy enemy in Enemies)
            {
                enemy.Path.Move((float)(enemy.Speed * gameTime.ElapsedGameTime.TotalSeconds * (enemy.IsSlowed ? Entity2D.SLOW_SPEED_MULTIPLIER : 1f)));
                enemy.UpdateStatusEffects(gameTime);
                enemy.HealthBar.Update(gameTime);
            }
            foreach (Enemy enemy in EnemiesToRemove)
            {
                Enemies.Remove(enemy);
                Application.Instance.Drawables.Remove(enemy);
            }
            EnemiesToRemove.Clear();
        }

        private void UpdatePowerups(GameTime gameTime)
        {
            foreach (Powerup powerup in Powerups)
            {
                powerup.UpdateLifespan(gameTime);
                if (powerup.TimeToLive <= 0)
                    PowerupsToRemove.Add(powerup);
            }
            foreach (Powerup powerup in PowerupsToRemove)
            {
                Powerups.Remove(powerup);
                Application.Instance.Drawables.Remove(powerup);
            }
            PowerupsToRemove.Clear();
        }

        private void Collisions(GameTime gameTime)
        {
            Vector2 distance;
            if (Core.BoundingArea.CollidesWith(Player.BoundingArea))
            {
                //Player.LoseTint(Core.Tint);
                GamePad.SetVibration(PlayerIndex.One, 0.5f, 0.5f);
                feedbackTime = DAMAGE_FEEDBACK_TIME;
                Player.Path.Recoil(Core.Position, RECOIL_DISTANCE);
            }
            foreach (Laser laser in Lasers)
            {
                // Check Collision with Power Core
                if (laser.BoundingArea.CollidesWith(Core.BoundingArea))
                {
                    if (laser.Alignment == LaserAlignment.Player)
                        Core.ProcessHit(laser.Tint, laser.Damage);
                    LasersToRemove.Add(laser);
                }
                if (laser.Alignment == LaserAlignment.Player)
                {

                    foreach (Enemy enemy in Enemies)
                    {
                        distance = enemy.Position - laser.Position;
                        if (distance.Length() <= COLLISION_DISTANCE)
                        {
                            enemy.ProcessHit(laser);
                            if (!enemy.IsAlive() && RNG.NextDouble() + (0.75 * laser.Charge) >= 0.75)
                            {
                                Powerup powerup = enemy.DropPowerup(Player.Tint, RNG);
                                if (powerup.Tint != Color.Black)
                                {
                                    Powerups.Add(powerup);
                                    Application.Instance.Drawables.Add(powerup);
                                }
                            }
                            LasersToRemove.Add(laser);
                        }
                    }
                }
                else if (laser.Alignment == LaserAlignment.Enemy)
                {
                    distance = Player.Position - laser.Position;
                    if (distance.Length() <= COLLISION_DISTANCE)
                    {
                        Player.ProcessHit(laser);
                        GamePad.SetVibration(PlayerIndex.One, 0.5f, 0.5f);
                        feedbackTime = DAMAGE_FEEDBACK_TIME;
                        LasersToRemove.Add(laser);
                    }
                }
            }
            foreach (Enemy enemy in Enemies)
            {
                distance = enemy.Position - Player.Position;
                if (distance.Length() <= COLLISION_DISTANCE)
                {
                    Color oldTint = Player.Tint;
                    Player.LoseTint(enemy.Tint);
                    if (oldTint == Player.Tint)
                    {
                        Player.CurrentHealthPoints -= enemy.CurrentHealthPoints;
                        if (Player.CurrentHealthPoints < 0) Player.CurrentHealthPoints = 0;
                        enemy.CurrentHealthPoints = 0;
                    }
                    GamePad.SetVibration(PlayerIndex.One, 0.5f, 0.5f);
                    feedbackTime = DAMAGE_FEEDBACK_TIME;
                }
                if (!enemy.IsAlive())
                {
                    ScoreKeeper.AddPoints(enemy.GetScoreValue() * Level);
                    EnemiesToRemove.Add(enemy);

                    Explosion explosion = enemy.GetExplosion(gameTime.TotalGameTime.TotalMilliseconds);
                    Explosions.Add(explosion);
                    Application.Instance.Drawables.Add(explosion);
                }
            }
            foreach (Powerup powerup in Powerups)
            {
                distance = Player.Position - powerup.Position;
                if (distance.Length() <= COLLISION_DISTANCE)
                {
                    SoundPlayer.PlayEffect(SoundEffectType.PlayerPowersUp);
                    ScoreKeeper.AddPoints(50 * Level);
                    Player.AbsorbTint(powerup.Tint);
                    Player.CurrentHealthPoints += 50;
                    if (Player.CurrentHealthPoints > Player.MaxHealthPoints)
                        Player.CurrentHealthPoints = Player.MaxHealthPoints;
                    PowerupsToRemove.Add(powerup);
                }
            }
            foreach (Powerup powerup in PowerupsToRemove)
            {
                Powerups.Remove(powerup);
                Application.Instance.Drawables.Remove(powerup);
            }
            PowerupsToRemove.Clear();
        }

        private void SpawnRandomEnemyWave(GameTime gameTime)
        {
            EnemySpawnCounter += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (EnemySpawnCounter >= EnemyWaveSpawnTime)
            {
                EnemySpawnCounter = 0f;
                float min = Math.Max(Viewport.Width / 2, Viewport.Height / 2);
                SpawnRandomEnemies(EnemyWaveSize, min, min * 1.25f);
            }
        }

        private void SpawnRandomEnemies(int num, float minDistanceFromCenter, float maxDistanceFromCenter)
        {
            for (int i = 0; i < num; i++)
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

                // to spawn enemies just outside the playing area
                float angle = (float)RNG.NextDouble() * MathHelper.TwoPi;
                Vector2 direction = new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
                direction *= minDistanceFromCenter + (float)RNG.NextDouble() * (maxDistanceFromCenter - minDistanceFromCenter);
                Vector2 center = new Vector2(Viewport.Width / 2, Viewport.Height / 2);

                Enemy enemy;
                if (RNG.Next(3) > 0)
                    enemy = new Seeker(color, center + direction, Player, Enemies, Core);
                else
                    enemy = new Observer(color, center + direction, Player, Enemies, Core);
                enemy.Path.Move(10);
                Enemies.Add(enemy);
                Application.Instance.Drawables.Add(enemy);
            }
        }

        private void HandleForceFeedback(GameTime gameTime)
        {
            if (feedbackTime > 0.0f)
            {
                feedbackTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            
            if (feedbackTime < 0.0f)
            {
                GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
                feedbackTime = 0.0f;
            }
            
        }

        public int Level { get; private set; }
        private Random RNG;
        private Background mBackground;
        private Viewport Viewport;
        private Ship Player;
        private Crosshair Crosshair;
        private PowerCore Core;
        private List<Laser> Lasers, LasersToRemove;
        private List<Enemy> Enemies, EnemiesToRemove;
        private List<Powerup> Powerups, PowerupsToRemove;
        private List<Explosion> Explosions;
        private ScoreKeeper ScoreKeeper;
        private float LaserFireRateCounter, LaserCharge, EnemySpawnCounter, EnemyWaveSpawnTime;
        private int EnemyWaveSize;
        private float feedbackTime;

        public int Score
        {
            get
            {
                return ScoreKeeper.Value;
            }

            set
            {
                ScoreKeeper.Value = value;
            }
        }
    }
}
