// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Benjamin Waddell
// Astheroids lab
// CMPE 2800 
// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using IrrKlang;

namespace BWaddellAsteroids
{
    //SpaceShip class
    //creates a spaceship based on abstract ShapeBase class
    class SpaceShip : ShapeBase
    {
        private GraphicsPath _shipModel;            //graphics path of ship body
        private GraphicsPath _burnerModel;          //graphics path of thruster flame
        private GraphicsPath _warpModel;            //graphics path of warp speed flame
        float _heading;                             //direction that the ship is currently moving
        float _XPosChange;                          //distance the ship moves in current frame in x direction
        float _YPosChange;                          //distance the ship moves in current frame in y direction
        const int _size = 50;                       //maximum size of ship model from axis
        const float _rotInc = 5.0f;                 //amount ship rotates per frame in degrees
        const float _accelInc = 0.2f;               //acceleration of ship when thrustin (pixels/tick^2)
        public const float _warpInc = 0.5f;         //acceleration of ship when warp thrust
        public bool _alive;                         //true if ship is currently alive
        public bool _afterburn = false;             //true if ship under thrust
        public bool _warpSpeed = false;             //true if ship is in warp speed
        const int _gunCooldown = 10;                //amount of ticks between gun shots
        int _currentCooldown = 0;                   //current amount of ticks until gun is ready to shoot
        const int _maxBullets = 8;                  //maximum amount of bullets that may be in the game at once
        public float _speed = 0;                    //current speed of the ship
        public bool _invincible;                    //true if ship is invincible after spawning
        const int _invTimer = 200;                  //amount of invincibility frame after spawning
        int _curInvTim;                             //current amount of invincibility frames left
        public List<Bullet> _bullets ;              //list of bullets shot from the ship
        ISoundEngine blaster;                       //create the sound engine for player blaster sounds

        //spaceship constructor, leverages shapebase to initialize position and rotation
        public SpaceShip(Size s, PointF pos) : base(pos)
        {
            _bullets = new List<Bullet>();          //create list to hold bullets shot from ship
            _heading = 0;                           //initial direction ship is moving
            _alive = true;                          //true if ship has not collided with an asteroid
            _invincible = true;                     //true is ship has spawned and invincibility timer has not finished
            _curInvTim = _invTimer;                 //set invincibility timer when ship is create
            _currentCooldown = _gunCooldown;        //set the gun cooldown timer

            blaster = new ISoundEngine();           //create sound engine for blaster
            blaster.SoundVolume = 0.2f;             //adjust blaster volume

            //create ships graphics path
            _shipModel = new GraphicsPath();
            _shipModel.FillMode = FillMode.Winding;
            _shipModel.AddLine(0, -20, 20, 20);
            _shipModel.AddLine(20, 20, -20, 20);
            _shipModel.AddLine(-20, 20, 0, -20);
            _shipModel.CloseAllFigures();

            //create thrust flames graphics path
            _burnerModel = new GraphicsPath();
            _burnerModel.FillMode = FillMode.Winding;
            _burnerModel.AddLine(0, 40, 15, 20);
            _burnerModel.AddLine(15, 20, -15, 20);
            _burnerModel.AddLine(-15, 20, 0, 40);
            _burnerModel.CloseAllFigures();

            //create warp speed flame graphics path
            _warpModel = new GraphicsPath();
            _warpModel.FillMode = FillMode.Winding;
            _warpModel.AddLine(0, 50, 15, 20);
            _warpModel.AddLine(15, 20, -15, 20);
            _warpModel.AddLine(-15, 20, 0, 50);
            _warpModel.CloseAllFigures();

        }
        //GetPath() - clone and translate ship model to correct position and rotation
        public override GraphicsPath GetPath()
        {
            GraphicsPath gpClone = (GraphicsPath)_shipModel.Clone();
            Matrix mat = new Matrix();

            mat.Translate(_pos.X, _pos.Y);
            mat.Rotate(_rot, MatrixOrder.Prepend);

            gpClone.Transform(mat);

            //add clones on opposite edges if close to edge
            if (_edge)
                gpClone.AddPath(EdgeClone(_shipModel), true);

            return gpClone;
        }

