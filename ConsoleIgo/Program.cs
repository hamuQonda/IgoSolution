using System;

namespace RecursiveFunctionBoolSpace
{
    internal class Program
    {
        /******************************************************************************/
        // フィールド変数

        private const int boardSize = 9;

        // 碁盤表示用文字配列      空点=0,黒石=1,白石=2,盤外=3,連=4,ダメ=5
        private static Char[] chr = { '・', '○', '●', '？', '◎', '×' };

        // 確認用サンプル盤面配列
        private static int[,] array = new int[boardSize + 2, boardSize + 2]
        {
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, },
            { 3, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, },
            { 3, 0, 1, 1, 0, 2, 2, 2, 2, 2, 3, },
            { 3, 0, 1, 1, 2, 1, 1, 1, 1, 2, 3, },
            { 3, 1, 2, 1, 2, 1, 1, 1, 1, 2, 3, },
            { 3, 1, 2, 1, 2, 1, 1, 1, 1, 1, 3, },
            { 3, 0, 1, 0, 2, 1, 1, 1, 1, 1, 3, },
            { 3, 0, 0, 2, 2, 1, 1, 1, 1, 1, 3, },
            { 3, 1, 0, 0, 2, 1, 1, 1, 1, 1, 3, },
            { 3, 2, 1, 0, 2, 1, 1, 1, 1, 0, 3, },
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, },
        };

        //チェック用盤面配列
        private static int[,] checkArray = new int[boardSize + 2, boardSize + 2];


        /******************************************************************************/
        /// <summary>
        /// 座標(x,y)のcolor石が相手に囲まれているか調べる。
        /// 空点が無いなら true 、空点が有れば false を返す。
        /// </summary>
        /// <param name="ary">盤面の2次元配列</param>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="color">連の石の色</param>
        /// <param name="numSpace">空点の数</param>
        /// <param name="numStone">連の石数</param>
        /// <returns>numSpace==0 ?  true | false</returns>
        private static bool CheckRemove(ref int[,] ary, int x, int y, int color, ref int numSpace, ref int numStone)
        {
            //再帰しない条件の処理
            if (x < 1 || x > ary.GetLength(1) - 2) return false;   //ｘが範囲外
            if (y < 1 || y > ary.GetLength(0) - 2) return false;   //ｙが範囲外
            if (ary[y, x] >= 4) return false;   // チェック済 4 or 5
            if (ary[y, x] != color) return false;   // 自分のcolor石が置かれていない

            //隣が空点なら、空点チェック済みマーク 5 を入れ、空点カウント＋１
            if (ary[y, x - 1] == 0) { ary[y, x - 1] = 5; numSpace++; }            //左隣
            if (ary[y, x + 1] == 0) { ary[y, x + 1] = 5; numSpace++; }            //右隣
            if (ary[y - 1, x] == 0) { ary[y - 1, x] = 5; numSpace++; }            //上隣
            if (ary[y + 1, x] == 0) { ary[y + 1, x] = 5; numSpace++; }            //下隣
            ary[y, x] = 4; numStone++;  // チェック済みマーク 4 を入れ、連の石カウント＋１

            //再帰呼び出し
            _ = CheckRemove(ref ary, x - 1, y, color, ref numSpace, ref numStone);   //左隣
            _ = CheckRemove(ref ary, x, y + 1, color, ref numSpace, ref numStone);   //下隣
            _ = CheckRemove(ref ary, x + 1, y, color, ref numSpace, ref numStone);   //右隣
            _ = CheckRemove(ref ary, x, y - 1, color, ref numSpace, ref numStone);   //上隣
            return numSpace == 0;
        }


        /******************************************************************************/
        // メイン

        static void Main(string[] args)
        {
            Array.Copy(array, checkArray, array.Length); // チェック用配列 checkArray に array をコピー
            int ix = 6;                 // チェックを開始するx座標
            int iy = 4;                 // チェックを開始するy座標
            int color = checkArray[iy, ix];  // チェック対象の連の石の色
            int nSpace = 0;              // 空点カウント初期値０
            int nStone = 0;              // 石数カウント初期値０
            DispGoban(checkArray);

            if (CheckRemove(ref checkArray, ix, iy, color, ref nSpace, ref nStone))
                Console.WriteLine($"({ix},{iy})の連(◎:{nStone})は空点×が {nSpace} 個なので取れます");
            else
                Console.WriteLine($"({ix},{iy})の連(◎:{nStone})は空点×が {nSpace} 個あります");

            DispGoban(checkArray);
            Console.ReadLine();
        }


        /******************************************************************************/
        // 碁盤の表示

        static void DispGoban(int[,] array)
        {
            var zstr = "   1 2 3 4 5 6 7 8 910111213141516171819 ".Substring(0, boardSize * 2 + 2);
            Console.WriteLine(zstr);
            for (int i = 1; i <= boardSize; i++)
            {
                Console.Write($"{i,2:d}");
                for (int j = 1; j <= boardSize; j++)
                {
                    Console.Write($"{chr[array[i, j]]}");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}