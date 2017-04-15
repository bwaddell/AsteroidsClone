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

namespace BWaddellAsteroids
{
    //Asteroid class - creates a new asteroid for the player to destroy
    public class Asteroid : ShapeBase
    {
        private GraphicsPath _rockPath;                     //original grahics path of asteroid
        public enum astSize { large, medium, small };       //name of each possible size of asteroid
        public astSize _astSize { get; private set; }       //size type of asteroid
        public bool _markedForDeath;                        //true is asteroid has been destroyed          
        public bool _dangerous;                             //true is asteroid can destroy ship
        int _curSafeTim;                                    //ticks until asteroid is dangerous
        const int _safeTim = 300;                           //how many ticks the asteroid will 
        const float _rotMax = 1.0f;                         //the maximum rotating speed an asteroid can be created with
        const float _speedMax = 1.0f;                       //the maximum movement speed an asteroid can be created with
        const int _maxSize = 60;                            //the maximum radius of the largest asteroid type
        int _size;                                          //the radius of the asteroid
        float _fRotInc;                                     //the amount the asteroid will rotate per tick
        float _fXSpeed;                                     //the distance the asteroid will move in the x direction per tick
        float _fYSpeed;                                     //the distance the asteroid will move in the y direction per tick
        
        //asteroid constructor - recieve size of asteroid, leverage base constuctor to set position
        public Asteroid(PointF pos, astSize aSize) : base(pos)
        {
            _astSize = aSize;               //initialize asteroid size type

            //set the max size of the asteroid based on it's type
            switch (aSize)
            {
                case astSize.small:
                    _size = _maxSize / 4;
                    break;
                case astSize.medium:
                    _size = _maxSize / 2;
                    break;
                default:
                    _size = _maxSize;
                    break;
            }

            _rot = 0;                       //initialize rotation position

            //set random rotation and movement speeds based on constant maximums
            _fRotInc = (float)(_rng.NextDouble() * (_rotMax * 2) - _rotMax);
            _fXSpeed = (float)(_rng.NextDouble() * (_speedMax * 2) - _speedMax);
            _fYSpeed = (float)(_rng.NextDouble() * (_speedMax * 2) - _speedMax);

            //set the asteroid to living
            _markedForDeath = false;

            //if the asteroid is large, it will not be dangerous the ship for set time
            _dangerous = (aSize == astSize.large) ? false : true;

            //set time until asteroid is dangerous
            _curSafeTim = _safeTim;

            //create asteroids graphics path
            _rockPath = new GraphicsPath();
            _rockPath.FillMode = FillMode.Winding;

            //create a polygon with a minimum of 4 sides
            _rockPath = UberPoly(_rng.Next(4, _maxSides - 1), _size, _size / 2);

            _rockPath.CloseAllFigures();

        }
        //Tick() method - each tick will advance the asteroids position and rotation animation
        public void Tick(Size s)
        {
            //update size of game window
            _gameSize = s;

            //if the asteroid is not yet dangerous, decrease counter
            if (!_dangerous)
            {
                --_curSafeTim;

                //if counter is finished, make the asteroid dangerous
                if (_curSafeTim <= 0)
                {
                    _dangerous = true;
                    _curSafeTim = _safeTim;
                }
            }

            //detect if the asteroid is within its size to the edge to determine if it should be wrapped
            if (_pos.X >= s.Width - _size || _pos.X <= _size
                || _pos.Y >= s.Height - _size || _pos.Y <= _size)
                _edge = true;
            else
                _edge = false;

            //move asteroid to opposite edge if it leaves screen
            if (_pos.X + _fXSpeed > s.Width)
                _pos.X = 0;
            if (_pos.X + _fXSpeed < 0)
                _pos.X = s.Width;
            if (_pos.Y + _fYSpeed > s.Height)
                _pos.Y = 0;
            if (_pos.Y + _fYSpeed < 0)
                _pos.Y = s.Height;

            //increment rotation and movement
            _rot += _fRotInc;
            _pos.X += _fXSpeed;
            _pos.Y += _fYSpeed;
        }
        //clone and transform polygon to set position and rotation angle
        public override GraphicsPath GetPath()
        {
            //clone shape
            GraphicsPath gpClone = (GraphicsPath)_rockPath.Clone();

            Matrix mat = new Matrix();

            //move and rotate shape
            mat.Translate(_pos.X, _pos.Y);
            mat.Rotate(_rot, MatrixOrder.Prepend);

            gpClone.Transform(mat);

            //add clones on each opposite edge if the asteroid is near an edge
            if (_edge)
                gpClone.AddPath(base.EdgeClone(_rockPath), true);

            return gpClone;
        }
        //Render() - render the asteroids graphics path
        public override void Render(BufferedGraphics graf)
        {
            //adjust the asteroids alpha value to fade in until it becomes dangerous
            int alph = 255 - (int)((double)_curSafeTim / _safeTim * 255);

            //render dangerous asteroids in green, safe in white
            if (!_dangerous)
                graf.Graphics.DrawPath(new Pen(Color.FromArgb(alph, 255, 255, 255)), GetPath());
            else
                graf.Graphics.DrawPath(new Pen(Color.GreenYellow), GetPath());
        }

    }
}
