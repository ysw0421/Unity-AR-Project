using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TestVideoCapture : MonoBehaviour {

    public Material videoMaterial;
    GameObject Cgb;

    public static GameObject CreatePlane()
    {
        GameObject GB = new GameObject("ParentPlane");
      

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

    public void meshUodate(double[] x, double[] y, double[] z, GameObject GB_Copy)
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
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),
        };

        m.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        mf.mesh = m;
        m.RecalculateBounds();
        m.RecalculateNormals();
    }

    public static float[] TransFormate(double[] value)
    {
        float[] Value = new float[4];
        for (int i = 0; i < value.Length; i++)
        {
            Value[i] = (float)value[i];
        }
        return Value;
    }

    void Start () {
        Cgb = CreatePlane();
    }
	
	void Update () {
        double width = 500;
        double height = 500;
        double[] x = new double[4]; double[] y = new double[4]; double[] z = new double[4];
        x[0] = -width / 2; y[0] = -height / 2; z[0] = 1;
        x[1] = -width / 2; y[1] = height / 2; z[1] = 1;
        x[2] = width / 2; y[2] = height / 2; z[2] = 1;
        x[3] = width / 2; y[3] = -height / 2; z[3] = 1;
        meshUodate(x,y,z,Cgb);
    }
}
