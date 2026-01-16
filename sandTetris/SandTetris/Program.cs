using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public partial class MyForm : Form
{
    private const int CELL_SIZE = 4;
    private const int COL_COUNT = 100;
    private const int ROW_COUNT = 120;
    private const int CELL_MAGNIFY = 8;
    private const int DEFAULT_FALL_INTERVAL = 100;
    private const int DEFAULT_FLOW_INTERVAL = 100;
    private const int SPEED_UP_MULTIPLE = 3; // 加速倍率


    private Color[,] sandGrid;// 已经落地的沙块，也是整个画布
    private Color currentColor;//现在的颜色
    private bool[,] currentPattern;//现在的形状
    private Color[,] currentSandBlock = new Color[0, 0]; //现在下落的沙块
    private int sandBlockX; // 横
    private int sandBlockY; // 纵
    private int score;
    private int fall_interval = DEFAULT_FALL_INTERVAL; // 下落默认速度
    private int flow_interval = DEFAULT_FLOW_INTERVAL; // 流沙默认速度
    private bool isSpeedUp = false;

    private bool isGameOver = false;
    private Timer fallTimer; //下落计时器
    private Timer flowTimer;
    private Random random;


    private Color[] sandColors = new Color[] {
        Color.Red, Color.Green, Color.Blue,
    };
    private readonly bool[][,] patterns = new bool[][,]
        {
            new bool[,]{{false,true,true},{true,true,false}},
            new bool[,]{{true,false},{true,false},{true,true}},
            new bool[,]{{true,true,true},{false,true,false}},
            new bool[,]{{true},{true},{true},{true}}
        };

    public MyForm()
    {
        random = new Random();
        this.Size = new Size(COL_COUNT * CELL_SIZE + 20, ROW_COUNT * CELL_SIZE + 40); // 网格数 × 沙粒像素
        this.Text = $"流沙俄罗斯方块 | 得分：{score} | ← →移动 ↓速降";
        // 防闪屏
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                      ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);

        this.AutoScroll = true;  // 窗体自动显示滚动条
        this.AutoScrollMinSize = new Size(COL_COUNT * CELL_SIZE, ROW_COUNT * CELL_SIZE);
        this.ResizeRedraw = true; // 窗体缩放时自动重绘

        //初始化sandGrid
        sandGrid = new Color[ROW_COUNT, COL_COUNT];
        for (int r = 0; r < ROW_COUNT; r++)
            for (int c = 0; c < COL_COUNT; c++)
                sandGrid[r, c] = Color.Empty;
        //键盘监听
        this.KeyDown += MyForm_KeyDown;
        this.KeyUp += MyForm_KeyUp;
        this.MouseClick += MyForm_MouseClick;
        // 下落定时器
        fallTimer = new Timer { Interval = fall_interval };
        fallTimer.Tick += fallTimer_Tick;
        fallTimer.Start();
        // 流动定时器
        flowTimer = new Timer { Interval = flow_interval };
        flowTimer.Tick += flowTimer_Tick;
        flowTimer.Start();

        //生成第一个沙块
        spawnSandBlock();


    }

    private void setCurrentSandBlock() {
        int c_row = currentPattern.GetLength(0);
        int c_col = currentPattern.GetLength(1);
        currentSandBlock = new Color[c_row* CELL_MAGNIFY, c_col* CELL_MAGNIFY];
        for (int i = 0; i < c_row; i++) {
            for (int j = 0; j < c_col; j++) {
                if (currentPattern[i, j] == true)
                {
                    for (int b_i = 0; b_i < CELL_MAGNIFY; b_i++)
                    {
                        for (int b_j = 0; b_j < CELL_MAGNIFY; b_j++)
                        {
                            currentSandBlock[i * CELL_MAGNIFY + b_i, j * CELL_MAGNIFY + b_j] = currentColor;
                        }
                    }
                }
                else {
                    for (int b_i = 0; b_i < CELL_MAGNIFY; b_i++)
                    {
                        for (int b_j = 0; b_j < CELL_MAGNIFY; b_j++)
                        {
                            currentSandBlock[i * CELL_MAGNIFY + b_i, j * CELL_MAGNIFY + b_j] = Color.Empty;
                        }
                    }
                }
            }
        }

    }

    private void spawnSandBlock() {
        currentColor = sandColors[random.Next(sandColors.Length)];
        currentPattern = patterns[random.Next(patterns.Length)];
        setCurrentSandBlock();

        int centerCol = COL_COUNT / 2;
        int startRow = 1;

        sandBlockX = centerCol - (currentSandBlock.GetLength(1) / 2); ;
        sandBlockY = startRow;
        sandBlockX = Math.Max(0, sandBlockX);// 防止沙块X坐标越界（左边超出窗体）
        if (IsCollided())
        {
            isGameOver = true;
            fallTimer.Stop(); //停止下落
            flowTimer.Stop(); //停止流动
            MessageBox.Show($"游戏结束！\n你的最终得分：{score}", "流沙俄罗斯方块", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void fallTimer_Tick(object sender, EventArgs e)
    {
        if (isGameOver) return;
        sandBlockY++;
        if (IsCollided()) {
            sandBlockY--;
            AddBlockToGrid();
            //
            spawnSandBlock();
        }
        this.Invalidate();
    }

    private void flowTimer_Tick(object sender, EventArgs e) {
        if (isGameOver) return;
        DoSandFall();
        // 流动完成，检测连通
        if (CheckAndEliminateSameColor())
        {
            this.Text = $"流沙俄罗斯方块 | 得分：{score} | ←→移动 ↓速降 4连消除";
            this.Invalidate();
        }
    }
    private bool CheckAndEliminateSameColor() {
        bool isEliminate = false;//是否存在贯通
        Color lastColor = Color.Empty;
        bool[,] visited = new bool[ROW_COUNT, COL_COUNT];
        for (int row = 0; row < ROW_COUNT; row++) {
            if (sandGrid[row, 0] != Color.Empty && sandGrid[row, 0] != lastColor) { 
                lastColor = sandGrid[row, 0];
                Color targetColor = lastColor;
                Queue<Point> queue = new Queue<Point> ();
                List<Point> sameColorList = new List<Point> ();

                queue.Enqueue(new Point(0, row));
                visited[row,0] = true;
                sameColorList.Add(new Point(0,row));
                bool isCrossScreen = false;//该点开始，是否贯通
                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };

                while (queue.Count > 0) { 
                    Point p = queue.Dequeue ();
                    int p_col = p.X;
                    int p_row = p.Y;
                    if (p_col == COL_COUNT - 1) { 
                        isCrossScreen = true;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        int newCol = p_col + dx[i];
                        int newRow = p_row + dy[i];
                        // 边界合法 + 未被访问 + 和目标沙粒同色 → 加入队列继续检测
                        if (newRow >= 0 && newRow < ROW_COUNT && newCol >= 0 && newCol < COL_COUNT
                            && !visited[newRow, newCol] && sandGrid[newRow, newCol] == targetColor)
                        {
                            visited[newRow, newCol] = true;
                            queue.Enqueue(new Point(newCol, newRow));
                            sameColorList.Add(new Point(newCol, newRow));
                        }
                    }
                }
                if (isCrossScreen)
                {
                    int eliminateCount = 0;
                    // 遍历所有连通的同色沙粒，置空消除
                    foreach (Point p in sameColorList)
                    {
                        sandGrid[p.Y, p.X] = Color.Empty;
                        eliminateCount++;
                    }
                    score += eliminateCount; // 消除数量 = 加分数量
                    isEliminate = true;
                }
            }
        }
        return isEliminate;
    }
    private bool IsCollided() {
        int blockH = currentSandBlock.GetLength(0);
        int blockW = currentSandBlock.GetLength(1);

        for (int r = 0; r < blockH; r++)
        {
            for (int c = 0; c < blockW; c++)
            {
                if (currentSandBlock[r, c] != Color.Empty)
                {
                    int realRow = sandBlockY + r;
                    int realCol = sandBlockX + c;

                    if (realRow >= ROW_COUNT) return true;
                    if (realCol < 0 || realCol >= COL_COUNT) return true;
                    if (realRow >= 0 && sandGrid[realRow, realCol] != Color.Empty) return true;
                }
            }
        }
        return false;
    }

    private void DoSandFall()
    {
        Color[,] newSandGrid = (Color[,])sandGrid.Clone(); //克隆网格，防止帧内互相干扰
        bool isGridChanged = false;

        for (int row = ROW_COUNT - 2; row >= 0; row--)
        {
            for (int col = 0; col < COL_COUNT; col++)
            {
                if (sandGrid[row, col] == Color.Empty) continue; //空位置跳过
                Color currentSandColor = sandGrid[row, col];
                //如果下方是空的
                if (newSandGrid[row + 1, col] == Color.Empty)
                {
                    newSandGrid[row, col] = Color.Empty;
                    newSandGrid[row + 1, col] = currentSandColor;
                    isGridChanged = true;
                }
                //下方不空，要看能不能向两侧流动
                else {
                    bool canFlowLeftDown = col > 0 && newSandGrid[row + 1, col - 1] == Color.Empty;
                    bool canFlowRightDown = col < COL_COUNT - 1 && newSandGrid[row + 1, col + 1] == Color.Empty;
                    if (canFlowLeftDown && canFlowRightDown) {
                        // 随机选方向流
                        if (random.Next(2) == 0)
                        {
                            newSandGrid[row, col] = Color.Empty;
                            newSandGrid[row + 1, col - 1] = currentSandColor;
                        }
                        else
                        {
                            newSandGrid[row, col] = Color.Empty;
                            newSandGrid[row + 1, col + 1] = currentSandColor;
                        }
                        isGridChanged = true;
                    }
                    else if (canFlowLeftDown)
                    {
                        newSandGrid[row, col] = Color.Empty;
                        newSandGrid[row + 1, col - 1] = currentSandColor;
                        isGridChanged = true;
                    }
                    else if (canFlowRightDown)
                    {
                        newSandGrid[row, col] = Color.Empty;
                        newSandGrid[row + 1, col + 1] = currentSandColor;
                        isGridChanged = true;
                    }
                }
            }
        }
        // 有变化才重绘
        if (isGridChanged)
        {
            sandGrid = newSandGrid;
            this.Invalidate();
        }
    }

    private void AddBlockToGrid() {
        int blockH = currentSandBlock.GetLength(0);
        int blockW = currentSandBlock.GetLength(1);

        for (int r = 0; r < blockH; r++)
        {
            for (int c = 0; c < blockW; c++)
            {
                if (currentSandBlock[r, c] != Color.Empty)
                {
                    int realRow = sandBlockY + r;
                    int realCol = sandBlockX + c;
                    if (realRow >= 0 && realCol >= 0 && realRow < ROW_COUNT && realCol < COL_COUNT)
                    {
                        sandGrid[realRow, realCol] = currentColor;
                    }
                }
            }
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MyForm());
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); 

        Graphics g = e.Graphics;

        Pen canvasPen = new Pen(Color.Black, 1); // 画布边框
        Rectangle canvasRect = new Rectangle(0, 0, COL_COUNT * CELL_SIZE - 1, ROW_COUNT * CELL_SIZE - 1);
        g.DrawRectangle(canvasPen, canvasRect);

        Pen blackPen = new Pen(Color.LightGray, 1);
        // 绘制sandGrid
        for (int row = 0; row < ROW_COUNT; row++)
        {
            for (int col = 0; col < COL_COUNT; col++)
            {
                if (sandGrid[row, col] != Color.Empty)
                {
                    int drawX = col * CELL_SIZE; 
                    int drawY = row * CELL_SIZE; 
                    Rectangle sandRect = new Rectangle(drawX, drawY, CELL_SIZE, CELL_SIZE);
                    g.FillRectangle(new SolidBrush(sandGrid[row, col]), sandRect);
                    g.DrawRectangle(blackPen, sandRect);
                }
            }
        }
        // 绘制下落沙块
        int blockHeight = currentSandBlock.GetLength(0);
        int blockWidth = currentSandBlock.GetLength(1);  

        for (int row = 0; row < blockHeight; row++)
        {
            for (int col = 0; col < blockWidth; col++)
            {
                if (currentSandBlock[row, col] != Color.Empty)
                {
                    int drawX = (sandBlockX + col) * CELL_SIZE;
                    int drawY = (sandBlockY + row) * CELL_SIZE;
                    Rectangle sandRect = new Rectangle(drawX, drawY, CELL_SIZE, CELL_SIZE);
                    g.FillRectangle(new SolidBrush(currentSandBlock[row, col]), sandRect);
                    g.DrawRectangle(blackPen, sandRect); // 边框
                }
            }
        }

    }

    private void MyForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (isGameOver) return;
        if (e.KeyCode == Keys.Left)
        {
            sandBlockX -= 1;
            if (IsCollided()) sandBlockX += 1;
            sandBlockX = Math.Max(0, sandBlockX);
            this.Invalidate();
        }
        if (e.KeyCode == Keys.Right)
        {
            sandBlockX += 1; //
            if (IsCollided()) sandBlockX -= 1;
            sandBlockX = Math.Min(COL_COUNT - currentSandBlock.GetLength(1), sandBlockX);
            Console.WriteLine(sandBlockX);
            this.Invalidate();
        }
        if (e.KeyCode == Keys.Down && !isSpeedUp)
        {
            isSpeedUp = true;
            fallTimer.Interval = fall_interval / SPEED_UP_MULTIPLE; // 下落加速
            flowTimer.Interval = flow_interval / SPEED_UP_MULTIPLE; // 流沙加速
        }
    }

    private void MyForm_KeyUp(object sender, KeyEventArgs e)
    {
        if (isGameOver) return;
        if (e.KeyCode == Keys.Down && isSpeedUp)
        {
            isSpeedUp = false;
            fallTimer.Interval = fall_interval;
            flowTimer.Interval = flow_interval;
        }
    }

    private void MyForm_MouseClick(object sender, MouseEventArgs e)
    {
        //x = e.X;
        //y = e.Y;
        this.Invalidate();
    }
}