using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleIgoTest
{
    public class Class1
    {
        // 交点の要素  No.            空点=0,黒石=1,白石=2,盤外=3,連=4,ダメ=5,黒死=6,白死=7,黒地=8,白地=9
        internal static Char[] chr = { '・' , '○' , '●' , '※' , '◎' , '×' , '▽' , '▼' , '◇' , '◆' };
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
        private static Random rand = new Random();      // 乱数
        private static int bdWid = 9 + 2;
        private static int dL;                          // 左への移動量 -1
        private static int dR;                          // 右への移動量 +1
        private static int dU;                          // 上への移動量 -bdWid
        private static int dD;                          // 下への移動量 +bdWid

        private static int[] board = new int[]
        {
            3,3,3,3,3,3,3,3,3,3,3,
            3,0,0,0,0,0,0,0,0,0,3,
            3,0,0,0,0,0,0,2,2,0,3,
            3,0,0,0,0,0,2,1,1,2,3,
            3,0,0,0,0,0,2,0,1,2,3,
            3,0,0,0,0,0,2,1,1,2,3,
            3,0,0,0,0,0,0,2,2,0,3,
            3,0,0,0,0,0,3,3,0,0,3,
            3,0,0,0,0,0,0,0,0,0,3,
            3,0,0,0,0,0,0,0,0,0,3,
            3,3,3,3,3,3,3,3,3,3,3
        };
        private static int[] check = new int[board.Length];
        private static int[] count = new int[chr.Length]; // 各要素のカウンタ、e.g. count[Black] 黒石のカウンタ
        private static int[] dir4 = new int[4] { -bdWid, -1, +1, +bdWid }; // 上左右下への移動量
        private static int[] hama = new int[3];         // アゲハマ、hama[Black], hama[White]

        static void Main(string[] args)
        {
            dL = -1;        // 左 移動量
            dR = +1;        // 右 移動量
            dU = -bdWid;    // 上 移動量
            dD = +bdWid;    // 下 移動量


            DispGoban();
            int color = Black;
            int z, err;
            for(; ; )
            {
                z = HumanSelectMove(color);
                if (z > bdMax) break;

                err = Move(z, color);
                if (err == 0) { DispGoban(); color = 3 - color; }
                else if (err == 1) { Console.WriteLine($"err={err}"); }
               // color = board[z];
              //  kesu(z, color);
              //  DispGoban();
               // Console.ReadLine();

            }

            Console.ReadLine();
            

        }

        static int ko_z = 0;
        static int bdMax = bdWid * bdWid;
        /// <summary>
        /// 石を置く
        /// </summary>
        /// <param name="tz"></param>
        /// <param name="color"></param>
        /// <returns>0＝正常手、0以外＝不正手</returns>
        private static int Move(int tz, int color)
        {
            if (tz == 0) { ko_z = 0; return 0; }   // 座標値ゼロ＝パスならコウ解消0して戻る
            if (tz > bdMax) { return 0; }   // 盤外の座標なら 投了
            if (tz == ko_z) { return 2; }   // 直前のコウ抜き座標には置けない
            if (board[tz] != Space) { return 4; }   // 空点ではない座標には置けない

            var oppColor = 3 - color;       // 相手の石色
            var totalDeadStones = 0;        // 取れる石の合計
            var ko_kamo = 0;                // コウになるかもしれない場所
            var ko_kamo_count = 0;          // コウになるかもしれない場所を見つけたら+1,2になったら2方向のコウ抜きでコウ解消できる
            int[] countDir = new int[4];    // 隣の要素数 [0]=ダメ数、[color]=味方の石数、[3]=盤外数

            board[tz] = color;              // tz座標に置いてみる

            // 置いた石の 周囲と、相手の石を取れるか、を調べる
            for (int i = 0; i < 4; i++)
            {
                var z = tz + dir4[i];       // 隣の座標
                var c = board[z];           // 隣の状態、石or盤外
                if (c != oppColor)
                {        // 隣が 相手の石以外 なら、
                    countDir[c]++;              // 隣の要素をカウントして、
                    continue;                   // 次の方向へ
                }
                // 以下、隣は相手の石
                ClearCheckCount();                  // チェック配列とカウント配列を初期化して
                ScanRen(z, oppColor);               // 相手の連石カウント ＆ 連周囲要素をカウント
                if (count[Space] > 0) { continue; } // ダメがあるので取れない、次の方向へ
                // 以下、ダメが無いので取れる
                kesu(z, oppColor);                                          // 取れる石を消す
                totalDeadStones += count[oppColor];                         // 取石合計に連石数を加える
                if (count[oppColor] == 1) { ko_kamo = z; ko_kamo_count++; } // 取石が１つだけなら コウかも+1
                if (ko_kamo_count >= 2) { ko_kamo = 0; }                    // 2方向以上コウ抜きできるなら、コウかも解消
            }

            // 置いた石の連を調べる
            ClearCheckCount();              // チェック配列とカウント配列を初期化して
            ScanRen(tz, color);             // 置いた自分の石の連石カウント ＆ 連周囲要素をカウント
            if (count[Space] == 0) { board[tz] = Space; return 1; }     // ダメが無いので自殺手、置いた石は消しておく
            // 以下、ダメがある
            if (ko_kamo_count == 1 && totalDeadStones == 1 && count[Space] == 1)
            {
                ko_z = ko_kamo;             // (コウかもカウント１、取石合計１、ダメ1) なら コウ発生
            }
            else
            {
                ko_z = 0;
            }

            // モンテカルロ法の禁じ手として、自分の一眼をつぶす手は禁止
            if (countDir[OutSd] + countDir[color] == 4) { board[tz] = Space; return 3; }

            return 0;
        }

        ///// <summary>
        ///// 石を置く、てすと
        ///// </summary>
        ///// <param name="tz"></param>
        ///// <param name="color"></param>
        ///// <returns></returns>
        //private static int Move(int tz, int color)
        //{
        //    if (tz == 0) { ko_z = 0; return 0; } // パスならコウを初期化して戻る
        //    if (tz > bdMax) { return 0; }        // 投了

        //    // 4方向の連の状態を調べる
        //    var count = new int[4];//// count[0]:空点数、count[1]:黒石数、count[2]:白石数、count[3]:盤外数
        //    int[,] around = new int[4, 3];      // 4方向の連の 空点、石数、色
        //    int un_col = 3 - color;             // 相手の石色
        //    int mikata_safe = 0;                // ダメ２つ以上で安全な方向数
        //    int take_sum = 0;                   // 取れる石の合計
        //    int ko_kamo = 0;                    // コウになるかもしれない場所
        //    for (int i = 0; i < 4; i++)
        //    {
        //        int z = tz + dir4[i];
        //        int c = board[z];   // 石の色
        //        count[c]++;
        //        if (c == Space || c == OutSd) continue;
        //        int dame = 0;       // ダメの数
        //        int ishi = 0;       // 石の数
        //        Count_dame(z, ref dame, ref ishi);
        //        around[i, 0] = dame;
        //        around[i, 1] = ishi;
        //        around[i, 2] = c;
        //        if (c == un_col && dame == 1) { take_sum += ishi; ko_kamo = z; }// 相手の石を取れる場合は、取石を加算し、コウかも座標を保存
        //        if (c == color && dame >= 2) { mikata_safe++; }                 // 自分の石でダメが２以上の安全な方向カウント＋１
        //    }
        //    // 禁じ手チェック
        //    if (take_sum == 0 && count[Space] == 0 && mikata_safe == 0) return 1;  // 自殺手
        //    if (tz == ko_z) return 2;  // コウ
        //    if (count[OutSd] + mikata_safe == 4) return 3;  // 自分の一眼をつぶす：モンテカルロ法の特徴の為、禁止
        //    if (board[tz] != 0) return 4;  // 既に石がある
        //    // 上の禁じ手チェックをパスしたので、以下の処理を行う
        //    // 取石の処理、消して、ハマに追加
        //    for (int i = 0; i < 4; i++)
        //    {
        //        int d = around[i, 0];
        //        int n = around[i, 1];
        //        int c = around[i, 2];
        //        if (c == un_col && d == 1 && board[tz + dir4[i]] != 0)
        //        { // 石が取れる
        //            kesu(tz + dir4[i], un_col);
        //            hama[color] += n;
        //        }
        //    }
        //    board[tz] = color;  // 石を置く
        //    int dame2 = 0;       // ダメの数
        //    int ishi2 = 0;       // 石の数
        //    Count_dame(tz, ref dame2, ref ishi2);
        //    if (take_sum == 1 && ishi2 == 1 && dame2 == 1) {    // コウになる
        //        ko_z = ko_kamo;
        //    }
        //    else {
        //        ko_z = 0;
        //    }
        //    return 0;
        //}

        // 位置tzの石の数とダメの数を計算
        private static void Count_dame(int tz, ref int dame, ref int ishi)
        {
            for (int i = 0; i < bdMax; i++) { check[i] = 0; } // チェック用碁盤を初期化0
            CountDameSubZ(tz, board[tz], ref dame, ref ishi);        // 数える再帰関数
        }

        /******************************************************************************/
        /// <summary>
        /// <para> 盤面:board[]の座標(x,y)で指定した連の 石数 と ダメ数 を得る</para>
        /// </summary>
        /// <param name="z">碁盤のz(1次元化)座標</param>
        /// <param name="color">自分の石色</param>
        /// <param name="dame">連のダメ(空点)</param>
        /// <param name="ishi">連の石数</param>
        static void CountDameSubZ(int z, int color, ref int dame, ref int ishi)
        {
            // ishiカウンタを＋１、この座標はチェック済み(1)とする
            ishi++; check[z] = 1;
            // 隣が             空点    且つ      未チェック0  なら、dameカウンタを＋１,チェック済み(1)とする
            if (board[z - 1    ] == Space && check[z - 1    ] == 0) { dame++; check[z -     1] = 1; } // 左
            if (board[z + 1    ] == Space && check[z + 1    ] == 0) { dame++; check[z +     1] = 1; } // 右
            if (board[z - bdWid] == Space && check[z - bdWid] == 0) { dame++; check[z - bdWid] = 1; } // 上
            if (board[z + bdWid] == Space && check[z + bdWid] == 0) { dame++; check[z + bdWid] = 1; } // 下
            // 隣が        自分の石色     且つ      未チェック0   なら、 再帰呼び出し
            if (board[z - 1    ] == color && check[z - 1    ] == 0) CountDameSubZ(z - 1    , color, ref dame, ref ishi);   // 左
            if (board[z + 1    ] == color && check[z + 1    ] == 0) CountDameSubZ(z + 1    , color, ref dame, ref ishi);   // 右
            if (board[z - bdWid] == color && check[z - bdWid] == 0) CountDameSubZ(z - bdWid, color, ref dame, ref ishi);   // 上
            if (board[z + bdWid] == color && check[z + bdWid] == 0) CountDameSubZ(z + bdWid, color, ref dame, ref ishi);   // 下
        }

        /******************************************************************************/
        //  囲碁プログラムの為の多機能再帰関数（1次元配列用）
        /// <summary>
        /// <para>連の生死、連の石数、周囲の要素数 を調べる。</para>
        /// <para>count[color]：連の石数</para>
        /// <para>count[3-color]：連周囲の相手の石数</para>
        /// <para>count[Space]：連周囲のダメ数</para>
        /// <para>count[OutSd]：連周囲の盤外数</para>
        /// <para>フィールドに、 盤面配列 board[]、チェック用配列 check[]、カウント用 count[] が必要。</para>
        /// </summary>
        /// <param name="z">碁盤のz(1次元化)座標</param>
        /// <param name="color">連の石色</param>
        static void ScanRen(int z, int color)
        /// <returns>true(空点無) | false(空点有)</returns>
        //static bool ScanRen(int z, int color)
        {
            // 連の石数を＋１、連チェック済み(1)
            count[color]++; check[z] = 1;
            // 隣が 連のcolor石ではなく 且つ    未チェック0   なら、  各要素カウンタを＋１    チェック済み(-1)とする
            if (board[z + dU] != color && check[z + dU] == 0) { count[board[z + dU]]++; check[z + dU] = -1; } // 上
            if (board[z + dL] != color && check[z + dL] == 0) { count[board[z + dL]]++; check[z + dL] = -1; } // 左
            if (board[z + dR] != color && check[z + dR] == 0) { count[board[z + dR]]++; check[z + dR] = -1; } // 右
            if (board[z + dD] != color && check[z + dD] == 0) { count[board[z + dD]]++; check[z + dD] = -1; } // 下
            // 隣が    連のcolor石で   且つ     未チェック0  なら、再帰呼び出し
            if (board[z + dU] == color && check[z + dU] == 0) ScanRen(z + dU, color);   // 上
            if (board[z + dL] == color && check[z + dL] == 0) ScanRen(z + dL, color);   // 左
            if (board[z + dR] == color && check[z + dR] == 0) ScanRen(z + dR, color);   // 右
            if (board[z + dD] == color && check[z + dD] == 0) ScanRen(z + dD, color);   // 下
            //// 戻り値
            //return count[Space] == 0;         // true(空点無) | false(空点有)// 石を置いて判定する場合
            ////return count[Space] - 1 == 0;     // true(空点無) | false(空点有)// 石を置かずに判定する場合
        }

        /******************************************************************************/
        // 碁盤の表示
        private static void DispGoban()
        {
            Console.WriteLine("    1 2 3 4 5 6 7 8 910111213141516171819".Substring(0, (bdWid * 2) - 1));
            for (int i = 1; i < bdWid - 1; i++) {
                Console.Write($"{i,2} ");
                for (int j = 1; j < bdWid - 1; j++) {
                    Console.Write(chr[board[sw1z(j, i)]]);
                }
                Console.WriteLine();
            }
        }

        internal static int sw1z(int x, int y) 
        { 
            return y * bdWid + x; 
        }

        /******************************************************************************/
        /// <summary>
        /// 石を消す、再帰関数
        /// </summary>
        private static void kesu(int z, int color)
        {
            board[z] = Space;  // 石を消す
            if (board[z + dU] == color) kesu(z + dU, color);  // 上
            if (board[z + dL] == color) kesu(z + dL, color);  // 左
            if (board[z + dR] == color) kesu(z + dR, color);  // 右
            if (board[z + dD] == color) kesu(z + dD, color);  // 下
        }

        private static int HumanSelectMove(int color)
        {
            // 入力指示メッセージ
            Console.WriteLine(color == Black ? "\n黒番：座標(x,y)を入力" : "\n白番：座標(x,y)を入力");
            Console.WriteLine("パスは 0 、投了は 99を入力");
            var str = ""; int x; int y;
            // 入力ループ
            while (true)
            {
                Console.Write("x,y : ");
                str = Console.ReadLine();
                if (str == null || str.Length == 0) { continue; }   // 未入力なら再入力
                if (str == "0") { return 0; }          // パス：値 0で入力ループを抜ける、戻り値0
                if (str == "99") { return bdMax + 1; }  // 投了：値99で入力ループを抜ける、戻り値最大座標+1（盤外に置くことは投了を意味する）
                if (!str.Contains(',')) { continue; }   // , が無ければ再入力
                var sX = str.Split(',')[0]; // x座標文字
                var sY = str.Split(',')[1]; // y座標文字
                if (!int.TryParse(sX, out x)) continue;  // 整数でなければ再入力
                if (!int.TryParse(sY, out y)) continue;  // 整数でなければ再入力
                if (x < 1 || x > bdWid - 2) continue;  // 範囲外なら再入力
                if (y < 1 || y > bdWid - 2) continue;  // 範囲外なら再入力
                return sw1z(x, y);
            }
        }

        /******************************************************************************/
        // 1次元用、チェック配列とカウンタ配列の初期化
        private static void ClearCheckCount()
        {
            Array.Clear(check, 0, check.Length);
            Array.Clear(count, 0, count.Length);
        }

        /******************************************************************************/
        // 各カウンタ値を表示
        /// <param name="renElem">連の要素</param>
        private static void DispCount(int renElem)
        {
            for (int i = 0; i < count.Length; i++)
            {
                Console.WriteLine($"{chr[i]}={count[i]} 個"
                                    + (i == renElem ? "(連)" : "")
                                    + (i == Space ? "(空点)" : ""));
            }
        }
    }
}
