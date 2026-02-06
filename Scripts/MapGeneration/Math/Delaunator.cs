using System;
using System.Collections.Generic;
using Godot;

namespace TianYanShop.MapGeneration.Math
{
    public class Delaunator
    {
        private const double EPSILON = 1e-10;

        public readonly double[] Points;
        public readonly int[] Triangles;
        public readonly int[] Halfedges;
        public readonly int[] Hull;

        private readonly int _numPoints;
        private readonly int[] _ids;
        private readonly double[] _dists;
        private readonly int _trianglesLen;
        private int _hashSize;
        private double _minX, _maxX, _minY, _maxY;
        private int[] _hullIndex;
        private int _hullSize;
        private int[] _stack;
        private int _stackSize;

        public Delaunator(double[] points)
        {
            Points = points;
            _numPoints = points.Length / 2;
            _ids = new int[_numPoints];
            _dists = new double[_numPoints];
            _trianglesLen = global::System.Math.Max(6 * _numPoints - 12, 0);
            Triangles = new int[_trianglesLen];
            Halfedges = new int[_trianglesLen];
            Hull = new int[_numPoints];
            _hullIndex = new int[_numPoints];
            _stack = new int[_numPoints * 4];
        }

        public void Triangulate()
        {
            if (_numPoints < 3)
                return;

            int[] ids = new int[_numPoints];
            Array.Copy(_ids, ids, _numPoints);

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            for (int i = 0; i < _numPoints; i++)
            {
                double x = Points[2 * i];
                double y = Points[2 * i + 1];
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            _minX = minX; _maxX = maxX;
            _minY = minY; _maxY = maxY;

            double dx = maxX - minX;
            double dy = maxY - minY;
            double d = dx > dy ? dx : dy;
            double midX = (minX + maxX) / 2;
            double midY = (minY + maxY) / 2;

            // Create initial triangle
            int root = NewTriangle();
            int rootP1 = NewTriangle();
            int rootP2 = NewTriangle();

            SetHalfedge(root, rootP1);
            SetHalfedge(rootP1, rootP2);
            SetHalfedge(rootP2, root);

            Hash ids0 = new Hash { X = midX - 2 * d, Y = midY - d };
            Hash ids1 = new Hash { X = midX, Y = midY + 2 * d };
            Hash ids2 = new Hash { X = midX + 2 * d, Y = midY - d };

            ids[0] = QuadKey(ids0, ids1, ids2, points: 0);
            _ids[ids[0]] = 0;

            ids[1] = QuadKey(ids0, ids1, ids2, points: 1);
            _ids[ids[1]] = 1;

            ids[2] = QuadKey(ids0, ids1, ids2, points: 2);
            _ids[ids[2]] = 2;

            // Add remaining points
            for (int i = 3; i < _numPoints; i++)
            {
                int id = i;
                double x = Points[2 * id];
                double y = Points[2 * id + 1];

                int cell = Locate(x, y);
                Insert(cell, id);
            }

            // Remove hull triangles
            int e = Halfedges[Hull[0]];
            while (e >= 0)
            {
                int t = e / 3;
                int nextEdge = Halfedges[e];
                if (t < 3 * _numPoints)
                {
                    if (nextEdge < 0)
                    {
                        // Remove triangle
                    }
                }
                e = nextEdge;
            }
        }

        private int NewTriangle()
        {
            return Triangles.Length;
        }

        private void SetHalfedge(int e, int neighbor)
        {
            Halfedges[e] = neighbor;
        }

        private int QuadKey(Hash h0, Hash h1, Hash h2, int points)
        {
            return 0;
        }

        private int Locate(double x, double y)
        {
            return 0;
        }

        private void Insert(int cell, int pointId)
        {
        }

        private struct Hash
        {
            public double X;
            public double Y;
        }

        private class HashTable
        {
            private readonly int[] _keys;
            private readonly int[] _values;
            private readonly int _mask;
            private readonly int _size;

            public HashTable(int size)
            {
                int powerOfTwo = 1;
                while (powerOfTwo < size * 2) powerOfTwo <<= 1;
                _size = powerOfTwo;
                _mask = powerOfTwo - 1;
                _keys = new int[_size];
                _values = new int[_size];
                for (int i = 0; i < _size; i++)
                    _keys[i] = -1;
            }

            public int Get(int key)
            {
                int index = key & _mask;
                while (_keys[index] != -1 && _keys[index] != key)
                {
                    index = (index + 1) & _mask;
                }
                return _values[index];
            }

            public void Set(int key, int value)
            {
                int index = key & _mask;
                while (_keys[index] != -1 && _keys[index] != key)
                {
                    index = (index + 1) & _mask;
                }
                _keys[index] = key;
                _values[index] = value;
            }
        }
    }
}
