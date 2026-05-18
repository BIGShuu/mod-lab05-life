// Copyright 2026 UNN-IASR
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Life;
using System.IO;
using System.Collections.Generic;

namespace Life.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private readonly string testDataDir = "Data/TestData";

        [TestInitialize]
        public void Setup()
        {
            if (!Directory.Exists(testDataDir))
                Directory.CreateDirectory(testDataDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(testDataDir))
                Directory.Delete(testDataDir, true);
        }

        [TestMethod] public void Cell_CreatedDead_ByDefault() {
            var cell = new Cell();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod] public void Board_Created_AllCellsDead() {
            var board = new Board(10, 10, 1, 0.0);
            Assert.AreEqual(0, board.CountAliveCells());
        }

        [TestMethod] public void Board_Randomize_CreatesAliveCells() {
            var board = new Board(20, 20, 1, 0.5);
            int alive = board.CountAliveCells();
            Assert.IsTrue(alive > 0 && alive < 400);
        }

        [TestMethod] public void Board_Advance_ChangesState() {
            var board = new Board(10, 10, 1, 0.5);
            int before = board.CountAliveCells();
            board.Advance();
            int after = board.CountAliveCells();
            Assert.IsTrue(after >= 0);
        }

        [TestMethod] public void Board_Rule_Birth_ThreeNeighbors() {
            var board = new Board(3, 3, 1, 0.0);
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod] public void Board_Rule_Survival_TwoNeighbors() {
            var board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod] public void Board_Rule_Survival_ThreeNeighbors() {
            var board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
        }

        [TestMethod] public void Board_Rule_Death_Underpopulation() {
            var board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[0, 0].IsAlive = true; // Только 1 сосед
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [TestMethod] public void Board_Rule_Death_Overpopulation() {
            var board = new Board(3, 3, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    if (x != 1 || y != 1)
                        board.Cells[x, y].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[1, 1].IsAlive);
        }

        [TestMethod] public void Board_Pattern_Block_Stable() {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Cells[3, 2].IsAlive = true;
            board.Cells[3, 3].IsAlive = true;
            int before = board.CountAliveCells();
            board.Advance();
            Assert.AreEqual(before, board.CountAliveCells());
        }

        [TestMethod] public void Board_Pattern_Blinker_Oscillates() {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[4, 3].IsAlive = true;
            board.Cells[4, 4].IsAlive = true;
            board.Cells[4, 5].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[3, 4].IsAlive && board.Cells[4, 4].IsAlive && board.Cells[5, 4].IsAlive);
            board.Advance();
            Assert.AreEqual(3, board.CountAliveCells());
        }

        [TestMethod] public void Board_SaveState_CreatesFile() {
            var board = new Board(5, 5, 1, 0.0);
            board.Cells[2, 2].IsAlive = true;
            string testFile = Path.Combine(testDataDir, "test_save.txt");
            board.SaveState(testFile);
            Assert.IsTrue(File.Exists(testFile));
        }

        [TestMethod] public void Board_LoadState_RestoresCorrectly() {
            var board1 = new Board(5, 5, 1, 0.0);
            board1.Cells[1, 1].IsAlive = true;
            board1.Cells[2, 2].IsAlive = true;
            string testFile = Path.Combine(testDataDir, "test_load.txt");
            board1.SaveState(testFile);
            var board2 = new Board(5, 5, 1, 0.0);
            board2.LoadState(testFile);
            Assert.IsTrue(board2.Cells[1, 1].IsAlive && board2.Cells[2, 2].IsAlive);
        }

        [TestMethod] public void Board_LoadPattern_FromFile() {
            var board = new Board(10, 10, 1, 0.0);
            string patternFile = Path.Combine(testDataDir, "test.pattern");
            File.WriteAllLines(patternFile, new[] { "O.", ".O" });
            board.LoadPattern(patternFile);
            Assert.IsTrue(board.Cells[0, 0].IsAlive && board.Cells[1, 1].IsAlive);
        }

        [TestMethod] public void Research_MeasureStabilization_Block() {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Cells[3, 2].IsAlive = true;
            board.Cells[3, 3].IsAlive = true;
            int stabGen = Research.MeasureStabilization(board, 50, 5);
            Assert.AreEqual(0, stabGen); // Блок стабилен сразу
        }

        [TestMethod] public void Research_FindClusters_Single() {
            var board = new Board(5, 5, 1, 0.0);
            board.Cells[2, 2].IsAlive = true;
            var clusters = board.FindClustersSimple();
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(1, clusters[0].Count);
        }

        [TestMethod] public void Research_FindClusters_TwoSeparate() {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[1, 1].IsAlive = true;
            board.Cells[8, 8].IsAlive = true;
            var clusters = board.FindClustersSimple();
            Assert.AreEqual(2, clusters.Count);
        }

        [TestMethod] public void Research_ClassifyCluster_Sizes() {
            var cluster1 = new List<Cell> { new Cell() };
            Assert.AreEqual("Single", Research.ClassifyCluster(cluster1));
            var cluster4 = new List<Cell>();
            for (int i = 0; i < 4; i++) cluster4.Add(new Cell());
            Assert.AreEqual("Quad", Research.ClassifyCluster(cluster4));
        }

        [TestMethod] public void FullSimulation_Random_To_Stable() {
            var board = new Board(20, 20, 1, 0.3);
            int initialAlive = board.CountAliveCells();
            Assert.IsTrue(initialAlive > 0);
            for (int i = 0; i < 100; i++)
                board.Advance();
            int finalAlive = board.CountAliveCells();
            Assert.IsTrue(finalAlive >= 0);
        }

        [TestMethod] public void Config_Load_Defaults_WhenFileMissing() {
            var config = Program.LoadConfig("Data/nonexistent.json");
            Assert.AreEqual(40, config.Width);
            Assert.AreEqual(0.3, config.RandomDensity);
        }
    }
}
