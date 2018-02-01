using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;

[Serializable]
public class TangramResultModel
{
    public int[] datas = { 0, 0, 0, 0, 0, 0, 0 };
    public int option = 0;
}
[Serializable]
public class TangramFeatureModelList
{
    private bool DEBUG = false;

    public List<TangramFeatureModel> datas = new List<TangramFeatureModel>();
    public void ProcessInput(MyShape[] shapes, bool isLevel2 = false)
    {
        if (isLevel2)
        {
            datas = new List<TangramFeatureModel>();

            for (int i = 0; i < 4; i++)
            {
                MyShape[] shapesClone = new MyShape[shapes.Length];
                for (int j = 0; j < shapesClone.Length; j++)
                {
                    shapesClone[j] = shapes[j].Clone();
                }
                TangramFeatureModel tfm = new TangramFeatureModel();
                tfm.ProcessInput(shapesClone, i);
                datas.Add(tfm);
            }
        }
    }
    public TangramResultModel Detect(params MyShape[] shapes)
    {
        TangramResultModel trm = new TangramResultModel();
        int[] result = { 0, 0, 0, 0, 0, 0, 0 };
        int maxCount = 0;
        for (int i = 0; i < datas.Count; i++)
        {
            int count = 0;
            int[] re = datas[i].Detect(shapes);
            for (int j = 0; j < re.Length; j++)
            {
                if (re[j] > 0) count++;
            }
            if (count > maxCount)
            {
                maxCount = count;
                result = re;
                trm.datas = re;
                trm.option = i;
            }
        }
        if(DEBUG) Debug.Log("Detect: " + (JsonUtility.ToJson(trm)));
        return trm;
    }

}
[Serializable]
public class TangramFeatureModel
{
    public static bool DEBUG = false;

    public static double DELTA_A = 25;
    public static double DELTA_R = 0.3f;
    public enum TYPE { NODE = 0, PEAK_TO_PEAK = 1, PEAK_TO_EDGE = 2, EDGE_TO_EDGE = 3 };
    public int[] status;
    public FeatureModel[] features;
    public bool isFlip = false;
    public TangramFeatureModel()
    {
        status = new int[21];
        features = new FeatureModel[21];
    }

    public static int GetIndex(int i, int j)
    {
        int[] map = { 0, 6, 11, 15, 18, 20 };
        return map[Math.Min(i, j)] + Math.Max(i, j) - 1 - Math.Min(i, j);
    }

    public int GetStatus(int i, int j)
    {
        return status[GetIndex(i, j)];
    }

    public void SetFeatureAndStatus(int i, int j, FeatureModel fm)
    {
        int index = GetIndex(i, j);
        status[index] = fm.status;
        features[index] = fm;
    }

    public FeatureModel GetFeature(int i, int j)
    {
        int index = GetIndex(i, j);
        return features[index];
    }

