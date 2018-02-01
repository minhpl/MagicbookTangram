using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;

public class TangramShapeList
{
    public List<TangramShape> datas;
    public TangramShapeList()
    {
        datas = new List<TangramShape>();
    }
}
[Serializable]
public class TangramShape
{
    public List<MyShape> datas;
    public TangramShape()
    {
        datas = new List<MyShape>();
    }
}
[Serializable]
public class MyShape
{
    public enum S_TYPE { TRIANGLE = 0, SQUARE = 1, PARRELLGRAM = 2 };
    /*
	0: red,
	1: orange
	2: yellow
	3: green
	4: lightblue
	5: blie
	6: purple
	 */
    public int _id;
    /*
	với tam giác ps[0] là góc vuông, sau đó chạy ngược kim đồng hổ
	với hình thoi thì ps[0] là góc nhọn
	 */
    public Point[] ps;
    /*
	khi cạnh trước của góc nhọn hình thòi lớn hơn cạnh sau thì isFlip = true
	 */
    public bool isFlip = false;
    public int type;
    public MyShape()
    {

    }
    public MyShape(int __id, Point[] _ps, bool _isFlip = false)
    {
        this._id = __id;
        this.ps = _ps;
        this.isFlip = _isFlip;

        if (__id == 2) type = (int)S_TYPE.SQUARE;
        else if (__id == 1) type = (int)S_TYPE.PARRELLGRAM;
        else type = (int)S_TYPE.TRIANGLE;
    }

    public void ValidateType()
    {
        if (_id == 2) type = (int)S_TYPE.SQUARE;
        else if (_id == 1) type = (int)S_TYPE.PARRELLGRAM;
        else type = (int)S_TYPE.TRIANGLE;
    }
    public bool isParrellgram(Point[] _ps)
    {
        if (_ps.Length != 4) return false;
        double[] ds = new double[4];
        for (int i = 0; i < _ps.Length; i++)
        {
            ds[i] = d(_ps[i], _ps[(i + 1) % 4]);
        }
        if (Math.Abs(ds[0] - ds[2]) > (ds[0] + ds[2]) / 5) return false;
        if (Math.Abs(ds[1] - ds[3]) > (ds[1] + ds[3]) / 5) return false;
        if (ds[0] > ds[1])
        {
            // if (Math.Abs(ds[0] - ds[1]) < ds[0] / 5) return false;
        }
        else
        {
            // if (Math.Abs(ds[1] - ds[0]) < ds[1] / 5) return false;
        }
        return true;
    }
    public bool isParrellgram()
    {
        return isParrellgram(this.ps);
    }

    public bool isTriangle()
    {
        double d1 = d(ps[0], ps[1]);
        double d2 = d(ps[0], ps[2]);
        if ((d1 - d2) > (d1 + d2) / 4) return false;
        double angel = Angel(ps[0], ps[1], ps[0], ps[2]);
        if (Math.Abs(angel - 90) > 45) return false;
        return true;
    }

    public bool isSquare()
    {
        if (this.ps.Length != 4) return false;
        double[] ds = new double[4];
        for (int i = 0; i < this.ps.Length; i++)
        {
            ds[i] = d(this.ps[i], this.ps[(i + 1) % 4]);
            double angel = Angel(ps[(i + 1) % 4], ps[i], ps[(i + 1) % 4], ps[(i + 2) % 4]);
            if (Math.Abs(angel - 90) > 45) return false;
        }
        for (int i = 0; i < ds.Length; i++)
        {
            if ((ds[i] - ds[(i + 1) % 4]) > (ds[i] + ds[(i + 1) % 4]) / 4) return false;
        }
        return true;
    }

    public MyShape Clone()
    {
        return new MyShape(this._id, this.ps, this.isFlip);
    }

    public Point GetCenter()
    {
        if (type == (int)S_TYPE.TRIANGLE)
        {
            if (ps.Length == 3)
                return Center(ps[1], ps[2]);
        }
        else if (type == (int)S_TYPE.SQUARE || type == (int)S_TYPE.PARRELLGRAM)
        {
            if (ps.Length == 4)
                return Center(ps[0], ps[2]);
        }
        return null;
    }

    public static Vector3 PointToVector3(Point p)
    {
        if (p == null) return Vector3.zero;
        return new Vector3((float)p.x - 4096, 4096 - (float)p.y, 0);
    }
    public double GetRotate()
    {
        if (type == (int)S_TYPE.TRIANGLE)
        {
            if (ps.Length == 3)
                return Angel(ps[1], ps[2]);
        }
        else if (type == (int)S_TYPE.SQUARE || type == (int)S_TYPE.PARRELLGRAM)
        {
            if (ps.Length == 4)
                return Angel(ps[0], ps[2]);
        }
        return 0;
    }
    Point Center(Point p1, Point p2)
    {
        return new Point((p1.x + p2.x) / 2, (p1.y + p2.y) / 2);
    }

