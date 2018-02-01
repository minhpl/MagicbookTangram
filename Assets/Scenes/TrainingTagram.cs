using OpenCVForUnity;
using OpenCVForUnityExample;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrainingTagram : MonoBehaviour
{
    private bool debug = false;
    public RawImage mainDebug;
    public RawImage debug1;
    public RawImage debug2;
    public RawImage debug3;
    public RawImage debug4;
    public RawImage debug5;
    public RawImage debug6;
    public RawImage debug7;
    public RawImage debug8;

    Texture2D texture;
    Texture2D dbTxt1;
    Texture2D dbTxt2;
    Texture2D dbTxt3;
    Texture2D dbTxt4;
    Texture2D dbTxt5;
    Texture2D dbTxt6;
    Texture2D dbTxt7;
    Texture2D dbTxt8;


    TangramObject red = new TangramObject("red");
    TangramObject orange = new TangramObject("orange");
    TangramObject yellow = new TangramObject("yellow");
    TangramObject green = new TangramObject("green");
    TangramObject lightBlue = new TangramObject("lightBlue");
    TangramObject blue = new TangramObject("blue");
    TangramObject purple = new TangramObject("purple");
    const int MIN_OBJECT_AREA = 1000;
    const int thickness = 5;
    enum tgr { ORANGE = 1, RED = 0, YELLOW = 2, GREEN = 3, BLUE = 5, LIGHTBLUE = 4, PURPLE = 6 };
    TangramObject[] ls_obj;


    Dictionary<string, string> map;
    int maxWidth = 3000;
    int maxHeight = 0;
    void Awake()
    {
        ls_obj = new TangramObject[7];

        red.HSVmin = new Scalar(0, 70, 40);
        red.HSVmax = new Scalar(2, 255, 255);
        red.lower_HSVMin = new Scalar(175, 45, 30);
        red.lower_HSVMax = new Scalar(180, 255, 255);
        red.setColor(new Scalar(180, 255, 255));

        //ngoai troi
        orange.HSVmin = new Scalar(10, 40, 50);
        orange.HSVmax = new Scalar(20, 255, 255);
        orange.setColor(new Scalar(20, 255, 255));

        //ngoai troi
        yellow.HSVmin = new Scalar(27, 40, 40);
        yellow.HSVmax = new Scalar(30, 255, 255);
        yellow.setColor(new Scalar(45, 160, 255));

        //ngoai troi
        green.HSVmin = new Scalar(55, 80, 40);
        green.HSVmax = new Scalar(65, 255, 255);
        green.setColor(new Scalar(75, 255, 255));

        lightBlue.HSVmin = new Scalar(75, 30, 30);
        lightBlue.HSVmax = new Scalar(100, 255, 255);
        lightBlue.setColor(new Scalar(95, 255, 255));

        blue.HSVmin = new Scalar(105, 40, 40);
        blue.HSVmax = new Scalar(120, 255, 255);
        blue.setColor(new Scalar(120, 255, 255));

        purple.HSVmin = new Scalar(153, 80, 20);
        purple.HSVmax = new Scalar(160, 255, 255);
        purple.setColor(new Scalar(170, 255, 255));

        ls_obj[0] = red;
        ls_obj[1] = orange;
        ls_obj[2] = yellow;
        ls_obj[3] = green;
        ls_obj[4] = lightBlue;
        ls_obj[5] = blue;
        ls_obj[6] = purple;

        //var tgrFolderPath = "C:/Users/phamleminh/Desktop/Re Shapes";
        var tgrFolderPath = "./tangram";
        var subjectTgrPath = Directory.GetDirectories(tgrFolderPath, "M*");
        int i = 0;
        foreach (var subjectPath in subjectTgrPath)
        {
            var ext = new List<string> { ".jpg", ".png" };
            var files = Directory.GetFiles(subjectPath, "*.*", SearchOption.AllDirectories)
                 .Where(s =>
                 {
                     bool b = ext.Contains(Path.GetExtension(s));
                     return b;
                 }).ToList();

            var parentDir = Directory.GetParent(subjectPath);
            var copyPath = parentDir + "/" + i + ".png";
            Debug.LogFormat("Directory is {0}", parentDir.FullName.ToString());

            var subjectName = new DirectoryInfo(subjectPath).Name;
            var jsonsubDir = "./tangramJson/" + subjectName;
            if (!Directory.Exists(jsonsubDir))
                Directory.CreateDirectory(jsonsubDir);
            else
            {
                Directory.Delete(jsonsubDir, true);
                Directory.CreateDirectory(jsonsubDir);
            }


            foreach (var filePath in files)
            {
                i++;
                Debug.LogFormat("File Path is {0}", filePath);
                var nameNoExt = Path.GetFileNameWithoutExtension(filePath);

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, copyPath, true);
                }
                try
                {
                    var json = getFeatureTangram(copyPath);
                    Debug.LogFormat("File Name : {0}, JSON is {1}", nameNoExt, json);
                    File.WriteAllText(jsonsubDir + "/" + nameNoExt + ".json", json);
                    File.Delete(copyPath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    Debug.Log("Stack trace is : " + e.StackTrace.ToString());
                }
            }
        }
    }

    void matToTexture(Mat mat, Texture2D tex)
    {
        Mat a = new Mat();
        var rat = (float)mat.width() / (float)mat.height();
        var newW = maxWidth;
        var newH = newW / rat;
        Imgproc.resize(mat, a, new Size(newW, newH));
        Utils.matToTexture2D(a, tex);
    }

    string getFeatureTangram(string path)
    {
        Mat rgbMat = Imgcodecs.imread(path);

        var width = rgbMat.width();
        var height = rgbMat.height();
        var ofsetx = 0;
        var ofsety = 0;
        if (width > 4096) ofsetx = (width - 4096) / 2;
        if (height > 4096) ofsety = (height - 4096) / 2;

        var rat = (float)rgbMat.width() / (float)rgbMat.height();
        Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_RGBA2BGR);
        Mat rgbMat2 = new Mat(rgbMat.size(), rgbMat.type());

        if (debug == true)
        {
            mainDebug.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug1.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug2.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug3.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug4.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug5.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug6.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug7.GetComponent<AspectRatioFitter>().aspectRatio = rat;
            debug8.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        }
        Mat hsvMat = new Mat();
        Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
        Debug.Log(rgbMat.width());

        if (debug == true)
        {
            maxHeight = (int)(maxWidth / rat);

            texture = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt1 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt2 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt3 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt4 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt5 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt6 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt7 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            dbTxt8 = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);

            mainDebug.texture = texture;
            debug1.texture = dbTxt1;
            debug2.texture = dbTxt2;
            debug3.texture = dbTxt3;
            debug4.texture = dbTxt4;
            debug5.texture = dbTxt5;
            debug6.texture = dbTxt6;
            debug7.texture = dbTxt7;
            debug8.texture = dbTxt8;
        }

        if (debug) {
            Mat a = new Mat();
            Imgproc.resize(rgbMat, a, new Size(maxWidth, maxHeight));
            Utils.matToTexture2D(a, dbTxt4);
        }


        Mat threshold = new Mat();
        Mat threshold2 = new Mat();

        List<MatOfPoint> contours = new List<MatOfPoint>();
        Mat hierarchy = new Mat();
        MatOfPoint2f mop2f = new MatOfPoint2f();


        TangramShape blackShape = new TangramShape();
        List<MyShape> ls_shapes = new List<MyShape>();
        blackShape.datas = ls_shapes;


        bool[] OK = new bool[7];

        for (var obj_i = 0; obj_i < 7; obj_i++)
        {
            var obj = ls_obj[obj_i];

            Core.inRange(hsvMat, obj.HSVmin, obj.HSVmax, threshold);
            if (obj_i == (int)tgr.RED)
            {
                Core.inRange(hsvMat, obj.lower_HSVMin, obj.lower_HSVMax, threshold2);
                threshold2.copyTo(threshold, threshold2);
            }

            if (obj_i == (int)tgr.YELLOW)
            {
                if(debug)  matToTexture(threshold, dbTxt3);
            }

            contours.Clear();

            Imgproc.findContours(threshold, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            for (int ct_i = 0; ct_i < contours.Count; ct_i++)
            {
                if (Imgproc.contourArea(contours[ct_i]) < MIN_OBJECT_AREA)
                {
                    contours.RemoveAt(ct_i);
                    ct_i--;
                }
            }


            Scalar c = hsv2rgb(obj.getColor());
            for (int ct_i = 0; ct_i < contours.Count; ct_i++)
            {
                var ct = contours[ct_i];
                var peri = Imgproc.arcLength(new MatOfPoint2f(ct.toArray()), true);

                Imgproc.approxPolyDP(new MatOfPoint2f(ct.toArray()), mop2f, 0.05 * peri, true);
                {

                    MyShape ms = new MyShape();

                    var points = mop2f.toArray();

                    var index = -1;
                    var max = -1d;
                    var numPoints = points.Length;
                    ms._id = obj_i;
                    ms.ps = new Point[numPoints];

                    if (numPoints == 3)
                    {
                        OK[obj_i] = true;

                        for (var p_i = 0; p_i < numPoints; p_i++)
                        {
                            //Debug.LogFormat("p1 = {0}, p2 = {1}", p_i % numPoints, (p_i + 1) % numPoints);
                            var p1 = points[p_i % numPoints];
                            var p2 = points[(p_i + 1) % numPoints];
                            var vt = p2 - p1;
                            float len = (float)(vt.x * vt.x + vt.y * vt.y);
                            if (len > max)
                            {
                                index = p_i;
                                max = len;
                            }
                        }
                        var i_nhon1 = index;
                        var i_nhon2 = (index + 1) % numPoints;
                        var i_vuong = (index + 2) % numPoints;

                        ms.ps[0] = points[i_vuong];
                        ms.ps[1] = points[i_nhon1];
                        ms.ps[2] = points[i_nhon2];

                        Imgproc.putText(rgbMat2, "1", points[i_nhon1], 1, 20, c, 10);
                        Imgproc.putText(rgbMat2, "2", points[i_nhon2], 1, 20, c, 10);
                        Imgproc.putText(rgbMat2, "0", points[i_vuong], 1, 20, c, 10);

                    }
                    else if (numPoints == 4)
                    {
                        if (obj_i == (int)tgr.YELLOW)
                        {
                            OK[obj_i] = true;
                            Debug.Log("Xin chao the mau vang");
                            ms.ps[0] = points[0];
                            ms.ps[1] = points[1];
                            ms.ps[2] = points[2];
                            ms.ps[3] = points[3];
                        }
                        else if (obj_i == (int)tgr.ORANGE)
                        {
                            OK[obj_i] = true;
                            Debug.Log("Xin chao the gioi");

                            var vt_cheo1 = points[0] - points[2];
                            var vt_cheo2 = points[1] - points[3];

                            var len_cheo1 = vt_cheo1.x * vt_cheo1.x + vt_cheo1.y * vt_cheo1.y;
                            var len_cheo2 = vt_cheo2.x * vt_cheo2.x + vt_cheo2.y * vt_cheo2.y;
                            var i_nhon = 0;
                            if (len_cheo2 > len_cheo1)
                            {
                                i_nhon = 1;
                            }
                            ms.ps[0] = points[i_nhon];
                            ms.ps[1] = points[(i_nhon + 1)];
                            ms.ps[2] = points[(i_nhon + 2)];
                            ms.ps[3] = points[(i_nhon + 3) % numPoints];

                            var i_prvNhon = (i_nhon + 4 - 1) % numPoints;
                            var i_aftNhon = i_nhon + 1;
                            var vt_prvNhon = points[i_prvNhon] - points[i_nhon];
                            var vt_aftNhon = points[i_aftNhon] - points[i_nhon];

                            //Imgproc.line(rgbMat2, points[i_prvNhon], points[i_nhon], c, 10);

                            var len_prvNhon = vt_prvNhon.x * vt_prvNhon.x + vt_prvNhon.y * vt_prvNhon.y;
                            var len_aftNhon = vt_aftNhon.x * vt_aftNhon.x + vt_aftNhon.y * vt_aftNhon.y;
                            if (len_prvNhon > len_aftNhon)
                            {
                                ms.isFlip = true;
                                Imgproc.putText(rgbMat2, " IsFLIP", ms.ps[3], 1, 20, c, 10);
                            }
                            else
                            {

                                ms.isFlip = false;
                                Imgproc.putText(rgbMat2, " IsNOTFLIP", ms.ps[3], 1, 20, c, 10);
                            }



                            Debug.Log(ms.ps.Length);
                            Debug.Log((i_nhon + 3) % numPoints);

                            if (debug == true)
                            {
                                Imgproc.putText(rgbMat2, "0", ms.ps[0], 1, 20, c, 10);
                                Imgproc.putText(rgbMat2, "1", ms.ps[1], 1, 20, c, 10);
                                Imgproc.putText(rgbMat2, "2", ms.ps[2], 1, 20, c, 10);
                                Imgproc.putText(rgbMat2, "3", ms.ps[3], 1, 20, c, 10);
                            }

                        }

                       
                    }

                    ls_shapes.Add(ms);
                }
            }
        }

        for(var ok_i = 0;ok_i<7;ok_i++)
        {
            if (OK[ok_i] == false) Debug.LogError("Sai mau: " + ok_i);
        }


        if (debug)
        {
            Imgproc.circle(rgbMat2, new Point(1851, 3172), 20, yellow.getColor(), 10);
            Imgproc.circle(rgbMat2, new Point(1245, 2565), 20, yellow.getColor(), 10);
            Imgproc.circle(rgbMat2, new Point(883, 2925), 20, red.getColor(), 10);
            Imgproc.circle(rgbMat2, new Point(2100, 1709), 20, red.getColor(), 10);

            Mat a = new Mat();
            Imgproc.resize(rgbMat, a, new Size(maxWidth, maxHeight));
            Utils.matToTexture2D(a, texture);
            Imgproc.resize(hsvMat, a, new Size(maxWidth, maxHeight));            
            Utils.matToTexture2D(a, dbTxt1);
            Imgproc.resize(rgbMat2, a, new Size(maxWidth, maxHeight));
            Utils.matToTexture2D(a, dbTxt2);
        }

        for (int i = 0; i < blackShape.datas.Count; i++)
        {
            for (int j = 0; j < blackShape.datas[i].ps.Length; j++)
            {
                blackShape.datas[i].ps[j].x -= ofsetx;
                blackShape.datas[i].ps[j].y -= ofsety;
            }
        }

        var json = JsonUtility.ToJson(blackShape);
        return json;
    }

    Scalar hsv2rgb(Scalar colorHSV)
    {
        Mat rgb = new Mat();
        Mat hsv = new Mat(1, 1, CvType.CV_8UC3, colorHSV);
        Imgproc.cvtColor(hsv, rgb, Imgproc.COLOR_HSV2RGB);
        var colorRGB = rgb.get(0, 0);
        Scalar c = new Scalar(colorRGB[0], colorRGB[1], colorRGB[2]);
        return c;
    }
}

