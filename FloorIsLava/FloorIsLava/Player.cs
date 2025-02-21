﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using LineBatch;

namespace FloorIsLava
{
    /*
     * The player class will inherit from the MoveableGameObject class.  It will be controlled 
     * by the player’s input from the keyboard/gamepad.  It will also have an attribute for 
     * the grappling hook that will tell if the grappling hook is attached to ceiling and an 
     * attribute for how much gold the player has collected.
     */
    /// <summary>
    /// The object that the player controls.
    /// </summary>
    public class Player                    // ****NOTE: IS NOT CURRENTLY INHERITING FROM ANYTHING. RUNS INDEPENDENTLY. THIS WILL CHANGE IN A FUTURE \****
    {
        #region Constants
        // constants
        const string ASSET_NAME = "Player";
        const int ACCELERATION = 1;
        const int GRAVITY = -1;
        const int MAX_JUMPS = 2;
        const int GRAPPLE_DISTANCE = 450;
        Vector2 RIGHT = new Vector2(1, 0);
        Vector2 LEFT = new Vector2(-1, 0);
        #endregion Constants

        #region Attributes
        // attributes
        private Rectangle playerRect;
        private Texture2D playerTexture;

        private Texture2D backwards;
        private int width;
        private int height;

        public enum State { Walking, Still, Stopping }
        private State currentState;
        private Vector2 position;
        private Vector2 velocity;
        private KeyboardState oldState;
        private int direction;
        private bool hasJumped;
        private Vector2 jumpStartPosition;
        private bool newPress;
        private int jumpNumber;
        Rectangle nextPlayerRect;
        List<Rectangle> collisionsToCheck;
        private Vector2 grappleEndpoint;
        int xPostion;
        int yPostion;
        private GameScreen gameScreen;
        bool isGrappled;
        private bool isStunned;
        private int timeSinceStun;
        private int frame;
        private Game1 game;

        //Kasey added attributes
        private Enemy enemy;
        private Platform platform;

        // images
        Texture2D back1;
        Texture2D back2;
        Texture2D forward1;
        Texture2D forward2;
        int count;

        int JUMP_STRENGTH = 21;
        int MAX_SPEED = 10;

        #endregion Attributes

        #region Constructor
        // constructor
        /// <summary>
        /// Sets up the default values.
        /// </summary>
        /// <param name="plrTx"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="wid"></param>
        /// <param name="hgt"></param>
        public Player(Texture2D plrTx, int x, int y, int wid, int hgt, List<Rectangle> colls, Game1 game1, GameScreen gS, Enemy enemy, Platform platform)
        {
            game = game1;
            oldState = Keyboard.GetState();
            playerTexture = plrTx;
            width = wid;
            height = hgt;
            playerRect = new Rectangle(x, y, width, height);
            xPostion = x;
            yPostion = y;
            position = new Vector2(x, y);
            velocity = new Vector2();
            hasJumped = false;
            currentState = State.Still;
            collisionsToCheck = colls;
            backwards = game.playerSprite_Backwards;
            gameScreen = gS;
            grappleEndpoint = new Vector2(playerRect.X, playerRect.Y);
            isGrappled = false;
            isStunned = false;
            timeSinceStun = 0;
            

            //Kasey added this // Alex added this... just the part of the comment mind you nothing else
            this.enemy = enemy;
            this.platform = platform;

            //images
            back1 = game.Content.Load<Texture2D>("playerBack2");
            back2 = game.Content.Load<Texture2D>("playerBack3");
            forward1 = game.Content.Load<Texture2D>("playerForward2");
            forward2 = game.Content.Load<Texture2D>("playerForward3");
            frame = 0;
            count = 0;

            if (MonitorSize.height > 1080)
            {
                JUMP_STRENGTH = 24;
                MAX_SPEED = 12;
                height += 32;
                width += 18;
            }
            else if (MonitorSize.height < 1080)
            {
                JUMP_STRENGTH = 18;
                MAX_SPEED = 9;
                height -= 32;
                width -= 18;
            }
        }
        #endregion Constructor

        #region Properties
        //Collision to check Property
        public List<Rectangle> CollisionsToCheck
        {
            get { return collisionsToCheck; }
            set
            {
                collisionsToCheck = value;
            }
        }

        //Player rect Property
        public Rectangle PlayerRect
        {
            get { return playerRect; }
            set 
            {
                playerRect = value;
            }
        }
        #endregion Properties

