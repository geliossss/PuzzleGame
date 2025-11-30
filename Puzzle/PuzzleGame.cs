using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;

namespace Puzzle
{
    public class PuzzleGame
    {
        private List<PuzzlePiece> pieces;
        private PuzzlePiece selectedPiece;
        public int gridSize;
        private int pieceWidth;
        private int pieceHeight;
        private Image originalImage;
        private Random random;
        private int cols;
        private int rows;

        public bool IsCompleted => pieces.All(p => p.IsPlacedCorrectly);
        public int PieceWidth => pieceWidth;
        public int PieceHeight => pieceHeight;
        public int Cols => cols;
        public int Rows => rows;
        public List<PuzzlePiece> Pieces => pieces;

        private SoundPlayer clickSound;
        private SoundPlayer bgMusic;

        public PuzzleGame(Image image, int gridSize)
        {
            this.gridSize = gridSize;
            this.originalImage = image;
            this.random = new Random();
            this.pieces = new List<PuzzlePiece>();

            InitializePuzzle();
            ShufflePieces();
            LoadSound();
        }

        private void LoadSound()
        {
            string soundFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Puzzle", "Assets", "Sounds");

            bgMusic = new SoundPlayer(Path.Combine(soundFolder, "puzzle.wav"));
            try { bgMusic.LoadAsync(); } catch { }
        }

        public void StartMusic()
        {
            try { bgMusic?.PlayLooping(); }
            catch { }
        }

        public void StopMusic()
        {
            try { bgMusic?.Stop(); }
            catch { }
        }

