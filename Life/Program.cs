// Copyright 2026 UNN-IASR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using ScottPlot;

namespace Life
{
    public class Cell
    {
        public bool IsAlive { get; set; }
        public readonly List<Cell> Neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = Neighbors.Count(x => x.IsAlive);
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);

        public Board(int width, int height, int cellSize = 1, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];

            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors(); // lec03.pdf, слайд 13
            Randomize(liveDensity);
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].Neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].Neighbors.Add(Cells[x, yT]);
                    Cells[x, y].Neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].Neighbors.Add(Cells[xL, y]);
                    Cells[x, y].Neighbors.Add(Cells[xR, y]);
                    Cells[x, y].Neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].Neighbors.Add(Cells[x, yB]);
                    Cells[x, y].Neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public void Randomize(double liveDensity)
        {
            var rand = new Random();
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        public int CountAliveCells() =>
            Cells.Cast<Cell>().Count(c => c.IsAlive);

        // Поиск кластеров (связных групп живых клеток) - BFS
        public List<List<Cell>> FindClustersSimple()
        {
            var visited = new bool[Columns, Rows];
            var clusters = new List<List<Cell>>();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = new List<Cell>();
                        BFS(x, y, visited, cluster);
                        if (cluster.Count > 0)
                            clusters.Add(cluster);
                    }
                }
            }
            return clusters;
        }

        private void BFS(int startX, int startY, bool[,] visited, List<Cell> cluster)
        {
            var queue = new Queue<(int, int)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                cluster.Add(Cells[x, y]);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < Columns && ny >= 0 && ny < Rows)
                        {
                            if (Cells[nx, ny].IsAlive && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }
        }

        public void LoadPattern(string filename, int offsetX = 0, int offsetY = 0)
        {
            if (!File.Exists(filename)) return;
            var lines = File.ReadAllLines(filename);
            for (int y = 0; y < lines.Length; y++)
            {
                for (int x = 0; x < lines[y].Length; x++)
                {
                    char c = lines[y][x];
                    if (c == 'O' || c == '1' || c == '*')
                    {
                        int px = x + offsetX, py = y + offsetY;
                        if (px < Columns && py < Rows)
                            Cells[px, py].IsAlive = true;
                    }
                }
            }
        }

        public void SaveState(string filename)
        {
            var lines = new List<string>();
            for (int y = 0; y < Rows; y++)
            {
                var line = "";
                for (int x = 0; x < Columns; x++)
                    line += Cells[x, y].IsAlive ? 'O' : '.';
                lines.Add(line);
            }
            File.WriteAllLines(filename, lines);
        }

        public void LoadState(string filename)
        {
            if (!File.Exists(filename)) return;
            var lines = File.ReadAllLines(filename);
            for (int y = 0; y < Math.Min(lines.Length, Rows); y++)
            {
                for (int x = 0; x < Math.Min(lines[y].Length, Columns); x++)
                {
                    Cells[x, y].IsAlive = lines[y][x] == 'O' || lines[y][x] == '1';
                }
            }
        }

        public override string ToString()
        {
            var result = "";
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                    result += Cells[x, y].IsAlive ? "█ " : ". ";
                result += "\n";
            }
            return result;
        }
    }

    public static class Research
    {
        public static int MeasureStabilization(Board board, int maxGenerations = 200, int window = 10)
        {
            var history = new List<int>();
            for (int gen = 0; gen < maxGenerations; gen++)
            {
                int aliveCount = board.CountAliveCells();
                history.Add(aliveCount);

                if (history.Count >= window)
                {
                    var recent = history.Skip(history.Count - window);
                    if (recent.Distinct().Count() == 1)
                        return gen - window + 1;
                }
                board.Advance();
            }
            return -1;
        }

        public static string ClassifyCluster(List<Cell> cluster)
        {
            int size = cluster.Count;
            if (size == 1) return "Single";
            if (size == 2) return "Pair";
            if (size == 3) return "Triplet";
            if (size == 4) return "Quad";
            if (size >= 5 && size <= 10) return "Small";
            if (size > 10) return "Large";
            return "Unknown";
        }

        public static void GenerateDensityPlot(string dataFile, string plotFile, int width = 40, int height = 25)
        {
            var results = new List<(double density, int stabilizationGen)>();
            Console.WriteLine("Generating density plot data...");

            for (double density = 0.1; density <= 0.9; density += 0.1)
            {
                var board = new Board(width, height, 1, density);
                int stabGen = MeasureStabilization(board, 200, 10);
                results.Add((density, stabGen >= 0 ? stabGen : 200));
                Console.WriteLine($"  Density {density:F1}: stabilized at gen {results.Last().stabilizationGen}");
            }

            var lines = new List<string> { "Density,StabilizationGen" };
            foreach (var r in results)
                lines.Add($"{r.density:F1},{r.stabilizationGen}");
            File.WriteAllLines(dataFile, lines);
            PlotGraph(results, plotFile);
        }

        private static void PlotGraph(List<(double density, int stabilizationGen)> data, string outputFile)
        {
            var plt = new ScottPlot.Plot();
            double[] xs = data.Select(d => d.density).ToArray();
            double[] ys = data.Select(d => (double)d.stabilizationGen).ToArray();

            var scatter = plt.Add.Scatter(xs, ys);
            scatter.LineWidth = 2;
            scatter.MarkerSize = 8;
            scatter.Color = ScottPlot.Colors.Blue;

            plt.XLabel("Initial Density");
            plt.YLabel("Stabilization Generation");
            plt.Title("Game of Life: Stabilization Time vs Density");
            // Сетка в ScottPlot 5.x включена по умолчанию, не нужно вызывать методы

            plt.SavePng(outputFile, 800, 600);
            Console.WriteLine($"Plot saved to {outputFile}");
        }
    }

    public class Config
    {
        public int Width { get; set; } = 40;
        public int Height { get; set; } = 25;
        public int CellSize { get; set; } = 1;
        public int MaxGenerations { get; set; } = 100;
        public int StabilizationWindow { get; set; } = 10;
        public string PatternFile { get; set; } = "";
        public string StateFile { get; set; } = "";
        public double RandomDensity { get; set; } = 0.3;
        public bool SaveFinalState { get; set; } = true;
        public bool RunResearch { get; set; } = true;
    }

    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Conway's Game of Life ===\n");
            var config = LoadConfig("Data/config.json");
            var board = new Board(config.Width, config.Height, config.CellSize);

            // Загрузка начального состояния (приоритет: State > Pattern > Random)
            if (!string.IsNullOrEmpty(config.StateFile) && File.Exists(config.StateFile))
            {
                Console.WriteLine($"Loading state from {config.StateFile}...");
                board.LoadState(config.StateFile);
            }
            else if (!string.IsNullOrEmpty(config.PatternFile) && File.Exists(config.PatternFile))
            {
                Console.WriteLine($"Loading pattern from {config.PatternFile}...");
                board.LoadPattern(config.PatternFile);
            }
            else
            {
                Console.WriteLine($"Randomizing with density {config.RandomDensity}...");
                board.Randomize(config.RandomDensity);
            }

            Console.WriteLine("\nInitial state:");
            Console.WriteLine(board);
            Console.WriteLine($"Alive cells: {board.CountAliveCells()}");
            Console.WriteLine($"\nRunning simulation for {config.MaxGenerations} generations...\n");
            for (int gen = 0; gen < config.MaxGenerations; gen++)
            {
                board.Advance();
                if (gen % 10 == 0 || gen == config.MaxGenerations - 1)
                {
                    Console.WriteLine($"Generation {gen + 1}: {board.CountAliveCells()} alive");
                }
            }

            if (config.SaveFinalState)
            {
                string finalStateFile = "Data/final_state.txt";
                board.SaveState(finalStateFile);
                Console.WriteLine($"\nFinal state saved to {finalStateFile}");
            }

            // Исследования (Task 2)
            if (config.RunResearch)
            {
                Console.WriteLine("\n=== Research Results ===");
                var clusters = board.FindClustersSimple();
                Console.WriteLine($"\nClusters found: {clusters.Count}");

                var classification = new Dictionary<string, int>();
                foreach (var cluster in clusters)
                {
                    string type = Research.ClassifyCluster(cluster);
                    if (!classification.ContainsKey(type))
                        classification[type] = 0;
                    classification[type]++;
                }

                Console.WriteLine("\nCluster classification:");
                foreach (var kvp in classification)
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");

                Console.WriteLine("\nGenerating density plot...");
                Research.GenerateDensityPlot("Data/data.txt", "Data/plot.png", config.Width, config.Height);
            }

            Console.WriteLine("\n=== Simulation Complete ===");
        }

        public static Config LoadConfig(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            Console.WriteLine($"Config file {path} not found, using defaults.");
            return new Config();
        }
    }
}
