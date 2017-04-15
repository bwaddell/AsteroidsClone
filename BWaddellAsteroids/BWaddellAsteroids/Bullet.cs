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
    //Bullet class - creates a bullet shot from the spaceship.  Based on ShapeBase class
    class Bullet : ShapeBase
    {
        private GraphicsPath _bulletPath;       //graphics path of the bullet
        const int _size = 8;                    //size of each bullet
        float _speed = 10.0f;                   //base speed of the bullet
        int _lifeSpan = 80;                     //how many ticks the bullet will last before dying on its own
        float _direction;                       //direction the bullet is flying in
        public bool _alive;                     //true if bullet is still active
        float _XPosChange;                      //the x position change per tick
        float _YPosChange;                      //the y position change per tick

        //bullet contructor.  leverages the base constructor 
        public Bullet (PointF pos, float dir, float s) : base (pos)
        {
            _direction = dir;           //direction of bullet is direction of ship when fired
            _alive = true;              //true when bullet is active 
            _speed += s;                //add speed of ship to base speed of bullet to prevent ship from outrunning the bullet

            //create circular graphics path for bullet
            _bulletPath = new GraphicsPath();
            _bulletPath.FillMode = FillMode.Winding;
            _bulletPath.AddEllipse((float)(-_size / 2.0), (float)(-_size / 2.0), _size, _size);
            _bulletPath.CloseAllFigures();
        }

        //Graphics Path() - clone and translate bullet to current position
        public override GraphicsPath GetPath()
        {
            GraphicsPath gpClone = (GraphicsPath)_bulletPath.Clone();
            Matrix mat = new Matrix();

            mat.Translate(_pos.X, _pos.Y);

            gpClone.Transform(mat);

            //clone bullet to edges if it is nearing an edge
            if (_edge)
                gpClone.AddPath(EdgeClone(_bulletPath), true);

            return gpClone;
        }

        //Tick() - adjust the postion of the bullet with each tick
        public void Tick(Size s)
        {
            _gameSize = s;      //update size of game window
            _lifeSpan--;        //decrement lifepan of bullet

            //kill bullet if its lifespan has run out
            if (_lifeSpan <= 0)
                _alive = false;

            //calculate how much position will change based on direction and current speed
            _YPosChange = -(float)Math.Cos(_direction * Math.PI / 180) * _speed;
            _XPosChange = (float)Math.Sin(_direction * Math.PI / 180) * _speed;

            //detect if it is nearing edge
            if (_pos.X >= s.Width - _size || _pos.X <= _size
                || _pos.Y >= s.Height - _size || _pos.Y <= _size)
                _edge = true;
            else
                _edge = false;

            //move bullet to opposite edge if it leaves screen, else add position change to position
            if (_pos.X + _XPosChange > s.Width - 1)
                _pos.X = 0;
            else if (_pos.X + _XPosChange < 0)
                _pos.X = s.Width;
            else
                _pos.X += _XPosChange;

            if (_pos.Y + _YPosChange > s.Height - 1)
                _pos.Y = 0;
            else if (_pos.Y + _YPosChange < 0)
                _pos.Y = s.Height;
            else
                _pos.Y += _YPosChange;
        }

        //Render() - render bullet buffered graphics frame
        public override void Render(BufferedGraphics graf)
        {
            graf.Graphics.FillPath(new SolidBrush(Color.White), GetPath());

        }
    }
}
