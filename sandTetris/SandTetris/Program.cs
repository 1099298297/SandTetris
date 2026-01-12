using System;
using System.Drawing;
using System.Windows.Forms;

namespace SandTetris
{
    public partial class SandTetrisGame : Form {

        private const int CELL_SIZE = 10;
        private const int ROW_COUNT = 40;
        private const int COL_COUNT = 20;
        private const int FALL_SPEED = 300;

        private Color[,] sandGrid;
        private Timer gameTimer;
        private Point[] currentShape;
        private Color currentShapeColor;
        private int currentShapeX;
        private int currentShapeY;

        private readonly Point[][] shapeLibrary = new Point[][] {
            new[] { new Point(-1,0), new Point(0,0), new Point(1,0), new Point(2,0) }, // I型
            new[] { new Point(-1,0), new Point(0,0), new Point(1,0), new Point(0,1) }, // T型
            new[] { new Point(-1,0), new Point(0,0), new Point(0,1), new Point(1,1) }, // Z型
            new[] { new Point(0,0), new Point(1,0), new Point(-1,1), new Point(0,1) }, // S型
            new[] { new Point(-1,0), new Point(0,0), new Point(1,0), new Point(1,1) }, // L型
            new[] { new Point(-1,0), new Point(0,0), new Point(1,0), new Point(-1,1) }, // J型
            new[] { new Point(0,0), new Point(1,0), new Point(0,1), new Point(1,1) }  // 田字型
        };

        private readonly Color[] colorLibrary = new Color[] {
            Color.Red,Color.LimeGreen,Color.Blue,Color.Orange,Color.Yellow,Color.Purple,Color.Pink
        };

        public SandTetrisGame() {
            InitGame();
        }
        public void InitGame() {
            this.Size = new Size(COL_COUNT * CELL_SIZE + 20,ROW_COUNT * CELL_SIZE + 40);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MinimizeBox = false;
            this.Text = "SandTetris";

            sandGrid = new Color[ROW_COUNT,COL_COUNT];
            for (int row=0;row<ROW_COUNT;row++) {
                for (int col = 0; col < COL_COUNT; col++) { 
                    sandGrid[row,col] = Color.Empty;
                }
            }

            gameTimer = new Timer();
            gameTimer.Interval = FALL_SPEED;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            SpawnNewShape();
        }

        private void SpawnNewShape() { 
            Random rand = new Random();
            int randomShapeIndex = rand.Next(shapeLibrary.Length);
            currentShape = shapeLibrary[randomShapeIndex];
            int randomColorIndex = rand.Next(colorLibrary.Length);
            currentShapeColor = colorLibrary[randomColorIndex];
            currentShapeX = COL_COUNT / 2;
            currentShapeY = 2;
        }
    }
}