    public static double d(Point p1, Point p2)
    {
        try
        {
            return (double)Math.Sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y));
        }
        catch (Exception e)
        {
            Logger.WARN(e.StackTrace);
        }
        return 0;
    }

    public static double Angel(Point p1, Point p2, Point p3, Point p4)
    {
        try
        {
            // return (double)Math.Sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y));
            double angel = Math.Abs(Math.Atan2((p1.y - p2.y), (p1.x - p2.x)) -
             Math.Atan2((p3.y - p4.y), (p3.x - p4.x))) / Math.PI * 180;
            if (angel > 180) angel = 360 - angel;
            return angel;
        }
        catch (Exception e)
        {
            Logger.WARN(e.StackTrace);
        }
        return 0;
    }

    public static double Angel(Point p1, Point p2)
    {
        try
        {
            double angel = Math.Atan2(4096 - p1.y - (4096 - p2.y), (p1.x - p2.x)) / Math.PI * 180 + 180;
            return angel;
        }
        catch (Exception e)
        {
            Logger.WARN(e.StackTrace);
        }
        return 0;
    }

    public static bool On(Point p, Point p1, Point p2, bool isDetect = false, int _id = -1, int peak = 0, bool isFlip = false)
    {
        double d0 = d(p1, p2);
        double d1 = d(p, p1);
        double d2 = d(p, p2);
        double dev = 20;
        if (isDetect) dev = 4;
        if (isDetect && _id != -1)
        {
            if (_id == 0 || _id == 5)
            {
                if (peak == 1) dev = 11;
                else dev = 8;
            }
            else if (_id == 3)
            {
                if (peak == 1) dev = 8;
                else dev = 5.5;
            }
            else if (_id == 1)
            {
                if (peak == 0 || peak == 0)
                {
                    if (!isFlip) dev = 5.5;
                    else dev = 4;
                }
                else
                {
                    if (isFlip) dev = 5.5;
                    else dev = 4;
                }
            }
            // dev *= 0.85;
        }
        if (d1 < d0 / 10 || d2 < d0 / 10)
        {
            return false;
        }
        if (Math.Abs(d1 + d2 - d0) < d0 / dev) return true;
        // double pp = (d0 + d1 + d2) / 2;
        // double h = 2 * Math.Sqrt(pp * (pp - d0) * (pp - d1) * (pp - d2)) / d0;
        // if (h < d0 / 8) return true;
        return false;
    }
    public static double GetDisP2P(int _id, Point[] _ps, bool _isFlip)
    {
        double p = 1;
        if (_id == 0 || _id == 5)
        {
            return d(_ps[0], _ps[1]) / 2 / p;
        }
        if (_id == 1)
        {
            if (!_isFlip)
                return d(_ps[0], _ps[1]) / 1.4 / p;
            else
                return d(_ps[0], _ps[1]) / 1 / p;
        }
        return d(_ps[0], _ps[1]) / 1 / p;
    }
    public static double GetLength(int _id, Point[] _ps, int _peak, bool _isFlip)
    {
        double p = 1;
        if (_id == 0 || _id == 5)
        {
            if (_peak == 1) return 2.8;
            else return 2;
        }
        if (_id == 1)
        {
            if (!_isFlip)
            {
                if (_peak == 0 || _peak == 2) return 1.4;
                else return 1;
            }
            else
            {
                if (_peak == 0 || _peak == 2) return 1;
                else return 1.4;
            }
        }
        if (_id == 2) return 1;
        if (_peak == 1) return 1.4;
        else return 1;
    }
    public static double Ratio(Point p, Point p1, Point p2)
    {
        double d0 = d(p1, p2);
        double d1 = d(p, p1);
        double d2 = d(p, p2);
        return (d1 + d2) / d0;
    }

    public static double Ratio2(Point p, Point p1, Point p2)
    {
        double d0 = d(p1, p2);
        double d1 = d(p, p1);
        double d2 = d(p, p2);
        return (d1 + d1 == 0) ? -1 : Math.Min(d1, d2) / (d1 + d2);
    }
    public static int IsDuplicate(Point p1, Point p2, Point p3, Point p4, bool isDetect = false, bool big = false)
    {
        double d1 = d(p1, p2);
        double d2 = d(p3, p4);
        double delta = d1 / 10;
        if (isDetect) delta = (d1 + d2) / 6;
        if (isDetect && big) delta = (d1 + d2) / 16;
        if (d1 - d2 > delta) return 0;
        double d3 = d(p1, p3);
        double d4 = d(p2, p4);
        if ((d3 + d4) / 2 < delta)
        {
            if (d3 < delta && d4 < delta)
                return 1;
        }
        double d5 = d(p1, p4);
        double d6 = d(p2, p3);
        // Debug.Log(d1 + " - " + d2 + " - " + d5 + " - " + d6);
        if ((d5 + d6) / 2 < delta)
            if (d5 < delta && d6 < delta)
                return -1;
        return 0;
    }

    public bool IsZeroAll()
    {
        if (ps == null) return true;
        for (int i = 0; i < ps.Length; i++)
        {
            if (d(ps[i], ps[(i + 1) % ps.Length]) == 0) return true;
        }
        for (int i = 0; i < ps.Length; i++)
        {
            if (d(ps[i], new Point(0, 0)) != 0) return false;
        }
        return true;
    }
}
