using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


    public class StatusMonitor : MonoBehaviour
    {
        int tick = 0;
        float elapsed = 0;
        float fps = 0;

        public enum Alignment
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
        }

        public Alignment alignment = Alignment.RightTop;

        const float GUI_WIDTH = 75f;
        const float GUI_HEIGHT = 30f;
        const float MARGIN_X = 10f;
        const float MARGIN_Y = 10f;
        const float INNER_X = 8f;
        const float INNER_Y = 5f;
        const float GUI_CONSOLE_HEIGHT = 50f;

        public Vector2 offset = new Vector2(MARGIN_X, MARGIN_Y);
        public bool boxVisible = true;
        public float boxWidth = GUI_WIDTH;
        public float boxHeight = GUI_HEIGHT;
        public Vector2 padding = new Vector2(INNER_X, INNER_Y);
        public float consoleHeight = GUI_CONSOLE_HEIGHT;

        GUIStyle console_labelStyle;

        float x, y;
        Rect outer;
        Rect inner;

        float console_x, console_y;
        Rect console_outer;
        Rect console_inner;

        int oldScrWidth;
        int oldScrHeight;

        Dictionary<string, string> outputDict = new Dictionary<string, string>();
        public string consoleText;


        Dropdown Select_resolution;
        public int resolution;


        private void Select_resolutionValueChangedHandler(Dropdown target)
        {
            resolution = target.value;
            Debug.Log("selected: " + target.value);
        }

        // Start FPS Frame GUI 설정.
        // Use this for initialization
        void Start() {
            console_labelStyle = new GUIStyle();
            console_labelStyle.fontSize = 32;
            console_labelStyle.fontStyle = FontStyle.Normal;
            console_labelStyle.wordWrap = true;
            console_labelStyle.normal.textColor = Color.white;

            oldScrWidth = Screen.width;
            oldScrHeight = Screen.height;
            LocateGUI();
            Select_resolution = GameObject.Find("RequestedResolutionDropdown").GetComponent<Dropdown>();
            Select_resolution.onValueChanged.AddListener(delegate{
                Select_resolutionValueChangedHandler(Select_resolution);
            });
        }



        // 3 -> Update (FPS 계산)
        // Update is called once per frame
        void Update () {
            tick++;
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) {
                fps = tick / elapsed;
                tick = 0;
                elapsed = 0;
            }
        }

        //OnGUI는 Start, Update같은 MonoBehaviour 내부 객체
        //OnGUI 수행이 프레임 당 여러 번 호출될 수 있음을 의미합니다.
        void OnGUI () {
            if (oldScrWidth != Screen.width || oldScrHeight != Screen.height) {
                LocateGUI();
            }
            oldScrWidth = Screen.width;
            oldScrHeight = Screen.height;

            // GUI 틀 선언.
            if (boxVisible) {
                GUI.Box(outer, "");
            }

            
            GUILayout.BeginArea(inner);
            {
                GUILayout.BeginVertical();
                //화면에 찍히는 부분.
                GUILayout.Label("FPS : " + fps.ToString("F1"));
                /*
                foreach (KeyValuePair<string, string> pair in outputDict) {
                    GUILayout.Label(pair.Key + " : " + pair.Value);
                }
                */
                GUILayout.EndVertical();
            }
            GUILayout.EndArea ();

            if (!string.IsNullOrEmpty(consoleText)) {
                if (boxVisible) {
                    GUI.Box (console_outer, "");
                }

                GUILayout.BeginArea (console_inner);
                {
                    GUILayout.BeginVertical ();
                    GUILayout.Label (consoleText, console_labelStyle);
                    GUILayout.EndVertical ();
                }
                GUILayout.EndArea ();
            }
        }

        public void Add (string key, string value) {
            if (outputDict.ContainsKey (key)) {
                outputDict [key] = value;
            } else {
                outputDict.Add (key, value);
            }
        }

        public void Remove (string key) {
            outputDict.Remove (key);
        }

        public void Clear () {
            outputDict.Clear ();
        }

        //Start -> 1
        public void LocateGUI() {
            x = GetAlignedX(alignment, boxWidth);
            y = GetAlignedY(alignment, boxHeight);
            outer = new Rect(x, y, boxWidth, boxHeight);
            inner = new Rect(x + padding.x, y + padding.y, boxWidth, boxHeight);

            console_x = GetAlignedX(Alignment.LeftBottom, Screen.width);
            console_y = GetAlignedY(Alignment.LeftBottom, consoleHeight);
            console_outer = new Rect(console_x, console_y, Screen.width - offset.x*2, consoleHeight);
            console_inner = new Rect(console_x + padding.x, console_y + padding.y, Screen.width - offset.x*2 - padding.x, consoleHeight);
        }
            
        //1 -> 2
        float GetAlignedX(Alignment anchor, float w) {
            switch (anchor) {
            default:
            case Alignment.LeftTop:
            case Alignment.LeftBottom:
                return offset.x;

            case Alignment.RightTop:
            case Alignment.RightBottom:
                return Screen.width - w - offset.x;
            }
        }

        //1 -> 3
        float GetAlignedY(Alignment anchor, float h) {
            switch (anchor) {
            default:
            case Alignment.LeftTop:
            case Alignment.RightTop:
                return offset.y;

            case Alignment.LeftBottom:
            case Alignment.RightBottom:
                return Screen.height - h - offset.y;
            }
        }
    }
