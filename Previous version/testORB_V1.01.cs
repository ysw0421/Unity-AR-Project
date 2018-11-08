using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

//manual화 화요일날

public class testORB_V1.01 : MonoBehaviour
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

    // 퀵 정렬
    //http://codingplus.tistory.com/3
    public static void sort(DMatch[] distance, int left, int right) {
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
        webcamTexture = new WebCamTexture(cam_devices[0].name, 640, 480, 30);
        webcamTexture.Play();

        texture_web = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
        rgbaMat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);

        // Lion 카드 - 고정용.
        imgTexture = Resources.Load("TEST") as Texture2D;
        img1Mat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(imgTexture, img1Mat);

        detector = ORB.create();
        extractor = ORB.create();

        keypoints1 = new MatOfKeyPoint();
        descriptors1 = new Mat();

        detector.detect(img1Mat, keypoints1);
        extractor.compute(img1Mat, keypoints1, descriptors1);
    }

    // Update is called once per frame
    void Update()
    {
        colors = new Color32[webcamTexture.width * webcamTexture.height];
        texture_web = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
        rgbaMat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
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
        //한동안 주석처리
        //Mat resultImg = new Mat();
        //Features2d.drawMatches(img1Mat, keypoints1, img2Mat, keypoints2, matches, resultImg);

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

            Mat H = Calib3d.findHomography(srcPoints, dstPoints,Calib3d.RANSAC,5.0f); // 애가 문제가 있음 오류가 뜨긴함...................

            //Create the unscaled array of corners, representing the object size
            Point[] cornersObject = new Point[4];
            cornersObject[0] = new Point (0, 0);
            cornersObject[1] = new Point (0,imgTexture.height);
            cornersObject[2] = new Point (imgTexture.width,imgTexture.height);
            cornersObject[3] = new Point (imgTexture.width,0);

            Point[] cornersSceneTemp = new Point[0];

            MatOfPoint2f cornersSceneMatrix = new MatOfPoint2f(cornersSceneTemp);
            MatOfPoint2f cornersObjectMatrix = new MatOfPoint2f(cornersObject);

            Core.perspectiveTransform(cornersObjectMatrix, cornersSceneMatrix, H);

            Point[] cornersScene = cornersSceneMatrix.toArray();
            //AB,CD   B1 -> C1,  B2 -> C2 , C1 -> B1, C2 -> B2
            //cornersScene[2] <-> cornersScene[1]
            //double t = ((d1 - b1)*(a2 - b2) - (d2 - b2)*(a1 - b1))/((d2 - b2)*(c1 - a1) - (d1 - b1)*(c2 - a2))
            //double s = ((c1 - a1)*(a2 - b2) - (c2 - a2)*(a1 - b1))/((d2 - b2)*(c1 - a1) - (d1 - b1)*(c2 - a2))
            double under = ((cornersScene[3].y - cornersScene[1].y) * (cornersScene[2].x - cornersScene[0].x) - (cornersScene[3].x - cornersScene[1].x) * (cornersScene[2].y - cornersScene[0].y));
            if (under == 0)
            {
                Debug.Log("AC,BD 대각선이 평행.");
            }
            else {
                double t = ((cornersScene[3].x - cornersScene[1].x) * (cornersScene[0].y - cornersScene[1].y) - (cornersScene[3].y - cornersScene[1].y) * (cornersScene[0].x - cornersScene[1].x)) / under;
                double s = ((cornersScene[2].x - cornersScene[0].x) * (cornersScene[0].y - cornersScene[1].y) - (cornersScene[2].y - cornersScene[0].y) * (cornersScene[0].x - cornersScene[1].x)) / under;
                if (t < 0 || t > 1 || s < 0 || s > 1)
                {
                    Debug.Log("두 대각선이 교차하지 않음.");
                    Texture2D texture = new Texture2D(img2Mat.cols(), img2Mat.rows(), TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(img2Mat, texture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;
                }
                else
                {
                    Imgproc.line(img2Mat, new Point(cornersScene[0].x, cornersScene[0].y), new Point(cornersScene[1].x, cornersScene[1].y), new Scalar(200, 0, 0), 5);
                    Imgproc.line(img2Mat, new Point(cornersScene[1].x, cornersScene[1].y), new Point(cornersScene[2].x, cornersScene[2].y), new Scalar(200, 0, 0), 5);
                    Imgproc.line(img2Mat, new Point(cornersScene[2].x, cornersScene[2].y), new Point(cornersScene[3].x, cornersScene[3].y), new Scalar(200, 0, 0), 5);
                    Imgproc.line(img2Mat, new Point(cornersScene[3].x, cornersScene[3].y), new Point(cornersScene[0].x, cornersScene[0].y), new Scalar(200, 0, 0), 5);
                    Texture2D texture = new Texture2D(img2Mat.cols(), img2Mat.rows(), TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(img2Mat, texture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;
                }

            }
            /*
            Point AC = cornersScene[0] - cornersScene[2];
            Point BD = cornersScene[1] - cornersScene[3];
            Vector2 A = new Vector2((float)cornersScene[0].x, (float)cornersScene[0].y);
            Vector2 B = new Vector2((float)cornersScene[1].x, (float)cornersScene[1].y);
            Vector2 C = new Vector2((float)cornersScene[2].x, (float)cornersScene[2].y);
            Vector2 D = new Vector2((float)cornersScene[3].x, (float)cornersScene[3].y);
            */
        }
    }
}
