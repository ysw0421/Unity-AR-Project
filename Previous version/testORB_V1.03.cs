using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
#endif
using OpenCVForUnity;

public class testORB_V1.03 : MonoBehaviour
{
    // Use this for initialization
    Texture2D imgTexture;
    WebCamTexture webcamTexture;
    Mat rgbaMat;
    Color32[] colors;
    Texture2D texture_web;

    Mat img1Mat;
    Mat img2Mat;

    ORB detector;
    ORB extractor;

    MatOfKeyPoint keypoints1;
    Mat descriptors1;
    MatOfKeyPoint keypoints2;
    Mat descriptors2;

    DescriptorMatcher matcher;
    MatOfDMatch matches;
    DescriptorMatcher Kmatcher;
    Mat resultImg;
    Texture2D texture;

    MatOfPoint2f srcPoints;
    MatOfPoint2f dstPoints;

    int resoution_value;


    public float world_x;
    public float world_y;
    public float[,] points;


    //Create GameObject for Video
    public Material videoMaterial;
    GameObject Cgb;

    public static GameObject VideoPlane()
    {
        GameObject GB = new GameObject("VideoPlane");

        GB.transform.localScale = new Vector3(1, 1, 1);
        GB.transform.localPosition = new Vector3(1, 1, 1);
        MeshFilter mf = GB.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer mr = GB.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        VideoPlayer VP = GB.AddComponent<VideoPlayer>();
        mr.material.color = Color.white;

        VP.url = "C:/test/asd.mp4";
        VP.isLooping = true;
        VP.renderMode = VideoRenderMode.MaterialOverride;
        VP.targetMaterialProperty = "_MainTex";
        mr.material.shader = Shader.Find("Unlit/Transparent");
        VP.Play();
        return GB;
    }

    // Update every second for video mesh
    public void MovieObject(double[] x, double[] y, double[] z, GameObject GB_Copy)
    {
        MeshFilter mf = GB_Copy.GetComponent<MeshFilter>();
        Mesh m = new Mesh();

        m.vertices = new Vector3[] {
            new Vector3(TransFormate(x)[0],TransFormate(y)[0],TransFormate(z)[0]),
            new Vector3(TransFormate(x)[1],TransFormate(y)[1],TransFormate(z)[1]),
            new Vector3(TransFormate(x)[2],TransFormate(y)[2],TransFormate(z)[2]),
            new Vector3(TransFormate(x)[3],TransFormate(y)[3],TransFormate(z)[3])
        };

        m.uv = new Vector2[] {
            new Vector2(0,1),
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1)
        };

        m.triangles = new int[] { 3, 2, 1, 3, 1, 0 };