        private void InitializePuzzle()
        {
            // Рассчитываем количество кусочков на основе пропорций изображения
            double aspectRatio = (double)originalImage.Width / originalImage.Height;

            if (aspectRatio > 1) // Горизонтальное изображение
            {
                cols = gridSize;
                rows = Math.Max(1, (int)Math.Round(gridSize / aspectRatio));
            }
            else // Вертикальное или квадратное изображение
            {
                rows = gridSize;
                cols = Math.Max(1, (int)Math.Round(gridSize * aspectRatio));
            }

            cols = Math.Max(2, cols);
            rows = Math.Max(2, rows);

            // Размеры кусочков
            pieceWidth = originalImage.Width / cols;
            pieceHeight = originalImage.Height / rows;

            pieces.Clear();
            int pieceId = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // Для последних кусочков в ряду/столбце берем оставшуюся часть
                    int actualWidth = (col == cols - 1) ? originalImage.Width - col * pieceWidth : pieceWidth;
                    int actualHeight = (row == rows - 1) ? originalImage.Height - row * pieceHeight : pieceHeight;

                    Rectangle sourceRect = new Rectangle(
                        col * pieceWidth,
                        row * pieceHeight,
                        actualWidth,
                        actualHeight
                    );

                    Point correctPosition = new Point(col, row);

                    var piece = new PuzzlePiece(
                        pieceId++,
                        originalImage,
                        sourceRect,
                        correctPosition
                    );

                    pieces.Add(piece);
                }
            }
        }

        private void ShufflePieces()
        {
            int margin = 50;
            int playAreaWidth = originalImage.Width + 200;
            int playAreaHeight = originalImage.Height + 200;

            foreach (var piece in pieces)
            {
                int x = random.Next(margin, playAreaWidth - pieceWidth - margin);
                int y = random.Next(-margin, playAreaHeight - pieceHeight - margin*2);

                piece.DrawPosition = new Point(x, y);
                piece.UpdateCorrectness(pieceWidth, pieceHeight);
            }
        }

        public void SelectPiece(Point mousePosition)
        {
            // Снимаем выделение со всех кусочков
            foreach (var piece in pieces)
            {
                piece.IsSelected = false;
            }

            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                var piece = pieces[i];

                // Пропускаем заблокированные кусочки при поиске
                if (piece.IsLocked) continue;

                if (piece.ContainsPoint(mousePosition, pieceWidth, pieceHeight))
                {
                    selectedPiece = piece;
                    piece.IsSelected = true;
                    piece.IsBeingDragged = true;

                    piece.DragOffset = new Point(
                        mousePosition.X - piece.DrawPosition.X,
                        mousePosition.Y - piece.DrawPosition.Y
                    );

                    pieces.RemoveAt(i);
                    pieces.Add(piece);
                    return;
                }
            }

            selectedPiece = null;
        }

        public void MoveSelectedPiece(Point mousePosition)
        {
            if (selectedPiece != null && selectedPiece.IsBeingDragged && !selectedPiece.IsLocked)
            {
                selectedPiece.DrawPosition = new Point(
                    mousePosition.X - selectedPiece.DragOffset.X,
                    mousePosition.Y - selectedPiece.DragOffset.Y
                );

                selectedPiece.UpdateCorrectness(pieceWidth, pieceHeight);
            }
        }

        public void DropPiece()
        {
            if (selectedPiece != null)
            {
                selectedPiece.IsBeingDragged = false;
                selectedPiece.SnapToGrid(pieceWidth, pieceHeight);
                selectedPiece = null;
            }
        }

        public void Draw(Graphics g)
        {
            Rectangle playField = new Rectangle(0, 0, cols * pieceWidth, rows * pieceHeight);
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(whiteBrush, playField);
            }
            // Рисуем полупрозрачное исходное изображение как фон
            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                // Устанавливаем прозрачность 30%
                float[][] colorMatrixElements = {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, 0.3f, 0}, // Альфа-канал (прозрачность)
                    new float[] {0, 0, 0, 0, 1}
                };

                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(colorMatrixElements);
                attributes.SetColorMatrix(colorMatrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);

                // Рисуем исходное изображение с прозрачностью
                g.DrawImage(originalImage,
                    new Rectangle(0, 0, cols * pieceWidth, rows * pieceHeight),
                    0, 0, originalImage.Width, originalImage.Height,
                    GraphicsUnit.Pixel, attributes);
            }

            // Рисуем фон для игрового поля (полупрозрачный белый поверх изображения)
             playField = new Rectangle(0, 0, cols * pieceWidth, rows * pieceHeight);
            using (var whiteOverlay = new SolidBrush(Color.FromArgb(75, 255, 255, 255)))
            {
                g.FillRectangle(whiteOverlay, playField);
            }
            g.DrawRectangle(Pens.DarkGray, playField);

            // Рисуем контуры правильных позиций
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Rectangle cellRect = new Rectangle(
                        col * pieceWidth,
                        row * pieceHeight,
                        pieceWidth,
                        pieceHeight
                    );
                    g.DrawRectangle(Pens.DarkGray, cellRect);
                }
            }

            //playField = new Rectangle(0, 0, cols * pieceWidth, rows * pieceHeight);
            //using (var whiteBrush = new SolidBrush(Color.White))
            //{
            //    g.FillRectangle(whiteBrush, playField);
            //}

            // Рисуем все кусочки
            foreach (var piece in pieces.OrderBy(p => !p.IsLocked))
            {
                piece.Draw(g, pieceWidth, pieceHeight);
            }

           // if (selectedPiece != null) g.DrawString($"({selectedPiece.DrawPosition.X}, {selectedPiece.DrawPosition.Y})", new Font("Arial", 10), Brushes.Black, selectedPiece.DrawPosition.X, selectedPiece.DrawPosition.Y - 15);



        }

        public void SolvePuzzle()
        {
            foreach (var piece in pieces)
            {
                piece.DrawPosition = new Point(
                    piece.CorrectPosition.X * pieceWidth,
                    piece.CorrectPosition.Y * pieceHeight
                );
                piece.IsPlacedCorrectly = true;
                piece.IsSelected = false;
                piece.IsBeingDragged = false;
                piece.IsLocked = true;
            }
        }
    }
}