// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Benjamin Waddell
// Astheroids lab
// CMPE 2800 
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IrrKlang;

namespace BWaddellAsteroids
{
    public partial class AsteroidForm : Form
    {
        Random _rng = new Random();                 //random number generator
        public GameInput.Input VGInput;             //import keyboard and 360 gamepad control
        SpaceShip ss;                               //the current living spaceship
        List<Asteroid> _astBelt;                    //list containing all non-distroyed asteroids
        List<SpaceShip> _livesDisplay;              //list of spaceships used to display number of lives left
        const int _lives = 3;                       //max amount of lives
        int _currentLives;                          //current amount of lives
        int _score;                                 //user score
        int _asteroidNumber = 5;                    //number of asteroids at game start
        int _astDelay = 500;                        //initial amount of ticks between asteroid spawns
        int _curAstDelay;                           //current amount of ticks between asteroid spawns
        bool _gameOver = true;                      //true if gameover.  initially true to trigger menu
        bool firstRun = true;                       //initially true so that "game over" string is not displayed when menu is first shown
        bool _paused = false;                       //bool indicates if game is currently paused
        bool _lastPause = false;                    //holds state of pause button in previous tick for pause detection
        bool _nextPause = false;                    //holds state of pause button in current tick for pause detection
        bool _restart = true;                       //holds state of restart button in previous tick for restart detection
        int _scoreToLife = 20000;                   //points until extra life is won
        ISoundEngine music;                         //create sound engine for game music
        ISoundEngine death;                         //create sound engine for death sound

        public AsteroidForm()
        {
            InitializeComponent();

            //play theme music
            music = new ISoundEngine();
            music.SoundVolume = 0.5f;
            music.Play2D("../../../MACINTOSH_PLUS_-_------420_-_--------.wav", true);
            
        }

