using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
#endif
using OpenCVForUnity;

public class testORB_V1.04 : MonoBehaviour
{
    // Use this for initialization
    Texture2D imgTexture;
    WebCamTexture webcamTexture;
    Mat rgbaMat;
    Color32[] colors;
    Texture2D texture_web;

    Mat img1Mat;
    Mat img2Mat;

    Mat H;  // Homography를 위한 Matrix

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

    public float world_x;   // OpenCV좌표가 아닌 world 좌표계로 x좌표
    public float world_y;   // OpenCV좌표가 아닌 world 좌표계로 y좌표
    public float[,] points; // plane_control.cs에서 쓰기 위한 값들 사각형을 그리기 위한 점 4개와 world_x, world_y 총 10개 값을 넘겨줌.

    public bool FindObject; // 그리려는 사각형의 두 대각선이 평행일 경우, 두 대각선이 만나지 않을 경우, Homography Matrix가 NULL일 경우를 체크.

    public enum Dropdown { QRCodeRole, VideoMesh, ARTarget };
    public Dropdown dropdown;

    bool QR;
    bool VM;
    public bool AT;

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

    public void DrawTarget(double[] x, double[] y)
    {
        Imgproc.line(img2Mat, new Point(x[0], y[0]), new Point(x[1], y[1]), new Scalar(200, 0, 0), 5); // A,B
        Imgproc.line(img2Mat, new Point(x[1], y[1]), new Point(x[2], y[2]), new Scalar(200, 0, 0), 5); // B,C
        Imgproc.line(img2Mat, new Point(x[2], y[2]), new Point(x[3], y[3]), new Scalar(200, 0, 0), 5); // C,D
        Imgproc.line(img2Mat, new Point(x[3], y[3]), new Point(x[0], y[0]), new Scalar(200, 0, 0), 5); // D,A
    }
    public void DrawTarget(Point[] cornersScene)
    {
        Imgproc.line(img2Mat, new Point(cornersScene[0].x, cornersScene[0].y), new Point(cornersScene[1].x, cornersScene[1].y), new Scalar(200, 0, 0), 5); // A,B
        Imgproc.line(img2Mat, new Point(cornersScene[1].x, cornersScene[1].y), new Point(cornersScene[2].x, cornersScene[2].y), new Scalar(200, 0, 0), 5); // B,C
        Imgproc.line(img2Mat, new Point(cornersScene[2].x, cornersScene[2].y), new Point(cornersScene[3].x, cornersScene[3].y), new Scalar(200, 0, 0), 5); // C,D
        Imgproc.line(img2Mat, new Point(cornersScene[3].x, cornersScene[3].y), new Point(cornersScene[0].x, cornersScene[0].y), new Scalar(200, 0, 0), 5); // D,A
    }

