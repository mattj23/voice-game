using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice_Game
{
    public class Vector : Notifier
    {
        private double _x;
        private double _y;
        private double _z;

        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged("X");
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        public double Z
        {
            get { return _z; }
            set
            {
                _z = value;
                OnPropertyChanged("Z");
            }
        }

        public double Length
        {
            get { return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2)); }
        }

        /* Class methods */
        public void Normalize()
        {
            double len = Length;
            X = X / len;
            Y = Y / len;
            Z = Z / len;
        }

        public static double Dot(Vector a, Vector b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public static Vector Cross(Vector a, Vector b)
        {
            return new Vector(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        public static Vector AveragePoint(Vector[] vectors)
        {
            double sumx = 0, sumy = 0, sumz = 0;
            foreach (Vector v in vectors)
            {
                sumx += v.X;
                sumy += v.Y;
                sumz += v.Z;
            }
            return new Vector(sumx / vectors.Length, sumy / vectors.Length, sumz / vectors.Length);
        }

        public static double CompareNormals(Vector normal1, Vector normal2)
        {
            Vector n1 = normal1.Clone();
            Vector n2 = normal2.Clone();
            n1.Normalize();
            n2.Normalize();

            // Project n2 onto n1
            if (Dot(n1, n2) < 0)
                return -1;
            else
                return 1;
        }


        /// <summary>
        /// Returns a new instance of a Vector3 with the same values as this one.
        /// </summary>
        /// <returns></returns>
        public Vector Clone()
        {
            return new Vector(X, Y, Z);
        }

        /* Class construtors */

        public Vector() { }

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /* Rotational functions */
        public Vector RotateAboutZ(double angle)
        {
            double nx = X * Math.Cos(angle) - Y * Math.Sin(angle);
            double ny = X * Math.Sin(angle) + Y * Math.Cos(angle);
            return new Vector(nx, ny, Z);
        }

        /* Operator overloading */
        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector operator *(Vector a, double c)
        {
            return new Vector(a.X * c, a.Y * c, a.Z * c);
        }

        public static Vector operator *(double c, Vector a)
        {
            return new Vector(a.X * c, a.Y * c, a.Z * c);
        }


    }
}