    public void ProcessInput(MyShape[] shapes, int iCase = 0)
    {
        if (iCase == 1)
        {
            Point[] ps = shapes[0].Clone().ps;
            shapes[0].ps = shapes[5].Clone().ps;
            shapes[5].ps = ps;
        }
        else if (iCase == 2)
        {
            Point[] ps1 = shapes[4].Clone().ps;
            shapes[4].ps = shapes[6].Clone().ps;
            shapes[6].ps = ps1;
        }
        else if (iCase == 3)
        {
            Point[] ps = shapes[0].Clone().ps;
            shapes[0].ps = shapes[5].Clone().ps;
            shapes[5].ps = ps;

            Point[] ps1 = shapes[4].Clone().ps;
            shapes[4].ps = shapes[6].Clone().ps;
            shapes[6].ps = ps1;
        }
        for (int i = 0; i < shapes.Length; i++)
        {
            for (int j = i + 1; j < shapes.Length; j++)
            {
                if (shapes[j]._id < shapes[i]._id)
                {
                    MyShape temp = shapes[i];
                    shapes[i] = shapes[j];
                    shapes[j] = temp;
                }
            }
        }
        for (int i = 0; i < shapes.Length; i++)
        {
            shapes[i].ValidateType();
        }
        for (int i = 0; i < shapes.Length; i++)
        {
            for (int j = i + 1; j < shapes.Length; j++)
            {
                // Debug.Log(shapes[i]._id + " -  " + shapes[j]._id);
                FeatureModel fm = new FeatureModel();
                fm.CalculatorFeatur(shapes[i], shapes[j]);
                SetFeatureAndStatus(i, j, fm);
            }
        }
        isFlip = shapes[1].isFlip;
    }
    public int[] Detect(params MyShape[] shapes)
    {
        shapes = ValidateMyShape(shapes);
        if (DEBUG) Debug.Log("================= Start Detect =================");
        int[] result = new int[7];
        for (int i = 0; i < shapes.Length; i++)
        {
            shapes[i].ValidateType();
        }
        bool[] check = new bool[21];
        bool checkFlip = true;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i].type == (int)MyShape.S_TYPE.PARRELLGRAM)
            {
                checkFlip = (isFlip == shapes[i].isFlip);
            }
            for (int j = i + 1; j < shapes.Length; j++)
            {
                // if (DEBUG) Debug.Log("==================== Check ====================");
                if (i == j) continue;
                if (GetStatus(shapes[i]._id, shapes[j]._id) != (int)TangramFeatureModel.TYPE.NODE)
                {
                    FeatureModel fm = new FeatureModel();
                    FeatureModel _fm = GetFeature(shapes[i]._id, shapes[j]._id);

                    fm.CalculatorFeatur(shapes[i], shapes[j], true, _fm);
                    if (Compare(shapes[i], shapes[j], fm))
                    {
                        if (DEBUG) Debug.Log(shapes[i]._id + " -  " + shapes[j]._id + " : true");
                        check[GetIndex(shapes[i]._id, shapes[j]._id)] = true;

                        if (DEBUG) Debug.Log(JsonUtility.ToJson(_fm));
                        if (DEBUG) Debug.Log(JsonUtility.ToJson(fm));
                    }
                    else
                    {
                        if (DEBUG) Debug.Log(shapes[i]._id + " -  " + shapes[j]._id + " : false");
                        if (DEBUG) Debug.Log(JsonUtility.ToJson(_fm));
                        if (DEBUG) Debug.Log(JsonUtility.ToJson(fm));
                    }
                }
            }
        }
        if (DEBUG) Debug.Log("==================== Check ====================");
        for (int i = 0; i < shapes.Length; i++)
        {
            int completeCount = 0;
            for (int j = 0; j < shapes.Length; j++)
            {
                if (i == j) continue;
                int index = GetIndex(shapes[i]._id, shapes[j]._id);
                if (check[index]) completeCount++;
                if (i == 1)
                {
                    // Debug.Log(i + " - " + j + " - " + check[index]);
                }
            }
            if (completeCount > 0)
            {
                result[shapes[i]._id] = 1;
            }
        }
        ValidateCompare(result, check);
        if (result[1] == 1)
            if (!checkFlip) result[1] = -1;
        if (DEBUG) Debug.Log(ArrayToString(result));
        return result;
    }
    public MyShape[] ValidateMyShape(MyShape[] shapes)
    {
        List<MyShape> list = new List<MyShape>();
        for (int i = 0; i < shapes.Length; i++)
        {
            if (!shapes[i].IsZeroAll())
            {
                list.Add(shapes[i]);
            }
        }
        return list.ToArray();
    }

    void ValidateCompare(int[] result, bool[] check)
    {
        List<int> list = new List<int>();
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == 1)
            {
                list.Add(i);
                break;
            }
        }
        List<int> listCom = new List<int>();
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == 1)
                if (!list.Contains(i))
                    listCom.Add(i);
        }
        while (listCom.Count > 0)
        {
            int count = 0;
            for (int i = 0; i < listCom.Count; i++)
            {
                bool ok = true;
                int matchCount = 0;
                for (int j = 0; j < list.Count; j++)
                {
                    int index = GetIndex(list[j], listCom[i]);
                    if (status[index] != (int)TangramFeatureModel.TYPE.NODE)
                    {
                        if (!check[index]) ok = false;
                        else matchCount++;
                    }
                }
                if (ok && matchCount > 0)
                {
                    list.Add(listCom[i]);
                    listCom.RemoveAt(i);
                    count++;
                    break;
                }
            }
            if (count == 0) break;
        }

        if (list.Count == 1)
        {
            result[list[0]] = 0;
            ValidateCompare(result, check);
        }
        else
        {
            for (int i = 0; i < result.Length; i++)
            {
                if (!list.Contains(i)) result[i] = 0;
            }
        }
    }

    public static string ArrayToString(int[] arr)
    {
        string s = "";
        for (int i = 0; i < arr.Length; i++)
        {
            s += arr[i] + ", ";
        }
        return s;
    }

    public bool Compare(MyShape ms1, MyShape ms2, FeatureModel _fm)
    {
        double DEVICE = 1;
        if (_fm.status == (int)TYPE.EDGE_TO_EDGE)
        {
            if (_fm._ids.Length == 2 && _fm.peaks.Length == 4)
            {
                if (_fm._ids[0] < _fm._ids[1])
                {
                    DEVICE = MyShape.GetLength(_fm._ids[0], ms1.ps, _fm.peaks[2], ms1.isFlip);
                }
                else
                {
                    DEVICE = MyShape.GetLength(_fm._ids[0], ms2.ps, _fm.peaks[0], ms2.isFlip);
                }
            }
        }
        // DEVICE *= 1.5;
        int index = GetIndex(ms1._id, ms2._id);
        if (status[index] != _fm.status) return false;
        FeatureModel fm = GetFeature(ms1._id, ms2._id);
        if (ms1.type != (int)MyShape.S_TYPE.SQUARE
         && ms2.type != (int)MyShape.S_TYPE.SQUARE
         && ms1.type != (int)MyShape.S_TYPE.PARRELLGRAM
         && ms2.type != (int)MyShape.S_TYPE.PARRELLGRAM)
        {
            if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_PEAK)
            {
                if (fm.peaks.Length == _fm.peaks.Length)
                {
                    for (int i = 0; i < fm.peaks.Length; i++)
                    {
                        if (fm.peaks[i] != _fm.peaks[i])
                        {
                            return false;
                        }
                    }
                }
                else return false;
                if (Math.Abs(fm.angle - _fm.angle) > DELTA_A) return false;
                else return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE)
            {
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R)
                {
                    return false;
                }
                if (fm.ratioPeaks[0] != _fm.ratioPeaks[0])
                {
                    return false;
                }
                if (Math.Abs(fm.angle - _fm.angle) > 15)
                {
                    return false;
                }
                return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE)
            {
                if (_fm.duplicate != 0)
                {
                    if (fm.duplicate != _fm.duplicate) return false;
                    else
                    {
                        for (int i = 0; i < _fm.peaks.Length; i++)
                        {
                            if (fm.peaks[i] != _fm.peaks[i]) return false;
                        }
                        return true;
                    }
                }
                if (fm._ids.Length == _fm._ids.Length)
                {
                    for (int i = 0; i < fm._ids.Length; i++)
                    {
                        if (fm._ids[i] != _fm._ids[i]) return false;
                    }
                }
                else return false;
                if (fm.ratioPeaks.Length == _fm.ratioPeaks.Length)
                {
                    if (fm.ratioPeaks[0] != _fm.ratioPeaks[0]) return false;
                }
                else return false;
                if (fm.peaks.Length == _fm.peaks.Length)
                {
                    for (int i = 0; i < fm.peaks.Length; i++)
                    {
                        if (fm.peaks[i] != _fm.peaks[i]) return false;
                    }
                }
                else return false;
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R / DEVICE) return false;
                return true;
            }
        }
        else if ((ms1.type == (int)MyShape.S_TYPE.SQUARE
         && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
         ||
         (ms2.type == (int)MyShape.S_TYPE.SQUARE
         && ms1.type == (int)MyShape.S_TYPE.TRIANGLE))
        {
            if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_PEAK)
            {
                if (fm.peaks.Length == _fm.peaks.Length)
                {
                    for (int i = 0; i < fm.peaks.Length; i++)
                    {
                        if (fm.peaks[i] != _fm.peaks[i] && ((i == 0 && ms1.type == (int)MyShape.S_TYPE.TRIANGLE) || i == 1 && ms2.type == (int)MyShape.S_TYPE.TRIANGLE))
                        {
                            return false;
                        }
                    }
                }
                else return false;
                if (Math.Abs(fm.angle - _fm.angle) > DELTA_A) return false;
                else return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE)
            {
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R)
                {
                    return false;
                }
                if (fm._ids[0] != 2 && fm.ratioPeaks[0] != _fm.ratioPeaks[0])
                {
                    return false;
                }
                if (Math.Abs(fm.angle - _fm.angle) > DELTA_A)
                {
                    return false;
                }
                return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE)
            {
                if (_fm.duplicate != 0)
                {
                    if (fm.duplicate != _fm.duplicate) return false;
                    else
                    {
                        for (int i = 0; i < _fm.peaks.Length; i++)
                        {
                            if (fm.peaks[i] != _fm.peaks[i])
                            {
                                if (i < 2 && ms1.type == (int)MyShape.S_TYPE.TRIANGLE)
                                    return false;
                                if (i >= 2 && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                    return false;
                            }
                        }
                        return true;
                    }
                }
                if (fm._ids.Length == _fm._ids.Length)
                {
                    for (int i = 0; i < fm._ids.Length; i++)
                    {
                        if (fm._ids[i] != _fm._ids[i]) return false;
                    }
                }
                else return false;
                if (fm.ratioPeaks.Length == _fm.ratioPeaks.Length)
                {
                    if (fm._ids[0] != 2 && fm.ratioPeaks[0] != _fm.ratioPeaks[0]) return false;
                }
                else return false;
                if (fm.peaks.Length != _fm.peaks.Length) return false;
                for (int i = 0; i < fm.peaks.Length; i++)
                {
                    if (i < 2 && ms1.type == (int)MyShape.S_TYPE.TRIANGLE)
                    {
                        if (fm.peaks[i] != _fm.peaks[i]) return false;
                    }
                    else if (i >= 2 && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                    {
                        if (fm.peaks[i] != _fm.peaks[i]) return false;
                    }
                }
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R / DEVICE) return false;
                if (fm.forward != _fm.forward) return false;
                return true;
            }
        }
        else if ((ms1.type == (int)MyShape.S_TYPE.PARRELLGRAM
         && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
         ||
         (ms2.type == (int)MyShape.S_TYPE.PARRELLGRAM
         && ms1.type == (int)MyShape.S_TYPE.TRIANGLE))
        {
            if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_PEAK)
            {
                if (fm.peaks.Length == _fm.peaks.Length)
                {
                    for (int i = 0; i < fm.peaks.Length; i++)
                    {
                        if (fm.peaks[i] != _fm.peaks[i] &&
                        ((i == 0 && ms1.type == (int)MyShape.S_TYPE.TRIANGLE) || i == 1 && ms2.type == (int)MyShape.S_TYPE.TRIANGLE))
                        {
                            return false;
                        }
                        if ((i == 0 && ms1.type == (int)MyShape.S_TYPE.PARRELLGRAM) || (i == 1 && ms2.type == (int)MyShape.S_TYPE.PARRELLGRAM))
                        {
                            if (fm.peaks[i] != _fm.peaks[i] && fm.peaks[i] != (_fm.peaks[i] + 2) % 4)
                                return false;
                        }
                    }
                }
                else return false;
                if (Math.Abs(fm.angle - _fm.angle) > 15) return false;
                else return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE)
            {
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R)
                {
                    return false;
                }
                if (fm._ids[0] != 1 && fm.ratioPeaks[0] != _fm.ratioPeaks[0])
                {
                    return false;
                }

                if (fm._ids[0] == 1 && (fm.ratioPeaks[0] != _fm.ratioPeaks[0] && fm.ratioPeaks[0] != (_fm.ratioPeaks[0] + 2) % 4))
                {
                    return false;
                }
                if (Math.Abs(fm.angle - _fm.angle) > DELTA_A)
                {
                    return false;
                }
                return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE)
            {
                if (_fm.duplicate != 0)
                {
                    if (fm.duplicate != _fm.duplicate) return false;
                    else
                    {
                        for (int i = 0; i < _fm.peaks.Length; i++)
                        {
                            if (fm.peaks[i] != _fm.peaks[i])
                            {
                                if (i < 2 && ms1.type == (int)MyShape.S_TYPE.TRIANGLE)
                                    return false;
                                if (i >= 2 && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                    return false;
                            }
                        }
                        return true;
                    }
                }
                if (fm._ids.Length == _fm._ids.Length)
                {
                    for (int i = 0; i < fm._ids.Length; i++)
                    {
                        if (fm._ids[i] != _fm._ids[i]) return false;
                    }
                }
                else return false;
                if (fm.ratioPeaks.Length == _fm.ratioPeaks.Length)
                {
                    if (fm._ids[0] != 1 && fm.ratioPeaks[0] != _fm.ratioPeaks[0]) return false;
                }
                else return false;

                if (fm._ids[0] == 1 && (fm.ratioPeaks[0] != _fm.ratioPeaks[0] && fm.ratioPeaks[0] != (_fm.ratioPeaks[0] + 2) % 4))
                {
                    return false;
                }
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R / DEVICE) return false;
                return true;
            }
        }
        else if ((ms1.type == (int)MyShape.S_TYPE.SQUARE
         && ms2.type == (int)MyShape.S_TYPE.PARRELLGRAM)
         ||
         (ms2.type == (int)MyShape.S_TYPE.SQUARE
         && ms1.type == (int)MyShape.S_TYPE.PARRELLGRAM))
        {
            if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_PEAK)
            {
                if (fm.peaks.Length == _fm.peaks.Length)
                {
                    for (int i = 0; i < fm.peaks.Length; i++)
                    {
                        if (i == 0 && ms1.type == (int)MyShape.S_TYPE.PARRELLGRAM)
                        {
                            if (fm.peaks[0] != _fm.peaks[0] && fm.peaks[0] != (_fm.peaks[0] + 2) % 4)
                            {
                                return false;
                            }

                        }

                        if (i == 1 && ms2.type == (int)MyShape.S_TYPE.PARRELLGRAM)
                        {
                            if (fm.peaks[i] != _fm.peaks[i] && fm.peaks[i] != (_fm.peaks[i] + 2) % 4) return false;
                        }
                    }
                }
                else return false;
                if (Math.Abs(fm.angle - _fm.angle) > DELTA_A) return false;
                else return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE)
            {
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R)
                {
                    return false;
                }
                if (fm._ids[0] == 1 && (fm.ratioPeaks[0] != _fm.ratioPeaks[0] && fm.ratioPeaks[0] != (_fm.ratioPeaks[0] + 2) % 4))
                {
                    return false;
                }
                if (Math.Abs(fm.angle - _fm.angle) > DELTA_A)
                {
                    return false;
                }
                return true;
            }
            else if (_fm.status == (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE)
            {
                if (_fm.duplicate != 0)
                {
                    if (fm.duplicate != _fm.duplicate) return false;
                    else
                    {
                        for (int i = 0; i < _fm.peaks.Length; i++)
                        {
                            if (fm.peaks[i] != _fm.peaks[i] && fm.peaks[i] != (_fm.peaks[i] + 2) % 4)
                            {
                                if (i < 2 && ms1.type == (int)MyShape.S_TYPE.PARRELLGRAM)
                                    return false;
                                if (i >= 2 && ms2.type == (int)MyShape.S_TYPE.PARRELLGRAM)
                                    return false;
                            }
                        }
                        return true;
                    }
                }
                if (fm._ids.Length == _fm._ids.Length)
                {
                    for (int i = 0; i < fm._ids.Length; i++)
                    {
                        if (fm._ids[i] != _fm._ids[i]) return false;
                    }
                }
                else return false;

                if (fm.ratioPeaks.Length == _fm.ratioPeaks.Length)
                {
                }
                else return false;

                if (fm._ids[0] == 1 && (fm.ratioPeaks[0] != _fm.ratioPeaks[0] && fm.ratioPeaks[0] != (_fm.ratioPeaks[0] + 2) % 4))
                {
                    return false;
                }
                if (Math.Abs(fm.ratios[0] - _fm.ratios[0]) > DELTA_R / DEVICE) return false;
                return true;
            }
        }
        return false;
    }
}
[Serializable]
public class SquareFeature
{

}
[Serializable]
public class FeatureModel
{
    public int status;
    public int[] peaks;
    public double[] ratios;
    public int[] ratioPeaks;
    public int[] _ids = new int[0];
    public int squareIndex = -1;
    public double angle = -1;
    public int duplicate = 0;
    public bool forward = true;
    public FeatureModel()
    {

    }