        mf.mesh = m;
        m.RecalculateBounds();
        m.RecalculateNormals();
    }

    // mesh transform
    public static float[] TransFormate(double[] value)
    {
        float[] Value = new float[4];
        for (int i = 0; i < value.Length; i++)
        {
            Value[i] = (float)value[i];
        }
        return Value;
    }

    public string url = "file:///C:\\Users\\yeom4\\OneDrive\\document\\OpenCV_Test_1\\Assets\\Card\\TEST.jpg";

    // 퀵 정렬
    //http://codingplus.tistory.com/3
    public void sort(DMatch[] distance, int left, int right)
    {
        int l_hold = left, r_hold = right;
        int pivot = (int)distance[left].distance;
        while (left < right)
        {
            while ((pivot <= (int)distance[right].distance) && (left < right))
                right--;
            if (left != right)
                distance[left] = distance[right];

            while ((pivot >= (int)distance[left].distance) && (left < right))
                left++;
            if (left != right)
            {
                distance[right] = distance[left];
                right--;
            }
        }
        distance[left].distance = pivot;
        pivot = left;
        left = l_hold;
        right = r_hold;

        // 이 과정을 재귀호출을 하여 정렬이 될 때까지 반복을 한다.
        if (left < pivot)
            sort(distance, left, pivot - 1);
        if (right > pivot)
            sort(distance, pivot + 1, right);
    }

    void Start()
    {
        // WebCamDevice 설정
        WebCamDevice[] cam_devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(cam_devices[0].name);
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = webcamTexture;
        webcamTexture.Play();
        Camera.main.orthographicSize = webcamTexture.height/2;
        texture_web = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
        rgbaMat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
#if UNITY_ANDROID
        url = Application.streamingAssetsPath + "/TEST.jpg";
#elif UNITY_STANDALONE_WIN
        url = "file:///" + Application.dataPath + "/Card/TEST.jpg";
#endif
        imgTexture = new Texture2D(4, 4, TextureFormat.DXT1, false);
        WWW www = new WWW(url);
        if (www.isDone == false)
        {
            while (true)
            {
                if (www.isDone)
                {
                    www.LoadImageIntoTexture(imgTexture);
                    break;
                }
            }
        }
        else {
            www.LoadImageIntoTexture(imgTexture);
        }
        Debug.Log(imgTexture.height+","+ imgTexture.width);

        img1Mat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(imgTexture, img1Mat);

        detector = ORB.create();
        extractor = ORB.create();

        keypoints1 = new MatOfKeyPoint();
        descriptors1 = new Mat();

        detector.detect(img1Mat, keypoints1);
        extractor.compute(img1Mat, keypoints1, descriptors1);

        // 동영상 기능을 위한 game
        //Cgb = VideoPlane();
    }

    public void DetectObject() {
        img2Mat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC3);
        Utils.webCamTextureToMat(webcamTexture, img2Mat);

        // 동영상 행렬 img2Mat 필요
        keypoints2 = new MatOfKeyPoint();
        descriptors2 = new Mat();

        // 동영상 행렬 img2Mat 필요
        detector.detect(img2Mat, keypoints2);
        extractor.compute(img2Mat, keypoints2, descriptors2);

        matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMINGLUT);
        matches = new MatOfDMatch();
        matcher.match(descriptors1, descriptors2, matches);

        if (keypoints1 != null && keypoints2 != null && matches.toArray().Length != 0)
        {
            DMatch[] value = matches.toArray();
            sort(value, 0, value.Length - 1);

            List<Point> key1_mat = new List<Point>();
            KeyPoint[] s_key1 = keypoints1.toArray();
            for (int i = 0; i < s_key1.Length; i++)
            {
                key1_mat.Add(s_key1[value[i].queryIdx].pt);
            }
            srcPoints = new MatOfPoint2f(key1_mat.ToArray());
            List<Point> key2_mat = new List<Point>();
            KeyPoint[] s_key2 = keypoints2.toArray();
            for (int i = 0; i < s_key2.Length; i++)
            {
                key2_mat.Add(s_key2[value[i].trainIdx].pt);
            }
            //Point -> mat
            dstPoints = new MatOfPoint2f(key2_mat.ToArray());
             
            Mat H = Calib3d.findHomography(srcPoints, dstPoints, Calib3d.RANSAC, 5.0f); // 애가 문제가 있음 오류가 뜨긴함...................

            Point[] cornersObject = new Point[4];
            cornersObject[0] = new Point(0, 0);
            cornersObject[1] = new Point(0, imgTexture.height);
            cornersObject[2] = new Point(imgTexture.width, imgTexture.height);
            cornersObject[3] = new Point(imgTexture.width, 0);

            Point[] cornersSceneTemp = new Point[0];

            MatOfPoint2f cornersSceneMatrix = new MatOfPoint2f(cornersSceneTemp);
            MatOfPoint2f cornersObjectMatrix = new MatOfPoint2f(cornersObject);

            Core.perspectiveTransform(cornersObjectMatrix, cornersSceneMatrix, H);

            Point[] cornersScene = cornersSceneMatrix.toArray();
            double under = ((cornersScene[3].y - cornersScene[1].y) * (cornersScene[2].x - cornersScene[0].x) - (cornersScene[3].x - cornersScene[1].x) * (cornersScene[2].y - cornersScene[0].y));
            if (under == 0)
            {
                Debug.Log("AC,BD 대각선이 평행.");
            }
            else
            {
                double t = ((cornersScene[3].x - cornersScene[1].x) * (cornersScene[0].y - cornersScene[1].y) - (cornersScene[3].y - cornersScene[1].y) * (cornersScene[0].x - cornersScene[1].x)) / under;
                double s = ((cornersScene[2].x - cornersScene[0].x) * (cornersScene[0].y - cornersScene[1].y) - (cornersScene[2].y - cornersScene[0].y) * (cornersScene[0].x - cornersScene[1].x)) / under;
                if (t < 0 || t > 1 || s < 0 || s > 1)
                {
                    Debug.Log("두 대각선이 교차하지 않음.");
                    Texture2D texture = new Texture2D(img2Mat.cols(), img2Mat.rows(), TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(img2Mat, texture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;
                    double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
                    x[0] = 0; y[0] = 0; z[0] = 500;
                    x[1] = 0; y[1] = 0; z[1] = 500;
                    x[2] = 0; y[2] = 0; z[2] = 500;
                    x[3] = 0; y[3] = 0; z[3] = 500;

                    //MovieObject(x, y, z, Cgb);

                }
                else
                {
                    /*
                   Center_x = ((a1 * c2 - a2 * c1) * (b1 - d1) - (a1 - c1) * (b1 * d2 - b2 * d1)) / ((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
                   Center_y = ((a1 * c2 - a2 * c1) * (b2 - d2) - (a2 - c2) * (b1 * d2 - b2 * d1)) / ((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
                   */
                    Imgproc.line(img2Mat, new Point(cornersScene[0].x, cornersScene[0].y), new Point(cornersScene[1].x, cornersScene[1].y), new Scalar(200, 0, 0), 5); // A,B
                    Imgproc.line(img2Mat, new Point(cornersScene[1].x, cornersScene[1].y), new Point(cornersScene[2].x, cornersScene[2].y), new Scalar(200, 0, 0), 5); // B,C
                    Imgproc.line(img2Mat, new Point(cornersScene[2].x, cornersScene[2].y), new Point(cornersScene[3].x, cornersScene[3].y), new Scalar(200, 0, 0), 5); // C,D
                    Imgproc.line(img2Mat, new Point(cornersScene[3].x, cornersScene[3].y), new Point(cornersScene[0].x, cornersScene[0].y), new Scalar(200, 0, 0), 5); // D,A

                    //((a1 * c2 - a2 * c1) * (b1 - d1) - (a1 - c1) * (b1 * d2 - b2 * d1))
                    //              /((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
                    float Center_x = (float)(((cornersScene[0].x * cornersScene[2].y - cornersScene[0].y * cornersScene[2].x) * (cornersScene[1].x - cornersScene[3].x) - (cornersScene[0].x - cornersScene[2].x) * (cornersScene[1].x * cornersScene[3].y - cornersScene[1].y * cornersScene[3].x))
                        / ((cornersScene[0].x - cornersScene[2].x) * (cornersScene[1].y - cornersScene[3].y) - (cornersScene[0].y - cornersScene[2].y) * (cornersScene[1].x - cornersScene[3].x)));
                    //((a1 * c2 - a2 * c1) * (b2 - d2) - (a2 - c2) * (b1 * d2 - b2 * d1))
                    //              /((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
                    float Center_y = (float)(((cornersScene[0].x * cornersScene[2].y - cornersScene[0].y * cornersScene[2].x) * (cornersScene[1].y - cornersScene[3].y) - (cornersScene[0].y - cornersScene[2].y) * (cornersScene[1].x * cornersScene[3].y - cornersScene[1].y * cornersScene[3].x))
                        / ((cornersScene[0].x - cornersScene[2].x) * (cornersScene[1].y - cornersScene[3].y) - (cornersScene[0].y - cornersScene[2].y) * (cornersScene[1].x - cornersScene[3].x)));
                    Imgproc.line(img2Mat, new Point(Center_x, Center_y), new Point(Center_x, Center_y), new Scalar(0, 0, 200), 10);

                    world_x = Center_x - (webcamTexture.width / 2);
                    world_y = Center_y - (webcamTexture.height / 2);

                    //1: 월드 좌표, 2: 1번 좌표, 3: 2번 좌표, 4: 3번 좌표, 5: 4번 좌표
                    points = new float[5, 2] {{world_x,-world_y},
                        {(float)cornersScene[0].x - (webcamTexture.width / 2), -((float)cornersScene[0].y - (webcamTexture.height / 2))},
                        {(float)cornersScene[1].x - (webcamTexture.width / 2), -((float)cornersScene[1].y - (webcamTexture.height / 2))},
                        {(float)cornersScene[2].x - (webcamTexture.width / 2), -((float)cornersScene[2].y - (webcamTexture.height / 2))},
                        {(float)cornersScene[3].x - (webcamTexture.width / 2), -((float)cornersScene[3].y - (webcamTexture.height / 2))}};

                    Texture2D texture = new Texture2D(img2Mat.cols(), img2Mat.rows(), TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(img2Mat, texture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                    /*
                    double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
                    x[0] = -gameObject.transform.localScale.x / 2 + cornersScene[0].x; y[0] = gameObject.transform.localScale.y / 2 - cornersScene[0].y; z[0] = 500;
                    x[1] = -gameObject.transform.localScale.x / 2 + cornersScene[1].x; y[1] = gameObject.transform.localScale.y / 2 - cornersScene[1].y; z[1] = 500;
                    x[2] = -gameObject.transform.localScale.x / 2 + cornersScene[2].x; y[2] = gameObject.transform.localScale.y / 2 - cornersScene[2].y; z[2] = 500;
                    x[3] = -gameObject.transform.localScale.x / 2 + cornersScene[3].x; y[3] = gameObject.transform.localScale.y / 2 - cornersScene[3].y; z[3] = 500;
                    

                    // 1번 기능 : 트레킹 오브젝트 위에 동영상 첨가.
                    //MovieObject(x, y, z, Cgb);
                    
                    // 2번 기능 : 발견 즉시 동영상 화면 띄우기.
                    VideoPlayer VP = gameObject.AddComponent<VideoPlayer>();

                    VP.url = "C:/test/asd.mp4";
                    VP.isLooping = true;
                    VP.renderMode = VideoRenderMode.MaterialOverride;
                    VP.targetMaterialProperty = "_MainTex";
                    VP.Play();
                    */
                }
                Resources.UnloadUnusedAssets();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (webcamTexture.isPlaying != true) {
            webcamTexture.Play();
        }
        DetectObject();
    }

}
