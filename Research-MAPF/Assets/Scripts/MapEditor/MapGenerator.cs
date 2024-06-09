using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PathFinding
{
    public readonly struct Cell
    {
        public readonly bool IsPassable;
        public readonly Renderer Renderer;
        public readonly Renderer EndPointRenderer;
        public readonly Color DefaultColor;

        public Cell(bool isPassable, Renderer renderer, Renderer endPointRenderer, Color defaultColor, Color color)
        {
            IsPassable = isPassable;
            Renderer = renderer;
            EndPointRenderer = endPointRenderer;
            DefaultColor = defaultColor;
            SetColor(color);
        }

        public void SetColor(Color color)
        {
            Renderer.material.color = color;
        }

        public void SetEndPoint(Color color)
        {
            EndPointRenderer.gameObject.SetActive(true);
            EndPointRenderer.material.color = color;
        }

        public void ResetColor()
        {
            Renderer.material.color = DefaultColor;
            EndPointRenderer.gameObject.SetActive(false);
        }
    }

    public class MapData
    {
        public readonly int Height;
        public readonly int Width;
        public readonly int PassableCount;
        public readonly List<Cell> Cells;

        public MapData(int height, int width, int passableCount, List<Cell> cells)
        {
            Height = height;
            Width = width;
            PassableCount = passableCount;
            Cells = cells;
        }
    }

    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private int defaultHeight;
        [SerializeField] private int defaultWidth;
        [SerializeField] private MapSaveData mapData;

        private int[,] mapIdData;
        private MapData currentMapData;
        private bool isEditMode;

        [SerializeField] private GameObject prefab;
        [SerializeField] private Color firstColor;
        [SerializeField] private Color secondColor;
        [SerializeField] private Color obstacleColor;

        [SerializeField] private TextMeshProUGUI obstacleModeText;

        public void SwitchEditMode()
        {
            isEditMode = !isEditMode;

            if (isEditMode)
            {
                mapIdData = ParseMapData();
                obstacleModeText.SetText("Export");
            }
            else
            {
                SaveCurrentMap(mapIdData);
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                obstacleModeText.SetText("Edit");
            }
        }

        private void SaveCurrentMap(int[,] mapIds)
        {
            string[] data = mapData.Data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int width = data.Length != 0 ? data[0].Length : defaultWidth;
            int height = data.Length != 0 ? data.Length : defaultHeight;

            char[] str = new char[width * height + height];
            Array.Fill(str, '.');

            for (int i = 0; i < mapIds.GetLength(0); i++)
            {
                int index = 0;
                for (int j = 0; j < mapIds.GetLength(1); j++)
                {
                    int v = mapIds[i, j];
                    index = i * width + j + i;
                    str[index] = v == 0 ? '.' : '*';
                }

                str[index + 1] = '\n';
            }

            string result = new string(str);
            mapData.SetData(result);
        }

        private int[,] ParseMapData()
        {
            string[] data = mapData.Data.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            int width = data.Length != 0 ? data[0].Length : defaultWidth;
            int height = data.Length != 0 ? data.Length : defaultHeight;

            int[,] mapIds = new int[height, width];

            for (var i = 0; i < height; i++)
            {
                var h = data[i];

                for (var j = 0; j < width; j++)
                {
                    mapIds[i, j] = h[j] == '.' ? 0 : 1;
                }
            }

            return mapIds;
        }

        public MapData Generate()
        {
            int[,] mapIds;
            int width, height;

            if (mapData.Data.Length == 0)
            {
                mapIds = new int[defaultHeight, defaultWidth];
                width = defaultWidth;
                height = defaultHeight;
                SaveCurrentMap(mapIds);
            }
            else
            {
                mapIds = ParseMapData();
                height = mapIds.GetLength(0);
                width = mapIds.GetLength(1);
            }

            transform.position = Vector3.zero;
            for (int i = 0; i < transform.childCount; i++)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            List<Cell> cells = new List<Cell>();
            bool first = true;
            int passableCount = 0;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 position = new Vector3(x, 0f, z);
                    bool isPassable = mapIds[z, x] == 0;

                    GameObject cube = Instantiate(prefab, transform);
                    Renderer rend = cube.GetComponent<Renderer>();
                    Renderer endPointRend = cube.transform.GetChild(0).GetComponent<Renderer>();
                    cube.transform.position = position;

                    Grid grid = cube.GetComponent<Grid>();
                    grid.OnSelected += SetObstacle;
                    grid.OnUnselected += UnsetObstacle;

                    Color defaultColor = (x + z) % 2 != 0 ? firstColor : secondColor;
                    Color color = isPassable ? defaultColor : obstacleColor;
                    Cell cell = new Cell(isPassable, rend, endPointRend, defaultColor, color);

                    cells.Add(cell);

                    if (isPassable)
                    {
                        passableCount++;
                    }

                    first = !first;
                }
            }

            transform.position = new Vector3(-height * 0.5f + 0.5f, 0f, -width * 0.5f + 0.5f);

            currentMapData = new MapData(height, width, passableCount, cells);

            return currentMapData;
        }

        private void SetObstacle(Vector2Int point)
        {
            if (!isEditMode)
            {
                return;
            }

            mapIdData[point.y, point.x] = 1;

            Cell cell = currentMapData.Cells[point.y * defaultWidth + point.x];
            cell.SetColor(obstacleColor);
        }

        private void UnsetObstacle(Vector2Int point)
        {
            if (!isEditMode)
            {
                return;
            }

            mapIdData[point.y, point.x] = 0;

            Cell cell = currentMapData.Cells[point.y * defaultWidth + point.x];
            cell.ResetColor();
        }

        public MapSaveData GetMapSaveData()
        {
            return mapData;
        }
    }
}