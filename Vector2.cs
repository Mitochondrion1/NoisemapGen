using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerlinNoiseUI
{
    internal class Vector2
    {
        private float x;
        private float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float GetX()
        {
            return x;
        }

        public float GetY()
        {
            return y;
        }

        public void SetX(float x)
        {
            this.x = x;
        }
        public void SetY(float y)
        {
            this.y = y;
        }

        public override string ToString()
        {
            return "(" + x + ";" + y + ")";
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static float operator *(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        public static Vector2 CreateRotatedUnitVector(float angle)
        {
            Vector2 vct = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            if (Math.Abs(vct.x) < 0.000001f) vct.x = 0;
            else if (Math.Abs(vct.y) < 0.000001f) vct.y = 0;
            return vct;
        }
    }
}