        //GetBurner() - clone and translate burner model to correct position and rotation
        public GraphicsPath GetBurner()
        {
            GraphicsPath gpClone = (GraphicsPath)_burnerModel.Clone();
            Matrix mat = new Matrix();

            mat.Translate(_pos.X, _pos.Y);
            mat.Rotate(_rot, MatrixOrder.Prepend);

            gpClone.Transform(mat);

            //add clones on opposite edges if close to edge
            if (_edge)
                gpClone.AddPath(EdgeClone(_burnerModel), true);

            return gpClone;
        }
        //GetWarp() - clone and translate warp speed model to correct position and rotation
        public GraphicsPath GetWarp()
        {
            GraphicsPath gpClone = (GraphicsPath)_warpModel.Clone();
            Matrix mat = new Matrix();

            mat.Translate(_pos.X, _pos.Y);
            mat.Rotate(_rot, MatrixOrder.Prepend);

            gpClone.Transform(mat);

            //add clones on opposite edges if close to edge
            if (_edge)
                gpClone.AddPath(EdgeClone(_warpModel), true);

            return gpClone;
        }

        //Tick() - adjust ships position, and rotation with each tick
        //add bullets when fired from ship
        public void Tick(bool left, bool right, bool thrust, bool warp, bool shoot, Size s)
        {
            _afterburn = thrust;                //true if thrust is applied to ship
            _warpSpeed = warp;                  //true if ship is in warp speed
            _gameSize = s;                      //update size of game window

            //if ship is still invincible after spawning decrease amount of ticks until it is not
            if (_invincible)
            {
                --_curInvTim;

                //if timer is complete, turn off invincibility
                if (_curInvTim <= 0)
                {
                    _invincible = false;
                    _curInvTim = _invTimer;
                }
            }

            //rotate ship
            if (left)
                _rot -= _rotInc;
            if (right)
                _rot += _rotInc;

            //calculate how much the ship will move this frame
            if (thrust)
            {
                //calculate position change based on current rotation warp/thrust multiplyer
                if (warp)
                {
                    _YPosChange += -(float)Math.Cos(_rot * Math.PI / 180) * _warpInc;
                    _XPosChange += (float)Math.Sin(_rot * Math.PI / 180) * _warpInc;
                }
                else
                {
                    _YPosChange += -(float)Math.Cos(_rot * Math.PI / 180) * _accelInc;
                    _XPosChange += (float)Math.Sin(_rot * Math.PI / 180) * _accelInc;
                }       
                //get speed for adding momentum to bullets when moving     
                _speed = (float)Math.Sqrt(Math.Pow(_XPosChange, 2) + Math.Pow(_YPosChange, 2));

                //save heading so if thrust is released the ship will maitain direction
                _heading = _rot;
            }

            //tick down cooldown for weapons
            _currentCooldown = (_currentCooldown > 0) ? _currentCooldown - 1 : 0;

            //add bullet if cooldown done only allow a specified amount of bullets at a time
            if (shoot && _currentCooldown <= 0 && _bullets.Count() < _maxBullets)
            {
                //play the blaster wav file
                blaster.Play2D("../../../blaster.wav" ,false);

                _bullets.Add(new Bullet(_pos, _rot, _speed));
                _currentCooldown = _gunCooldown;
            }

            //remove dead bullets
            _bullets.RemoveAll(b => !b._alive);

            //detect whether ship is nearing edge to determine if it should wrap
            if (_pos.X >= s.Width - _size || _pos.X <= _size
                || _pos.Y >= s.Height - _size || _pos.Y <= _size)
                _edge = true;
            else
                _edge = false;

            //move ship to opposite edge if it leaves screen, else add position change to position
            if (_pos.X + _XPosChange >= s.Width)
                _pos.X = 0;
            else if (_pos.X + _XPosChange < 0)
                _pos.X = s.Width - 1;
            else
                _pos.X += _XPosChange;

            if (_pos.Y + _YPosChange >= s.Height)
                _pos.Y = 0;
            else if (_pos.Y + _YPosChange < 0)
                _pos.Y = s.Height - 1;
            else
                _pos.Y += _YPosChange;

            //tick each of the ships bullets
            _bullets.ForEach(o => o.Tick(s));
        }
        //Render() - display the ship and it's bullets
        public override void Render(BufferedGraphics graf)
        {
            //invincible ship displayed as solid white, non-invincible is white outline
            if (_invincible)
                 graf.Graphics.FillPath(new SolidBrush(Color.White), GetPath());
            else
                graf.Graphics.DrawPath(new Pen(Color.White), GetPath());

            //render afterburner and warp burner if they are active
            if (_afterburn)
            {
                if (_warpSpeed)
                    graf.Graphics.DrawPath(new Pen(Color.White), GetWarp());

                graf.Graphics.DrawPath(new Pen(Color.GreenYellow), GetBurner());           
            }

            //render each bullet
            _bullets.ForEach(b => b.Render(graf));
        }
    }
}
