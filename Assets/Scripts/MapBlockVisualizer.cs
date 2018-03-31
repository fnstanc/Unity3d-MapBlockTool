using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uf {
    public class MapBlockVisualizer : MonoBehaviour {
        public const string MAPBLOCK_LAYER = "MapBlock";
        public const float GRID_SIZE = 1f;

        public Texture2D blockTexture;
        public Texture2D emptyTexture;
        public Texture2D brushTexture;

        public Vector3 mapOrigin = Vector3.zero;
        [Range(1, 16)]
        public int brushSize = 1;
        public bool brushMode = false;

        private MapBlock mapGrid_;

        public MapBlock GetMapBlock()
        {
            return mapGrid_;
        }

        public void Reload(MapBlock mg)
        {
            mapGrid_ = mg;
            if (mg.IsValid())
            {
                CreateGridObject();
                RefreshAll();
            }
            else
            {
                Debug.LogError("MapGrid is invalid");
            }
        }

        private void CreateGridObject()
        {
            Terrain[] gridTerrains = this.GetComponentsInChildren<Terrain>();
            foreach (var terrain in gridTerrains)
            {
                GameObject.DestroyImmediate(terrain.gameObject);
            }

            Terrain[] terrains = GameObject.FindObjectsOfType<Terrain>();
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogError("当前版本仅支持有地形的场景");
                return;
            }

            for (int i = 0; i < terrains.Length; ++i)
            {
                CreateSubTerrain(terrains[i]);
            }
        }

        private GameObject CreateSubTerrain(Terrain terrain)
        {
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = terrain.terrainData.heightmapResolution;
            terrainData.size = terrain.terrainData.size;
            float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth,
                                    terrain.terrainData.heightmapHeight);
            terrainData.SetHeights(0, 0, heights);

            int gridCount = (int)(terrainData.size.x / GRID_SIZE);

            terrainData.splatPrototypes = new SplatPrototype[] {
                    new SplatPrototype () {
                        texture = blockTexture,
                        tileSize = Vector2.one * GRID_SIZE
                    },
                    new SplatPrototype () {
                        texture = emptyTexture,
                        tileSize = Vector2.one * GRID_SIZE
                    },
                    new SplatPrototype () {
                        texture = brushTexture,
                        tileSize = Vector2.one * GRID_SIZE
                    },
                };

            terrainData.alphamapResolution = gridCount;

            terrainData.alphamapTextures[0].filterMode = FilterMode.Point;

            var go = Terrain.CreateTerrainGameObject(terrainData);
            go.name = "Grid";
            go.layer = LayerMask.NameToLayer(MAPBLOCK_LAYER);
            go.transform.SetParent(this.transform, false);
            go.transform.position = new Vector3(terrain.transform.position.x,
                    terrain.transform.position.y + 0.1f, terrain.transform.position.z);
            go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            var t = go.GetComponent<Terrain>();
            t.castShadows = false;

            return go;
        }


        private void RefreshAll()
        {
            Terrain[] terrains = this.GetComponentsInChildren<Terrain>();
            for (int i = 0; i < terrains.Length; ++i)
            {
                Terrain terrain = terrains[i];
                TerrainData terrainData = terrain.terrainData;
                float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
                int width = terrainData.alphamapWidth;
                int height = terrainData.alphamapHeight;

                int xoffset, yoffset;
                TileOffset(terrain, out xoffset, out yoffset);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < height; x++)
                    {
                        if (mapGrid_.IsBlocked(x + xoffset, y + yoffset))
                        {
                            splatmapData[y, x, 0] = 1f;
                            splatmapData[y, x, 1] = 0f;
                            splatmapData[y, x, 2] = 0f;
                        }
                        else
                        {
                            splatmapData[y, x, 0] = 0f;
                            splatmapData[y, x, 1] = 1f;
                            splatmapData[y, x, 2] = 0f;
                        }
                    }
                }

                terrainData.SetAlphamaps(0, 0, splatmapData);
            }
        }

        public void OnBrushed(Terrain target, int x, int y, bool brushed)
        {
            if (mapGrid_ == null)
            {
                Debug.LogErrorFormat("脚本重新编译了，请重新加载占位文件");
                return;
            }

            if (!brushMode)
                return;
            ClearLastHoveredBrush();

            int minX, minY, width, height;
            GerArea(target, x, y, out minX, out minY, out width, out height);
            int xoff, yoff;
            TileOffset(target, out xoff, out yoff);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int ox = i + minX + xoff;
                    int oy = j + minY + yoff;
                    mapGrid_.SetBlocked(ox, oy, brushed);
                }
            }

            Refresh(target, minX, minY, width, height, false);
        }

        private Terrain lastTarget_;
        private int lastHoverdX_ = -1;
        private int lastHoverdY_ = -1;

        public void OnBrushHover(Terrain target, int x, int y)
        {
            if (!brushMode)
                return;

            ClearLastHoveredBrush();
            int minX, minY, width, height;
            GerArea(target, x, y, out minX, out minY, out width, out height);
            Refresh(target, minX, minY, width, height, true);

            lastHoverdX_ = x;
            lastHoverdY_ = y;
            lastTarget_ = target;

        }

        public void ClearLastHoveredBrush()
        {
            if (lastTarget_ != null && lastHoverdX_ >= 0 && lastHoverdY_ >= 0)
            {
                int minX, minY, width, height;
                GerArea(lastTarget_, lastHoverdX_, lastHoverdY_, out minX, out minY, out width, out height);
                Refresh(lastTarget_, minX, minY, width, height, false);
                lastHoverdX_ = -1;
                lastHoverdY_ = -1;
            }
        }

        private void GerArea(Terrain target, int x, int y, out int minX, out int minY, out int width, out int height)
        {
            int size = ((brushSize * 2) - 1) / 2;
            minX = Mathf.Max(0, x - size);
            minY = Mathf.Max(0, y - size);

            int maxX = x + Mathf.Max(size, 1);
            int maxY = y + Mathf.Max(size, 1);

            maxX = Mathf.Min(maxX, target.terrainData.alphamapWidth);
            maxY = Mathf.Min(maxY, target.terrainData.alphamapHeight);

            width = Mathf.Max(maxX - minX, 1);
            height = Mathf.Max(maxY - minY, 1);
        }

        private void Refresh(Terrain target, int minX, int minY, int width, int height, bool hover)
        {
            if (mapGrid_ == null)
                return;
            int xoff, yoff;
            TileOffset(target, out xoff, out yoff);

            TerrainData terrainData = target.terrainData;
            float[,,] splatmapData = terrainData.GetAlphamaps(minX, minY, width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (hover)
                    {
                        splatmapData[j, i, 0] = 0f;
                        splatmapData[j, i, 1] = 0f;
                        splatmapData[j, i, 2] = 1f;
                    }
                    else
                    {
                        int ox = i + minX + xoff;
                        int oy = j + minY + yoff;
                        bool blocked = mapGrid_.IsBlocked(ox, oy);
                        if (blocked)
                        {
                            splatmapData[j, i, 0] = 1f;
                            splatmapData[j, i, 1] = 0f;
                            splatmapData[j, i, 2] = 0f;
                        }
                        else
                        {
                            splatmapData[j, i, 0] = 0f;
                            splatmapData[j, i, 1] = 1f;
                            splatmapData[j, i, 2] = 0f;
                        }
                    }
                   
                }
            }
            terrainData.SetAlphamaps(minX, minY, splatmapData);
        }

        void TileOffset(Terrain target, out int xoff, out int yoff)
        {
            Vector3 off = target.transform.position - mapOrigin;
            xoff = (int)(off.x / GRID_SIZE);
            yoff = (int)(off.z / GRID_SIZE);
        }
    }

}
