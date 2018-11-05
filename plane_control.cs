using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class plane_control : MonoBehaviour {
    /* 
     * 이 컴포넌트는 Lion Card Game Object에 붙어있습니다.
     * 1. Quad에서 나오는 4개의 2D좌표를 받아옵니다.
     * 2. Lind 카드 비율에 맞게 직사각형 생성.
     * 3. 그렇게 생성된 직사각형 위에 Game Object를 올려 놓습니다.
     */
    public GameObject plane;            // Lion Card plane을 쓰기위함.
    public GameObject quad;             // 만들어져 있는 Quad gameobject를 가져오기 위한 객체
    public testORB_V3 quad2;            // plane에 붙어있는 public 값을 가져오기 위해 만들어짐. - 스크립트는 동일한 디렉토리에 위치해야 가져올 수 있다.
    public GameObject lionessPrefab;    // Lion 
    public SpriteRenderer SR;           // Card의 width, height를 추가하기 위해 만들어진 객체
    public GameObject sphere;
    public float[,] Ppoint;             // 후에 quad2에서 좌표값들을 받아서 가져오는데 꼭지점좌표 4개의 2D 값을 받아오는데 가독성을 위해 선언.
    double a, b, c, d;                  // Quad에 붙어있는 2D 4개 좌표를 3D 4개 좌표로 만들기 위해
    double[] x, y, z;                   // 직사각형 4개의 x,y,z를 받기 위해 생성.

    LineRenderer lineRenderer;

    GameObject Cgb;                     // Created Game Object
    GameObject inst;                    // Instantiate 인스턴트화하다.
    GameObject line;

    // 외적을 통해 두 벡터간의 사잇각을 구하는 함수. - 사잇각을 반환
    public float GetAngleBetween3DVector(Vector3 vec1, Vector3 vec2)
    {
        float theta = Vector3.Dot(vec1, vec2) / (vec1.magnitude * vec2.magnitude);
        Vector3 dirAngle = Vector3.Cross(vec1, vec2);
        float angle = Mathf.Acos(theta) * Mathf.Rad2Deg;
        if (dirAngle.z < 0.0f) angle = 360 - angle;
        return angle;
    }


    // 후에 만들어질 Mesh를 위한 각도를 계산할 GameObject 생성. - 생성된 GameObject를 반환.
    public static GameObject CreatePlane()
    {
        GameObject GB = new GameObject("ParentPlane");
        GB.transform.localScale = new Vector3(1, 1, 1);
        GB.transform.localPosition = new Vector3(1, 1, 1);
        MeshRenderer mr = GB.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
        MeshFilter mf = GB.AddComponent(typeof(MeshFilter)) as MeshFilter;
        return GB;
    }

    // 계산이 이루어진 뒤 만들어진 Mesh 생성 직사각형의 꼭지점 좌표 4개가 들어갈 x,y,z를 대입.
    public void ShowPlane(double[] x, double[] y, double[] z, int color, GameObject GB_Copy)
    {
        MeshFilter mf = GB_Copy.GetComponent<MeshFilter>();
        MeshRenderer mr = GB_Copy.GetComponent<MeshRenderer>();
        if (color == 1) { mr.material.color = Color.red; }
        else if (color == 2) { mr.material.color = Color.yellow; }
        else if (color == 3) { mr.material.color = Color.blue; }
        else { mr.material.color = Color.white; };
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
  
    // 들어온 꼭지점 좌표 4개를 벡터에 분배해주기 위해 만들어졌다.
    public static float[] TransFormate(double[] value)
    {
        float[] Value = new float[4];
        for (int i = 0; i < value.Length; i++)
        {
            Value[i] = (float)value[i];
        }
        return Value;
    }

    // Use this for initialization
    void Start () {
        // Sprite Renderer로 만들어진 Lion Card의 Game Object인 plane에 사자를 추가, 
        inst = Instantiate(lionessPrefab, plane.transform);
        // 법선 벡터를 만들기 위한 GameObject
        line = new GameObject();
        // 시작하지마자 GameObject 하나 생성.
        Cgb = CreatePlane();

    }
	
	// Update is called once per frame
	void Update () {
        // 2D좌표 -> 3D좌표 만들기 위한 Z 추가 
        double[] z1;double[] z2;
        
        //외부 컨포넌트들 중 이 컨포넌트가 먼저실행 될때를 생각해서 null이 아닐때를 처리
        if (quad2.points != null)
        {
            // LionCard Z 좌표를 1000중 500의 위치 X,Y는 Quad에서 중점 계산해서 보내줌.
            plane.transform.position = new Vector3(quad2.world_x, -quad2.world_y, 500);
            /*
             * 1.  Quad에서 나오는 4개의 2D좌표를 받아옵니다.
             * X,Y,Z 좌표 추상화
             * a(0,1) d(1,1)         A(Ppoint[1, 0], Ppoint[1, 1]) D(Ppoint[4, 0], Ppoint[4, 1])
             * b(0,0) c(1,0)         B(Ppoint[2, 0], Ppoint[2, 1]) C(Ppoint[3, 0], Ppoint[3, 1])
            (Ppoint[2, 0] - Ppoint[1, 0]), (Ppoint[2, 1] - Ppoint[1, 1]), z1-z2; 벡터AB
            (Ppoint[3, 0] - Ppoint[2, 0]), (Ppoint[3, 1] - Ppoint[2, 1]), z1+z2; 벡터BC
            */
            Ppoint = quad2.points;

            /* 2. Lind 카드 비율에 맞게 직사각형 생성.
             *  a. 내가 사용한 직사각형 결정 방법 
             *      1. 가로의 길이와 세로의 길이 비율이 정해저있다.
             *      2. 가로와 세로의 사이 각이 직각이다.
             *      Z좌표 정의 
             *          두 대각선의 교점은 대각선들의 중점들이 만난다. 그러면 각 대각선의 시작 좌표와 끝자표는 중점 기준 일정 값의 차이가 존재한다.
             *          예시 - 중점의 Z좌표 500,  직선 BD 거리의 중점이 500,  B의 Z좌표: 500-Z2,  D의 Z좌표: 500+Z2 
             */
             /*
              * 두 벡터의 내적의 값 = 0
              * AB.z = z1-z2, BC.z = z1+z2
              * AB.x*BC.X + AB.y*BC.y + AB.z*BC.z = 0
              * AB.z*BC.z = a
              * a = -(AB.x*BC.X + AB.y*BC.y) == (AB.z*BC.z) == (z1^2 - z2^2)
              */
            a = -((Ppoint[2, 0] - Ppoint[1, 0]) * (Ppoint[3, 0] - Ppoint[2, 0]) + (Ppoint[2, 1] - Ppoint[1, 1]) * (Ppoint[3, 1] - Ppoint[2, 1]));
             /*
              * 두 직선의 비율이 정해져 있음.  A = B
              * 여기가 잘 기억이 나지 않음..... 여기 수정 필요.
              * i_A : b*A^2 - c*B^2 = d : i_B
              */
            b = Math.Pow(SR.size.x, 2);
            c = Math.Pow(SR.size.y, 2);
            // (BC.x^2+BC.y^2)*BC(길이)^2 - (AB.x^2+AB.y^2)*AB(길이)^2
            d = (Math.Pow((Ppoint[3, 0] - Ppoint[2, 0]), 2) + Math.Pow((Ppoint[3, 1] - Ppoint[2, 1]), 2)) * Math.Pow(SR.size.y, 2) - (Math.Pow(Ppoint[2, 0] - Ppoint[1, 0], 2) + Math.Pow(Ppoint[2, 1] - Ppoint[1, 1], 2) * Math.Pow(SR.size.x, 2));


            /*
             * 위의 벡터의 내적 
             * Texture에 나와있는 사각형을 공간상으로 가져왔을때 사각형의 모양이 최대 4가지의 경우가 생깁니다.
             * a*x^2 + b*x + c = 0  근의 공식 : (-b +- sqrt(b^2-4*a*c))/2*a
             * sqrt(b^2 - 4*a*c) 음수 2가지 0 3가지 양수 경우 4가지.
             * d^2 - b^2 - 4*c*a^2 > 0
             */
            if (Math.Pow(d, 2) - Math.Pow(b, 2) - 4 * c * Math.Pow(a, 2) > 0)
            {
                z1 = new double[4];
                z2 = new double[4];
                // 답이 4개
                z1[0] = (Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z1[1] = (Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z1[2] = (-Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z1[3] = (-Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;

                z2[0] = (-Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z2[1] = (-Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z2[2] = (Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z2[3] = (Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d - Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;

                //Debug.Log("답이 4개" + z1[0] + "," + z2[0] + "/" + z1[1] + "," + z2[1] + "/" + z1[2] + "," + z2[2] + "/" + z1[3] + "," + z2[3]);
            }
            else if (Math.Pow(d, 2) - Math.Pow(b, 2) - 4 * c * Math.Pow(a, 2) == 0)
            {
                z1 = new double[3];
                z2 = new double[3];
                // 답이 3개
                z1[0] = (Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z1[1] = 0;
                z1[2] = (-Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;

                z2[0] = (-Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z2[1] = 0;
                z2[2] = (Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;

                //Debug.Log("답이 3개" + z1[0] + "," + +z2[0] + "/" + z1[1] + "," + z2[1] + "/" + z1[2] + "," + z2[2]);
            }
            else
            {
                z1 = new double[2];
                z2 = new double[2];
                // 답이 2개
                z1[0] = (Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z1[1] = (-Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;

                z2[0] = (-Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) + a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;
                z2[1] = (Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b)) - a / Math.Sqrt((d + Math.Sqrt(Math.Pow(b, 2) + 4 * c * Math.Pow(a, 2))) / (2 * b))) / 2;

                //Debug.Log("답이 2개" + z1[0] + "," + z2[0] + "/" + z1[1] + "," + z2[1]);
            }

            /*
             * 3. 생성된 직사각형 위에 Game Object를 올려 놓습니다. 
             *  a. Mesh로 직사각형을 그린다.
             *  b. 사각형의 법선 벡터를 그린다.
             *  c. 법선 벡터를 오일러화 시켜서 Plane을 현재 위치, 현재 모양으로 돌려놓는다.
             */

            // 위에서 나오는 답안 개수에 따라 Mesh를 그린다.

            //Ppoint[1, 0], Ppoint[1, 1],500-z1
            //Ppoint[2, 0], Ppoint[2, 1],500-z2;
            //Ppoint[3, 0], Ppoint[3, 1],500+z1;
            //Ppoint[4, 0], Ppoint[4, 1],500+z2;

            for (int i = 0; i < z1.Length; i++)
            {
                x = new double[4];
                y = new double[4];
                z = new double[4];
                x[0] = Ppoint[1, 0]; x[1] = Ppoint[2, 0]; x[2] = Ppoint[3, 0]; x[3] = Ppoint[4, 0];
                y[0] = Ppoint[1, 1]; y[1] = Ppoint[2, 1]; y[2] = Ppoint[3, 1]; y[3] = Ppoint[4, 1];
                z[0] = 500 - z1[i]; z[1] = 500 - z2[i]; z[2] = 500 + z1[i]; z[3] = 500 + z2[i];
                
                Vector3 scale_a = new Vector3((float)x[0], (float)y[0], (float)z[0]);
                Vector3 scale_b = new Vector3((float)x[1], (float)y[1], (float)z[1]);
                // Mesh의 높이를 재고 Sprite Renderer인 height를 맞춤.
                double scale = Vector3.Distance(scale_a, scale_b) / SR.size.y;
                plane.transform.localScale = new Vector3((float)scale, (float)scale, (float)scale);


                // 위의 답안 중 하나만 건져서 보여준다.
                if (i == 3)
                {
                    //ShowPlane
                    ShowPlane(x, y, z, i, Cgb);
                    line.transform.parent = Cgb.transform;
                    // a(0,1), d(1,1)
                    // b(0,0), c(1,0)

                    // 여기가 벡터 외적을 통해 법선 벡터를 구함.
                    //직선 ba b(x,y,z) - a(x,y,z);  2 1
                    Vector3 side1 = new Vector3((float)(x[3] - x[1]), (float)(y[3] - y[1]), (float)(z[3] - z[1]));
                    //직선 bc b(x,y,z) - c(x,y,z);  0 1
                    Vector3 side2 = new Vector3((float)(x[0] - x[2]), (float)(y[0] - y[2]), (float)(z[0] - z[2]));
                    Vector3 NV = Vector3.Cross(side1, side2);
                    //float dist = Vector3.Distance(new Vector3(0, 0, 0), NV);

                    Vector3 meshplane = new Vector3(quad2.world_x,-quad2.world_y,500);

                    // 법선 벡터를 렌더링 한다.
                    /*
                    if (line.GetComponent<LineRenderer>() == null)
                    {
                        lineRenderer = line.AddComponent<LineRenderer>();

                        lineRenderer.SetPosition(0, meshplane);
                        lineRenderer.SetPosition(1, -NV + meshplane);
                    }
                    else
                    {
                        lineRenderer.SetPosition(0, meshplane);
                        lineRenderer.SetPosition(1, -NV + meshplane);
                    }
                    */
                    transform.rotation = Quaternion.LookRotation(NV);

                    // Mesh Plane의 중심점과 왼쪽 상단 꼭지점과 이어진 벡터
                    Vector3 checkPoint = new Vector3((float)x[0], (float)y[0], (float)z[0]);
                    Vector3 vectorA = checkPoint - meshplane;

                    // 게임 오브젝트의 중심점과 왼쪽 상단 꼭지점과 이어진 벡터
                    //x:-6.97 y:10.16
                    Vector3 vectorB = sphere.transform.position - plane.transform.position;
                    //GetAngleBetween3DVector 함수를 이용해 사잇각을 구한 다음 현재 각도에서 일부분 회전 - z축으로 회전이 되지 않아 현 시점에서 회전.
                    plane.transform.RotateAround(transform.position, NV, -GetAngleBetween3DVector(vectorA, vectorB));
                }
            }
        }
    }
}
