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

        private bool ReadMemory { get; set; }

        private bool refreshFields = false;

        public Field(bool readMemory)
        {
            ReadMemory = readMemory;

            CellStart = new Point(13, 101);
            CellSize = new Size(16, 16);

            CellCount = new Size(30, 16);
            MineCount = 99;

            ButtonRect = new Rectangle(240, 61, 26, 26);

            BaseAddress = (IntPtr)0x01005361;
            MemoryLineSize = 32;

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
                    cell.Index = CellCount.Width * j + i;
                    Cells[i, j] = cell;
                }
            }

            //build neighbors
            if (!ReadMemory)
            {
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
            if (ReadMemory)
            {
                ResolveByReadMemory();
                return;
            }

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
                                int idleCount = cell.GetCountInNeighbors(CellStatus.Idle);
                                if (idleCount > 0 && flagCount == (int)cell.Status)
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
                                int idleCount = cell.GetCountInNeighbors(CellStatus.Idle);
                                if (idleCount > 0 && flagCount + idleCount == (int)cell.Status)
                                {
                                    foreach (var neighbor in cell.Neighbors)
                                    {
                                        if (neighbor.Value != null && neighbor.Value.Status == CellStatus.Idle)
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

                    if (true)
                    {
                        Dictionary<ListStruct, ElementDetail> totalDict = new Dictionary<ListStruct, ElementDetail>(new ListStructComparer());
                        for (int i = 0; i < CellCount.Width; i++)
                        {
                            for (int j = 0; j < CellCount.Height; j++)
                            {
                                var cell = GetCell(i, j);

                                if (cell.Status >= CellStatus._1 && cell.Status <= CellStatus._8)
                                {
                                    Dictionary<ListStruct, ElementDetail> new_element_dict = new Dictionary<ListStruct, ElementDetail>((new ListStructComparer()));
                                    Dictionary<ListStruct, ElementDetail> curr_element_dict = new Dictionary<ListStruct, ElementDetail>((new ListStructComparer()));
                                    Dictionary<ListStruct, ElementDetail> checked_element_dict = new Dictionary<ListStruct, ElementDetail>((new ListStructComparer()));
                                    var unknownList = new ListStruct(cell.GetNeighborsList(CellStatus.Idle));
                                    int leftMineCount = (int)cell.Status - cell.GetCountInNeighbors(CellStatus.Flag);
                                    if (leftMineCount > 0 && !totalDict.ContainsKey(unknownList))
                                    {
                                        bool firstRound = true;
                                        new_element_dict.Add(unknownList, new ElementDetail(leftMineCount));
                                        while (firstRound || !found && (new_element_dict.Count > 0 || checked_element_dict.Count > 0))
                                        {

                                            firstRound = false;
                                            foreach (var kvp in checked_element_dict)
                                            {
                                                if (!totalDict.ContainsKey(kvp.Key))
                                                {
                                                    totalDict.Add(kvp.Key, kvp.Value);
                                                }
                                                if (curr_element_dict.ContainsKey(kvp.Key))
                                                {
                                                    curr_element_dict.Remove(kvp.Key);
                                                }
                                            }
                                            foreach (var kvp in new_element_dict)
                                            {
                                                if (!curr_element_dict.ContainsKey(kvp.Key))
                                                {
                                                    curr_element_dict.Add(kvp.Key, kvp.Value);
                                                }
                                            }
                                            new_element_dict = new Dictionary<ListStruct, ElementDetail>((new ListStructComparer()));
                                            checked_element_dict = new Dictionary<ListStruct, ElementDetail>((new ListStructComparer()));
                                            foreach (var kvp_curr in curr_element_dict)
                                            {
                                                bool findChildOrParent = false;
                                                foreach (var kvp in totalDict)
                                                {
                                                    if (kvp.Value.abondoned)
                                                    {
                                                        continue;
                                                    }
                                                    var curr_element = kvp_curr.Key;
                                                    ListDiff listDiff = new ListDiff(curr_element.list, kvp.Key.list);
                                                    if (listDiff.Diff1.Count > 0)
                                                    {
                                                        if (listDiff.Diff1.Count == kvp_curr.Value.unknownCount - kvp.Value.unknownCount)
                                                        {
                                                            AllCellIndexClickRight(listDiff.Diff1);
                                                            found = true;
                                                            break;
                                                        }
                                                        if (listDiff.Diff2.Count == 0)
                                                        {
                                                            if (kvp_curr.Value.unknownCount == kvp.Value.unknownCount)
                                                            {
                                                                AllCellIndexClickLeft(listDiff.Diff1);
                                                                found = true;
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                ListStruct diffListStruct = new ListStruct(listDiff.Diff1);
                                                                if (!totalDict.ContainsKey(diffListStruct))
                                                                {
                                                                    findChildOrParent = true;
                                                                    new_element_dict.Add(diffListStruct, new ElementDetail(Math.Abs(kvp_curr.Value.unknownCount - kvp.Value.unknownCount)));
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (listDiff.Diff2.Count == kvp.Value.unknownCount - kvp_curr.Value.unknownCount)
                                                        {
                                                            AllCellIndexClickRight(listDiff.Diff2);
                                                            found = true;
                                                            break;
                                                        }
                                                        else if (kvp_curr.Value.unknownCount == kvp.Value.unknownCount)
                                                        {
                                                            AllCellIndexClickLeft(listDiff.Diff2);
                                                            found = true;
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            ListStruct diffListStruct = new ListStruct(listDiff.Diff2);
                                                            if (!totalDict.ContainsKey(diffListStruct))
                                                            {
                                                                findChildOrParent = true;
                                                                kvp.Value.abondoned = true;
                                                                new_element_dict.Add(diffListStruct, new ElementDetail(Math.Abs(kvp_curr.Value.unknownCount - kvp.Value.unknownCount)));
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                if (found)
                                                {
                                                    break;
                                                }

                                                else
                                                {
                                                    if (!checked_element_dict.ContainsKey(kvp_curr.Key))
                                                    {
                                                        checked_element_dict.Add(kvp_curr.Key, kvp_curr.Value);
                                                    }
                                                    if (findChildOrParent)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                if (found)
                                {
                                    break;
                                }
                            }
                            if (found)
                            {
                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        //click random field
                        int i = random.Next(0, CellCount.Width);
                        int j = random.Next(0, CellCount.Height);

                        var cell = GetCell(i, j);
                        while (cell.Status != CellStatus.Idle)
                        {
                            cell = GetCellByIndex((cell.Index + 1) % (CellCount.Height * CellCount.Width));
                        }
                        cell.LeftClick();
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
                    if (!refreshFields)
                    {
                        if ((cell.Status >= CellStatus._1 && cell.Status <= CellStatus._8) || cell.Status == CellStatus.Flag)
                        {
                            continue;
                        }
                    }

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
            refreshFields = true;
            Operator.Instance.LeftClick(ButtonRect.Center().X, ButtonRect.Center().Y);
        }

        private IntPtr BaseAddress;

        private UInt32 MemoryLineSize;

        [Flags]
        public enum CellMemoryFlags : uint
        {
            HighIdle = 0x00,
            HighOpen = 0x40,
            HighMine = 0x80,
            HighTriggered = 0xC0,

            //Low 0 ~ 8 is Mine count in neighbors

            //in case of game over
            LowMineIdle = 0x0A,
            LowWrong = 0x0B,
            LowTriggered = 0x0C,

            //in case of playing
            LowQuestion = 0x0D,
            LowFlag = 0x0E,
            LowIdle = 0x0F,
        }

        public void ResolveByReadMemory()
        {
            ClickStartButton();

            var hWnd = Operator.Instance.GetWindowsHandle();
            var bytes = MemoryUtils.ReadProcessMemory(hWnd, BaseAddress, (UInt32)(this.CellCount.Width * MemoryLineSize));

            for (int j = 0; j < CellCount.Height; j++)
            {
                for (int i = 0; i < CellCount.Width; i++)
                {
                    CellMemoryFlags cellFlag = (CellMemoryFlags)bytes[i + j * MemoryLineSize];
                    
                    if (cellFlag == (CellMemoryFlags.HighIdle | CellMemoryFlags.LowIdle))
                    {
                        var cell = GetCell(i, j);
                        cell.LeftClick();
                        //read again
                        bytes = MemoryUtils.ReadProcessMemory(hWnd, BaseAddress, (UInt32)(this.CellCount.Width * MemoryLineSize));
                    }
                }
            }
        }

        private Cell GetCellByIndex(int index)
        {
            int i = index % CellCount.Width;
            int j = index / CellCount.Width;
            return Cells[i, j];
        }

        private void AllCellIndexClickLeft(List<int> l)
        {
            foreach (int i in l)
            {
                GetCellByIndex(i).LeftClick();
            }
        }
        private void AllCellIndexClickRight(List<int> l)
        {
            foreach (int i in l)
            {
                var cell = GetCellByIndex(i);
                cell.RightClick();
                cell.Status = CellStatus.Flag;
            }
        }
    }
}
