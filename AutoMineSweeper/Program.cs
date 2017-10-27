namespace AutoMineSweeper
{
    class Program
    {
        const int CELL_START_X = 13;
        const int CELL_START_Y = 101;
        const int CELL_WIDTH = 16;
        const int CELL_HEIGHT = 16;

        const int CELL_COUNT_X = 30;
        const int CELL_COUNT_Y = 16;

        const int MINE_COUNT = 99;

        const int BUTTON_START_X = 240;
        const int BUTTON_START_Y = 61;
        const int BUTTON_WIDTH = 26;
        const int BUTTON_HEIGHT = 26;

        static void Main(string[] args)
        {
            Operator.Instance.Init();

            var field = new Field(true);
            field.Resolve();

            Operator.Instance.Dispose();
        }
    }
}