        private void AsteroidForm_Load(object sender, EventArgs e)
        {
            //force fullscreen
            //WindowState = FormWindowState.Maximized;

            //create game control instance
            VGInput = new GameInput.Input();

            //create key up and down events
            this.KeyUp += VGInput.XboxGame_KeyUp;
            this.KeyDown += VGInput.XboxGame_KeyDown;

            //create death sound engine
            death = new ISoundEngine();

            //call method to initialize game variables and lists
            ResetGameWorld();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            //double buffer graffics to remove stutter
            using (BufferedGraphicsContext bgc = new BufferedGraphicsContext())
            {
                using (BufferedGraphics bg = bgc.Allocate(CreateGraphics(), ClientRectangle))
                {
                    //if game over is false, animate and render game
                    if (!_gameOver)
                    {
                        //smooth edges of shapes
                        bg.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        //create black background
                        bg.Graphics.FillRectangle(new SolidBrush(Color.Black), ClientRectangle);

                        //determine if pause is selected
                        _nextPause = VGInput.pause;

                        //if pause is pressed, and not pressed last frame, toggle pause
                        if (_nextPause && ! _lastPause)
                            _paused = !_paused;                     

                        //save current pause for next tick
                        _lastPause = _nextPause;

                        //if not paused, move each animated object and check for collisions
                        if (!_paused)
                        {
                            //add asteroid every n ticks
                            _curAstDelay--;

                            //of delay counter is finish, reset and reduce delay, add asteroid
                            if (_curAstDelay <= 0)
                            {
                                //reduce delay before asteroid spawn, limit to above 100 ticks
                                _astDelay = (_astDelay - 20 >= 100) ? _astDelay - 20 : 100;
                                _curAstDelay = _astDelay;

                                //add new asteroid to game
                                _astBelt.Add(new Asteroid(new PointF(_rng.Next(ClientRectangle.Width), _rng.Next(ClientRectangle.Height)), Asteroid.astSize.large));
                            }

                            //tick asteroids and ship/bullets
                            _astBelt.ForEach((s) => s.Tick(ClientRectangle.Size));
                            ss.Tick(VGInput.left, VGInput.right, VGInput.thrust, VGInput.warp, VGInput.shoot, ClientRectangle.Size);

                            //check if ship is dead
                            Region shipReg = new Region(ss.GetPath());

                            //bool causes loop to exit if collision detected to prevent more than one death from being registered
                            bool exitColLoop = false;

                            //if ship is not invincible, check for collisions with asteroids
                            if (!ss._invincible)
                            {
                                //iterate through all asteroids
                                for (int i = 0; i < _astBelt.Count(); i++)
                                {
                                    //if asteroid is not fading in test for collision with ship
                                    if (_astBelt[i]._dangerous && !exitColLoop)
                                    {
                                        Region astReg = new Region(_astBelt[i].GetPath());

                                        //intersect asteroid and ship regions
                                        astReg.Intersect(shipReg);

                                        //is intersection is not empty, there is a collision
                                        if (!astReg.IsEmpty(bg.Graphics))
                                        {
                                            //kill old ship, make new one
                                            ss._alive = false;
                                            ss = new SpaceShip(ClientRectangle.Size, new PointF(ClientRectangle.Width / 2, ClientRectangle.Height / 2));

                                            //play death sound
                                            music.Play2D("../../../wilhelm.wav", false);

                                            //don't check any more asteroids to prevent muptiple collitions
                                            exitColLoop = true;

                                            //remove life from display and counter
                                            if (_livesDisplay.Count() > 0)
                                                _livesDisplay.RemoveAt(_livesDisplay.Count() - 1);
                                            _currentLives--;

                                            //game over if all lives gone
                                            if (_currentLives <= 0)
                                                _gameOver = true;
                                        }
                                    }
                                }
                            }

                            //reset for use with bullet/asteroid collisions
                            exitColLoop = false;

                            //iterate through bullets and asteroids
                            for (int j = 0; j < ss._bullets.Count(); j++)
                            {
                                for (int k = 0; k < _astBelt.Count(); k++)
                                {
                                    //intersect bullet and asteroid regions
                                    Region bullReg = new Region(ss._bullets[j].GetPath());
                                    Region asReg = new Region(_astBelt[k].GetPath());

                                    asReg.Intersect(bullReg);

                                    //if collision detected, kill asteroid, add score
                                    if (!asReg.IsEmpty(bg.Graphics) && !exitColLoop)
                                    {
                                        //kill asteroid and bullet
                                        ss._bullets[j]._alive = false;
                                        _astBelt[k]._markedForDeath = true;
                                        exitColLoop = true;

                                        //check size of dead asteroid
                                        if (_astBelt[k]._astSize == Asteroid.astSize.large)
                                        {
                                            //if large, add two medium asteroids
                                            for (int l = 0; l < 2; l++)
                                                _astBelt.Add(new Asteroid(_astBelt[k]._pos, Asteroid.astSize.medium));

                                            //increase score for large
                                            _score += 100;
                                        }
                                        else if (_astBelt[k]._astSize == Asteroid.astSize.medium)
                                        {
                                            //if medium add 3 small
                                            for (int m = 0; m < 3; m++)
                                                _astBelt.Add(new Asteroid(_astBelt[k]._pos, Asteroid.astSize.small));

                                            //increase score for medium
                                            _score += 200;
                                        }
                                        else
                                        {
                                            //increase score for small
                                            _score += 300;
                                        }

                                        //see if score has reached points to extra life
                                        if (_score > _scoreToLife)
                                        {
                                            //increase lives and add to live display ship list
                                            _currentLives++;
                                            _livesDisplay.Add(new SpaceShip(ClientRectangle.Size, new PointF(_currentLives * 50, 50)));
                                            //double score needed to next life
                                            _scoreToLife *= 2;
                                        }
                                            
                                    }
                                }
                            }

                            //remove dead asteroids from list
                            _astBelt.RemoveAll(a => a._markedForDeath);
                        }
                        else
                        {
                            //if game paused display "pause" on screen
                            bg.Graphics.DrawString("Pause", new Font(FontFamily.GenericSansSerif, 60), new SolidBrush(Color.YellowGreen), ClientRectangle.Width / 2 - 130 , ClientRectangle.Height / 2 - 50);
                        }

                        //display score and lives
                        _livesDisplay.ForEach(l => l.Render(bg));
                        bg.Graphics.DrawString("score: " + _score.ToString(), new Font(FontFamily.GenericSansSerif, 16), new SolidBrush(Color.GreenYellow), 20, 0);


                        //render asteroids and ship/bullets
                        _astBelt.ForEach((s) => s.Render(bg));
                        ss.Render(bg);
                  
                    }
                    else
                    {
                        //if gameover is true, display main menu


                        bg.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        bg.Graphics.FillRectangle(new SolidBrush(Color.Black), ClientRectangle);

                        bg.Graphics.DrawString("Astheroids", new Font(FontFamily.GenericSansSerif, 100), new SolidBrush(Color.White), 160, 100);
                        bg.Graphics.DrawString("PAUSE = ESC / START", new Font(FontFamily.GenericSansSerif, 20), new SolidBrush(Color.GreenYellow), 0, ClientRectangle.Height - 200);
                        bg.Graphics.DrawString("SHOOT = SPACE / A BUTTON", new Font(FontFamily.GenericSansSerif, 20), new SolidBrush(Color.GreenYellow), 0, ClientRectangle.Height - 160);
                        bg.Graphics.DrawString("TURN = LEFT/RIGHT / RIGHT ANALOG STICK", new Font(FontFamily.GenericSansSerif, 20), new SolidBrush(Color.GreenYellow), 0, ClientRectangle.Height - 120);
                        bg.Graphics.DrawString("THRUST = UP / RIGHT TRIGGER", new Font(FontFamily.GenericSansSerif, 20), new SolidBrush(Color.GreenYellow), 0, ClientRectangle.Height - 80);
                        bg.Graphics.DrawString("WARP SPEED = W / LEFT TRIGGER", new Font(FontFamily.GenericSansSerif, 20), new SolidBrush(Color.GreenYellow), 0, ClientRectangle.Height - 40);
                        bg.Graphics.DrawString("SHOOT to start game", new Font(FontFamily.GenericSansSerif, 20), new SolidBrush(Color.GreenYellow), ClientRectangle.Width / 2 - 160, ClientRectangle.Height / 2);

                        //only display game over and score if a game has been finished
                        if (!firstRun)
                        {
                            bg.Graphics.DrawString("GAME OVER", new Font(FontFamily.GenericSansSerif, 80), new SolidBrush(Color.White), 150, 220);
                            bg.Graphics.DrawString("score: " + _score, new Font(FontFamily.GenericSansSerif, 40), new SolidBrush(Color.White), 320, 420);
                        }
                       
                        //detect button press to restart game.
                        //button must be released and pressed to prevent new game from starting immediately if shoot button is held down when 
                        //game ends
                        if (VGInput.shoot && !_restart)
                        {
                            //reset game world
                            _gameOver = false;
                            firstRun = false;
                            ResetGameWorld();                      
                        }
                        _restart = VGInput.shoot;
                    }

                    //render graphics from backbuffer
                    bg.Render();
                }
            }
        }
        // ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ResetGameWorld() method
        // resets variables and initializes and empties lists and objects to prepare new game
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ResetGameWorld()
        {
            _currentLives = _lives;             //reset player lives
            _score = 0;                         //reset score
            _astDelay = 500;                    //reset asteroid tick delay
            _curAstDelay = _astDelay;           //reset current asteroid delay

            //create new ship. 
            ss = new SpaceShip(ClientRectangle.Size, new PointF(ClientRectangle.Width / 2, ClientRectangle.Height / 2));

            //clear any leftover bullets from last game
            //ss._bullets.Clear();

            //reset life display ships
            _livesDisplay = new List<SpaceShip>();
            for (int i = 1; i <= _lives; i++)
                _livesDisplay.Add(new SpaceShip(ClientRectangle.Size, new PointF(i * 50, 50)));

            //create initial asteroids
            _astBelt = new List<Asteroid>();
            for (int i = 0; i < _asteroidNumber; i++)
                _astBelt.Add(new Asteroid(new PointF(_rng.Next(ClientRectangle.Width), _rng.Next(ClientRectangle.Height)), Asteroid.astSize.large));
        }
    }
}
 