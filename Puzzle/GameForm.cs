using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Media;
using System.IO;
using System.Windows.Forms;

namespace Puzzle
{
    public class GameForm : Form
    {
        private PuzzleGame game;
        private Bitmap puzzleImage;
        private Timer gameTimer;
        private int gridSize;

        private string currentUsername;
        private int currentUserID;

        // Для статистики
        private DateTime gameStartTime;
        private int moveCount;
        private int score;

        // Текстовое поле сложности
        private string difficultyText;

        // UI элементы
        private Label movesLabel;
        private Label timeLabel;
        private Label scoreLabel;

        private string connectionString;

        private int puzzleID;
        private string puzzleTitle;

        public GameForm(Bitmap image, int size, string username, int userID, int puzzleID, string puzzleTitle)
        {
            puzzleImage = ResizeImage(image);
            gridSize = size;
            currentUsername = username;
            currentUserID = userID;
            this.puzzleID = puzzleID;
            this.puzzleTitle = puzzleTitle;

            connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database=puzzleGame;
                          Integrated Security=True;TrustServerCertificate=True;";

            // Определяем текст сложности сразу
            switch (gridSize)
            {
                case 4: difficultyText = "Легко"; break;
                case 7: difficultyText = "Средне"; break;
                case 10: difficultyText = "Сложно"; break;
                default: difficultyText = "Легко"; break;
            }

            InitializeComponents();
            SetupGameUI();
            StartNewGame();
        }

        private Bitmap ResizeImage(Image image)
        {
            int targetHeight = 500;
            double ratio = (double)image.Width / image.Height;
            int targetWidth = (int)(targetHeight * ratio);
            var resizedImage = new Bitmap(targetWidth, targetHeight);
            using (var g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, targetWidth, targetHeight);
            }
            return resizedImage;
        }

        private void InitializeComponents()
        {
            this.Text = $"Пазл: {puzzleTitle} - Игрок: {currentUsername}";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // UI лейблы для статистики
            movesLabel = new Label { Location = new Point(20, 20), Size = new Size(200, 20), Text = "Ходы: 0" };
            timeLabel = new Label { Location = new Point(20, 50), Size = new Size(200, 20), Text = "Время: 0 сек" };
            scoreLabel = new Label { Location = new Point(20, 80), Size = new Size(200, 20), Text = "Очки: 0" };

            this.Controls.Add(movesLabel);
            this.Controls.Add(timeLabel);
            this.Controls.Add(scoreLabel);

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.KeyDown += GameForm_KeyDown;

            // Таймер
            gameTimer = new Timer { Interval = 100 };
            gameTimer.Tick += GameTimer_Tick;

            // События мыши
            this.MouseDown += GameForm_MouseDown;
            this.MouseMove += GameForm_MouseMove;
            this.MouseUp += GameForm_MouseUp;
        }

        private void SetupGameUI()
        {
            game = new PuzzleGame(puzzleImage, gridSize);

            int formWidth = game.Cols * game.PieceWidth + 300;
            int formHeight = game.Rows * game.PieceHeight + 100;
            this.ClientSize = new Size(Math.Max(formWidth, 800), Math.Max(formHeight, 700));
        }

        private void StartNewGame()
        {
            moveCount = 0;
            score = 0;
            gameStartTime = DateTime.Now;

            if (puzzleImage != null)
            {
                game = new PuzzleGame(puzzleImage, gridSize);
                game.StartMusic();
                gameTimer.Start();
                UpdateStatsLabels();
                this.Invalidate();
            }
        }

        private void UpdateStatsLabels()
        {
            movesLabel.Text = $"Ходы: {moveCount}";
            timeLabel.Text = $"Время: {(DateTime.Now - gameStartTime).TotalSeconds:F1} сек";
            scoreLabel.Text = $"Очки: {score}";
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (game == null) return;

            if (game.IsCompleted)
            {
                gameTimer.Stop();
                game.StopMusic();

                TimeSpan timeTaken = DateTime.Now - gameStartTime;

                int baseScore;
                switch (difficultyText)
                {
                    case "Легко": baseScore = 1000; break;
                    case "Средне": baseScore = 2000; break;
                    case "Сложно": baseScore = 3000; break;
                    default: baseScore = 1000; break;
                }

                score = Math.Max(0, baseScore - (int)Math.Round(timeTaken.TotalSeconds) - moveCount * 10);
                UpdateStatsLabels();

                string soundFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Puzzle", "Assets", "Sounds");
                string winSoundPath = Path.Combine(soundFolder, "win.wav");

                if (File.Exists(winSoundPath))
                {
                    SoundPlayer player = new SoundPlayer(winSoundPath);
                    player.Play();
                }

                MessageBox.Show($"Пазл собран!\nСложность: {difficultyText}\nВремя: {timeTaken.TotalSeconds:F1} сек\nХоды: {moveCount}\nОчки: {score}");

                SaveGameResult(timeTaken, moveCount, score);
            }
            else
            {
                UpdateStatsLabels();
            }

            this.Invalidate();
        }

        private void GameForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (game == null || e.Button != MouseButtons.Left) return;

            Point p = TranslatePointToGame(e.Location);
            game.SelectPiece(p);
            this.Invalidate();
        }

        private void GameForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (game == null || e.Button != MouseButtons.Left) return;

            Point p = TranslatePointToGame(e.Location);
            game.MoveSelectedPiece(p);
            this.Invalidate();
        }

        private void GameForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (game == null || e.Button != MouseButtons.Left) return;

            game.DropPiece();
            moveCount++;
            UpdateStatsLabels();
            this.Invalidate();
        }

        private Point TranslatePointToGame(Point mousePoint)
        {
            int offsetX = (this.ClientSize.Width - game.Cols * game.PieceWidth) / 2;
            int offsetY = (this.ClientSize.Height - game.Rows * game.PieceHeight) / 2;
            return new Point(mousePoint.X - offsetX, mousePoint.Y - offsetY);
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (game == null) return;

            if (e.KeyCode == Keys.R)
            {
                StartNewGame();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (game == null) return;

            int offsetX = (this.ClientSize.Width - game.Cols * game.PieceWidth) / 2;
            int offsetY = (this.ClientSize.Height - game.Rows * game.PieceHeight) / 2;

            e.Graphics.TranslateTransform(offsetX, offsetY);
            game.Draw(e.Graphics);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            gameTimer?.Stop();
            game?.StopMusic();
            puzzleImage?.Dispose();
            base.OnFormClosed(e);
        }

        private void SaveGameResult(TimeSpan timeTaken, int moves, int score)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Records 
                    (UserID, PuzzleID, Score, TimeSpent, MovesCount, DifficultyLevel)
                     VALUES (@UserID, @PuzzleID, @Score, @TimeSpent, @MovesCount, @DifficultyLevel)";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", currentUserID);
                        cmd.Parameters.AddWithValue("@PuzzleID", puzzleID);
                        cmd.Parameters.AddWithValue("@Score", score);
                        cmd.Parameters.AddWithValue("@TimeSpent", (int)Math.Round(timeTaken.TotalSeconds));
                        cmd.Parameters.AddWithValue("@MovesCount", moves);
                        cmd.Parameters.AddWithValue("@DifficultyLevel", difficultyText);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения результата: {ex.Message}");
            }
        }
    }
}