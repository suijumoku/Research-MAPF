﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathFinding
{
    public class GridGraphMediator : MonoBehaviour
    {
        public List<Color> Colors => colors;

        [SerializeField] private List<Color> colors;
        [SerializeField] private Color pathColor;
        [SerializeField] private MapGenerator mapGenerator;

        private MapData mapData;
        private Dictionary<int, int> nodeIndexList;
        private Dictionary<int, int> indexNodeList;
        private Graph graph;

        private readonly Vector2Int[] direction = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1)
        };

        public bool Initialize()
        {
            mapData = mapGenerator.Generate();
            nodeIndexList = CreateNodeIndexList();
            indexNodeList = nodeIndexList.ToDictionary(x => x.Value, x => x.Key);
            return ValidateEndPoints();
        }

        private bool ValidateEndPoints()
        {
            var endPoints = mapGenerator.GetMapSaveData().EndPoints;
            bool isUniqueStarts = !endPoints.GroupBy(p => p.Start).SelectMany(g => g.Skip(1)).Any();
            bool isUniqueGoals = !endPoints.GroupBy(p => p.Goal).SelectMany(g => g.Skip(1)).Any();
            bool isUniquePoints = isUniqueStarts && isUniqueGoals;

            if (!isUniquePoints)
            {
                Debug.LogError("スタートとゴールのデータが重複しています。");
            }

            return isUniquePoints;
        }

        public IReadOnlyList<EndPoint> GetEndPoints()
        {
            return mapGenerator.GetMapSaveData().EndPoints;
        }

        public void PaintPath(Agent agent, List<int> path)
        {
            IReadOnlyList<EndPoint> endPoints = GetEndPoints();
            int[] endPointNodes = endPoints.Select(point => GetNode(point.Start))
                .Concat(endPoints.Select(point => GetNode(point.Goal)))
                .ToArray();

            //スタートとゴールに色付け
            GetCell(path[0]).SetEndPoint(colors[agent.Index]);
            GetCell(path[path.Count - 1]).SetEndPoint(colors[agent.Index]);

            //パスに色付け
            for (var i = 1; i < path.Count - 1; i++)
            {
                var node = path[i];

                if (!endPointNodes.Contains(node))
                {
                    Cell cell = GetCell(node);
                    cell.SetColor(pathColor);
                }
            }
        }

        public int GetNode(int index)
        {
            return indexNodeList[index];
        }

        public int GetNode(Vector2Int pos)
        {
            int index = pos.y * mapData.Width + pos.x;
            return indexNodeList[index];
        }

        public int GetIndex(int node)
        {
            return nodeIndexList[node];
        }

        public Vector2Int GetPos(int node)
        {
            int index = nodeIndexList[node];
            return new Vector2Int(index % mapData.Width, index / mapData.Width);
        }

        public Cell GetCell(int node)
        {
            return mapData.Cells[GetIndex(node)];
        }

        private Dictionary<int, int> CreateNodeIndexList()
        {
            Dictionary<int, int> list = new Dictionary<int, int>();

            int nodeCount = 0;
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    int index = y * mapData.Width + x;
                    Cell cell = mapData.Cells[index];

                    if (cell.IsPassable)
                    {
                        list.Add(nodeCount++, index);
                    }
                }
            }

            return list;
        }

        public Graph ConstructGraph()
        {
            graph = new Graph(mapData.PassableCount);

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    int fromIndex = y * mapData.Width + x;
                    Cell from = mapData.Cells[fromIndex];

                    //障害物のバスならNodeを作らない
                    if (!from.IsPassable)
                    {
                        continue;
                    }

                    //４方向にEdgeをつなぐ
                    foreach (Vector2Int dir in direction)
                    {
                        Vector2Int pos = new Vector2Int(x, y) + dir;
                        bool isConnectable = 0 <= pos.x && pos.x < mapData.Width && 0 <= pos.y && pos.y < mapData.Height;

                        if (isConnectable)
                        {
                            int toIndex = pos.y * mapData.Width + pos.x;
                            Cell to = mapData.Cells[toIndex];

                            if (to.IsPassable)
                            {
                                graph.AddEdge(GetNode(fromIndex), GetNode(toIndex));
                            }
                        }
                    }
                }
            }

            return graph;
        }
    }
}