        #region Methods
        public void DetectCollisions()
        {
            nextPlayerRect = new Rectangle((int)(position.X + velocity.X), (int)position.Y, width, height);
            foreach (Rectangle r in collisionsToCheck)
            {
                if (nextPlayerRect.Intersects(r))
                {
                    if (velocity.X > 0)
                        position.X -= velocity.X / 2;
                    else if (velocity.X < 0)
                        position.X -= velocity.X / 2;
                    velocity.X = 0;
                }
            }

            foreach (Enemy e in gameScreen.enemyList)
            {
                if (nextPlayerRect.Intersects(e.EnemyRect))
                {
                    //Stopping x velocity and position (just copied Alex's code that he had above)
                    if (velocity.X > 0)
                        position.X -= velocity.X / 2;
                    else if (velocity.X < 0)
                        position.X -= velocity.X / 2;
                    velocity.X = 0;
                }
            }
            nextPlayerRect = new Rectangle((int)position.X, (int)(position.Y + velocity.Y), width, height);
            foreach (Rectangle r in collisionsToCheck)
            {
                if (nextPlayerRect.Intersects(r))
                {
                    if (velocity.Y < 0)
                        position.Y = r.Bottom;
                    else if (velocity.Y > 0)
                    {
                        position.Y = r.Top - height;
                        hasJumped = false;
                        jumpNumber = 0;
                    }
                    velocity.Y = 0;
                }
            }

            foreach (Enemy e in gameScreen.enemyList)
            {
                if (nextPlayerRect.Intersects(e.EnemyRect))
                {
                    //Stopping y velocity and position (just copied Alex's code that he had above)
                    if (velocity.Y < 0)
                        position.Y = e.EnemyRect.Bottom;
                    else if (velocity.Y > 0)
                    {
                        position.Y = e.EnemyRect.Top - height;
                        hasJumped = false;
                    }
                    velocity.Y = 0;

                    foreach (EnemyPathEnd epe in gameScreen.enemyPathList)
                    {
                        if ((position.Y == epe.enemyPathRect.Y + playerRect.Height/2) && (e.top == true))
                        {
                            e.top = false;
                        }
                        else if ((position.Y == epe.enemyPathRect.Y - playerRect.Height/2) && (e.top == false))
                        {
                            e.top = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the vertical velocity and handles the jump/double jumps.
        /// </summary>
        /// <param name="newPress"></param>
        private void UpdateJump(bool newPress)
        {
            if (newPress && isGrappled)
            {
                hasJumped = false;
                jumpNumber = 1;
                isGrappled = false;                
            }
            if (!hasJumped && newPress && !isStunned)     // code that executes when a jump is performed
            {                
                jumpStartPosition = position;
                velocity.Y = -JUMP_STRENGTH;
                jumpNumber++;
                if(Music.canPlay)
                {
                    game.jumpS.Play();
                }
                if (jumpNumber >= MAX_JUMPS)    // stops it from executing when the maximum amount of midair jumps are performed
                {
                    jumpNumber = 0;
                    hasJumped = true;
                }
            }
        }

        /// <summary>
        /// Handles the controls for the player (jumping, movement, grappling)
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            KeyboardState newState = Keyboard.GetState();   // gets keyboard state
            if (!isStunned && (newState.IsKeyDown(Keys.LeftShift) && oldState.IsKeyUp(Keys.LeftShift) || newState.IsKeyDown(Keys.RightShift) && oldState.IsKeyUp(Keys.RightShift) || newState.IsKeyDown(Keys.E) && oldState.IsKeyUp(Keys.E)))
            {
                this.FireGrapple();
            }                         
           
            if(direction == 0)      // sets state and direction (if player is not grappled)
            {
                direction = 1;
            }
            if (currentState != State.Still && newState.IsKeyUp(Keys.A) && newState.IsKeyUp(Keys.D) && newState.IsKeyUp(Keys.Left) && newState.IsKeyUp(Keys.Right))
                currentState = State.Stopping;
            if (newState.IsKeyDown(Keys.A) && newState.IsKeyUp(Keys.D) || newState.IsKeyDown(Keys.Left) && newState.IsKeyUp(Keys.Right))
            {
                currentState = State.Walking;
                direction = -1;
                frame++;
            }
            if (newState.IsKeyDown(Keys.D) && newState.IsKeyUp(Keys.A) || newState.IsKeyDown(Keys.Right) && newState.IsKeyUp(Keys.Left))
            {
                currentState = State.Walking;
                direction = 1;
                frame++;
            }
            if (newState.IsKeyDown(Keys.A) && newState.IsKeyDown(Keys.D))
            {
                if (oldState.IsKeyUp(Keys.A) || oldState.IsKeyUp(Keys.Left))
                    direction = -1;
                if (oldState.IsKeyUp(Keys.D) || oldState.IsKeyUp(Keys.Right))
                    direction = 1;
                currentState = State.Walking;
            }
            
            if (isGrappled)     // handles direction and movement when grappled
            {
                if (newState.IsKeyDown(Keys.W) && Math.Abs((grappleEndpoint.Y - playerRect.Y)) > 30 || newState.IsKeyDown(Keys.Up) && Math.Abs((grappleEndpoint.Y - playerRect.Y)) > 30)
                {
                    position.Y -= 10;
                    if(Music.canPlay)
                    {
                        game.grappleS.Play(.5f, 0f, 0f);
                    }
                }
                if (newState.IsKeyDown(Keys.S) && Math.Abs((grappleEndpoint.Y - playerRect.Y)) < GRAPPLE_DISTANCE || newState.IsKeyDown(Keys.Down) && Math.Abs((grappleEndpoint.Y - playerRect.Y)) < GRAPPLE_DISTANCE)
                {
                    position.Y += 10;
                    if(Music.canPlay)
                    {
                        game.grappleS.Play(.5f, 0f, 0f);
                    }
                }

            }


            if (newState.IsKeyDown(Keys.Space) && oldState.IsKeyUp(Keys.Space))     // handles jumping
            {
                newPress = true;
            }
            else
            {
                newPress = false;
            }
            this.UpdateJump(newPress);

            if(!isGrappled)
            {
                if (!isStunned)
                {
                    switch (currentState)                           // master switch statement for the various states
                    {
                        case State.Still:
                            {
                                break;
                            }
                        case State.Walking:
                            {
                                if (direction == 1)
                                {
                                    if (velocity.X < MAX_SPEED)
                                    {
                                        velocity += RIGHT * ACCELERATION;
                                    }
                                }
                                else if (direction == -1)
                                {
                                    if (velocity.X > -1 * MAX_SPEED)
                                    {
                                        velocity += LEFT * ACCELERATION;
                                    }
                                }

                                break;
                            }
                        case State.Stopping:
                            {
                                if (velocity.X > 0)
                                    velocity -= RIGHT * ACCELERATION;
                                else if (velocity.X < 0)
                                    velocity -= LEFT * ACCELERATION;

                                if (velocity.X == 0)
                                {
                                    currentState = State.Still;
                                }
                                break;
                            }
                    }
                    
                }
                else if (timeSinceStun >= 1500)
                {
                    isStunned = false;
                }
                else
                {
                    timeSinceStun += gameTime.ElapsedGameTime.Milliseconds;
                }
                velocity.Y -= GRAVITY;
            }
            
            

            this.DetectCollisions();       // detects and adjusts for collisions

            position += velocity;           // adds the player's velocity to his current position
            playerRect = new Rectangle((int)position.X, (int)position.Y, width, height);    // sets the new position
            oldState = newState;            // sets the old keyboard state
        }

        /// <summary>
        /// Draws the player at the specified position (playerRect)
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if(frame > 3)
            {
                frame = 0;
            }
            if(direction == 1)
            {
                if(frame == 0 || frame == 2)
                {
                    spriteBatch.Draw(playerTexture, playerRect, Color.White);
                }
                else if(frame == 1)
                {
                    spriteBatch.Draw(forward1, playerRect, Color.White);
                }
                else if (frame == 3)
                {
                    spriteBatch.Draw(forward2, playerRect, Color.White);
                    frame = 0;
                }
                
            }
            else
            {
                if(frame == 0 || frame == 2)
                {
                    spriteBatch.Draw(backwards, playerRect, Color.White);
                }
                else if (frame == 1)
                {
                    spriteBatch.Draw(back1, playerRect, Color.White);
                }
                else if (frame == 3)
                {
                    spriteBatch.Draw(back2, playerRect, Color.White);
                    frame = 0;
                }
            }
            if (isGrappled)
            {
                spriteBatch.DrawLine(new Vector2(playerRect.X + (playerRect.Width / 2), playerRect.Y), grappleEndpoint, Color.Black, 5);
            }
        }
        public void FireGrapple()
        {
            if (isGrappled)
            {
                isGrappled = false;
                return;
            }               
            Rectangle grappleRect = new Rectangle(playerRect.X, playerRect.Y - 20, playerRect.Width, 1);
            bool foundObject = false;
            while (Math.Abs((grappleRect.Y - playerRect.Y)) < GRAPPLE_DISTANCE && !foundObject)
            {
                grappleRect = new Rectangle(grappleRect.X, grappleRect.Y - 15, playerRect.Width, 1);
                foreach (GameObject g in gameScreen.GrappleableObjectList)
                {
                    if (g.rect.Intersects(grappleRect))
                    {
                        foundObject = true;
                        grappleEndpoint = new Vector2(grappleRect.X + (playerRect.Width / 2), grappleRect.Y);
                        velocity = new Vector2(0, 0);
                        isGrappled = true;
                        if(Music.canPlay)
                        {
                            game.grappleS.Play();
                        }
                        break;
                    }
                }
            }
        }

        public void Stun()
        {
            velocity.X = 0;
            isStunned = true;
            timeSinceStun = 0;
            isGrappled = false;
        }
        #endregion Methods

        internal void MoveDown(int y)
        {
            if (isGrappled)
            {                
                position.Y += y;
                grappleEndpoint.Y += y;
            }
        }

        internal void MoveUp(int y)
        {
            if (isGrappled)
            {
                position.Y -= y;
                grappleEndpoint.Y -= y;
            }
        }
    }
}

