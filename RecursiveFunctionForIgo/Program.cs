using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecursiveFunctionForIgo
{
    internal class Program
    {
        /******************************************************************************/
        // フィールド変数
        private const int gobanSize = 9;
        private const int boardSize = gobanSize + 2;
        // 交点の要素  No.            空点=0,黒石=1,白石=2,盤外=3,連=4,ダメ=5,黒死=6,白死=7,黒地=8,白地=9
        private static Char[] chr = { '・' , '○' , '●' , '？' , '◎' , '×' , '▽' , '▼' , '◇' , '◆' };
        private const int Space = 0;
        private const int Black = 1;
        private const int White = 2;
        private const int OutSd = 3;
        private const int RenMk = 4;
        private const int XDame = 5;
        private const int BDead = 6;
        private const int WDead = 7;
        private const int BArea = 8;
        private const int WArea = 9;
        private static int[] countMk = new int[chr.Length]; // 各要素のカウンタ、e.g. countMk[Black] 黒石のカウンタ

        // 確認用サンプル盤面配列
        private static int[,] board = new int[boardSize, boardSize] {
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, },
            { 3, 0, 0, 0, 0, 0, 1, 2, 2, 0, 3, },
            { 3, 0, 1, 0, 1, 1, 1, 2, 0, 2, 3, },
            { 3, 2, 2, 1, 2, 1, 0, 1, 2, 2, 3, },
            { 3, 1, 2, 1, 2, 1, 1, 1, 1, 1, 3, },
            { 3, 0, 1, 1, 2, 1, 0, 1, 0, 0, 3, },
            { 3, 1, 1, 1, 1, 1, 1, 1, 1, 0, 3, },
            { 3, 1, 1, 2, 2, 2, 2, 1, 0, 0, 3, },
            { 3, 2, 2, 0, 0, 0, 2, 2, 2, 2, 3, },
            { 3, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, },
            { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, },
        };

        //チェック用盤面配列
        private static int[,] check = new int[boardSize, boardSize]; // 値0は未チェック、0以外はチェック済み


        /******************************************************************************/
        //  囲碁プログラムの為の多機能再帰関数（2次元配列用）
        /// <summary>
        /// <para>座標(x,y)の連を elem1 から elem2 に置き換える。</para>
        /// <para>連の要素数が countMk [ elem1 ] に入る。</para>
        /// <para>連周囲の各要素数が countMk [ 交点の要素No. ] に入る。</para>
        /// <para>フィールド変数は、 board[,] check[,] countMk[] 交点マーク要素No が必要。</para>
        /// </summary>
        /// <param name="x">碁盤のｘ座標</param>
        /// <param name="y">碁盤のｙ座標</param>
        /// <param name="elem1">連の構成要素</param>
        /// <param name="elem2">この要素に置き換え</param>
        /// <returns>true(空点無) | false(空点有)</returns>
        static bool CheckRen(int x, int y, int elem1, int elem2)
        {
            // 再帰しない条件
            if (check[y, x] != 0)     return false; // この座標は、カウント済み
            if (board[y, x] != elem1) return false; // この座標は、連の要素 elem1 ではない
            //  隣が 連の要素ではなく     且つ        未チェック  なら、   各要素カウンタを＋１     周囲チェック済み(-1)とする
            if (board[y, x - 1] != elem1 && check[y, x - 1] == 0) { countMk[board[y, x - 1]]++; check[y, x - 1] = -1; } // 左
            if (board[y, x + 1] != elem1 && check[y, x + 1] == 0) { countMk[board[y, x + 1]]++; check[y, x + 1] = -1; } // 右
            if (board[y - 1, x] != elem1 && check[y - 1, x] == 0) { countMk[board[y - 1, x]]++; check[y - 1, x] = -1; } // 上
            if (board[y + 1, x] != elem1 && check[y + 1, x] == 0) { countMk[board[y + 1, x]]++; check[y + 1, x] = -1; } // 下
            // 連要素をelem2にして、連カウンタを＋１、連チェック済み(elem1+1) ※elem1=空点(0値)でもチェック済みになるように+1）
            board[y, x] = elem2; countMk[elem1]++; check[y, x] = elem1 + 1;
            // 隣を調べる為に、再帰呼び出し
            CheckRen(x - 1, y, elem1, elem2);   // 左
            CheckRen(x + 1, y, elem1, elem2);   // 右
            CheckRen(x, y - 1, elem1, elem2);   // 上
            CheckRen(x, y + 1, elem1, elem2);   // 下
            // 戻り値
            return countMk[Space] == 0;         // true(空点無) | false(空点有)
        }

        /******************************************************************************/
        // メイン      
        static void Main(string[] args)
        {
            int x, y, color;
            var mes = "";

            //-----------------------------------------------------------------------
            // 連が相手に囲まれているか判定
            DispGoban("\n[ 元の盤面 ]");
            InitCheckCountArry();                   // チェック配列とカウンタ配列の初期化
            Console.WriteLine("[ 連が相手に囲まれているか判定 ]");
            (x, y) = (4, 5); color = board[y, x];   // 指定座標と連(石)の色
            var result = CheckRen(x, y, color, WDead);  // 連に空点が無ければ、true 
            mes = result ? $"{result}:空点が無いのでとれます。" : $"{result}:空点があるのでとれません。";
            DispGoban($"座標({x},{y})の連{chr[WDead]}の空点は{countMk[Space]}個です。" + mes);
            DispCount(color);                       // 各カウンタ値を表示

            RestoreBoard();                         // 盤面を元に戻しておく
            Console.WriteLine("Enterで次の使用例へ...");
            Console.ReadLine();

            //-----------------------------------------------------------------------
            // 連の石を消す
            DispGoban("\n[ 元の盤面 ]");
            InitCheckCountArry();                   // チェック配列とカウンタ配列の初期化
            Console.WriteLine("[ 連の石を消す ]");
            (x, y) = (4, 3); color = board[y, x];   // 指定座標と連(石)の色
            CheckRen(x, y, color, Space);           // 第4パラメータを Space にすると '消す'
            DispGoban($"座標({x},{y})の連{chr[color]}を {countMk[color]}個 消しました。");
            DispCount(color);                       // カウンタ値を表示

            RestoreBoard();                         // 盤面を元に戻しておく
            Console.WriteLine("Enterで次の使用例へ...");
            Console.ReadLine();

            //-----------------------------------------------------------------------
            // 空点を数える
            DispGoban("\n[ 元の盤面 ]");
            InitCheckCountArry();                   // チェック配列とカウンタ配列の初期化
            Console.WriteLine("[ 空点を数える ]");
            (x, y) = (9, 7);                        // 指定座標
            CheckRen(x, y, Space, XDame);           // 数える空点を ×印に 
            DispGoban($"座標({x},{y})の地{chr[XDame]}は、{countMk[Space]}目です。" + 
                      $"\n周囲は、黒石{countMk[Black]}個、白石{countMk[White]}個、盤外{countMk[OutSd]}個 に囲まれています。");
            DispCount(Space);                       // 各カウンタ値を表示

            RestoreBoard();                         // 盤面を元に戻しておく
            Console.WriteLine("Enterで次の使用例へ...");
            Console.ReadLine();
        }

        /******************************************************************************/
        // 盤面を元に戻す
        private static void RestoreBoard()
        {
            for (int y = 0; y < boardSize - 1; y++) {
                for (int x = 0; x < boardSize - 1; x++) {
                    if (check[y, x] > 0) board[y, x] = check[y, x] - 1;
                }
            }
        }

        /******************************************************************************/
        // 各カウンタ値を表示
        /// <param name="renElem">連の要素</param>
        private static void DispCount(int renElem)
        {
            for(int i = 0; i < countMk.Length; i++) {
                Console.WriteLine($"{chr[i]}={countMk[i]} 個"
                                    + (i == renElem ? "(連)" : "") 
                                    + (i == Space ? "(空点)" : ""));
            }
        }

        /******************************************************************************/
        // チェック配列とカウンタ配列の初期化
        private static void InitCheckCountArry()
        {
            Array.Clear(check, 0, check.Length);
            Array.Clear(countMk, 0, countMk.Length);
        }

        /******************************************************************************/
        // 碁盤の表示
        static void DispGoban(string mesg)
        {
            Console.WriteLine(mesg);
            var zstr = "    1 2 3 4 5 6 7 8 910111213141516171819 ".Substring(0, boardSize * 2 -1);
            Console.WriteLine(zstr);
            for (int i = 1; i < boardSize - 1; i++) {
                Console.Write($"{i,2:d} ");
                for (int j = 1; j < boardSize -1; j++) {
                    Console.Write($"{chr[board[i, j]]}");
                }
                Console.WriteLine();
            }
        }
    }
}
