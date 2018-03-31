using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;

namespace uf {
	[CustomEditor (typeof(MapBlockVisualizer))]
	public class MapBlockVisualizerInspector : Editor
	{
        private static readonly string GridResPath = "Assets/Scripts";
        public const string OBJECT_NAME = "MapBlockVisualizer";

        private MapBlockVisualizer mapBlockVisualizer = null;
        private int width_ = 256;
        private int height_ = 256;

        void OnEnable ()
		{
            mapBlockVisualizer = target as MapBlockVisualizer;
		}

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            MapBlock curr = mapBlockVisualizer.GetMapBlock();
            if (curr != null)
            {
                EditorGUILayout.LabelField(string.Format("当前网格 {0}", curr.name));
                EditorGUILayout.LabelField(string.Format("当前网格宽度 {0}", curr.Width));
                EditorGUILayout.LabelField(string.Format("当前网格高度 {0}", curr.Height));
            }

            EditorGUILayout.Space();
            GUILayout.BeginVertical("Box");

            width_ = EditorGUILayout.IntField("网格宽度", width_);
            height_ = EditorGUILayout.IntField("网格高度", height_);

            if (GUILayout.Button("创建网格"))
            {
                if (width_ < 16)
                {
                    width_ = 16;
                }
                if (height_ < 16)
                {
                    height_ = 16;
                }
                MapBlock mb = new MapBlock();
                mb.Init(width_, height_);
                mapBlockVisualizer.Reload(mb);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("加载数据"))
            {
                string initdir = PlayerPrefs.GetString("mbpath", Application.dataPath);
                string path = EditorUtility.OpenFilePanel("加载数据", initdir, "bytes");
                if (path != string.Empty)
                {
                    if (File.Exists(path))
                    {
                        FileStream fs = File.OpenRead(path);
                        BinaryReader reader = new BinaryReader(fs);
                        MapBlock mb = new MapBlock();
                        if (mb.Load(reader))
                        {
                            mb.name = path;
                            mapBlockVisualizer.Reload(mb);
                        }
                        reader.Close();
                        fs.Close();
                    }
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("保存文件"))
            {
                MapBlock mb = mapBlockVisualizer.GetMapBlock();
                if (mb != null)
                {
                    string path = mb.name;
                    if (path == string.Empty)
                    {
                        string initdir = PlayerPrefs.GetString("mbpath", Application.dataPath);
                        path = EditorUtility.SaveFilePanel("保存文件", initdir, "a.bytes", "bytes");
                    }
                    if (path != string.Empty)
                    {
                        var fs = File.OpenWrite(path);
                        var bw = new BinaryWriter(fs);
                        mb.Save(bw);
                        bw.Close();
                        fs.Close();
                        mb.name = path;
                        Debug.LogWarningFormat("保存成功 {0}", path);
                    }
                }
            }

            GUILayout.EndVertical();

        }

        Vector3 lastDragPosition = Vector3.zero;
        void OnSceneGUI()
        {
            if (Event.current == null)
            {
                return;
            }
            Event e = Event.current;

            if (Tools.viewTool != ViewTool.Orbit && e.button == 0)
            {
                if (e.type == EventType.mouseMove || e.type == EventType.mouseDown || e.type == EventType.mouseUp || e.type == EventType.mouseDrag)
                {
                    Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    RaycastHit hitInfo;
                    if (Physics.Raycast(worldRay, out hitInfo, 10000, 1 << LayerMask.NameToLayer(MapBlockVisualizer.MAPBLOCK_LAYER)))
                    {
                        Vector3 position = hitInfo.point - hitInfo.collider.transform.position;

                        Terrain target = hitInfo.collider.GetComponent<Terrain>();

                        int x = (int)(position.x / MapBlockVisualizer.GRID_SIZE);
                        int y = (int)(position.z / MapBlockVisualizer.GRID_SIZE);

                        bool brushed = e.control == false;
                        if (e.type == EventType.mouseDown)
                        {
                            mapBlockVisualizer.OnBrushed(target, x, y, brushed);
                        }
                        else
                        {
                            if (Vector3.Distance(lastDragPosition, position) >= MapBlockVisualizer.GRID_SIZE / 2f)
                            {
                                if (e.type == EventType.mouseDrag)
                                {
                                    mapBlockVisualizer.OnBrushed(target, x, y, brushed);
                                }
                                else if (e.type == EventType.mouseMove)
                                {
                                    mapBlockVisualizer.OnBrushHover(target, x, y);
                                }
                                lastDragPosition = position;
                            }
                        }
                    }
                    else
                    {
                        mapBlockVisualizer.ClearLastHoveredBrush();
                    }
                }
            }


            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }

        }

        [MenuItem("Tool/MapBlock Tool")]
        static void LoadGrid()
        {
            var target = GameObject.Find(OBJECT_NAME);
            if (target != null)
            {
                GameObject.DestroyImmediate(target);
            }

            var go = new GameObject(OBJECT_NAME);
            go.hideFlags = HideFlags.DontSave;
            go.transform.position = Vector3.zero;
            MapBlockVisualizer mbv = go.AddComponent<MapBlockVisualizer>();
            mbv.blockTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GridResPath + "/red.png");
            mbv.emptyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GridResPath + "/white.png");
            mbv.brushTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GridResPath + "/brush.png");
        }
    }
}