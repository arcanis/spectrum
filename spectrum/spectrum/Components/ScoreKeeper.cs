using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spectrum.Library.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Spectrum.Components
{
    /// <summary>
    /// Class that keeps track of the current game's Score
    /// </summary>
    public class ScoreKeeper : Drawable
    {
        /// <summary>
        /// Score Value of an Observer Ennemy
        /// </summary>
        public static readonly int ENNEMY_OBSERVER_SCORE_VALUE = 20;

        /// <summary>
        /// Score Value of an Follower Ennemy
        /// </summary>
        public static readonly int ENNEMY_FOLLOWER_SCORE_VALUE = 20;

        /// <summary>
        /// Score Value of a Power Core Hit
        /// </summary>
        public static readonly int POWERCORE_HIT_SCORE_VALUE = 5;
        
        /// <summary>
        /// Position of the Score String on the Field.
        /// </summary>
        private static readonly Vector2 POSITION = new Vector2(Application.Instance.GraphicsDevice.Viewport.Width * 0.1f,
                                                               Application.Instance.GraphicsDevice.Viewport.Height * 0.1f);

        public int Value { get; private set; }

        private static SpriteFont SpriteFont;

        public ScoreKeeper() 
        {
            SpriteFont = Application.Instance.Content.Load<SpriteFont>("ScoreFont");
        }

        /// <summary>
        /// Add numOfPoints points to the CurrentScore.
        /// </summary>
        /// <param name="numOfPoints"></param>
        public void AddPoints(int numOfPoints) 
        {
            Value += numOfPoints;
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) 
        {
            spriteBatch.DrawString(SpriteFont, "Score", POSITION, Color.Black);
            spriteBatch.DrawString(SpriteFont, String.Format("{0:D8}", Value), POSITION + new Vector2(0, 18), Color.White);
        }
    }
}
