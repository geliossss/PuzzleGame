using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace Puzzle
{
    public partial class PuzzleForm : Form
    {
        private Bitmap selectedImage;
        private string[] imagePaths;
        private int gridSize = 4;

        private Button startGameBtn;
        private Button recordsBtn;
        private FlowLayoutPanel imagesFlowPanel;
        private Label titleLabel;

        private PuzzleItem selectedPuzzle;

        public int UserID { get; private set; }

        private SoundPlayer clickSound;
        private SoundPlayer startSound;
        private SoundPlayer errorSound;
        private SoundPlayer selectSound;

        public PuzzleForm()
        {
            InitializeComponent();
            InitializeMainMenu();
            LoadSounds(); 
        }

        private void LoadSounds()
        {
            string soundFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Puzzle", "Assets", "Sounds");

            clickSound = new SoundPlayer(Path.Combine(soundFolder, "click.wav"));
            startSound = new SoundPlayer(Path.Combine(soundFolder, "start.wav"));
            errorSound = new SoundPlayer(Path.Combine(soundFolder, "error.wav"));
            selectSound = new SoundPlayer(Path.Combine(soundFolder, "select.wav"));

            try
            {
                clickSound.LoadAsync();
                startSound.LoadAsync();
                errorSound.LoadAsync();
                selectSound.LoadAsync();
            }
            catch { }
        }

        private void InitializeMainMenu()
        {
            this.Text = "Пазл - Выбор изображения и сложности";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            LoadImagesFromFolder();
            SetupMainMenuUI();
        }

        private void SetupMainMenuUI()
        {
            titleLabel = new Label
            {
                Text = "Выберите изображение и сложность",
                Font = new Font("Arial", 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            var difficultyPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50
            };

            var difficultyLabel = new Label
            {
                Text = "Сложность:",
                Font = new Font("Arial", 10),
                Location = new Point(20, 15),
                AutoSize = true
            };

            difficultyComboBox = new ComboBox
            {
                Location = new Point(120, 12),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 10)
            };
            difficultyComboBox.Items.AddRange(new object[] { "Легко", "Средне", "Сложно" });
            difficultyComboBox.SelectedIndex = 0;
            difficultyComboBox.SelectedIndexChanged += DifficultyComboBox_SelectedIndexChanged;

            difficultyPanel.Controls.Add(difficultyLabel);
            difficultyPanel.Controls.Add(difficultyComboBox);

            startGameBtn = new Button
            {
                Text = "Начать игру",
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.LightGreen,
                Size = new Size(200, 40),
                Dock = DockStyle.Bottom,
                Margin = new Padding(20)
            };
            startGameBtn.Click += StartGameBtn_Click;

            recordsBtn = new Button
            {
                Text = "Таблица рекордов",
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.LightSkyBlue,
                Size = new Size(200, 40),
                Dock = DockStyle.Bottom,
                Margin = new Padding(20)
            };
            recordsBtn.Click += RecordsBtn_Click;

            imagesFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = SystemColors.ControlLight
            };

            LoadImagesToPanel();

            this.Controls.Add(imagesFlowPanel);
            this.Controls.Add(startGameBtn);
            this.Controls.Add(recordsBtn);
            this.Controls.Add(difficultyPanel);
            this.Controls.Add(titleLabel);
        }

        private void DifficultyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (difficultyComboBox.SelectedItem.ToString())
            {
                case "Легко": gridSize = 4; break;
                case "Средне": gridSize = 7; break;
                case "Сложно": gridSize = 10; break;
                default: gridSize = 4; break;
            }
        }

        private void LoadImagesFromFolder()
        {
            string imageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Puzzle", "Assets", "Images");

            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
                imagePaths = new string[0];
                return;
            }

            string[] extensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" };
            imagePaths = extensions.SelectMany(ext => Directory.GetFiles(imageFolder, ext)).ToArray();
        }

        private void LoadImagesToPanel()
        {
            imagesFlowPanel.Controls.Clear();

            if (imagePaths.Length > 0)
            {
                foreach (string imagePath in imagePaths)
                {
                    var panel = CreateImagePanel(imagePath);
                    imagesFlowPanel.Controls.Add(panel);
                }
            }
            else
            {
                imagesFlowPanel.Controls.Add(new Label
                {
                    Text = "Нет доступных изображений. Добавьте изображения в папку Assets/Images",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = new Font("Arial", 12)
                });
            }
        }

        private Panel CreateImagePanel(string imagePath)
        {
            var panel = new Panel
            {
                Size = new Size(180, 200),
                Margin = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                BackColor = Color.White
            };

            try
            {
                using (var originalImage = Image.FromFile(imagePath))
                {
                    var thumbnail = new Bitmap(originalImage, new Size(160, 140));

                    var pictureBox = new PictureBox
                    {
                        Image = new Bitmap(thumbnail),
                        Size = new Size(160, 140),
                        Location = new Point(10, 10),
                        SizeMode = PictureBoxSizeMode.Zoom
                    };

                    var label = new Label
                    {
                        Text = Path.GetFileNameWithoutExtension(imagePath),
                        Location = new Point(10, 155),
                        Size = new Size(160, 35),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9)
                    };

                    panel.Click += (s, e) => SelectImage(panel, imagePath, label.Text);
                    pictureBox.Click += (s, e) => SelectImage(panel, imagePath, label.Text);
                    label.Click += (s, e) => SelectImage(panel, imagePath, label.Text);

                    panel.Controls.Add(pictureBox);
                    panel.Controls.Add(label);
                }
            }
            catch
            {
                panel.Controls.Add(new Label
                {
                    Text = "Ошибка загрузки",
                    Location = new Point(10, 10),
                    Size = new Size(160, 180),
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }

            return panel;
        }

        private void SelectImage(Panel selectedPanel, string imagePath, string puzzleTitle)
        {
            foreach (Control control in imagesFlowPanel.Controls)
            {
                if (control is Panel panel)
                {
                    panel.BackColor = Color.White;
                    panel.BorderStyle = BorderStyle.FixedSingle;
                }
            }

            selectedPanel.BackColor = Color.LightBlue;
            selectedPanel.BorderStyle = BorderStyle.Fixed3D;

            try
            {
                selectedImage?.Dispose();
                using (var originalImage = Image.FromFile(imagePath))
                {
                    selectedImage = new Bitmap(originalImage);
                }

                selectedPuzzle = new PuzzleItem
                {
                    PuzzleID = GetPuzzleIDByTitle(puzzleTitle),
                    Title = puzzleTitle
                };

                selectSound?.Play();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                errorSound?.Play();
            }
        }

        private int GetPuzzleIDByTitle(string title)
        {
            int id = 0;
            string connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database=puzzleGame;
                                         Integrated Security=True;TrustServerCertificate=True;";
            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT PuzzleID FROM Puzzles WHERE Title=@Title";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            id = Convert.ToInt32(result);
                    }
                }
            }
            catch { }
            return id;
        }

        private void StartGameBtn_Click(object sender, EventArgs e)
        {
            clickSound?.Play();

            if (selectedImage == null)
            {
                errorSound?.Play();
                MessageBox.Show("Пожалуйста, выберите изображение для пазла!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedPuzzle == null || selectedPuzzle.PuzzleID == 0)
            {
                errorSound?.Play();
                MessageBox.Show("Пазл не найден в базе данных!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    string username = loginForm.Username;
                    int userID = loginForm.UserID;

                    startSound?.Play();

                    var gameForm = new GameForm(selectedImage, gridSize, username, userID, selectedPuzzle.PuzzleID, selectedPuzzle.Title);
                    gameForm.Show();
                }
            }
        }

        private void RecordsBtn_Click(object sender, EventArgs e)
        {
            clickSound?.Play();

            var tableRecordsForm = new TableRecords();
            tableRecordsForm.ShowDialog();
        }

        private class PuzzleItem
        {
            public int PuzzleID { get; set; }
            public string Title { get; set; }
        }
    }
}