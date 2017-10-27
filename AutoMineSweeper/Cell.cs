using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace AutoMineSweeper
{
    enum CellStatus
    {
        _0,
        _1,
        _2,
        _3,
        _4,
        _5,
        _6,
        _7,
        _8,
        Flag,
        Mine,
        Triggered,
        Idle,
        Wrong
    }

    enum Direction
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    class Cell
    {
        public Rectangle Rect { get; set; }

        public Point Center
        {
            get
            {
                return Rect.Center();
            }
        }

        public CellStatus Status { get; set; } = CellStatus.Idle;

        public Dictionary<Direction, Cell> Neighbors { get; private set; } = new Dictionary<Direction, Cell>();

        public void LeftClick()
        {
            Debug.Assert(Status == CellStatus.Idle);
            Operator.Instance.LeftClick(Center.X, Center.Y);
        }

        public void RightClick()
        {
            Debug.Assert(Status == CellStatus.Idle);
            Operator.Instance.RightClick(Center.X, Center.Y);
        }

        public void DoubleClick()
        {
            Debug.Assert(Status >= CellStatus._1 && Status <= CellStatus._8);
            Operator.Instance.DoubleClick(Center.X, Center.Y);
        }

        public int GetCountInNeighbors(CellStatus status)
        {
            return Neighbors.Count(x => x.Value != null && x.Value.Status == status);
        }
    }
}