    public void CalculatorFeatur(MyShape _ms1, MyShape _ms2, bool isInDetect = false, FeatureModel fm = null)
    {
        try
        {
            status = (int)TangramFeatureModel.TYPE.NODE;
            ratios = new double[1];
            ratioPeaks = new int[1];
            if (_ms1.ps.Length < 3 || _ms2.ps.Length < 3) return;
            MyShape ms1 = (_ms1._id < _ms2._id) ? _ms1.Clone() : _ms2.Clone();
            MyShape ms2 = (_ms1._id > _ms2._id) ? _ms1.Clone() : _ms2.Clone();
            if (ms1.type == (int)MyShape.S_TYPE.SQUARE)
                squareIndex = 0;
            if (ms1.type == (int)MyShape.S_TYPE.SQUARE)
                squareIndex = 1;
            double d = (MyShape.d(ms1.ps[0], ms1.ps[1]) + MyShape.d(ms1.ps[1], ms1.ps[2]) + MyShape.d(ms2.ps[0], ms2.ps[1]) + MyShape.d(ms2.ps[1], ms2.ps[2])) / 4;

            if (!isInDetect || (isInDetect && fm.duplicate != 0))
            {
                for (int i = 0; i < ms1.ps.Length; i++)
                {
                    for (int j = 0; j < ms2.ps.Length; j++)
                    {
                        double a = MyShape.Angel(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                        int deltaA = 5;
                        if (isInDetect) deltaA = 15;
                        if (Mathf.Abs((float)a) < deltaA || Mathf.Abs((float)a - 180) < deltaA)
                        {
                            bool big = ((ms1._id == 0 && ms2._id == 5) || (ms1._id == 5 && ms2._id == 0));
                            int _duplicate = MyShape.IsDuplicate(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length], isInDetect, big);
                            // if (ms2._id == 6 || ms1._id == 6)
                            // {
                            //     Debug.Log("a: " + a);
                            //     Debug.Log(i + " - " + ((i + 1) % ms1.ps.Length) + " - " + j + " - " + ((j + 1) % ms2.ps.Length));
                            //     Debug.Log("_duplicate: " + _duplicate);
                            // }
                            if (_duplicate != 0)
                            {
                                peaks = new int[4];
                                peaks[0] = i;
                                peaks[1] = (i + 1) % ms1.ps.Length;
                                peaks[2] = j;
                                peaks[3] = (j + 1) % ms2.ps.Length;
                                duplicate = _duplicate;
                                status = (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE;
                                return;
                            }
                        }
                    }
                }
            }
            if (!isInDetect || (isInDetect && fm.status == (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE))
            {
                for (int i = 0; i < ms1.ps.Length; i++)
                {
                    for (int j = 0; j < ms2.ps.Length; j++)
                    {
                        double a = MyShape.Angel(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                        int deltaA = 5;
                        if (isInDetect) deltaA = 15;
                        // Debug.Log("peak: " + i + " - " + j + " - " + a);
                        if (Mathf.Abs((float)a) < deltaA || Mathf.Abs((float)a - 180) < deltaA)
                        {
                            //case 1
                            if (MyShape.On(ms1.ps[i], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length], isInDetect, ms2._id, j, ms2.isFlip))
                            {
                                if ((isInDetect) || ratios[0] < MyShape.Ratio2(ms1.ps[(i + 1) % _ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]))
                                {
                                    _ids = new int[2]; _ids[0] = _ms1._id; _ids[1] = ms2._id;
                                    ratios[0] = MyShape.Ratio2(ms1.ps[i], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                                    ratioPeaks[0] = i;
                                    peaks = new int[4];
                                    peaks[0] = i;
                                    peaks[1] = (i + 1) % ms1.ps.Length;
                                    peaks[2] = j;
                                    peaks[3] = (j + 1) % ms2.ps.Length;
                                    status = (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE;

                                    if (isInDetect)
                                        if (fm.forward == forward)
                                            if (ms1._id == 2)
                                            {
                                                if (Math.Abs(ratios[0] - fm.ratios[0]) < 0.25) return;
                                            }
                                            else if (_ids[0] == fm._ids[0] && _ids[1] == fm._ids[1])
                                                if (ratios[0] > 0)
                                                {
                                                    if (ms1._id == 1)
                                                    {
                                                        if (ratioPeaks[0] == fm.ratioPeaks[0] || ratioPeaks[0] == (fm.ratioPeaks[0] + 2) % 4) return;
                                                    }
                                                    else if (ratioPeaks[0] == fm.ratioPeaks[0])
                                                    {
                                                        if (ms1.type == (int)MyShape.S_TYPE.TRIANGLE && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                                        {
                                                            bool notMatch = false;
                                                            for (int k = 0; k < peaks.Length; k++)
                                                            {
                                                                if (peaks[k] != fm.peaks[k]) notMatch = true;
                                                            }
                                                            if (!notMatch) return;
                                                        }
                                                        else return;
                                                    }
                                                }
                                }
                            }

                            //case 2
                            if (MyShape.On(ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length], isInDetect, ms2._id, j, ms2.isFlip))
                            {
                                if ((isInDetect) || ratios[0] < MyShape.Ratio2(ms1.ps[(i + 1) % _ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]))
                                {
                                    _ids = new int[2]; _ids[0] = _ms1._id; _ids[1] = ms2._id;
                                    ratios[0] = MyShape.Ratio2(ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                                    ratioPeaks[0] = (i + 1) % ms1.ps.Length;
                                    peaks = new int[4];
                                    peaks[0] = i;
                                    peaks[1] = (i + 1) % ms1.ps.Length;
                                    peaks[2] = j;
                                    peaks[3] = (j + 1) % ms2.ps.Length;
                                    status = (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE;
                                    forward = false;
                                    if (isInDetect)
                                        if (fm.forward == forward)
                                            if (ms1._id == 2)
                                            {
                                                if (Math.Abs(ratios[0] - fm.ratios[0]) < 0.25) return;
                                            }
                                            else if (_ids[0] == fm._ids[0] && _ids[1] == fm._ids[1])
                                                if (ratios[0] > 0)
                                                {
                                                    if (ms1._id == 1)
                                                    {
                                                        if (ratioPeaks[0] == fm.ratioPeaks[0] || ratioPeaks[0] == (fm.ratioPeaks[0] + 2) % 4) return;
                                                    }
                                                    else if (ratioPeaks[0] == fm.ratioPeaks[0])
                                                    {
                                                        if (ms1.type == (int)MyShape.S_TYPE.TRIANGLE && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                                        {
                                                            bool notMatch = false;
                                                            for (int k = 0; k < peaks.Length; k++)
                                                            {
                                                                if (peaks[k] != fm.peaks[k]) notMatch = true;
                                                            }
                                                            if (!notMatch) return;
                                                        }
                                                        else return;
                                                    }
                                                }
                                }
                            }
                            //Case 3
                            if (MyShape.On(ms2.ps[j], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], isInDetect, ms1._id, i, ms1.isFlip))
                            {

                                if ((isInDetect) || ratios[0] < MyShape.Ratio2(ms1.ps[(i + 1) % _ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]))
                                {
                                    _ids = new int[2]; _ids[0] = _ms2._id; _ids[1] = ms1._id;
                                    ratios[0] = MyShape.Ratio2(ms2.ps[j], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length]);
                                    ratioPeaks[0] = j;

                                    peaks = new int[4];
                                    peaks[0] = i;
                                    peaks[1] = (i + 1) % ms1.ps.Length;
                                    peaks[2] = j;
                                    peaks[3] = (j + 1) % ms2.ps.Length;
                                    status = (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE;

                                    if (isInDetect)
                                        if (fm.forward == forward)
                                            if (ms2._id == 2)
                                            {
                                                if (Math.Abs(ratios[0] - fm.ratios[0]) < 0.25) return;
                                            }
                                            else if (_ids[0] == fm._ids[0] && _ids[1] == fm._ids[1])
                                                if (ratios[0] > 0)
                                                {
                                                    if (ms2._id == 1)
                                                    {
                                                        if (ratioPeaks[0] == fm.ratioPeaks[0] || ratioPeaks[0] == (fm.ratioPeaks[0] + 2) % 4) return;
                                                    }
                                                    else if (ratioPeaks[0] == fm.ratioPeaks[0])
                                                    {
                                                        if (ms1.type == (int)MyShape.S_TYPE.TRIANGLE && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                                        {
                                                            bool notMatch = false;
                                                            for (int k = 0; k < peaks.Length; k++)
                                                            {
                                                                if (peaks[k] != fm.peaks[k]) notMatch = true;
                                                            }
                                                            if (!notMatch) return;
                                                        }
                                                        else return;
                                                    }
                                                }
                                }

                            }
                            //case 4
                            if (MyShape.On(ms2.ps[(j + 1) % ms2.ps.Length], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], isInDetect, ms1._id, i, ms1.isFlip))
                            {

                                if ((isInDetect) || ratios[0] < MyShape.Ratio2(ms1.ps[(i + 1) % _ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]))
                                {
                                    _ids = new int[2]; _ids[0] = _ms2._id; _ids[1] = ms1._id;
                                    ratios[0] = MyShape.Ratio2(ms2.ps[(j + 1) % ms2.ps.Length], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length]);
                                    ratioPeaks[0] = (j + 1) % ms2.ps.Length;

                                    peaks = new int[4];
                                    peaks[0] = i;
                                    peaks[1] = (i + 1) % ms1.ps.Length;
                                    peaks[2] = j;
                                    peaks[3] = (j + 1) % ms2.ps.Length;
                                    status = (int)TangramFeatureModel.TYPE.EDGE_TO_EDGE;
                                    forward = false;
                                    if (isInDetect)
                                        if (fm.forward == forward)
                                            if (ms2._id == 2)
                                            {
                                                if (Math.Abs(ratios[0] - fm.ratios[0]) < 0.25) return;
                                            }
                                            else if (_ids[0] == fm._ids[0] && _ids[1] == fm._ids[1])
                                                if (ratios[0] > 0)
                                                {
                                                    if (ms2._id == 1)
                                                    {
                                                        if (ratioPeaks[0] == fm.ratioPeaks[0] || ratioPeaks[0] == (fm.ratioPeaks[0] + 2) % 4) return;
                                                    }
                                                    else if (ratioPeaks[0] == fm.ratioPeaks[0])
                                                    {
                                                        if (ms1.type == (int)MyShape.S_TYPE.TRIANGLE && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                                        {
                                                            bool notMatch = false;
                                                            for (int k = 0; k < peaks.Length; k++)
                                                            {
                                                                if (peaks[k] != fm.peaks[k]) notMatch = true;
                                                            }
                                                            if (!notMatch) return;
                                                        }
                                                        else return;
                                                    }
                                                }
                                }

                            }
                        }
                    }
                }
                if (ratios[0] != 0)
                {
                    return;
                }
            }
            d = MyShape.GetDisP2P(ms1._id, ms1.ps, ms1.isFlip);
            if (!isInDetect || (isInDetect && fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_PEAK))
                for (int i = 0; i < ms1.ps.Length; i++)
                {
                    for (int j = 0; j < ms2.ps.Length; j++)
                    {
                        double k = 0.05f;
                        if (isInDetect) k = 0.4f;
                        //can tinh toan lai d cho ngon hon

                        if (MyShape.d(ms1.ps[i], ms2.ps[j]) < k * d)
                        {
                            status = (int)TangramFeatureModel.TYPE.PEAK_TO_PEAK;
                            peaks = new int[2];
                            peaks[0] = i;
                            peaks[1] = j;
                            angle = MyShape.Angel(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                            if (isInDetect)
                            {
                                if (ms1._id == 2)
                                {
                                    if (ms2._id == 1)
                                    {
                                        if (peaks[1] == fm.peaks[1] || peaks[1] == (fm.peaks[1] + 2) % 4) return;
                                    }
                                    else if (peaks[1] == fm.peaks[1]) return;
                                }
                                else if (ms2._id == 2)
                                {
                                    if (ms1._id == 1)
                                    {
                                        if (peaks[0] == fm.peaks[0] || peaks[0] == (fm.peaks[0] + 2) % 4) return;
                                    }
                                    else if (peaks[0] == fm.peaks[0]) return;
                                }
                                else if (ms1._id == 1)
                                {
                                    if ((peaks[0] == fm.peaks[0] || peaks[0] == (fm.peaks[0] + 2) % 4) && peaks[1] == fm.peaks[1]) return;
                                }
                                else if (ms2._id == 1)
                                {
                                    if (peaks[0] == fm.peaks[0] && (peaks[1] == fm.peaks[1] || peaks[1] == (fm.peaks[1] + 2) % 4)) return;
                                }
                                else
                                {
                                    if (peaks[0] == fm.peaks[0] && peaks[1] == fm.peaks[1]) return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
            if (!isInDetect || (isInDetect && fm.status == (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE))
                for (int i = 0; i < ms1.ps.Length; i++)
                {
                    for (int j = 0; j < ms2.ps.Length; j++)
                    {
                        if (MyShape.On(ms1.ps[i], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length], isInDetect, ms2._id, j, ms2.isFlip))
                        {
                            if ((isInDetect) || ratios[0] < MyShape.Ratio2(ms1.ps[i], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]))
                            {
                                status = (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE;
                                ratios[0] = MyShape.Ratio2(ms1.ps[i], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                                ratioPeaks[0] = i;
                                _ids = new int[2]; _ids[0] = _ms1._id; _ids[1] = ms2._id;

                                angle = MyShape.Angel(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                                peaks = new int[4];
                                peaks[0] = i;
                                peaks[1] = (i + 1) % ms1.ps.Length;
                                peaks[2] = j;
                                peaks[3] = (j + 1) % ms2.ps.Length;

                                if (isInDetect)
                                // if (ms1._id == 2)
                                // {
                                //     Debug.Log("ij" + j);
                                //     if (Math.Abs(ratios[0] - fm.ratios[0]) < 0.2) return;
                                // }
                                // else
                                {
                                    if (_ids[0] == fm._ids[0] && _ids[1] == fm._ids[1])
                                        if (ratios[0] > 0)
                                        {
                                            if (ms1._id == 1)
                                            {
                                                if (ratioPeaks[0] == fm.ratioPeaks[0] || ratioPeaks[0] == (fm.ratioPeaks[0] + 2) % 4)
                                                {
                                                    if (peaks[0] == fm.peaks[0] || peaks[0] == (fm.peaks[0] + 2) % 4)
                                                        if (peaks[1] == fm.peaks[1] || peaks[1] == (fm.peaks[1] + 2) % 4)
                                                            if (peaks[2] == fm.peaks[2])
                                                                if (peaks[3] == fm.peaks[3])
                                                                    return;
                                                }
                                            }
                                            else if (ratioPeaks[0] == fm.ratioPeaks[0])
                                            {
                                                if (ms1.type == (int)MyShape.S_TYPE.TRIANGLE && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                                {
                                                    bool notMatch = false;
                                                    for (int k = 0; k < peaks.Length; k++)
                                                    {
                                                        if (peaks[k] != fm.peaks[k]) notMatch = true;
                                                    }
                                                    if (!notMatch) return;
                                                }
                                                else return;
                                            }
                                        }
                                }
                            }
                        }
                        if (MyShape.On(ms2.ps[j], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], isInDetect, ms1._id, i, ms1.isFlip))
                        {
                            if ((isInDetect) || ratios[0] < MyShape.Ratio2(ms2.ps[j], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length]))
                            {
                                status = (int)TangramFeatureModel.TYPE.PEAK_TO_EDGE;
                                ratios[0] = MyShape.Ratio2(ms2.ps[j], ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length]);
                                ratioPeaks[0] = j;
                                _ids = new int[2]; _ids[0] = _ms2._id; _ids[1] = ms1._id;

                                angle = MyShape.Angel(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                                peaks = new int[4];
                                peaks[0] = i;
                                peaks[1] = (i + 1) % ms1.ps.Length;
                                peaks[2] = j;
                                peaks[3] = (j + 1) % ms2.ps.Length;
                                if (isInDetect)
                                // if (ms2._id == 2)
                                // {
                                //     if (Math.Abs(ratios[0] - fm.ratios[0]) < 0.2) return;
                                // }
                                // else
                                {
                                    if (_ids[0] == fm._ids[0] && _ids[1] == fm._ids[1])
                                        if (ratios[0] > 0)
                                        {
                                            if (ms2._id == 1)
                                            {
                                                if (ratioPeaks[0] == fm.ratioPeaks[0] || ratioPeaks[0] == (fm.ratioPeaks[0] + 2) % 4)
                                                    if (peaks[2] == fm.peaks[2] || peaks[2] == (fm.peaks[2] + 2) % 4)
                                                        if (peaks[3] == fm.peaks[3] || peaks[3] == (fm.peaks[3] + 2) % 4)
                                                            if (peaks[0] == fm.peaks[0])
                                                                if (peaks[1] == fm.peaks[1])
                                                                    return;
                                            }
                                            else if (ratioPeaks[0] == fm.ratioPeaks[0])
                                            {
                                                if (ms1.type == (int)MyShape.S_TYPE.TRIANGLE && ms2.type == (int)MyShape.S_TYPE.TRIANGLE)
                                                {
                                                    bool notMatch = false;
                                                    for (int k = 0; k < peaks.Length; k++)
                                                    {
                                                        if (peaks[k] != fm.peaks[k]) notMatch = true;
                                                    }
                                                    if (!notMatch) return;
                                                }
                                                else return;
                                            }
                                        }
                                }
                            }
                        }
                        if (status != (int)TangramFeatureModel.TYPE.NODE)
                        {
                            // angle = MyShape.Angel(ms1.ps[i], ms1.ps[(i + 1) % ms1.ps.Length], ms2.ps[j], ms2.ps[(j + 1) % ms2.ps.Length]);
                            // peaks = new int[4];
                            // peaks[0] = i;
                            // peaks[1] = (i + 1) % ms1.ps.Length;
                            // peaks[2] = j;
                            // peaks[3] = (j + 1) % ms2.ps.Length;
                            // return;
                        }
                    }
                }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}
