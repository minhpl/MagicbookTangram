using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx;
using System.Threading;
using OpenCVForUnityExample;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

/// <summary>
/// Multi Object Tracking Based on Color Example
/// Referring to https://www.youtube.com/watch?v=hQ-bpfdWQh8.
/// </summary>
[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class TagramDetect : MonoBehaviour
{
    public static string TS_DATA = "{\"datas\":[{\"_id\":0,\"ps\":[{\"x\":1012.0,\"y\":1974.0},{\"x\":1012.0,\"y\":3183.0},{\"x\":2222.0,\"y\":1973.0}],\"isFlip\":false},{\"_id\":1,\"ps\":[{\"x\":1010.0,\"y\":1973.0},{\"x\":402.0,\"y\":2582.0},{\"x\":403.0,\"y\":3194.0},{\"x\":1012.0,\"y\":2583.0}],\"isFlip\":false},{\"_id\":2,\"ps\":[{\"x\":2709.0,\"y\":1495.0},{\"x\":2709.0,\"y\":2098.0},{\"x\":3321.0,\"y\":2097.0},{\"x\":3320.0,\"y\":1494.0}],\"isFlip\":false},{\"_id\":3,\"ps\":[{\"x\":1844.0,\"y\":2352.0},{\"x\":1236.0,\"y\":2961.0},{\"x\":2453.0,\"y\":2960.0}],\"isFlip\":false},{\"_id\":4,\"ps\":[{\"x\":3693.0,\"y\":1493.0},{\"x\":3693.0,\"y\":886.0},{\"x\":3086.0,\"y\":1493.0}],\"isFlip\":false},{\"_id\":5,\"ps\":[{\"x\":1849.0,\"y\":2351.0},{\"x\":2708.0,\"y\":3211.0},{\"x\":2703.0,\"y\":1495.0}],\"isFlip\":false},{\"_id\":6,\"ps\":[{\"x\":2321.0,\"y\":1494.0},{\"x\":2927.0,\"y\":1493.0},{\"x\":2320.0,\"y\":887.0}],\"isFlip\":false}]}";
    private bool debug = true;

    private Mutex mut;
    public RawImage rawdebugDL;
    public RawImage rawdebugDL2;
    public RawImage rawdebugDL3;
    public RawImage rawdebugDL4;
    public RawImage rawdebugDL5;
    public RawImage rawdebugDL6;
    public RawImage rawdebugDL7;
    public RawImage rawdebugDL8;
    public RawImage rawmainDL;
    public UnityEngine.UI.Text uitext;
    public Toggle t_red, t_orange, t_yellow, t_green, t_lightblue, t_blue, t_purple;
    public Toggle[] t_tangram;
    public InputField toggleColor;

    public WarpPerspective warp;
    private int thickness = 1;
    private float cnyThres = 1;
    private int nH_goc, nW_goc;
    private int nW, nH;

    Texture2D texture;
    Texture2D dbText1;
    Texture2D dbText2;
    Texture2D dbText3;
    Texture2D dbText4;
    Texture2D dbText5;
    Texture2D dbText6;
    Texture2D dbText7;
    Texture2D dbText8;
    const int MAX_NUM_OBJECTS = 50;
    const int MIN_OBJECT_AREA = 150;
    int[] MIN_OBJECT_AREAS = { 450, 300, 300, 300, 150, 450, 150 };
    int[] MAX_OBJECT_AREAS = { 10000, 7000, 7000, 7000, 3500, 10000, 3500 };

    Mat gray;
    Mat canny;
    Mat rgb;
    List<MatOfPoint> all_cts;
    Mat hierarchy;
    MatOfPoint2f approx_ct;
    Point cterTgr;
    Mat erodeElement;
    Mat dilateElement;
    Mat closeElement;
    Mat rgbaMat;

    Mat rgbMat;
    Mat rgbMat2;
    Mat rgbMat3;
    Mat rgbMat4;

    Mat rgbMat2copy;

    Mat hsvMat;
    Mat hsvMat2;
    Mat hsvMat3;
    Mat hsvMat4;

    Mat thresholdMat;
    Mat thresholdMat2;

    Mat all_thresh;
    Mat all_thresh_afct;
    Mat dbMat;
    Mat all_thresh_af;

    Mat kernel;
    List<Mat> channels;
    List<Mat> color_filter;

    TangramObject red = new TangramObject("red");
    TangramObject orange = new TangramObject("orange");
    TangramObject yellow = new TangramObject("yellow");
    TangramObject green = new TangramObject("green");
    TangramObject lightBlue = new TangramObject("lightBlue");
    TangramObject blue = new TangramObject("blue");
    TangramObject purple = new TangramObject("purple");
    enum tgr { ORANGE = 1, RED = 0, YELLOW = 2, GREEN = 3, BLUE = 5, LIGHTBLUE = 4, PURPLE = 6 };
    Scalar[] colorRGB;

    TangramObject[] ls_obj;
    Mat[] thresholdMatArr;

    public TangramFeatureModelList tangramFeatureModelList;

    bool[] toggle_db = { true, true, true, true, true, true, true };
    private void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        mut = new Mutex();
        if (debug == true)
        {
            t_tangram = new Toggle[7];
            if (t_red != null && debug) t_tangram[(int)tgr.RED] = t_red;
            if (t_orange != null && debug) t_tangram[(int)tgr.ORANGE] = t_orange;
            if (t_yellow != null && debug) t_tangram[(int)tgr.YELLOW] = t_yellow;
            if (t_green != null && debug) t_tangram[(int)tgr.GREEN] = t_green;
            if (t_lightblue != null && debug) t_tangram[(int)tgr.LIGHTBLUE] = t_lightblue;
            if (t_blue != null && debug) t_tangram[(int)tgr.BLUE] = t_blue;
            if (t_purple != null && debug) t_tangram[(int)tgr.PURPLE] = t_purple;

            for (int i = 0; i < 7; i++)
            {
                var toggle = t_tangram[i];
                var index = i;
                if(toggle!=null)
                {
                    toggle.onValueChanged.AddListener((b) =>
                    {
                        toggle_db[index] = b;
                    });
                }                
            }
        }
        

        //if (toggleColor != null && debug == true)
        //{
        //    toggleColor.onValueChanged.AddListener((string s2) =>
        //    {
        //        var text = s2.Trim();
        //        //if (string.IsNullOrEmpty(text)) { text = "1111111"; toggleColor.text = text; }
        //        var leng = text.Length > 7 ? 7 : text.Length;
        //        for (int i = 0; i < leng; i++)
        //        {
        //            var b = int.Parse(text[i].ToString());
        //            if (b == 1) toggle_db[i] = true;
        //            else toggle_db[i] = false;
        //        }
        //        for (int i = leng; i < 7; i++)
        //        {
        //            toggle_db[i] = false;
        //        }
        //    });

        //    if (string.IsNullOrEmpty(toggleColor.text)) { toggleColor.text = "1111111"; }
        //}

        //init Tangram Feature Detect
        TangramShape msl = JsonUtility.FromJson<TangramShape>(TS_DATA);
        tangramFeatureModelList = new TangramFeatureModelList();
        tangramFeatureModelList.ProcessInput(msl.datas.ToArray(), true);


        colorRGB = new Scalar[7];

        ls_obj = new TangramObject[7];
        red.setHSVmin(new Scalar(170, 30, 45));
        red.setHSVmax(new Scalar(180, 255, 255));
        red.setColor(new Scalar(175, 255, 255));
        red.ColorRGB = hsv2rgb(red.getColor());

        //ngoai troi
        orange.setHSVmin(new Scalar(6, 75, 45));
        orange.setHSVmax(new Scalar(18, 255, 255));
        orange.setColor(new Scalar(13, 255, 255));
        orange.ColorRGB = hsv2rgb(orange.getColor());

        //ngoai troi
        yellow.setHSVmin(new Scalar(23, 75, 100));
        yellow.setHSVmax(new Scalar(34, 255, 255));
        yellow.setColor(new Scalar(30, 160, 255));
        yellow.ColorRGB = hsv2rgb(yellow.getColor());
        //ngoai troi
        green.setHSVmin(new Scalar(35, 30, 40));
        green.setHSVmax(new Scalar(72 , 255, 255));
        green.setColor(new Scalar(60, 255, 255));
        green.ColorRGB = hsv2rgb(green.getColor());

        //may ios nhỏ, lúc đầu camera chưa sáng trong 30s đầu
        lightBlue.setHSVmin(new Scalar(70, 30, 45));
        lightBlue.setHSVmax(new Scalar(108, 255, 255));
        lightBlue.setColor(new Scalar(90, 255, 255));
        lightBlue.ColorRGB = hsv2rgb(lightBlue.getColor());

        //xem lai can nsua la tru di mau xanh
        blue.setHSVmin(new Scalar(108, 30, 45));
        blue.setHSVmax(new Scalar(135, 255, 255));
        blue.setColor(new Scalar(115, 255, 255));
        blue.ColorRGB = hsv2rgb(blue.getColor());

        purple.setHSVmin(new Scalar(136, 30, 40));
        purple.setHSVmax(new Scalar(172, 255, 255));
        purple.setColor(new Scalar(150, 255, 255));
        purple.ColorRGB = hsv2rgb(purple.getColor());

        ls_obj[(int)tgr.ORANGE] = orange;
        ls_obj[(int)tgr.RED] = red;
        ls_obj[(int)tgr.YELLOW] = yellow;
        ls_obj[(int)tgr.GREEN] = green;
        ls_obj[(int)tgr.BLUE] = blue;
        ls_obj[(int)tgr.LIGHTBLUE] = lightBlue;
        ls_obj[(int)tgr.PURPLE] = purple;

        gray = new Mat();
        canny = new Mat();
        rgb = new Mat();
        all_cts = new List<MatOfPoint>();
        hierarchy = new Mat();
        approx_ct = new MatOfPoint2f();
        cterTgr = new Point();


        for (int i = 0; i < ls_obj.Length; i++)
        {
            var obj = ls_obj[i];
            Mat hsv = new Mat(1, 1, CvType.CV_8UC3, obj.getHSVmax());
            Imgproc.cvtColor(hsv, rgb, Imgproc.COLOR_HSV2RGB);
            var colorRGB = rgb.get(0, 0);
            Scalar c = new Scalar(colorRGB[0], colorRGB[1], colorRGB[2]);
            obj.setColor(c);
            hsv.Dispose();
        }
        //create structuring element that will be used to "dilate" and "erode" image.
        //the element chosen here is a 3px by 3px rectangle
        erodeElement = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));
        //dilate with larger element so make sure object is nicely visible
        dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));
        closeElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(8, 8));


        kernel = new Mat(3, 3, CvType.CV_32F);
        kernel.put(0, 0, -1);
        kernel.put(0, 1, -1);
        kernel.put(0, 2, -1);
        kernel.put(1, 0, -1);
        kernel.put(1, 1, 9);
        kernel.put(1, 2, -1);
        kernel.put(2, 0, -1);
        kernel.put(2, 1, -1);
        kernel.put(2, 2, -1);
        channels = new List<Mat>();
        color_filter = new List<Mat>();
        Utilities.Log("NUMBER of system Threads : " + SystemInfo.processorCount);
    }

    private void setColor(Scalar scalar)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// The webcam texture to mat helper.
    /// </summary>
    WebCamTextureToMatHelper webCamTextureToMatHelper;

    // Use this for initialization
    void Start()
    {
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

        webCamTextureToMatHelper.onInitialized.AddListener(() =>
        {
            OnWebCamTextureToMatHelperInitialized();

        });
        webCamTextureToMatHelper.onErrorOccurred.AddListener((WebCamTextureToMatHelper.ErrorCode errorCode) =>
        {
            OnWebCamTextureToMatHelperErrorOccurred(errorCode);
        });
        webCamTextureToMatHelper.onDisposed.AddListener(() =>
        {
            OnWebCamTextureToMatHelperDisposed();
        });

        webCamTextureToMatHelper.Initialize();
    }

    /// <summary>
    /// Raises the webcam texture to mat helper initialized event.
    /// </summary>
    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("OnWebCamTextureToMatHelperInitialized");
        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();
        gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
        Utilities.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
        Utilities.Log("Webcam width " + webCamTextureMat.cols() + ".Webcam height " + webCamTextureMat.rows());
        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        var rat = (float)webCamTextureMat.cols() / (float)webCamTextureMat.rows();
        nW_goc = webCamTextureMat.cols();
        nW_goc = 200;
        nH_goc = (int)((float)nW_goc / (float)rat);

        nW = nW_goc;
        nH = (int)(nH_goc * 0.815f);
        rat = (float)nW / (float)nH;

        if (rawmainDL != null && debug == true) rawmainDL.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL != null && debug == true) rawdebugDL.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL2 != null && debug == true) rawdebugDL2.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL3 != null && debug == true) rawdebugDL3.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL4 != null && debug == true) rawdebugDL4.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL5 != null && debug == true) rawdebugDL5.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL6 != null && debug == true) rawdebugDL6.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL7 != null && debug == true) rawdebugDL7.GetComponent<AspectRatioFitter>().aspectRatio = rat;
        if (rawdebugDL8 != null && debug == true) rawdebugDL8.GetComponent<AspectRatioFitter>().aspectRatio = rat;

       
        if (rawmainDL != null && debug == true) texture = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL != null && debug == true) dbText1 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL2 != null && debug == true) dbText2 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL3 != null && debug == true) dbText3 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL4 != null && debug == true) dbText4 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL5 != null && debug == true) dbText5 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL6 != null && debug == true) dbText6 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL7 != null && debug == true) dbText7 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        if (rawdebugDL8 != null && debug == true) dbText8 = new Texture2D(nW, nH, TextureFormat.RGBA32, false);
        rgbMat = new Mat(nW, nH, CvType.CV_8UC3);
        if (rawmainDL != null && debug == true) rawmainDL.texture = texture;
        if (rawdebugDL != null && debug == true) rawdebugDL.texture = dbText1;
        if (rawdebugDL2 != null && debug == true) rawdebugDL2.texture = dbText2;
        if (rawdebugDL3 != null && debug == true) rawdebugDL3.texture = dbText3;
        if (rawdebugDL4 != null && debug == true) rawdebugDL4.texture = dbText4;
        if (rawdebugDL5 != null && debug == true) rawdebugDL5.texture = dbText5;
        if (rawdebugDL6 != null && debug == true) rawdebugDL6.texture = dbText6;
        if (rawdebugDL7 != null && debug == true) rawdebugDL7.texture = dbText7;
        if (rawdebugDL8 != null && debug == true) rawdebugDL8.texture = dbText8;

        thresholdMat = new Mat();
        thresholdMat2 = new Mat();
        hsvMat = new Mat();
        hsvMat2 = new Mat();
        hsvMat3 = new Mat();
        hsvMat4 = new Mat();
       
        rgbaMat = new Mat(1, 1, CvType.CV_8UC3);
        rgbMat2 = new Mat(1, 1, CvType.CV_8UC3);
        rgbMat3 = new Mat(1, 1, CvType.CV_8UC3);
        rgbMat4 = new Mat(1, 1, CvType.CV_8UC3);
        rgbMat2copy = new Mat(1, 1, CvType.CV_8UC3);

        thresholdMatArr = new Mat[7];

        Observable.FromMicroCoroutine(worker).Subscribe();
    }

    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

        if (rgbMat != null)
            rgbMat.Dispose();
        if (thresholdMat != null)
            thresholdMat.Dispose();
        if (hsvMat != null)
            hsvMat.Dispose();
    }

    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }

    public delegate void Handler(TangramResultModel trm, List<MyShape> lms, TangramFeatureModelList tfml);
    public Handler handler = null;
    public void RegisterHandler(Handler _handler)
    {
        Debug.Log("Register handle");
        this.handler = _handler;
    }

    IEnumerator worker()
    {
        bool inProcess = false;
        while (true)
        {
            yield return null;
            if (inProcess) yield return null;
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                color_filter.Clear();

                Mat t_rgbaMat = webCamTextureToMatHelper.GetMat();


                tagramDetect(t_rgbaMat, (TangramResultModel trm, List<MyShape> lms) =>
                 {
                     if (trm != null)
                     {
                         if (this.handler != null) handler(trm, lms, tangramFeatureModelList);
                         
                         if (debug == true)
                         {
                             string s = "";
                             for (var i = 0; i < trm.datas.Length; i++)
                             {
                                 s += trm.datas[i] + " ";
                             }
                             if (uitext != null && debug == true) uitext.text = s;
                         }
                     }

                 });
            }
        }
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
    //public delegate void Process(int[] tgrdeteced);
    void tagramDetect(Mat t_rgbaMat, Action<TangramResultModel, List<MyShape>> prc)
    {
        
        List<MyShape> lms = new List<MyShape>();
        System.Diagnostics.Stopwatch watch = null;
        
        long elapsedMs;
        TangramResultModel trm = null;
        Observable.Start(() =>
        {
            mut.WaitOne();
            Imgproc.resize(t_rgbaMat, rgbaMat, new Size(nW_goc, nH_goc));
            watch = System.Diagnostics.Stopwatch.StartNew();

            if (warp != null)
            {
                warp.Init(rgbaMat);
                Mat wMat = warp.warpPerspective(rgbaMat);
                rgbaMat = wMat.submat(0, nH, 0, nW);
            }
            else
            {
                rgbaMat = rgbaMat.submat(0, nH, 0, nW);
            }

            all_thresh = Mat.zeros(nH, nW, CvType.CV_8UC3);
            all_thresh_afct = Mat.zeros(nH, nW, CvType.CV_8UC3);
            dbMat = Mat.zeros(nH, nW, CvType.CV_8UC3);
            all_thresh_af = Mat.zeros(nH, nW, CvType.CV_8UC3);

            rgbaMat.copyTo(rgbMat);
            rgbMat.convertTo(rgbMat2, CvType.CV_8UC3, 0.8, 60);
            rgbMat2.copyTo(rgbMat2copy);
            rgbMat.convertTo(rgbMat3, CvType.CV_8UC3, 1, 60);
            rgbMat.convertTo(rgbMat4, CvType.CV_8UC3, 1.25, 35);
            rgbMat.convertTo(rgbMat, CvType.CV_8UC3, 1.25, 35);


            Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
            Imgproc.cvtColor(rgbMat2, hsvMat2, Imgproc.COLOR_RGB2HSV);
            Imgproc.cvtColor(rgbMat3, hsvMat3, Imgproc.COLOR_RGB2HSV);
            Imgproc.cvtColor(rgbMat3, hsvMat4, Imgproc.COLOR_RGB2HSV);

            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;

            Mat markers = Mat.zeros(rgbaMat.size(), CvType.CV_32SC1);

            watch = System.Diagnostics.Stopwatch.StartNew();

            for (int obj_i = 0; obj_i < ls_obj.Length; obj_i++)
            {
                var obj = ls_obj[obj_i];

                if (obj_i == (int)tgr.ORANGE | obj_i == (int)tgr.YELLOW | obj_i == (int)tgr.GREEN)
                {
                    Core.inRange(hsvMat2, obj.getHSVmin(), obj.getHSVmax(), thresholdMat);
                }
                else if (obj_i == (int)tgr.LIGHTBLUE)
                {
                    Core.inRange(hsvMat, obj.getHSVmin(), obj.getHSVmax(), thresholdMat);
                }
                else
                {
                    Core.inRange(hsvMat, obj.getHSVmin(), obj.getHSVmax(), thresholdMat);
                }


                if (obj_i == (int)tgr.RED)
                {
                    Core.inRange(hsvMat, new Scalar(0, 20, 45), new Scalar(5, 255, 255), thresholdMat2);
                    thresholdMat2.copyTo(thresholdMat, thresholdMat2);
                }


                thresholdMatArr[obj_i] = thresholdMat.clone();
            }

            //thresholdMatArr[(int)tgr.LIGHTBLUE].setTo(new Scalar(0), thresholdMatArr[(int)tgr.BLUE]);
            //thresholdMatArr[(int)tgr.LIGHTBLUE].setTo(new Scalar(0), thresholdMatArr[(int)tgr.GREEN]);


            for (int obj_i = 0; obj_i < ls_obj.Length; obj_i++)
            {
                var obj = ls_obj[obj_i];

                all_cts.Clear();
                thresholdMat = thresholdMatArr[obj_i];
                if (toggle_db[obj_i] == true ) all_thresh.setTo(obj.ColorRGB, thresholdMat);

                if (true | obj_i == (int)tgr.PURPLE | obj_i == (int)tgr.YELLOW | obj_i == (int)tgr.RED | obj_i == (int)tgr.GREEN | obj_i == (int)tgr.ORANGE)
                {
                    Imgproc.erode(thresholdMat, thresholdMat2, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(5, 5)), new Point(-1, -1), 1);                                
                }
                if(obj_i == (int)tgr.LIGHTBLUE | obj_i == (int)tgr.PURPLE)
                {
                    Imgproc.erode(thresholdMat, thresholdMat2, Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(5, 5)), new Point(-1, -1), 1);
                }

                if (toggle_db[obj_i] == true) all_thresh_af.setTo(obj.ColorRGB, thresholdMat2);
                all_thresh_afct.setTo(new Scalar(obj_i + 1), thresholdMat2);

                color_filter.Add(thresholdMat2.clone());

                Imgproc.findContours(thresholdMat2, all_cts, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
                Scalar c = obj.getColor();

                for (int ct_i = 0; ct_i < all_cts.Count; ct_i++)
                {
                    double area = Imgproc.contourArea(all_cts[ct_i]);
                    // if (area < MIN_OBJECT_AREA)
                    if (area < MIN_OBJECT_AREAS[obj_i] * 0.55)
                    {
                        all_cts.RemoveAt(ct_i);
                        ct_i--;
                    }
                    if (area > MAX_OBJECT_AREAS[obj_i] * 1.3)
                    {
                        all_cts.RemoveAt(ct_i);
                        ct_i--;
                    }
                }

                MyShape chon = null;
                MyShape ms = new MyShape();
                float dt = 1000000;

                for (int ct_i = 0; ct_i < all_cts.Count; ct_i++)
                {
                    var ct = all_cts[ct_i];
                    var peri = Imgproc.arcLength(new MatOfPoint2f(ct.toArray()), true);
                    var epsilon = 0.1 * peri;
                    if (obj_i == (int)tgr.ORANGE || obj_i == (int)tgr.YELLOW)
                    {
                        epsilon = 0.065 * peri;
                    }
                    Imgproc.approxPolyDP(new MatOfPoint2f(ct.toArray()), approx_ct, epsilon, true);

                    MatOfInt pts_cvh = new MatOfInt();
                    Imgproc.convexHull(ct, pts_cvh, true);
                    var cvh_numPts = pts_cvh.toArray().Length;
                    Point[] cvh_pts = new Point[cvh_numPts];
                    var ct_pts = ct.toArray();



                    for (int i = 0; i < cvh_numPts; i++)
                    {
                        var i1 = pts_cvh.toArray()[i];
                        var p1 = ct_pts[i1];
                        cvh_pts[i] = p1;

                        try
                        {
                            if (debug == true)
                            {
                                var i2 = pts_cvh.toArray()[(i + 1) % cvh_numPts];
                                var p2 = ct_pts[i2];
                                Imgproc.circle(rgbMat2, p1, 1, c, 2);
                            }
                        }
                        catch (Exception e)
                        {
                            Utilities.LogFormat("Here3:{0},{1},{2}", rgbMat2 == null, p1 == null, c == null);
                            Utilities.Log("Exception is {0}", e.ToString());
                            Utilities.Log("Trace is {0}", e.StackTrace.ToString());
                        }
                    }


                    MatOfPoint2f approx_cvh = new MatOfPoint2f();

                    var epsilon2 = peri * 0.1;
                    if (obj_i == (int)tgr.ORANGE)
                        epsilon2 = peri * 0.065;
                    Imgproc.approxPolyDP(new MatOfPoint2f(cvh_pts), approx_cvh, epsilon2, true);

                    var ct_ori = new MatOfPoint(ct.toArray());
                    MatOfPoint approx_ct2 = new MatOfPoint(approx_ct.toArray());

                    List<MatOfPoint> approx_cvh2 = new List<MatOfPoint>();
                    approx_cvh2.Add(new MatOfPoint(approx_cvh.toArray()));

                    var mu = Imgproc.moments(approx_cvh2[0], true);
                    cterTgr.x = mu.m10 / mu.m00;
                    cterTgr.y = mu.m01 / mu.m00;

                    if (approx_ct2.size().height == 3 | approx_ct2.size().height == 4)
                    {

                        var points = approx_cvh2[0].toArray();
                        var numpoints = points.Length;

                        ms._id = obj_i;
                        ms.ps = new Point[numpoints];


                        double rat = 1.16;
                        if (obj_i == (int)tgr.PURPLE ) rat = 1.20;
                        else if (obj_i == (int)tgr.LIGHTBLUE) rat = 1.20;
                        else if (obj_i == (int)tgr.RED | obj_i == (int)tgr.BLUE) rat = 1.09;
                        else if (obj_i == (int)tgr.YELLOW) rat = 1.10;
                        else if (obj_i == (int)tgr.ORANGE) rat = 1.10;
                        else if (obj_i == (int)tgr.GREEN) rat = 1.10;

                        var ind_huyen = 0;
                        var max = -1d;

                        if (numpoints == 3 || numpoints == 4)
                        {
                            for (int p_i = 0; p_i < numpoints; p_i++)
                            {
                                var p = points[p_i];
                                var p2 = points[(p_i + 1) % numpoints];

                                var vect = p - cterTgr;

                                vect = vect * rat;

                                var p_new = cterTgr + vect;
                                points[p_i].x = (int)(p_new.x * 100) / 100f;
                                points[p_i].y = (int)(p_new.y * 100) / 100f;


                                if (numpoints == 4) ms.ps[p_i] = p_new;

                                if (numpoints == 3)
                                {
                                    var vt = p2 - p;
                                    var length = vt.x * vt.x + vt.y * vt.y;
                                    if (length > max)
                                    {
                                        ind_huyen = p_i;
                                        max = length;
                                    }
                                }
                            }
                        }

                        if (numpoints == 3)
                        {
                            var i_nhon1 = ind_huyen;
                            var i_nhon2 = (ind_huyen + 1) % numpoints;
                            var i_vuong = (ind_huyen + 2) % numpoints;

                            ms.ps[0] = points[i_vuong];
                            ms.ps[1] = points[i_nhon1];
                            ms.ps[2] = points[i_nhon2];

                        }
                        else if (numpoints == 4)
                        {
                            if (obj_i == (int)tgr.ORANGE)
                            {
                                var vt_cheo1 = ms.ps[0] - ms.ps[2];
                                var vt_cheo2 = ms.ps[1] - ms.ps[3];
                                var leng_cheo1 = vt_cheo1.x * vt_cheo1.x + vt_cheo1.y * vt_cheo1.y;
                                var leng_cheo2 = vt_cheo2.x * vt_cheo2.x + vt_cheo2.y * vt_cheo2.y;
                                var i_nhon = 0;
                                if (leng_cheo2 > leng_cheo1)
                                {
                                    i_nhon = 1;
                                }

                                ms.ps[0] = points[i_nhon];
                                ms.ps[1] = points[(i_nhon + 1)];
                                ms.ps[2] = points[(i_nhon + 2)];
                                ms.ps[3] = points[(i_nhon + 3) % numpoints];

                                var i_prvNhon = (i_nhon + 4 - 1) % numpoints;
                                var i_aftNhon = i_nhon + 1;
                                var vt_prvNhon = points[i_prvNhon] - points[i_nhon];
                                var vt_aftNhon = points[i_aftNhon] - points[i_nhon];
                                var len_prvNhon = vt_prvNhon.x * vt_prvNhon.x + vt_prvNhon.y * vt_prvNhon.y;
                                var len_aftNhon = vt_aftNhon.x * vt_aftNhon.x + vt_aftNhon.y * vt_aftNhon.y;

                                Imgproc.line(dbMat, points[i_prvNhon], points[i_nhon], c, 1);

                                if (len_prvNhon > len_aftNhon)
                                {
                                    ms.isFlip = true;
                                    Imgproc.putText(dbMat, " IsFLIP", ms.ps[3], 1, 1, c, 1);
                                }
                                else
                                {
                                    ms.isFlip = false;
                                    Imgproc.putText(dbMat, " IsNOTFLIP", ms.ps[3], 1, 1, c, 1);
                                }
                            }
                        }

                        var centerMat = new Point(rgbMat.width() / 2f, rgbMat.height() / 2f);
                        var vtLech = centerMat - cterTgr;
                        var dt2 = vtLech.x * vtLech.x + vtLech.y * vtLech.y;
                        if (dt2 < dt)
                        {
                            chon = ms;
                        }
                    }
                    try
                    {

                        Imgproc.circle(rgbMat, cterTgr, 1, c, 1);
                        Imgproc.putText(rgbMat, mu.m00.ToString(), cterTgr, 1, 1, c, 1);
                    }
                    catch (Exception e)
                    {
                        Utilities.LogFormat("Here2:{0},{1},{2}", rgbMat == null, cterTgr == null, c == null);
                        Utilities.Log("Exception is {0}", e.ToString());
                        Utilities.Log("Trace is {0}", e.StackTrace.ToString());
                    }

                    //if (approx_ct2.size().height == 3 | approx_ct2.size().height == 4) break;
                }

                if (chon != null)
                {
                    lms.Add(chon);

                    var ps = chon.ps;
                    for (int i = 0; i < ps.Length; i++)
                    {
                        var p1 = ps[i];
                        var p2 = ps[(i + 1) % ps.Length];

                        try
                        {
                            Imgproc.line(rgbMat2, p1, p2, c, 1);
                            Imgproc.line(all_thresh_afct, p1, p2, new Scalar(255, 255, 255), 1);
                            Imgproc.line(dbMat, p1, p2, c, 1);
                            Imgproc.circle(dbMat, p1, 1, c);
                        }
                        catch (Exception e)
                        {
                            Utilities.LogFormat("Here1:{0},{1},{2}", rgbMat2 == null, p1 == null, p2 == null);
                            Utilities.Log("Exception is {0}", e.ToString());
                            Utilities.Log("Trace is {0}", e.StackTrace.ToString());
                        }
                    }
                }

                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;
            }

            TangramShape msl = new TangramShape();
            msl.datas = lms;
            var json = JsonUtility.ToJson(msl);

            watch = System.Diagnostics.Stopwatch.StartNew();
            trm = tangramFeatureModelList.Detect(msl.datas.ToArray());
            watch.Stop();
            elapsedMs = watch.ElapsedMilliseconds;

            mut.ReleaseMutex();
        }).ObserveOnMainThread().Subscribe((rx) =>
        {
            prc(trm, lms);
            if (debug == true)
            {
                mut.WaitOne();

                if (texture != null && debug == true) Utils.matToTexture2D(dbMat, texture);
                if (dbText1 != null && debug == true) Utils.matToTexture2D(rgbMat2copy, dbText1);
                if (dbText2 != null && debug == true) Utils.matToTexture2D(rgbMat3, dbText2);
                if (dbText3 != null && debug == true) Utils.matToTexture2D(rgbMat4, dbText3);
                if (dbText4 != null && debug == true) Utils.matToTexture2D(rgbMat, dbText4);

                all_thresh_afct = all_thresh_afct * 25;
                Imgproc.cvtColor(rgbMat2, rgbMat2, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(all_thresh, all_thresh, Imgproc.COLOR_RGBA2RGB);
                Mat a = new Mat(all_thresh.size(), CvType.CV_8UC3);
                Core.addWeighted(all_thresh, 0.2, rgbMat2, 0.8, 0, a);
                if (dbText5 != null && debug == true) Utils.matToTexture2D(a, dbText5);
                if (dbText6 != null && debug == true) Utils.matToTexture2D(all_thresh, dbText6);
                if (dbText7 != null && debug == true) Utils.matToTexture2D(all_thresh_afct, dbText7);
                if (dbText8 != null && debug == true) Utils.matToTexture2D(all_thresh_af, dbText8);
                mut.ReleaseMutex();
            }
        });
        
    }
    void OnDestroy()
    {
        webCamTextureToMatHelper.Dispose();
    }

    public void OnBackButtonClick()
    {
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
        SceneManager.LoadScene("OpenCVForUnityExample");
#else
            Application.LoadLevel ("OpenCVForUnityExample");
#endif
    }

    public void OnPlayButtonClick()
    {
        webCamTextureToMatHelper.Play();
    }

    public void OnPauseButtonClick()
    {
        webCamTextureToMatHelper.Pause();
    }

    public void OnStopButtonClick()
    {
        webCamTextureToMatHelper.Stop();
    }

    /// <summary>
    /// Raises the change camera button click event.
    /// </summary>
    public void OnChangeCameraButtonClick()
    {
        webCamTextureToMatHelper.Initialize(null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
    }

    private void drawObject(List<TangramObject> theColorObjects, Mat frame, Mat temp, List<MatOfPoint> contours, Mat hierarchy)
    {
        for (int i = 0; i < theColorObjects.Count; i++)
        {
            var colorHSV = theColorObjects[i].getColor();
            Mat rgb = new Mat();
            Mat hsv = new Mat(1, 1, CvType.CV_8UC3, colorHSV);
            Imgproc.cvtColor(hsv, rgb, Imgproc.COLOR_HSV2RGB);
            var colorRGB = rgb.get(0, 0);
            Scalar c = new Scalar(colorRGB[0], colorRGB[1], colorRGB[2]);

            Imgproc.drawContours(frame, contours, i, c, 3, 8, hierarchy, int.MaxValue, new Point());
            Imgproc.circle(frame, new Point(theColorObjects[i].getXPos(), theColorObjects[i].getYPos()), 5, c);
            Imgproc.putText(frame, theColorObjects[i].getXPos() + " , " + theColorObjects[i].getYPos()
                , new Point(theColorObjects[i].getXPos(), theColorObjects[i].getYPos() + 20), 1, 1, c, 2);
            Imgproc.putText(frame, theColorObjects[i].getType(),
                new Point(theColorObjects[i].getXPos(), theColorObjects[i].getYPos() - 20), 1, 2, c, 2);
        }
    }
 
}
