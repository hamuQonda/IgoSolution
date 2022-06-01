using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace ConsoleIgo
{
    public class GoBan
    {
        // フィールド変数
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
        
        private static int komi = 6;                    // コミ（持碁の場合は 白に+0.5あるとして、白勝ちとする;
        private static int nRoBan;                      // ｎ路盤
        private static int bdWid;                       // ボード幅 ｎ路盤＋２(盤外を設ける)
        private static int bdMax;                       // 
        private static int[] board;                     // 盤面配列
        private static int[] check;                     // チェック用盤面配列  要素値0は未チェック、0以外はチェック済み
        private static int dL;                          // 左への移動量 -1
        private static int dR;                          // 右への移動量 +1
        private static int dU;                          // 上への移動量 -bdWid
        private static int dD;                          // 下への移動量 +bdWid
        private static int[] dir4;                      // 左右上下への移動量(forで回す場合に使用)
        private static int[] dirX;                      // dir4から時計回りの移動、(元座標+dir4+dirX)で斜めの位置となる
        private static int[] hama = new int[3];         // アゲハマ、hama[Black], hama[White]
        private static int[] kifu = new int[1000];      // 棋譜
        private static int ko_z;                        // コウで打てない位置
        private static int allPlayOuts;                 // playoutを行った回数
        private static int[] count = new int[10];       // 各要素のカウンタ、e.g. count[Black] 黒石のカウンタ
        private static string[] player = new string[3]; // player[Black], player[White] に 人:"com"／ｺﾝﾋﾟｭｰﾀ:"hum" 

                        //private static int moveTesuu;                        // 手数
                        //private static int koNum;                       // コウが発生した手数
                        private static bool endFlag = false;            // パス2回連続したらtrue
                        //public struct KiFu                              // 手番と座標の構造体
                        //{
                        //    public int Col;
                        //    public Point Poi;
                        //}
                        //private static List<KiFu> kifuLog = new List<KiFu>();
                        private static Random rand = new Random();      // 乱数
                        ///*-- Monte Carlo 用 ----------------------------------------------------------*/
                        //private static int[] boardBkup;

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
        // メイン
        static int Main(string[] args)
        {
            GameInitialize();           // 対局の設定
            int color = Black;          // 手番の色
            int tesuu = 0;              // 手数
            DispGoban();
            // 対局ループ
            while (true)
            {
                allPlayOuts = 0;    // playout回数を初期化0 ///MonteCarlo用///
                // 打ち手が コンピュータか？ yes→モンテカルロ法で一手を決める、no→人間がキー入力で一手を決める
                int z = player[color] == "Com" ? SelectBestMove(color) : HumanSelectMove(color);
                int err = Move(z, color);
                if (err != 0) { continue; }
                kifu[tesuu++] = z;  // 棋譜に記録
                DispGoban();        // 碁盤表示
                Console.WriteLine($"Bh:{hama[Black]},Wh:{hama[White]}");
                Console.WriteLine($"{tesuu}:{sw2z(z)},色={color},all_playouts={allPlayOuts}");
                // 盤サイズより大きい座標なら、投了
                if (z > bdMax)
                {
                    string msgToURyoU = (color == Black) ? "白の勝ち！ 黒は投了しました。" : "黒の勝ち！ 白は投了しました。";
                    Console.WriteLine(msgToURyoU);
                    Console.ReadKey();
                    break;
                }
                // パスが2回連続したら終局処理
                if (z == 0 && tesuu > 1 && kifu[tesuu - 2] == 0)
                {
                    Console.WriteLine("パスが2回連続したので終局");
                    int kekka = CountScore(color);
                    string msg = kekka > 0 ? $"{kekka - 0.5}黒の勝ち！" : $"{-kekka + 0.5}白の勝ち！";
                    Console.WriteLine(msg);
                    Console.ReadKey();
                    break;
                }
                color = 3 - color;
            }
            Console.WriteLine("/////// 終了 ///////");
            Console.ReadKey();
            return 0;
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
                if (str == "0" ) { return 0; }          // パス：0 を返す
                if (str == "99") { return bdMax + 1; }  // 投了：盤外の座標を返す
                if (!str.Contains(',')) { continue; }   // ',' が無ければ再入力
                var sX = str.Split(',')[0]; // x座標文字
                var sY = str.Split(',')[1]; // y座標文字
                if (!int.TryParse(sX, out x)) continue;  // 整数でなければ再入力
                if (!int.TryParse(sY, out y)) continue;  // 整数でなければ再入力
                if (x < 1 || x > bdWid - 2) continue;  // 範囲外なら再入力
                if (y < 1 || y > bdWid - 2) continue;  // 範囲外なら再入力
                return sw1z(x, y);
            }
        }

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
            var atari = 0;                  // 当たりとなっている連数
            var nanameOpp = 0;              // 
            int[] countDir = new int[4];    // 隣の要素数 [0]=ダメ数、[elem]=味方の石数、[3]=盤外数

            // 座標tzの周囲と、周囲の状態を調べる
            for (int i = 0; i < 4; i++) {
                var z = tz + dir4[i];       // tzの隣の座標
                var c = board[z];           // tzの隣の要素、石or空点or盤外
                countDir[c]++;              // 隣の要素をカウント
                if(board[z + dirX[i]] == oppColor) { nanameOpp++; } // 斜めに相手の石があったらカウント

                if (c == Space || c == OutSd) { continue; } // 隣が 空点か壁 なら、次の方向へ
                // 以下、石(自分or相手の)がある
                ClearCheckCount();          // チェック&カウント配列を初期化
                ScanRen(z, ref c);              // tzの隣の連石カウント＆連周囲要素カウント

                if (c == oppColor) {        // 隣が相手の連石
                    if (count[Space] > 1) { continue; } // tzに置いてもダメがあるので取れない、次の方向へ
                    // 以下、ダメが無いので取れる
                    KeSu(z, ref oppColor);                                      // 取れる石を消す
                    totalDeadStones += count[oppColor];                         // 取石合計に連石数を加える
                    hama[color] += totalDeadStones;
                    if (count[oppColor] == 1) { ko_kamo = z; ko_kamo_count++; } // 取石が１つだけなら コウかも+1
                    if (ko_kamo_count >= 2) { ko_kamo = 0; }                    // 2方向以上コウ抜きできるなら、コウかも解消
                    continue;
                }
                // 以下、隣が自分の石
                if (count[Space] == 1) { atari++; } // 当たり状態の自分の石があったら atariカウント＋１
            }
            board[tz] = color;              // tz座標に置く
            // 置いた石の連を調べる
            ClearCheckCount();              // チェック配列とカウント配列を初期化して
            ScanRen(tz, ref color);         // 置いた自分の石の連石カウント ＆ 連周囲要素をカウント
            if (count[Space] == 0) { board[tz] = Space; return 1; }     // ダメが無いので自殺手、置いた石は消しておく
            // 以下、ダメがある

            // モンテカルロ法の禁じ手として、自分の一眼をつぶす手は禁止
            //      4方向が相手の石と壁  且つ  当たりとなっている自分の石が無い 且つ 欠け目ではない、なら一眼
            if (countDir[OutSd] + countDir[color] == 4 && atari == 0 && nanameOpp < 2) { 
                board[tz] = Space; return 3;                                                // 置いてはいけない
            }

            ko_z = 0;                       // コウ解消
            if (ko_kamo_count == 1 && totalDeadStones == 1 && count[Space] == 1) {
                ko_z = ko_kamo;             // (コウかもカウント１、取石合計１、ダメ1) なら コウ発生
            }
            return 0;
        }

        /******************************************************************************/
        //  囲碁プログラムの為の多機能再帰関数（1次元配列用）
        /// <summary>
        /// <para>連の要素数、周囲の要素数 を調べる。</para>
        /// <para>count[elem]：連の要素数</para>
        /// <para>count[3-elem]：連周囲の相手の石数</para>
        /// <para>count[Space]：連周囲のダメ数</para>
        /// <para>count[OutSd]：連周囲の盤外数</para>
        /// <para>フィールドに、 盤面配列 board[]、チェック用配列 check[]、カウント用 count[] が必要。</para>
        /// </summary>
        /// <param name="z">碁盤のz(1次元化)座標</param>
        /// <param name="elem">連の石色</param>
        static void ScanRen(int z, ref int elem)
        /// <returns>true(空点無) | false(空点有)</returns>
        //static bool ScanRen(int z, int elem)
        {
            // 連の石数を＋１、連チェック済み(1)
            count[elem]++; check[z] = 1;
            // 隣が 連のcolor石ではなく 且つ    未チェック0   なら、  各要素カウンタを＋１    チェック済み(-1)とする
            if (board[z + dL] != elem && check[z + dL] == 0) { count[board[z + dL]]++; check[z + dL] = -1; } // 左
            if (board[z + dR] != elem && check[z + dR] == 0) { count[board[z + dR]]++; check[z + dR] = -1; } // 右
            if (board[z + dU] != elem && check[z + dU] == 0) { count[board[z + dU]]++; check[z + dU] = -1; } // 上
            if (board[z + dD] != elem && check[z + dD] == 0) { count[board[z + dD]]++; check[z + dD] = -1; } // 下
            // 隣が    連のcolor石で   且つ     未チェック0  なら、再帰呼び出し
            if (board[z + dL] == elem && check[z + dL] == 0) ScanRen(z + dL, ref elem);   // 左
            if (board[z + dR] == elem && check[z + dR] == 0) ScanRen(z + dR, ref elem);   // 右
            if (board[z + dU] == elem && check[z + dU] == 0) ScanRen(z + dU, ref elem);   // 上
            if (board[z + dD] == elem && check[z + dD] == 0) ScanRen(z + dD, ref elem);   // 下
            //// 戻り値
            //return count[Space] == 0;         // true(空点無) | false(空点有)// 石を置いて判定する場合
            ////return count[Space] - 1 == 0;     // true(空点無) | false(空点有)// 石を置かずに判定する場合
        }

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
                    dir4 = new int[4] { -1    , +1    , -bdWid, +bdWid }; // 左右上下への移動量(forで回す場合に使用)
                    dirX = new int[4] { -bdWid, +bdWid, +1    , -1     }; // dir4から時計回りの移動
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
        // 碁盤の表示
        private static void DispGoban()
        {
            //Console.Clear();
            Console.WriteLine("    1 2 3 4 5 6 7 8 910111213141516171819".Substring(0, (bdWid * 2) - 1));
            for (int i = 1; i < bdWid - 1; i++) {
                Console.Write($"{i,2} ");
                for (int j = 1; j < bdWid - 1; j++) {
                    Console.Write(chr[board[sw1z(j, i)]]);
                }
                Console.WriteLine();
            }
        }

        /******************************************************************************/
        // 可能な手に対してplayoutを行う  ///MonteCarlo用///
        private static int SelectBestMove(int color)
        {
            int try_num = 30;       // playoutを繰り返す回数
            int best_z = 0;         // 最善手の座標
            double best_value = -100;

            int[] board_copy = new int[bdMax];
            Array.Copy(board, board_copy, bdMax);   // 現局面を保存
            int ko_z_copy = ko_z;                   // コウの位置も保存
            int pB = hama[Black];                   // アゲハマを保存
            int pW = hama[White];                   // アゲハマを保存

            // すべての空点を着手候補に
            for (int y = 1; y < bdWid - 1; y++) { 
                for (int x = 1; x < bdWid - 1; x++) {
                    int z = sw1z(x, y);
                    if (board[z] != 0) continue;  // 空点ではないなら、次の座標へ
                    if (Move(z, color) != 0) continue;  // 置いてみてエラーなら、次の座標へ

                    var win_sum = 0;                    // 勝ち数合計の初期化0
                    for (int i = 0; i < try_num; i++) {             // playoutを繰り返す
                        int[] board_cpy2 = new int[bdMax];
                        Array.Copy(board, board_cpy2, board.Length);// 局面が壊れるので保存
                        int ko_z_cpy2 = ko_z;                       // コウの位置も保存

                        int win = -PlayOut(3 - color);              // プレイアウト
                        win_sum += win;
                        //DispGoban();  // playoutを打ち切った局面を表示
                        //Console.WriteLine($"win={win},{win_sum}");
                        Array.Copy(board_cpy2, board, board.Length);// 局面を戻す
                        ko_z = ko_z_cpy2;                           // コウも
                    }
                    double win_rate = (double)win_sum / try_num;    // 勝率
                    //DispGoban();
                    //Console.WriteLine($"z=({x},{y}), win={win_rate}");

                    if (win_rate > best_value) {                         // 最善手を更新
                        best_value = win_rate;
                        best_z = z;
                        Console.WriteLine($"best={sw2z(z)}, v={best_value:F4}, try_num={try_num}");
                    }

                    Array.Copy(board_copy, board, board.Length);    // 局面を戻す
                    ko_z = ko_z_copy;                               // コウも
                    hama[Black] = pB;                           // アゲハマを戻す
                    hama[White] = pW;                           // アゲハマを戻す
                }
            }
            return best_z;
        }

        /******************************************************************************/
        // プレイアウトを行う  ///MonteCarlo用///
        private static int PlayOut(int turn_color)
        {
            allPlayOuts++;          // PlayOut関数が呼ばれた回数
            var color = turn_color;
            var before_z = -1;          // 一手前の手初期化-1
            var loop_max = bdWid * bdWid + bdWid * 4;   // 3コウ対策で手数を制限
            for(int loop = 0; loop < loop_max; loop++) {
                // すべての空点を着手候補にする
                var kouho = new int[bdMax];
                var kouho_num = 0;
                int z = 0;
                for (int y = 1; y < bdWid - 1; y++) {
                    for (int x = 1; x < bdWid - 1; x++) {
                        z = sw1z(x, y);
                        if (board[z] != 0) continue;    // 空点以外ならスキップ
                        kouho[kouho_num] = z;           // 空点ならその座標を候補配列に登録
                        kouho_num++;
                    }
                }
                int r = 0;
                while (true) {        // 着手可能な手を一手見つけるまでループ
                    if (kouho_num == 0) {
                        z = 0;   // パス 
                    } else {
                        r = rand.Next() % kouho_num;    // 乱数で一手選ぶ
                        z = kouho[r];
                    }
                    if (Move(z, color) == 0)  break;    // 着手可能なので、この手を選ぶ
                    //着手不可
                    kouho[r] = kouho[kouho_num - 1];    // 末尾の手を代入し、この手を削除
                    kouho_num--;
                }
                if (z == 0 && before_z == 0) break;     // 連続パス
                before_z = z;
                /*
                DispGoban();
                Console.WriteLine($"loop={loop}, z=({sw2z(z)}), c={elem}, kouho_num={kouho_num}, ko_z=({sw2z(ko_z)})");
                Console.ReadLine();
                */
                color = 3 - color;          // 手番を入れ替え
            }
            return CountScoreM(turn_color); // playoutを開始した手番を渡し、勝敗結果を返す
        }

        /******************************************************************************/
        // 座標をキー入力
        static int DeadStoneCoordinates()
        {
            // 入力指示メッセージ
            Console.WriteLine("\n死に石(連)を指定してください。");
            Console.WriteLine("座標(x,y)を入力、99=終了、0=取消");
            // 入力ループ
            var str = "";
            int z, c;
            while (true)
            {
                Console.Write("x,y : ");
                str = Console.ReadLine();
                if (str == null || str.Length == 0) { continue; }   // 未入力なら再入力
                if (str == "0" && endFlag == false) { z = 0; break; }   // 直前の死に石指定を取り消し：値 0で入力ループを抜ける
                if (str == "99"                   ) { z = 99; break; }  // 死に石指定を終了：値99で入力ループを抜ける
                if (!str.Contains(',')            ) { continue; }   // ',' が無ければ再入力
                var sX = str.Split(',')[0]; // x座標文字
                var sY = str.Split(',')[1]; // y座標文字
                if (!int.TryParse(sX, out int x)  ) { continue; }   // 整数でなければ再入力
                if (!int.TryParse(sY, out int y)  ) { continue; }   // 整数でなければ再入力
                if (x < 1 || x > bdWid - 2        ) { continue; }   // 範囲外なら再入力
                if (y < 1 || y > bdWid - 2        ) { continue; }   // 範囲外なら再入力
                z = sw1z(x, y);
                c = board[z];
                if (c == 0                        ) { continue; }   // 座標に石が無ければ再入力
                break;  // 入力座標に問題無し、ループを抜ける
            }
            return z;   // 座標zを返す
        }

        ///******************************************************************************/
        //// 周囲に解消できるコウがあるか？？
        //private static bool KoKaisho(int elem, int z)
        //{
        //    if (KoNoKatati(elem, z + dir4[0])) { return true; } // 左
        //    if (KoNoKatati(elem, z + dir4[1])) { return true; } // 右
        //    if (KoNoKatati(elem, z + dir4[2])) { return true; } // 上
        //    if (KoNoKatati(elem, z + dir4[3])) { return true; } // 下
        //    return false;
        //}

        ///******************************************************************************/
        //// コウの形？
        //private static bool KoNoKatati(int elem, int z)
        //{
        //    if (board[z] == OutSd) return false;
        //    // 周囲の 相手石or壁を、カウント＋１
        //    for (int y = 0; y < 9; y++) { count[y] = 0; }         // カウンタを初期化０
        //    count[board[z + dir4[0]]]++;  // 左
        //    count[board[z + dir4[1]]]++;  // 右
        //    count[board[z + dir4[2]]]++;  // 上
        //    count[board[z + dir4[3]]]++;  // 下

        //    // 相手の石と壁で3方向囲まれていれば、コウの形
        //    if (count[3 - elem] + count[OutSd] == 3) {
        //        return true; 
        //    }
        //    return false;
        //}

        /******************************************************************************/
        // チェック配列とカウンタ配列の初期化
        private static void InitCheckCountArry()
        {
            Array.Clear(check, 0, check.Length);
            Array.Clear(count, 0, count.Length);
        }

        /******************************************************************************/
        //  囲碁プログラムの為の多機能再帰関数（1次元化配列用）
        /// <summary>
        /// <para>座標(x,y)の連を elem1 から elem2 に置き換える。</para>
        /// <para>連の要素数が count [ elem1 ] に入る。</para>
        /// <para>連周囲の各要素数が count [ 交点の要素No. ] に入る。</para>
        /// <para>フィールド変数は、 board[,] check[,] count[] 交点マーク要素No が必要。</para>
        /// </summary>
        /// <param name="tz">連の一座標</param>
        /// <param name="elem1">連の構成要素</param>
        /// <param name="elem2">この要素に置き換え</param>
        /// <returns>true(空点無) | false(空点有)</returns>
        static bool CheckRen(int tz, int elem1, int elem2)
        {
            // 再帰しない条件
            if (check[tz] != 0) return false; // この座標は、カウント済み
            if (board[tz] != elem1) return false; // この座標は、連の要素 elem1 ではない
            // 4方向の要素のカウント処理
            for (int i = 0; i < 4; i++) {
                var z = tz + dir4[i];
                // 隣が連の要素ではなく、且つ 未チェック なら、各要素カウンタを＋１  周囲チェック済み(-1)とする
                if (board[z] != elem1 && check[z] == 0) { count[board[z]]++; check[z] = -1; }
            }
            // 連要素をelem2にして、連カウンタを＋１、連チェック済み(elem1+1) ※elem1=空点(0値)でもチェック済みになるように+1）
            board[tz] = elem2; count[elem1]++; check[tz] = elem1 + 1;
            // 隣を調べる為に、再帰呼び出し
            for (int i = 0; i < 4; i++) { CheckRen(tz + dir4[i], elem1, elem2); }
            // 戻り値
            return count[Space] == 0;         // true(空点無) | false(空点有)
        }

        /******************************************************************************/
        // 勝敗の判定、
        static int CountScore(int turn_color)
        {
            //int score = 0;
            //int[] kind = new int[10];   // 盤上に残っている石数
            int bScore = 0;
            int wScore = 0;

            SpecifyDeadStones();   // 盤上の死に石を指定し、数をアゲハマへ追加する
            // 死に石を消す
            for (int y = 1; y < bdWid - 1; y++) {
                for (int x = 1; x < bdWid - 1; x++) {
                    var z = sw1z(x, y);
                    if (board[z] == BDead || board[z] == WDead) { board[z] = Space; }
                } 
            }
            DispGoban();

            // 陣地を数える
            for (int y = 1; y < bdWid - 1; y++) {
                for (int x = 1; x < bdWid - 1; x++) {
                    var z = sw1z(x, y);
                    int c = board[z];
                    //kind[c]++;
                    if (board[z] != Space) { continue; }    // 空点でなければスキップ
                    // 以下、空点の処理
                    InitCheckCountArry();                   // カウンタを初期化０
                    CheckRen(z, Space, XDame);                  // 連空点の数と周囲要素のカウント
                    if (count[Black] > 0 && count[White] == 0) {   // 黒石だけに囲まれている
                        bScore += count[Space];
                        //score += count[Space];
                    }
                    if (count[White] > 0 && count[Black] == 0) {   // 白石だけに囲まれている
                        wScore += count[Space];
                        //score -= count[Space];
                    }
                }
            }

            DispGoban();
            Console.WriteLine("------結果-------");
            Console.WriteLine($"黒地{bScore}+黒浜{hama[Black]}");
            Console.WriteLine($"白地{wScore}+白浜{hama[White]}+コミ{komi}.5");

            bScore += hama[Black];
            wScore += hama[White] + komi;
            return bScore - wScore;
        }

        /******************************************************************************/
        // 地を数えて勝ちか負けを返す関数  ///MonteCarlo用///
        static int CountScoreM(int turn_color)
        {
            int score = 0;
            int[] kind = new int[3];    // 盤上に残っている石数
            // 陣地を数える
            for (int y = 1; y < bdWid - 1; y++) {
                for (int x = 1; x < bdWid - 1; x++) {
                    var z = sw1z(x, y);
                    int c = board[z];
                    kind[c]++;          // 色別に石数をカウント
                    if (c != 0) continue;   // 空点でなければ次の座標へ
                    // 空点なら以下の処理
                    int[] mk = new int[4];  // 4方向の石を種類別に数える
                    for (int i = 0; i < 4; i++) { mk[board[z + dir4[i]]]++; }
                    if (mk[Black] > 0 && mk[White] == 0) score++;   // 黒だけに囲まれているので黒地
                    if (mk[White] > 0 && mk[Black] == 0) score--;   // 白だけに囲まれているので白地
                }
            }
            score += (kind[Black] - kind[White]);   // 地差に盤上の石数の差を加える
            double final_score = score - komi - 0.5;
            int win = 0;
            if (final_score > 0) win = 1;           // 黒が勝っていれば  1
            if (turn_color == White) win = -win;    // 白が勝っていれば -1
            return win;
        }

        /******************************************************************************/
        // 死に石を指定
        static void SpecifyDeadStones()
        {
            int z;
            int c = 0;
            while (true)
            {
                // 死に石の座標をキー入力
                z = DeadStoneCoordinates();
                if (z == 99) { break; }         // 指定終了
                if (z == 0 && (c == Black || c == White)) { // 直前の死に石指定の取消
                    hama[3 - c] -= count[c];    // 死に石指定前のアゲハマ数に戻す
                    RestoreBoard();             // 死に石指定前の盤面に戻す
                    InitCheckCountArry();
                    DispGoban(); 
                    c = 0;
                    continue;
                }
                c = board[z];
                if (c == Black || c == White) {     // 黒か白の石だった場合
                    // 死に石のカウントとマーキング
                    InitCheckCountArry();
                    CheckRen(z, c, c + 5);
                    hama[3 - c] += count[c];        // 死に石数をアゲハマに追加
                }
                else if (c == BDead || c == WDead) {     // 死マーク済みの石だった場合
                    // 死に石のカウントとマーキングの取消
                    InitCheckCountArry();
                    CheckRen(z, c, c - 5);
                    hama[3 - (c - 5)] -= count[c];  // 死に石指定前のアゲハマ数に戻す
                }
                DispGoban();        // 死に石指定中の盤面表示
            }
        }

        /******************************************************************************/
        // 盤面を前の状態に戻す
        private static void RestoreBoard()
        {
            for(int z = 0; z < bdMax; z++) {
                if (check[z] > 0) { board[z] = check[z] - 1; }
            }
                    Array.Clear(check, 0, check.Length);
        }

        /******************************************************************************/
        /// <summary>
        /// 石を消す、再帰関数
        /// </summary>
        private static void KeSu(int z, ref int color)
        {
            // この座標の石を消す
            board[z] = Space;
            // 隣の石が  　同じ色    なら  再帰で消す
            if (board[z + dL] == color) KeSu(z + dL, ref color);  // 左
            if (board[z + dR] == color) KeSu(z + dR, ref color);  // 右
            if (board[z + dU] == color) KeSu(z + dU, ref color);  // 上
            if (board[z + dD] == color) KeSu(z + dD, ref color);  // 下
        }

        /******************************************************************************/
        // 1次元用、チェック配列とカウンタ配列の初期化
        private static void ClearCheckCount()
        {
            Array.Clear(check, 0, check.Length);
            Array.Clear(count, 0, count.Length);
        }

        ///******************************************************************************/
        //// 盤面を元に戻す
        //private static void RestoreBoard()
        //{
        //    for (int y = 0; y < bdWid - 1; y++) {
        //        for (int x = 0; x < bdWid - 1; x++) {
        //            var z = sw1z(x, y);
        //            if (check[z] > 0) board[z] = check[z] - 1;
        //        }
        //    }
        //}

        ///******************************************************************************/
        //// 思考エンジン（ランダムな ｘ、ｙ）
        //static void RandomXY(int elem, ref Point p)
        //{
        //    // すべての空点を調べ、合法な空点が無ければ、パス
        //    var countM = 0; // 合法な（石を置くことが可能な）空点をカウントする変数
        //    for (int y = 1; y < bdWid - 1; y++) for (int j = 1; j < bdWid - 1; j++)
        //        {
        //            var z = sw1z(j, y);
        //            if (board[z] != Space) continue;    // 空点以外はスキップ
        //            // 空点なら以下
        //            p.X = j; p.Y = y;
        //            if (CheckLegal(elem, z)) countM++; // 合法ならカウント＋１
        //        }
        //    if(countM > 0) { 
        //        // 置ける空点がある
        //        p.X = rand.Next(1, bdWid - 1);
        //        p.Y = rand.Next(1, bdWid - 1);
        //    }
        //    else
        //    {   // 置ける空点が無い
        //        p.X = 0; p.Y = 0;                   // パス
        //    }
        //}
    }
}
