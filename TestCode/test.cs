using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;

public class test : MonoBehaviour {
    // Use this for initialization
    Mat rgbaMat;
    Color32[] colors;
    Texture2D texture;
    WebCamTexture webcamTexture;
    int a1, a2;
    int b1, b2;
    int c1, c2;
    int d1, d2;
    float Center_x;
    float Center_y;

    public float world_x;
    public float world_y;
    public float[,] points;

    void Start () {
        WebCamDevice[] cam_devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(cam_devices[0].name, 640, 480, 30);
        webcamTexture.Play();
        while (true) {
            colors = new Color32[webcamTexture.width * webcamTexture.height];
            texture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false); // Debug.Log("Start 부분 "+texture.GetPixel(1, 1));
            rgbaMat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
            gameObject.transform.localScale = new Vector3(webcamTexture.width, webcamTexture.height, 1);
            float width = rgbaMat.width();
            float height = rgbaMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale){
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else{
                Camera.main.orthographicSize = height / 2;
            }
            break;
        }
        
    }
	// Update is called once per frame
	void Update () {
        a1 = 50; a2 = 50;
        b1 = 105; b2 = 400;
        c1 = 455; c2 = 455;
        d1 = 400; d2 = 105;
        Utils.webCamTextureToMat(webcamTexture, rgbaMat, colors);

        Imgproc.line(rgbaMat, new Point(webcamTexture.width/2, webcamTexture.height/2), new Point(webcamTexture.width/2, webcamTexture.height/2), new Scalar(200, 0, 0), 3);
        Imgproc.line(rgbaMat, new Point(a1, a2), new Point(b1, b2), new Scalar(0, 0, 200), 3); // A,B
        Imgproc.line(rgbaMat, new Point(b1, b2), new Point(c1, c2), new Scalar(0, 0, 200), 3); // B,C
        Imgproc.line(rgbaMat, new Point(c1, c2), new Point(d1, d2), new Scalar(0, 0, 200), 3); // C,D
        Imgproc.line(rgbaMat, new Point(d1, d2), new Point(a1, a2), new Scalar(0, 0, 200), 3); // D,A

        Center_x = ((a1 * c2 - a2 * c1) * (b1 - d1) - (a1 - c1) * (b1 * d2 - b2 * d1)) /((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
        Center_y = ((a1 * c2 - a2 * c1) * (b2 - d2) - (a2 - c2) * (b1 * d2 - b2 * d1))/ ((a1 - c1) * (b2 - d2) - (a2 - c2) * (b1 - d1));
        Imgproc.line(rgbaMat, new Point(Center_x, Center_y), new Point(Center_x, Center_y), new Scalar(0, 0, 200), 3);
        world_x = Center_x - (webcamTexture.width / 2);
        world_y = Center_y - (webcamTexture.height / 2);

        //1: 월드 좌표, 2: 1번 좌표, 3: 2번 좌표, 4: 3번 좌표, 5: 4번 좌표
        points = new float[5, 2] {{world_x,-world_y},
            {a1 - (webcamTexture.width / 2), -(a2 - (webcamTexture.height / 2))},
            {b1 - (webcamTexture.width / 2), -(b2 - (webcamTexture.height / 2))},
            {c1 - (webcamTexture.width / 2), -(c2 - (webcamTexture.height / 2))},
            {d1 - (webcamTexture.width / 2), -(d2 - (webcamTexture.height / 2))}};

        Utils.matToTexture2D(rgbaMat, texture, colors);
        Resources.UnloadUnusedAssets();
    }
}
