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

        public int Index;

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
        public List<int> GetNeighborsList(CellStatus status)
        {
            List<int> unknown = new List<int>();
            foreach (var nb in Neighbors)
            {
                if (nb.Value != null)
                {
                    if (nb.Value.Status == status)
                    {
                        unknown.Add(nb.Value.Index);
                    }
                }
            }
            return unknown;
        }
    }

    class ElementDetail
    {
        public int unknownCount;
        public bool abondoned;

        public ElementDetail(int count)
        {
            unknownCount = count;
            abondoned = false;
        }
    }

    struct ListStruct
    {
        public List<int> list;
        public ListStruct(List<int> l)
        {
            list = l;
        }


    }

    class ListStructComparer : IEqualityComparer<ListStruct>
    {
        public bool Equals(ListStruct l1, ListStruct l2)
        {
            return l1.list.SequenceEqual(l2.list);
        }

        public int GetHashCode(ListStruct l)
        {
            int hashCode = 0;
            foreach (int i in l.list)
            {
                hashCode ^= i;
            }
            return hashCode;
        }
    }

    struct ListDiff
    {
        public List<int> Diff1;
        public List<int> Diff2;
        public List<int> Both;

        public ListDiff(List<int> l, List<int> t)
        {
            Dictionary<int, int> total = new Dictionary<int, int>();
            Diff1 = new List<int>();
            Diff2 = new List<int>();
            Both = new List<int>();

            foreach (int a in l)
            {
                total.Add(a, 1);
            }

            foreach (int b in t)
            {
                if (!total.ContainsKey(b))
                {
                    total.Add(b, 2);
                }
                else
                {
                    total[b] = 1 + 2;
                }
            }

            foreach (var kvp in total)
            {
                if (kvp.Value == 1)
                {
                    Diff1.Add(kvp.Key);
                }
                else if (kvp.Value == 2)
                {
                    Diff2.Add(kvp.Key);
                }
                else
                {
                    Both.Add(kvp.Value);
                }
            }
        }
    }
}
