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
    //abstract Shapebase class provides attributes and methods that all game objects will share
    public abstract class ShapeBase
    {
        private GraphicsPath _GPModel = null;           //basic shapes graphicspath
        public static Random _rng = new Random();       //random number generator  
        public float _rot;                              //current rotation position of the shape
        public PointF _pos;                             //current position of shape 
        public Size _gameSize;                          //size of the client rectangle
        public bool _edge = false;                      //true if shape is nearing edge so it will be wrapped
        public const int _maxSides = 12;                //max sides a polygon can have

        //base shape constructor.  Assigns position and initializes the rotation of the shape
        public ShapeBase(PointF pos)
        {
            _pos = pos;
            _rot = 0;
        }

        //abstract GetPath will be overridden
        public abstract GraphicsPath GetPath();

        //Render shape to buffered graphics
        public abstract void Render(BufferedGraphics graf);


        //create a polygon with given number of sides, radius maximum, and change in radius
        public static GraphicsPath UberPoly(int sides, float radMax, float radChange)
        {
            GraphicsPath polyTemp = new GraphicsPath();

            List<PointF> lines = new List<PointF>();
            double angle = 0;
            for (int i = 0; i < sides; ++i, angle += (Math.PI * 2) / sides)
            {
                float localRad = (float)(_rng.NextDouble() * radChange);
                lines.Add(new PointF((float)(Math.Cos(angle) * (radMax - localRad)),
                    (float)(Math.Sin(angle) * (radMax - localRad))));
            }

            polyTemp.AddPolygon(lines.ToArray());

            return polyTemp;
        }

        // EdgeClone() method - clones graphics path of shape to wrap it to each edge
        // returns combined graphics path of all clones
        public GraphicsPath EdgeClone(GraphicsPath origPath)
        {
            //clone path 4 times
            GraphicsPath topClone = (GraphicsPath)origPath.Clone();
            GraphicsPath botClone = (GraphicsPath)origPath.Clone();
            GraphicsPath leftClone = (GraphicsPath)origPath.Clone();
            GraphicsPath rightClone = (GraphicsPath)origPath.Clone();

            Matrix matTop = new Matrix();
            Matrix matBot = new Matrix();
            Matrix matLeft = new Matrix();
            Matrix matRight = new Matrix();

            //translate a path to each edge
            matTop.Translate(_pos.X, _pos.Y + _gameSize.Height);
            matTop.Rotate(_rot, MatrixOrder.Prepend);

            matBot.Translate(_pos.X, _pos.Y - _gameSize.Height);
            matBot.Rotate(_rot, MatrixOrder.Prepend);

            matLeft.Translate(_pos.X - _gameSize.Width, _pos.Y);
            matLeft.Rotate(_rot, MatrixOrder.Prepend);

            matRight.Translate(_pos.X + _gameSize.Width, _pos.Y);
            matRight.Rotate(_rot, MatrixOrder.Prepend);

            topClone.Transform(matTop);
            botClone.Transform(matBot);
            leftClone.Transform(matLeft);
            rightClone.Transform(matRight);

            //combine all paths
            topClone.AddPath(botClone, true);
            topClone.AddPath(leftClone, true);
            topClone.AddPath(rightClone, true);

            return topClone;
        }
    }
}
