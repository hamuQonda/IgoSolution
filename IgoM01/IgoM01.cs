using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace IgoM01
{
    internal class MonteCarloGo
    {
        // フィールド
        // 交点の要素  No.          空点=0,黒石=1,白石=2,盤外=3
        private static Char[] chr = { '・', '○', '●', '※' };
        private const int Space = 0;
        private const int Black = 1;
        private const int White = 2;
        private const int OutSd = 3;

        private static int komi = 6;                    // コミ（持碁の場合は 白に+0.5あるとして、白勝ちとする;
        private static int nRoBan;                      // ｎ路盤
        private static int bdWid;                       // ボード幅 ｎ路盤＋２(盤外を設ける)
        private static int bdMax;                       // 
        private static int[] board;                     // 盤面配列
        private static int[] check;                     // チェック用盤面配列  要素値0は未チェック、0以外はチェック済み
        private static int[] hama = new int[3];         // アゲハマ、hama[Black], hama[White]
        private static int[] kifu = new int[1000];      // 棋譜
        private static int ko_z;                        // コウで打てない位置
        private static int allPlayOuts;                 // playoutを行った回数
        private static int[] count = new int[4];        // 各要素のカウンタ、e.g. count[Black] 黒石のカウンタ
        private static string[] player = new string[3]; // player[Black], player[White] に 人:"com"／ｺﾝﾋﾟｭｰﾀ:"hum" 
        private static int dL;                          // 左への移動量 -1
        private static int dR;                          // 右への移動量 +1
        private static int dU;                          // 上への移動量 -bdWid
        private static int dD;                          // 下への移動量 +bdWid
        private static int[] dir4;                      // 左右上下への移動量(forで回す場合に使用)

        private static Random rand = new Random();      // 乱数
        private static Stopwatch stpWch = new Stopwatch();

        /******************************************************************************/
        // メイン
        static void Main(string[] args)
        {
            GameInitialize();                                 // 対局の設定
            DispGoban();
            var color = Black;                              // 手番の色
            Move(sw1z(2, 3), color);
            var tesuu = 0;                                  // 手数

            //while (true)
            //{
            //    allPlayOuts = 0;
            //    int z = MonteCarloMove(color); // 可能な手に対してモンテカルロ法で、一手(座標ｚ)を返す

            //}

            PlayOut(color);
            DispGoban();

            Console.ReadLine();
        }

        /// <summary>
        /// // 可能な手に対してプレイアウトを行い、一手座標ｚを返す
        /// </summary>
        /// <param name="color">手番の石色</param>
        /// <returns></returns>
        private static int MonteCarloMove(int color)
        {
            int tryNum = 0; // 
            int bestZ = 0;

            // １．全ての
            return bestZ;
        }

        /// <summary>
        /// Switch to 1D  // (x,y)座標を1次元(y*boardSize+x)に変換
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int sw1z(int x, int y) { return y * bdWid + x; }

        /// <summary>
        /// Switch to 2D // 1次元座標を2次元に変換
        /// </summary>
        /// <param name="z"></param>
        /// <returns>(x,y)</returns>
        public static (int, int) sw2z(int z) { int x = z % bdWid; int y = z / bdWid; return (x, y); }

        /******************************************************************************/
        // 対局の設定
        private static void GameInitialize()
        {
            while (true)
            {
                Console.Write("盤の大きさ(5,9,13,19) = ");
                var gobanSizeStr = Console.ReadLine();
                if (gobanSizeStr == "5" || gobanSizeStr == "9" || gobanSizeStr == "13" || gobanSizeStr == "19")
                {
                    nRoBan = int.Parse(gobanSizeStr);
                    bdWid = nRoBan + 2;
                    dL = -1;        // 左 移動量
                    dR = +1;        // 右 移動量
                    dU = -bdWid;    // 上 移動量
                    dD = +bdWid;    // 下 移動量
                    dir4 = new int[4] { -1, +1, -bdWid, +bdWid }; // 左右上下への移動量(forで回す場合に使用)
                    bdMax = bdWid * bdWid;
                    InitializeBoard();
                    break;
                }
                Console.WriteLine("err!");
            }
            while (true)
            {
                Console.Write("黒番( 1:人 or 2:ｺﾝﾋﾟｭｰﾀ) = ");
                var numStr = Console.ReadLine();
                if (numStr == "1" || numStr == "2")
                {
                    player[Black] = numStr == "1" ? "Hum" : "Com";
                    break;
                }
                Console.WriteLine("err!");
            }
            while (true)
            {
                Console.Write("白番( 1:人 or 2:ｺﾝﾋﾟｭｰﾀ) = ");
                var numStr = Console.ReadLine();
                if (numStr == "1" || numStr == "2")
                {
                    player[White] = numStr == "1" ? "Hum" : "Com";
                    break;
                }
                Console.WriteLine("err!");
            }
        }

        /******************************************************************************/
        // 碁盤の表示
        private static void DispGoban()
        {
            //Console.Clear();
            Console.WriteLine("    1 2 3 4 5 6 7 8 910111213141516171819".Substring(0, (bdWid * 2) - 1));
            for (int i = 1; i < bdWid - 1; i++)
            {
                Console.Write($"{i,2} ");
                for (int j = 1; j < bdWid - 1; j++)
                {
                    Console.Write(chr[board[sw1z(j, i)]]);
                }
                Console.WriteLine();
            }
        }

        /******************************************************************************/
        /// <summary>
        /// 碁盤の初期化
        /// </summary>
        static void InitializeBoard()
        {
            board = new int[bdMax];
            check = new int[bdMax];
            for (int i = 0; i < bdWid; i++)
            {
                board[sw1z(0, i)] = 3;
                board[sw1z(i, 0)] = 3;
                board[sw1z(bdWid - 1, i)] = 3;
                board[sw1z(i, bdWid - 1)] = 3;
            }
        }

        /******************************************************************************/
        // プレイアウトを行う  ///MonteCarlo用///
        private static int PlayOut(int turn_color)
        {
            allPlayOuts++;          // PlayOut関数が呼ばれた回数
            var color = turn_color;
            var before_z = -1;          // 一手前の手初期化-1
            var loop_max = bdWid * bdWid + bdWid * 4;   // 3コウ対策で手数を制限
            for (int loop = 0; loop < loop_max; loop++)
            {
                // すべての空点を着手候補にする
                var kouho = new int[bdMax];
                var kouho_num = 0;
                int z = 0;
                for (int y = 1; y < bdWid - 1; y++)
                {
                    for (int x = 1; x < bdWid - 1; x++)
                    {
                        z = sw1z(x, y);
                        if (board[z] != 0) continue;    // 空点以外ならスキップ
                        kouho[kouho_num] = z;           // 空点ならその座標を候補配列に登録
                        kouho_num++;
                    }
                }
                int r = 0;
                while (true)
                {        // 着手可能な手を一手見つけるまでループ
                    if (kouho_num == 0)
                    {
                        z = 0;   // パス 
                    }
                    else
                    {
                        r = rand.Next() % kouho_num;    // 乱数で一手選ぶ
                        z = kouho[r];
                    }
                    if (Move(z, color) == 0) break;    // 着手可能なので、この手を選ぶ
                    //着手不可
                    kouho[r] = kouho[kouho_num - 1];    // 末尾の手を代入し、この手を削除
                    kouho_num--;
                }
                if (z == 0 && before_z == 0) break;     // 連続パス
                before_z = z;
                //*
                DispGoban();
                Console.WriteLine($"loop={loop}, z=({sw2z(z)}), c={color}, kouho_num={kouho_num}, ko_z=({sw2z(ko_z)})");
                Console.ReadLine();
                //*/

                color = 3 - color;          // 手番を入れ替え
            }
            return 0;//CountScoreM(turn_color); // playoutを開始した手番を渡し、勝敗結果を返す
        }

        /// <summary>
        /// 石を置く
        /// </summary>
        /// <param name="tz"></param>
        /// <param name="color"></param>
        /// <returns>0＝正常手、0以外＝不正手</returns>
        private static int Move(int tz, int color)
        {
            if (tz == 0) { ko_z = 0 ; return 0; }   // 座標値ゼロ＝パスならコウ解消0して戻る
            if (tz > bdMax)         { return 0; }   // 盤外の座標なら 投了
            if (tz == ko_z)         { return 2; }   // 直前のコウ抜き座標には置けない
            if (board[tz] != Space) { return 4; }   // 空点ではない座標には置けない

            var oppColor = 3 - color;       // 相手の石色
            var totalDeadStones = 0;        // 取れる石の合計
            var ko_kamo = 0;                // コウになるかもしれない場所
            var ko_kamo_count = 0;          // コウになるかもしれない場所を見つけたら+1,2になったら2方向のコウ抜きでコウ解消できる
            int[] countDir = new int[4];    // 隣の要素数 [0]=ダメ数、[color]=味方の石数、[3]=盤外数

            board[tz] = color;              // tz座標に置いてみる

            // 置いた石の 周囲と、相手の石を取れるか、を調べる
            for (int i = 0; i < 4; i++) {
                var z = tz + dir4[i];       // 隣の座標
                var c = board[z];           // 隣の状態、石or盤外
                if (c != oppColor) {        // 隣が 相手の石以外 なら、
                    countDir[board[c]]++;       // 隣の要素をカウントして、
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
            if (count[Space] == 0 ) { board[tz] = Space; return 1; }     // ダメが無いので自殺手、置いた石は消しておく
            // 以下、ダメがある
            if (ko_kamo_count == 1 && totalDeadStones == 1 && count[Space] == 1) {
                ko_z = ko_kamo;             // (コウかもカウント１、取石合計１、ダメ1) なら コウ発生
            } else {
                ko_z = 0;
            }

            // モンテカルロ法の禁じ手として、自分の一眼をつぶす手は禁止
            if (countDir[OutSd] + countDir[color] == 4) { board[tz] = Space; return 3; }

            return 0;
        }

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
            if (board[z + dL] == Space && check[z + dL] == 0) { dame++; check[z + dL] = 1; } // 左
            if (board[z + dR] == Space && check[z + dR] == 0) { dame++; check[z + dR] = 1; } // 右
            if (board[z + dU] == Space && check[z + dU] == 0) { dame++; check[z + dU] = 1; } // 上
            if (board[z + dD] == Space && check[z + dD] == 0) { dame++; check[z + dD] = 1; } // 下
            // 隣が        自分の石色     且つ      未チェック0   なら、 再帰呼び出し
            if (board[z + dL] == color && check[z + dL] == 0) CountDameSubZ(z + dL, color, ref dame, ref ishi);   // 左
            if (board[z + dR] == color && check[z + dR] == 0) CountDameSubZ(z + dR, color, ref dame, ref ishi);   // 右
            if (board[z + dU] == color && check[z + dU] == 0) CountDameSubZ(z + dU, color, ref dame, ref ishi);   // 上
            if (board[z + dD] == color && check[z + dD] == 0) CountDameSubZ(z + dD, color, ref dame, ref ishi);   // 下
        }

        /******************************************************************************/
        /// <summary>
        /// 石を消す、再帰関数
        /// </summary>
        private static void kesu(int z, int color)
        {
            if (board[z] != color) {
                Console.WriteLine($"err!!!{sw2z(z)}C={board[z]},color={color}");
                Console.ReadLine();
                return;
            }

            board[z] = Space;  // 石を消す, 隣が同じ石色なら再帰で消す
            if (board[z + dL] == color) kesu(z + dL, color);  // 左
            if (board[z + dR] == color) kesu(z + dR, color);  // 右
            if (board[z + dU] == color) kesu(z + dU, color);  // 上
            if (board[z + dD] == color) kesu(z + dD, color);  // 下
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
            if (board[z + dL] != color && check[z + dL] == 0) { count[board[z + dL]]++; check[z + dL] = -1; } // 左
            if (board[z + dR] != color && check[z + dR] == 0) { count[board[z + dR]]++; check[z + dR] = -1; } // 右
            if (board[z + dU] != color && check[z + dU] == 0) { count[board[z + dU]]++; check[z + dU] = -1; } // 上
            if (board[z + dD] != color && check[z + dD] == 0) { count[board[z + dD]]++; check[z + dD] = -1; } // 下
            // 隣が    連のcolor石で   且つ     未チェック0  なら、再帰呼び出し
            if (board[z + dL] == color && check[z + dL] == 0) ScanRen(z + dL, color);   // 左
            if (board[z + dR] == color && check[z + dR] == 0) ScanRen(z + dR, color);   // 右
            if (board[z + dU] == color && check[z + dU] == 0) ScanRen(z + dU, color);   // 上
            if (board[z + dD] == color && check[z + dD] == 0) ScanRen(z + dD, color);   // 下
            //// 戻り値
            //return count[Space] == 0;         // true(空点無) | false(空点有)// 石を置いて判定する場合
            ////return count[Space] - 1 == 0;     // true(空点無) | false(空点有)// 石を置かずに判定する場合
        }

        /******************************************************************************/
        // 1次元用、チェック配列とカウンタ配列の初期化
        private static void ClearCheckCount()
        {
            Array.Clear(check, 0, bdMax);
            Array.Clear(count, 0, 4);
        }
    }
}