    public DMatch[] MakeGoodValue(DMatch[] value) {
        int count = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i].distance < 0.75 * value[value.Length - 1 - i].distance)
            {
                count = count + 1;
            }
        }
        DMatch[] goodvalue = new DMatch[count];
        count = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i].distance < 0.75 * value[value.Length - 1 - i].distance)
            {
                goodvalue[count] = value[i];
                count = count + 1;
            }
        }
        return goodvalue;
    }

    public string url = "file:///C:\\Users\\yeom4\\OneDrive\\document\\OpenCV_Test_1\\Assets\\StreamingAssets\\TEST.jpg";

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


        // TEST.jpg 이미지를 StreamingAsset 디렉토리에서 가져와 Texture화 시킨다.
        // Resource.Load는 Unity 전체에 큰 부하를 준다.
        // Texture도 마찬가지로 메모리 할당을 풀어주지 않으면 메모리에 큰 부하를 준다.
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


        // 위에서 Texture화한 imgTexture(이미지)를 Matrix화 한다.
        img1Mat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(imgTexture, img1Mat);

        // ORB 알고리즘을 위해 detector,extractor 객체를 생성한다. 
        //500, 1.2f, 8, 31, 0, 2, ORB.HARRIS_SCORE,31
        detector = ORB.create();
        extractor = ORB.create();

        // KeyPoint를 위한 Matrix를 생성. 
        keypoints1 = new MatOfKeyPoint();
        descriptors1 = new Mat();

        // img1Mat에서 detector 객체를 통해 detect한 keypoint를 keypoint를 저장한다.
        detector.detect(img1Mat, keypoints1);
        // img1Mat와 keypoints들을 extractor를 통해 descriptors를 추출. 
        extractor.compute(img1Mat, keypoints1, descriptors1);

        // 동영상 기능을 위한 GameObject 생성자
        Cgb = VideoPlane();
    }


    // 위의 함수는 Update() 문에 들어가게 된다.
    public void DetectObject() {
        switch (dropdown)
        {
            case Dropdown.QRCodeRole:
                QR = true;
                VM = false;
                AT = false;
                break;
            case Dropdown.VideoMesh:
                QR = false;
                VM = true;
                AT = false;
                break;
            case Dropdown.ARTarget:
                QR = false;
                VM = false;
                AT = true;
                break;
            default:
                QR = false;
                VM = false;
                AT = false;
                break;
        }

        if (VM == true || AT == true)
        {
            VideoPlayer VP = gameObject.GetComponent<VideoPlayer>();
            Destroy(VP);
            double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
            x[0] = 0; y[0] = 700; z[0] = 500;
            x[1] = 0; y[1] = 700; z[1] = 500;
            x[2] = 0; y[2] = 700; z[2] = 500;
            x[3] = 0; y[3] = 700; z[3] = 500;
            MovieObject(x, y, z, Cgb);
        }
        else if (QR == true || AT == true)
        {
            double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
            x[0] = 0; y[0] = 700; z[0] = 500;
            x[1] = 0; y[1] = 700; z[1] = 500;
            x[2] = 0; y[2] = 700; z[2] = 500;
            x[3] = 0; y[3] = 700; z[3] = 500;
            MovieObject(x, y, z, Cgb);
        }

        // 웹캠에서 들어오는 프레임들을 Matrix화 시키기위한 객체
        img2Mat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC3);
        Utils.webCamTextureToMat(webcamTexture, img2Mat);

        // 동영상 행렬 img2Mat 필요
        keypoints2 = new MatOfKeyPoint();
        descriptors2 = new Mat();

        // 동영상 프레임 img2Mat에서 keypoints를 찾고 descriptors를 뽑아낸다.
        detector.detect(img2Mat, keypoints2);
        extractor.compute(img2Mat, keypoints2, descriptors2);

        // Descriptor를 Match를 시키기 위한 함수. 알고리즘 이름 : BRUTEFORCE_HAMMINGLUT
        matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMINGLUT);
        matches = new MatOfDMatch();
        matcher.match(descriptors1, descriptors2, matches);
        


        if (keypoints1 != null && keypoints2 != null && matches.toArray().Length != 0)
        {
            DMatch[] value = matches.toArray();
            // keypoints1, keypoints2를 퀵정렬을 통해 순서대로 나열하고 각각 필요한 개수만큼만 가져오게 설계
            // 퀵 정렬를 사용
            sort(value, 0, value.Length - 1);

            /*
            // 좋은 특징점 선별 - 이상하게 적용 후 더 안좋아짐...
            DMatch[] goodvalue = MakeGoodValue(value);
            */

            List<Point> key1_mat = new List<Point>();
            KeyPoint[] s_key1 = keypoints1.toArray();
            //goodvalue.Length-1
            for (int i = 0; i < value.Length - 1; i++)
            {
                key1_mat.Add(s_key1[value[i].queryIdx].pt);
            }

            srcPoints = new MatOfPoint2f(key1_mat.ToArray());
            List<Point> key2_mat = new List<Point>();
            KeyPoint[] s_key2 = keypoints2.toArray();
            //goodvalue.Length-1
            for (int i = 0; i < value.Length - 1; i++)
            {
                key2_mat.Add(s_key2[value[i].trainIdx].pt);
            }
            //Point -> mat
            dstPoints = new MatOfPoint2f(key2_mat.ToArray());
            
            
            
            // 위에서 뽑아낸 srcPoints와 dstPoints들을 findHomography 함수에 집어넣어 이미지가 동영상 프레임상에서 얼마정도 돌아가있는지 확인할 수 있다.
            // findHomography 함수에서 생성한 객체에서 null 값이 들어올 때 처리해야할게 있어서 try, catch 구문을 넣음.
            try
            {
                H = Calib3d.findHomography(srcPoints, dstPoints, Calib3d.RANSAC, 5.0f); // CVException.
            }
            catch(CvException)
            {
                FindObject = false;
                // plane_control.cs에 값을 world_x, world_y , point[] 함수들을 넘기는데 MeshPlane과 Plane GameObject를 만드는데 사용된다.
                // Plane GameObject는 world_x, world_y를 통해 월드 좌표의 x,y를 담당하게 되고 화면에서 잠깐 안보이게 하기위해 값을 지정했다. 
                world_x = 0;
                world_y = 600;
            }
            
            // image크기를 나타낼 꼭지점 4개를 집어넣고
            Point[] cornersObject = new Point[4];
            cornersObject[0] = new Point(0, 0);
            cornersObject[1] = new Point(0, imgTexture.height);
            cornersObject[2] = new Point(imgTexture.width, imgTexture.height);
            cornersObject[3] = new Point(imgTexture.width, 0);

            // 배경에서 나타낼 이미지 중 한 점을 축으로 둠. 이 축기준으로 H로 돌린다.
            Point[] cornersSceneTemp = new Point[0];

            MatOfPoint2f cornersSceneMatrix = new MatOfPoint2f(cornersSceneTemp);
            MatOfPoint2f cornersObjectMatrix = new MatOfPoint2f(cornersObject);

            Core.perspectiveTransform(cornersObjectMatrix, cornersSceneMatrix, H);

            Point[] cornersScene = cornersSceneMatrix.toArray();
            // 뒤에서 사용될 분모 값이 계속 under로 반복되기 때문에 under(분모)변수 선언.
            double under = ((cornersScene[3].y - cornersScene[1].y) * (cornersScene[2].x - cornersScene[0].x) - (cornersScene[3].x - cornersScene[1].x) * (cornersScene[2].y - cornersScene[0].y));
            // 사각형 결정조건.
            if (under == 0)
            {
                // 분모가 0일 수 없음.
                // 두 대각선이 평행. - 적용 x  :AC,BD 대각선이 평행
                FindObject = false;
            }
            else
            {
                double t = ((cornersScene[3].x - cornersScene[1].x) * (cornersScene[0].y - cornersScene[1].y) - (cornersScene[3].y - cornersScene[1].y) * (cornersScene[0].x - cornersScene[1].x)) / under;
                double s = ((cornersScene[2].x - cornersScene[0].x) * (cornersScene[0].y - cornersScene[1].y) - (cornersScene[2].y - cornersScene[0].y) * (cornersScene[0].x - cornersScene[1].x)) / under;
                if (t < 0 || t > 1 || s < 0 || s > 1)
                {
                    // 두 대각선이 만나지 않음. - 적용 x :두 대각선이 교차하지 않음
                    FindObject = false;
                    Texture2D texture = new Texture2D(img2Mat.cols(), img2Mat.rows(), TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(img2Mat, texture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;
                    
                    // Plane GameObject를 잠깐 안보이게 하기위해 값을 지정했다. 
                    world_x = 0;
                    world_y = 600;

                    // 기능1. 트레킹 오브젝트 위에 동영상 첨가.
                    if (VM == true)
                    {
                        double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
                        x[0] = 0; y[0] = 0; z[0] = 500;
                        x[1] = 0; y[1] = 0; z[1] = 500;
                        x[2] = 0; y[2] = 0; z[2] = 500;
                        x[3] = 0; y[3] = 0; z[3] = 500;

                        //DrawTarget(x, y);
                        MovieObject(x, y, z, Cgb);
                    }
                }
                else
                {
                    // 사각형이 만들어질 경우.
                    FindObject = true;
                    //((a1 * c2 - a2 * c1) * (b1 - d1) - (a1 - c1) * (b1 * d2 - b2 * d1))
                    //              /((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
                    float Center_x = (float)(((cornersScene[0].x * cornersScene[2].y - cornersScene[0].y * cornersScene[2].x) * (cornersScene[1].x - cornersScene[3].x) - (cornersScene[0].x - cornersScene[2].x) * (cornersScene[1].x * cornersScene[3].y - cornersScene[1].y * cornersScene[3].x))
                        / ((cornersScene[0].x - cornersScene[2].x) * (cornersScene[1].y - cornersScene[3].y) - (cornersScene[0].y - cornersScene[2].y) * (cornersScene[1].x - cornersScene[3].x)));
                    //((a1 * c2 - a2 * c1) * (b2 - d2) - (a2 - c2) * (b1 * d2 - b2 * d1))
                    //              /((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
                    float Center_y = (float)(((cornersScene[0].x * cornersScene[2].y - cornersScene[0].y * cornersScene[2].x) * (cornersScene[1].y - cornersScene[3].y) - (cornersScene[0].y - cornersScene[2].y) * (cornersScene[1].x * cornersScene[3].y - cornersScene[1].y * cornersScene[3].x))
                        / ((cornersScene[0].x - cornersScene[2].x) * (cornersScene[1].y - cornersScene[3].y) - (cornersScene[0].y - cornersScene[2].y) * (cornersScene[1].x - cornersScene[3].x)));

                    // 찾을 객체의 중심점을 그린다.
                    //Imgproc.line(img2Mat, new Point(Center_x, Center_y), new Point(Center_x, Center_y), new Scalar(0, 0, 200), 10);
                    // 찾을 객체에 빨간색 4선을 그린다.
                    DrawTarget(cornersScene);
                    
                    // plane_control.cs에 world_x, world_y를 보냄.
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
                    if (VM == true)
                    {
                        // 1번 기능 : 트레킹 오브젝트 위에 동영상 첨가.
                        double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
                        x[0] = -gameObject.transform.localScale.x / 2 + cornersScene[0].x; y[0] = gameObject.transform.localScale.y / 2 - cornersScene[0].y; z[0] = 500;
                        x[1] = -gameObject.transform.localScale.x / 2 + cornersScene[1].x; y[1] = gameObject.transform.localScale.y / 2 - cornersScene[1].y; z[1] = 500;
                        x[2] = -gameObject.transform.localScale.x / 2 + cornersScene[2].x; y[2] = gameObject.transform.localScale.y / 2 - cornersScene[2].y; z[2] = 500;
                        x[3] = -gameObject.transform.localScale.x / 2 + cornersScene[3].x; y[3] = gameObject.transform.localScale.y / 2 - cornersScene[3].y; z[3] = 500;
                        MovieObject(x, y, z, Cgb);

                    }
                    if (QR == true)
                    {
                        // 2번 기능 : 발견 즉시 동영상 화면 띄우기.
                        if (gameObject.GetComponent<VideoPlayer>() == null)
                        {
                            VideoPlayer VP = gameObject.AddComponent<VideoPlayer>();
                            VP.url = "C:/test/asd.mp4";
                            VP.isLooping = true;
                            VP.renderMode = VideoRenderMode.MaterialOverride;
                            VP.targetMaterialProperty = "_MainTex";
                            VP.Play();
                        }
                        else
                        {
                            VideoPlayer VP = gameObject.GetComponent<VideoPlayer>();
                            VP.url = "C:/test/asd.mp4";
                            VP.isLooping = true;
                            VP.renderMode = VideoRenderMode.MaterialOverride;
                            VP.targetMaterialProperty = "_MainTex";
                            VP.Play();
                        }
                    }
                }
                // Texture 때문에 메모리가 계속 증가해서 추가한 함수.
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
