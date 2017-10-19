using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AutoMineSweeper
{
    class Field
    {
        public Point CellStart { get; private set; }

        public Size CellSize { get; private set; }

        public Size CellCount { get; private set; }

        public int MineCount { get; private set; }

        public Rectangle ButtonRect { get; private set; }

        public Cell[,] Cells { get; private set; }

        public Bitmap LoseButton { get; set; }

        public Dictionary<CellStatus, Bitmap> CellTemplates { get; set; } = new Dictionary<CellStatus, Bitmap>();

        public Field()
        {
            CellStart = new Point(13, 101);
            CellSize = new Size(16, 16);

            CellCount = new Size(30, 16);
            MineCount = 99;

            ButtonRect = new Rectangle(240, 61, 26, 26);

            //create cells
            Cells = new Cell[CellCount.Width, CellCount.Height];
            for (int i = 0; i < CellCount.Width; i++)
            {
                for (int j = 0; j < CellCount.Height; j++)
                {
                    var cell = new Cell()
                    {
                        Rect = new Rectangle(CellStart.X + i * CellSize.Width, CellStart.Y + j * CellSize.Height, CellSize.Width, CellSize.Height)
                    };
                    Cells[i, j] = cell;
                }
            }

            //build neighbors

            for (int i = 0; i < CellCount.Width; i++)
            {
                for (int j = 0; j < CellCount.Height; j++)
                {
                    var cell = GetCell(i, j);
                    cell.Neighbors[Direction.TopLeft] = GetCell(i - 1, j - 1);
                    cell.Neighbors[Direction.TopCenter] = GetCell(i, j - 1);
                    cell.Neighbors[Direction.TopRight] = GetCell(i + 1, j - 1);

                    cell.Neighbors[Direction.CenterLeft] = GetCell(i - 1, j);
                    cell.Neighbors[Direction.CenterRight] = GetCell(i + 1, j);

                    cell.Neighbors[Direction.BottomLeft] = GetCell(i - 1, j + 1);
                    cell.Neighbors[Direction.BottomCenter] = GetCell(i, j + 1);
                    cell.Neighbors[Direction.BottomRight] = GetCell(i + 1, j + 1);
                }
            }


        }

        public Cell GetCell(int i, int j)
        {
            if (i >= 0 && i < CellCount.Width && j >= 0 && j < CellCount.Height)
            {
                return Cells[i, j];
            }
            return null;
        }

        public void Resolve()
        {
            //load existing files in images folder
            foreach (CellStatus status in Enum.GetValues(typeof(CellStatus)))
            {
                //try to load file
                try
                {
                    string fileName = $"CellTemplates/{status}.png";
                    var image = new Bitmap(fileName);
                    CellTemplates[status] = image;
                }
                catch (Exception)
                {
                }
            }

            LoseButton = new Bitmap("ButtonTemplates/Lose.png");

            var random = new Random((int)DateTime.Now.ToFileTimeUtc());

            bool everFound = false;

            for (int c = 0; c < 20; c++)
            {
                //scan the field
                while (Scan())
                {
                    bool found = false;

                    //if any has all flags
                    for (int i = 0; i < CellCount.Width; i++)
                    {
                        for (int j = 0; j < CellCount.Height; j++)
                        {
                            var cell = GetCell(i, j);

                            if (cell.Status >= CellStatus._1 && cell.Status <= CellStatus._8)
                            {
                                int flagCount = cell.GetCountInNeighbors(CellStatus.Flag);
                                int unknownCount = cell.GetCountInNeighbors(CellStatus.Unknown);
                                if (unknownCount > 0 && flagCount == (int)cell.Status)
                                {
                                    cell.DoubleClick();
                                    found = true;
                                }
                            }
                        }
                    }

                    if (found)
                    {
                        continue;
                    }

                    //if any should flag all neighbors
                    for (int i = 0; i < CellCount.Width; i++)
                    {
                        for (int j = 0; j < CellCount.Height; j++)
                        {
                            var cell = GetCell(i, j);

                            if (cell.Status >= CellStatus._1 && cell.Status <= CellStatus._8)
                            {
                                int flagCount = cell.GetCountInNeighbors(CellStatus.Flag);
                                int unknownCount = cell.GetCountInNeighbors(CellStatus.Unknown);
                                if (unknownCount > 0 && flagCount + unknownCount == (int)cell.Status)
                                {
                                    foreach (var neighbor in cell.Neighbors)
                                    {
                                        if (neighbor.Value != null && neighbor.Value.Status == CellStatus.Unknown)
                                        {
                                            neighbor.Value.RightClick();
                                            neighbor.Value.Status = CellStatus.Flag;
                                        }
                                    }
                                    found = true;
                                }
                            }
                        }
                    }

                    everFound = everFound || found;

                    if (!found)
                    {
                        //click random field
                        int i = random.Next(0, CellCount.Width);
                        int j = random.Next(0, CellCount.Height);

                        var cell = GetCell(i, j);
                        if (cell.Status == CellStatus.Unknown)
                        {
                            cell.LeftClick();
                        }
                    }
                }

                ClickStartButton();
            }
        }

        //take screenshot, update cell status
        private bool Scan()
        {
            var screenshot = Operator.Instance.GetScreenshot();

            //check if lose
            var buttonImage = screenshot.Clone(ButtonRect, screenshot.PixelFormat);
            if (ImageUtils.CompareMemCmp(buttonImage, LoseButton))
            {
                return false;
            }

            for (int i = 0; i < CellCount.Width; i++)
            {
                for (int j = 0; j < CellCount.Height; j++)
                {
                    var cell = GetCell(i, j);

                    var cellImage = screenshot.Clone(cell.Rect, screenshot.PixelFormat);

                    var pair = CellTemplates
                        .Where(x => ImageUtils.CompareMemCmp(x.Value, cellImage))
                        .Select(x => (KeyValuePair<CellStatus, Bitmap>?)x)
                        .FirstOrDefault();

                    if (pair == null)
                    {
                        Console.WriteLine("New Cell");
                        Debug.Assert(false, "New Cell Image Found");
                        cellImage.Save($"CellTemplates/{ Guid.NewGuid() }.png", ImageFormat.Png);
                        return false;
                    }

                    cell.Status = pair.Value.Key;
                }
            }


            return true;
        }

        private void ClickStartButton()
        {
            Operator.Instance.LeftClick(ButtonRect.Center().X, ButtonRect.Center().Y);
        }
    }
}
