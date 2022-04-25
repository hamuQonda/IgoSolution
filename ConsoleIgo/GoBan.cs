using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace ConsoleIgo
{
    partial class GoBan
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
        private static int[] countMk = new int[10];     // 各要素のカウンタ、e.g. countMk[Black] 黒石のカウンタ

        private static int boardSize;                   // ボードサイズ ｎ路盤＋２(盤外を設ける)
        private static int[] board;                    // 盤面配列
        private static int[] check;                    // チェック用盤面配列  要素値0は未チェック、0以外はチェック済み
        private static int komi = 6;                    // コミ（持碁の場合は 白に+0.5あるとして、白勝ちとする;
        private static string[] player = new string[3]; // player[Black], player[White] に 人:"com"／ｺﾝﾋﾟｭｰﾀ:"hum" 
        private static int move;                        // 手数
        private static int[] prisoner = new int[3];     // アゲハマ、prizoner[Black], prizoner[White]
        private static Point ko;                        // コウで打てない位置
        private static int koNum;                       // コウが発生した手数
        private static bool endFlag = false;            // パス2回連続したらtrue
        public struct KiFu                              // 手番と座標の構造体
        {
            public int Col;
            public Point Poi;
        }
        private static List<KiFu> kifuLog = new List<KiFu>();
        private static Random rand = new Random();      // 乱数
        /*-- Monte Carlo 用 ----------------------------------------------------------*/
        private static int allPlayOuts;                 // playoutを行った回数
        private static int[] boardBkup;

        /******************************************************************************/
        // メイン
        static void Main(string[] args)
        {
            GameSettings();                                 // 対局の設定
            int movColor = Black;                           // 手番の色
            Point movePoint = new Point(999, 999);          // 着手位置
            Point previousMovePoint = new Point(999, 999);  // ひとつ前の着手位置
            int score = 0;                                  // 黒地引く白地引くコミ

            InitializeBoard();// 碁盤の初期化
            Console.WriteLine("/////// コンソール碁盤 ///////");
            prisoner[Black] = 0;
            prisoner[White] = 0;
            move = 0;

            // 対局ループ
            while (true)
            {
                allPlayOuts = 0;    // playout回数を初期化0 ///MonteCarlo用///
                DispGoban();                        // 碁盤表示
                ThinkMove(movColor, ref movePoint); // 着手位置を決める

                // 盤サイズより大きい座標なら、投了
                if (movePoint.X > boardSize - 2 || movePoint.Y > boardSize - 2) {
                    string msgToURyoU = (movColor == Black) ? "白の勝ち！ 黒は投了しました。" : "黒の勝ち！ 白は投了しました。";
                    Console.WriteLine(msgToURyoU);
                    Console.ReadKey();
                    break;
                }

                // パスが2回連続したら終局処理
                if ((movePoint.X < 1 || movePoint.Y < 1) && (previousMovePoint.X < 1 || previousMovePoint.Y < 1)) {
                    endFlag = true;
                    score = (int)CountScore(movColor);
                    if (score > 0) { Console.WriteLine($"{score-.5}目、黒の勝ち！！"); }
                    if (score < 0) { Console.WriteLine($"{-score+.5}目、白の勝ち！！"); }
                    if (score == 0) Console.WriteLine("0.5目！白の勝ち！"); 
                    Console.ReadKey();
                    break;
                }

                // パスでなければ石を置く
                if ((movePoint.X + movePoint.Y) != 0) { SetStone(movColor, ref movePoint); }

                // 棋譜に記録
                RecordMove(movColor, movePoint);

                // 次の手番へ
                previousMovePoint.X = movePoint.X;  // 直前の座標を保存
                previousMovePoint.Y = movePoint.Y;  // 直前の座標を保存
                movColor = 3 - movColor;            // 手番の色交代
                move++;                             // 手数を＋１
            }
            Console.WriteLine("/////// 終了 ///////");
        }

        /******************************************************************************/
        // 対局の設定
        private static void GameSettings()
        {
            while (true)
            {
                Console.Write("盤の大きさ(5,9,13,19) = ");
                var gobanSizeStr = Console.ReadLine();
                if (gobanSizeStr == "5" || gobanSizeStr == "9" || gobanSizeStr == "13" || gobanSizeStr == "19")
                {
                    boardSize = int.Parse(gobanSizeStr) + 2;
                    board = new int[boardSize, boardSize];
                    check = new int[boardSize, boardSize];
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
        // 碁盤の初期化
        static void InitializeBoard()
        {
            board = new int[boardSize, boardSize];
            for (int i = 0; i < boardSize; i++)
            {
                board[0, i] = 3;
                board[i, 0] = 3;
                board[boardSize - 1, i] = 3;
                board[i, boardSize - 1] = 3;
            }
        }

        /******************************************************************************/
        // 碁盤の表示
        static void DispGoban()
        {
            Console.Clear();
            Console.WriteLine("    1 2 3 4 5 6 7 8 910111213141516171819".Substring(0, (boardSize - 2) * 2 + 3));
            for (int i = 1; i < boardSize - 1; i++) {
                Console.Write($"{i,2} ");
                for (int j = 1; j < boardSize - 1; j++) {
                    Console.Write(chr[board[i, j]]);
                }
                Console.WriteLine();
            }
        }

        /******************************************************************************/
        // 着手位置の入力、又は、思考エンジン
        static void ThinkMove(int color, ref Point p)
        {
            Point input = new Point(999, 999);
            while (true)
            {
                if (player[color]=="Hum") {             // プレイヤーが人間なら
                    InputCoordinate(color, ref input);      // 着手座標をキー入力
                }
                else {                                  // プレイヤーがコンピュータなら
                    //RandomXY(color, ref input);             // 思考エンジン（乱数）
                    input = SelectBestMove(color);                  // 可能な手に対してplayoutを行う // 思考エンジン（モンテカルロ）
                }

                if ((input.X > 0 && input.X < boardSize - 1) &&  // 座標が適正か？
                    (input.Y > 0 && input.Y < boardSize - 1)) {
                    // 合法手ならwhileループを抜ける
                    if (CheckLegal(color, input)) { break; }
                }
                else { break; }
            }
            p.X = input.X;
            p.Y = input.Y;
        }

        /******************************************************************************/
        // 可能な手に対してplayoutを行う  ///MonteCarlo用///
        private static Point SelectBestMove(int color)
        {
            int try_num = 5;                           // playoutを繰り返す回数
            Point bestP = new Point { X = 0, Y = 0 };   // 最善手
            double best_value = -100;

            int[,] board_copy = new int[boardSize, boardSize];
            Array.Copy(board, board_copy, board.Length);        // 現局面を保存
            Point ko_copy = new Point { X = ko.X, Y = ko.Y };   // コウの位置も保存
            int pB = prisoner[Black];                           // アゲハマを保存
            int pW = prisoner[White];                           // アゲハマを保存

            // すべての空点を着手候補に
            for (int y = 1; y < boardSize - 1; y++) {
                for (int x = 1; x < boardSize - 1; x++) { 
                    //int z = y * boardSize + x;
                    if (board[y, x] != 0) continue;         // 既に石がある

                    var p = new Point() { X = x, Y = y };
                    if (!CheckLegal(color, p)) continue;
                    SetStone(color, ref p);                 // 石を置いてみる

                    var win_sum = 0;                        // 勝ち数合計の初期化0
                    for (int i = 0; i < try_num; i++) {     // playoutを繰り返す
                        int[,] board_cpy2 = new int[boardSize, boardSize];
                        Array.Copy(board,board_cpy2, board.Length);         // 局面が壊れるので保存
                        Point ko_cpy2 = new Point { X = ko.X, Y = ko.Y };   // コウの位置も保存

                        int win = -PlayOut(3 - color);     // プレイアウト
                        win_sum += win;
                        //DispGoban();  // playoutを打ち切った局面を表示
                        //Console.WriteLine($"win={win},{win_sum}");
                        Array.Copy(board_cpy2, board, board.Length);    // 局面を戻す
                        ko.X = ko_cpy2.X; ko.Y = ko_cpy2.Y;             // コウも
                    }
                    double win_rate = (double)win_sum / try_num;        // 勝率
                    //DispGoban();
                    //Console.WriteLine($"z=({x},{y}), win={win_rate}");

                    if(win_rate > best_value) {                         // 最善手を更新
                        best_value = win_rate;
                        bestP.X = x;
                        bestP.Y = y;
                        Console.WriteLine($"best=({bestP.X},{bestP.Y}), v={best_value:F4}, try_num={try_num}");
                    }

                    Array.Copy(board_copy, board, board.Length);    // 局面を戻す
                    ko.X = ko_copy.X; ko.Y = ko_copy.Y;             // コウも
                    prisoner[Black] = pB;                           // アゲハマを戻す
                    prisoner[White] = pW;                           // アゲハマを戻す
                }
            }
            return bestP;
        }

        /******************************************************************************/
        // プレイアウトを行う  ///MonteCarlo用///
        private static int PlayOut(int turn_color)
        {
            allPlayOuts++;          // PlayOut関数が呼ばれた回数
            int color = turn_color;
            Point beforP = new Point() { X = -1, Y = -1 };          // 一手前の手初期化-1
            int loop_max = boardSize * boardSize + boardSize * 4;   // 3コウ対策で手数を制限
            for(int loop = 0; loop < loop_max; loop++) {    // すべての空点を着手候補にする
                var kouho = new Point[(boardSize - 2) * (boardSize - 2)];
                int kouho_num = 0;
                for (int y = 1; y < boardSize - 1; y++) {
                    for (int x = 1; x < boardSize - 1; x++) {
                        if (board[y, x] != 0) continue;
                        kouho[kouho_num] = new Point(x, y);
                        kouho_num++;
                    }
                }
                Point z;
                int r;
                while (true) {        // 着手可能な手を一手見つけるまでループ
                    if (kouho_num == 0) {
                        z = new Point() { X = 0, Y = 0 };   // パス 
                        break;
                    } else {
                        r = rand.Next(0, kouho_num - 1) % kouho_num;        // 乱数で一手選ぶ
                        z = kouho[r];
                    }
                    if (CheckLegal(color, z)) { SetStone(color, ref z); break; }    // 着手可能なので、この手を選ぶ
                    //着手不可
                    kouho[r] = kouho[kouho_num - 1];        // 末尾の手を代入し、この手を削除
                    kouho_num--;
                }
                if ((z.X + z.Y) == 0 && beforP.X + beforP.Y == 0) { break; } // 連続パス
                beforP.X = z.X;
                beforP.Y = z.Y;
                //DispGoban();
                //Console.WriteLine($"loop={loop}, z=({z.X},{z.Y}), c={color}, kouho_num={kouho_num}, ko_z=({ko.X},{ko.Y})");
                color = 3 - color;
            }
            return CountScoreM(turn_color); // playoutを開始した手番を渡す
        }

        /******************************************************************************/
        // 座標をキー入力
        static void InputCoordinate(int color, ref Point keyIn)
        {
            // 入力指示メッセージ
            if (endFlag) { Console.WriteLine("\n死んでいる石の座標(x,y)を入力、終了は 99を入力"); }
            else         { Console.WriteLine(color == Black ? "\n黒番：座標(x,y)を入力" : "\n白番：座標(x,y)を入力");
                           Console.WriteLine("パスは 0 、投了は 99を入力"); }
            var str = "";
            while (true)
            {
                Console.Write("x,y : ");
                str = Console.ReadLine();
                if (str == null || str.Length == 0) { continue; }
                if (str == "0" && endFlag == false) { keyIn.X = 0; keyIn.Y = 0; break; }
                if (str == "99") { keyIn.X = 99; keyIn.Y = 99; break; }
                if (!str.Contains(',')) { continue; }
                var sX = str.Split(',')[0];
                var sY = str.Split(',')[1];
                if (!int.TryParse(sX, out int x))  continue;  // 整数でなければ再入力
                if (!int.TryParse(sY, out int y))  continue;  // 整数でなければ再入力
                if (x < 1 || x > boardSize - 2)    continue;  // 範囲外なら再入力
                if (y < 1 || y > boardSize - 2)    continue;  // 範囲外なら再入力
                keyIn.X = x;
                keyIn.Y = y;
                break;
            }
        }

        /******************************************************************************/
        // 合法手か調べる
        static bool CheckLegal(int color, Point p)
        {
            if (board[p.Y, p.X] != Space) { return false; }                             // 空点じゃないと置けない
            if (move > 1) {                                         
                if (ko.X == p.X && ko.Y == p.Y && koNum == move - 1) { return false; }  // 一手前にコウを取られていたら置けない
            }
            if (CheckSuicide(color, p)) { return false; }                               // 自殺手なら置けない

            // 以上のチェックを通過したので、合法手だが、
            // 乱数打ちの場合、自分の目を埋める手は禁止しておく
            if ((board[p.Y, p.X - 1] == color || board[p.Y, p.X - 1] == OutSd) &&
                (board[p.Y, p.X + 1] == color || board[p.Y, p.X + 1] == OutSd) &&
                (board[p.Y - 1, p.X] == color || board[p.Y - 1, p.X] == OutSd) &&
                (board[p.Y + 1, p.X] == color || board[p.Y + 1, p.X] == OutSd))
            {
                //// 手数が盤サイズの二乗を超えたら、コウの形を埋めるのはOK
                //if (move > (boardSize - 1)* (boardSize - 1)) {
                //    if (KoKaisho(color, p)) return true;// 置けます
                //}
                return false;
            }
            return true;// 置けます
        }

        /******************************************************************************/
        // 周囲に解消できるコウがあるか？？
        private static bool KoKaisho(int color, Point p)
        {
            if (KoNoKatati(color, p.X - 1, p.Y)) { return true; } // 左
            if (KoNoKatati(color, p.X + 1, p.Y)) { return true; } // 右
            if (KoNoKatati(color, p.X, p.Y - 1)) { return true; } // 上
            if (KoNoKatati(color, p.X, p.Y + 1)) { return true; } // 下
            return false;
        }

        /******************************************************************************/
        // コウの形？
        private static bool KoNoKatati(int color, int x, int y)
        {
            if (board[y, x] == OutSd) return false;
            // 周囲の 相手石or壁を、カウント＋１
            for (int i = 0; i < 9; i++) { countMk[i] = 0; }         // カウンタを初期化０
            countMk[board[y, x + 1]]++;  // 右
            countMk[board[y - 1, x]]++;  // 上
            countMk[board[y + 1, x]]++;  // 下
            countMk[board[y, x - 1]]++;  // 左

            // 相手の石と壁で3方向囲まれていれば、コウの形
            if (countMk[3 - color] + countMk[OutSd] == 3) {
                return true; 
            }
            return false;
        }

        /******************************************************************************/
        // 自殺手か調べる
        static bool CheckSuicide(int color, Point p)
        {
            int opponentColor;              // 相手の色
            board[p.Y, p.X] = color;        // 仮に石を置く

            InitCheckCountArry();         // チェック用碁盤と、要素カウンタを初期化
            if (CheckRen( p.X, p.Y, color, color)) // 囲まれているなら自殺手かもしれない
            {
                opponentColor = 3 - color;  // 相手の色

                if (board[p.Y, p.X - 1] == opponentColor) {     // 右隣が相手の石
                    InitCheckCountArry();
                    if (CheckRen( p.X - 1, p.Y, opponentColor, opponentColor)) {
                        board[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                if (board[p.Y, p.X + 1] == opponentColor) {     // 左隣が相手の石
                    InitCheckCountArry();
                    if (CheckRen( p.X + 1, p.Y, opponentColor, opponentColor)) {
                        board[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                if (board[p.Y - 1, p.X] == opponentColor) {     // 上隣が相手の石
                    InitCheckCountArry();
                    if (CheckRen( p.X, p.Y - 1, opponentColor, opponentColor)) {
                        board[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                if (board[p.Y + 1, p.X] == opponentColor) {     // 下隣が相手の石
                    InitCheckCountArry();
                    if (CheckRen( p.X, p.Y + 1, opponentColor, opponentColor)) {
                        board[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                board[p.Y, p.X] = Space;    // 仮石を元に戻す
                return true;                // 相手の石を取れないので、自殺手になる
            }
            else {
                board[p.Y, p.X] = Space;    // 仮石を元に戻す
                return false;               // 囲まれていないので、自殺手ではない
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
            if (check[y, x] != 0) return false; // この座標は、カウント済み
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
        // 勝敗の判定、
        static int CountScore(int turn_color)
        {
            int score = 0;              //*******************
            int[] kind = new int[10];    // 盤上に残っている石数
            int bScore = 0;
            int wScore = 0;

            DeadStones();   // 盤上の死に石を指定し、数をアゲハマへ追加する
            // 死に石を消す
            for (int y = 1; y < boardSize - 1; y++) {
                for (int x = 1; x < boardSize - 1; x++) {
                    if (board[y, x] == 4 || board[y, x] == 5) { board[y, x] = Space; }
                } 
            }
            DispGoban();

            // 陣地を数える
            for (int y = 1; y < boardSize - 1; y++)
            {
                for (int x = 1; x < boardSize - 1; x++)
                {
                    int c = board[y, x];
                    kind[c]++;
                    if (board[y, x] != Space) { continue; } // 空点でなければスキップ
                    // 空点なら以下の処理
                    InitCheckCountArry();                   // カウンタを初期化０
                    CheckRen(x, y, Space, XDame);           // 連空点の数と周囲要素のカウント
                    if (countMk[Black] > 0 && countMk[White] == 0) {   // 黒石だけに囲まれている
                        bScore += countMk[Space];
                        score += countMk[Space];
                        //InitCheckCountArry(); CheckRen(x, y, XDame, BArea);
                    }
                    if (countMk[White] > 0 && countMk[Black] == 0) {   // 白石だけに囲まれている
                        wScore += countMk[Space];
                        score -= countMk[Space];
                        //InitCheckCountArry(); CheckRen(x, y, XDame, WArea);
                    }
                }
            }
            score += (kind[Black] - kind[White]);
            double final_score = score - komi;
            DispGoban();
            int win = 0;
            if (final_score > 0) win = 1;           // 黒が勝っていれば  1
            if (turn_color == White) win = -win;    // 白が勝っていれば -win
            Console.WriteLine("------結果-------");
            Console.WriteLine($"黒地{bScore}+黒浜{prisoner[Black]}");
            Console.WriteLine($"白地{wScore}+白浜{prisoner[White]}+コミ{komi}.5");

            bScore += prisoner[Black];
            wScore += prisoner[White] + komi;
            return bScore - wScore;

            //return (int) final_score * win;
        }

        /******************************************************************************/
        // 地を数えて勝ちか負けを返す関数  ///MonteCarlo用///
        static int CountScoreM(int turn_color)
        {
            int score = 0;
            int[] kind = new int[3];    // 盤上に残っている石数

            // 陣地を数える
            for (int y = 1; y < boardSize - 1; y++)
            {
                for (int x = 1; x < boardSize - 1; x++)
                {
                    int c = board[y, x];
                    kind[c]++;
                    if (c != 0) continue;   // 空点でなければスキップ
                    // 空点なら以下の処理
                    int[] mk = new int[4];  // 4方向の石を種類別に数える
                    mk[board[y, x - 1]]++;
                    mk[board[y, x + 1]]++;
                    mk[board[y - 1, x]]++;
                    mk[board[y + 1, x]]++;
                    if (mk[Black] > 0 && mk[White] == 0) score++;   // 黒だけに囲まれているので黒地
                    if (mk[White] > 0 && mk[Black] == 0) score--;   // 白だけに囲まれているので白地
                }
            }
            score += (kind[Black] - kind[White]);
            double final_score = score - komi;
            int win = 0;
            if (final_score > 0) win = 1;           // 黒が勝っていれば  1
            if (turn_color == White) win = -win;    // 白が勝っていれば -1
            return win;
        }

        /******************************************************************************/
        // 死に石を指定
        static void DeadStones()
        {
            int color;
            Point p = new Point();
            while (true)
            {
                // 死に石の座標をキー入力
                InputCoordinate(OutSd, ref p);
                if (p.X == 99) { break; }
                color = board[p.Y, p.X];
                if (!(color == Black || color == White)) { continue; }// 座標に石が無かったら再入力

                // 死に石のカウントとマークキング
                InitCheckCountArry(); 
                CheckRen(p.X, p.Y, color, color + 3);
                prisoner[3 - color] += countMk[color];

                DispGoban();        // 碁盤表示
            }
        }

        /******************************************************************************/
        // 碁盤に石を置く
        static void SetStone(int color, ref Point p)
        {
            var oppColor = 3 - color;           // 相手の色
            bool koFlag = false;
            board[p.Y, p.X] = color;            // 座標に石を置く
            // 置いた石の4方向隣に同じ色の石があるか？
            if (board[p.Y + 1, p.X] != color &&
                board[p.Y - 1, p.X] != color &&
                board[p.Y, p.X + 1] != color &&
                board[p.Y, p.X - 1] != color) {
                // 同じ色の石が無いならコウかもしれない
                koFlag = true;  
            } else {
                koFlag = false; // コウではない
            }
            //   左のアゲハマ 、 右のアゲハマ 、 上のアゲハマ 、 下のアゲハマ
            int prisonerW = 0, prisonerE = 0, prisonerN = 0, prisonerS = 0;
            // 隣の相手の連が死んでいれば、碁盤から取り除く
            InitCheckCountArry();             // 左
            if (CheckRen(p.X - 1, p.Y, oppColor, Space)) prisonerW = countMk[oppColor];
            else RestoreBoard();
            InitCheckCountArry();             // 右
            if (CheckRen( p.X + 1, p.Y, oppColor, Space)) prisonerE = countMk[oppColor];
            else RestoreBoard();
            InitCheckCountArry();             // 上
            if (CheckRen( p.X, p.Y - 1, oppColor, Space)) prisonerN = countMk[oppColor];
            else RestoreBoard();
            InitCheckCountArry();             // 下
            if (CheckRen( p.X, p.Y + 1, oppColor, Space)) prisonerS = countMk[oppColor];
            else RestoreBoard();

            // 取り除かれた石の合計
            int prisonerAll = prisonerW + prisonerE + prisonerN + prisonerS;

            // 置いた石の隣に同じ色の石が無く、取り除かれた石が１つだけなら、コウ！
            if (koFlag == true && prisonerAll == 1) {
                koNum = move; // コウが発生した手数を記録、コウの座標を記録
                     if (prisonerW == 1) { ko.X = p.X - 1; ko.Y = p.Y; }
                else if (prisonerE == 1) { ko.X = p.X + 1; ko.Y = p.Y; }
                else if (prisonerN == 1) { ko.X = p.X; ko.Y = p.Y - 1; }
                else if (prisonerS == 1) { ko.X = p.X; ko.Y = p.Y + 1; }
            }

            // アゲハマの更新
            prisoner[color] += prisonerAll;
            //Console.WriteLine($"アゲハマ：{prisonerAll}(合計：{prisoner[color]})");
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
        // 棋譜に記録
        static void RecordMove(int color, Point p)
        {
            KiFu kifu = new KiFu() { Col = color, Poi = p };
            kifuLog.Add(kifu);
        }

        /******************************************************************************/
        // 思考エンジン（ランダムな ｘ、ｙ）
        static void RandomXY(int color, ref Point p)
        {
            // すべての空点を調べ、合法な空点が無ければ、パス
            var countM = 0; // 合法な（石を置くことが可能な）空点をカウントする変数
            for (int i = 1; i < boardSize - 1; i++) for (int j = 1; j < boardSize - 1; j++)
                {
                    if (board[i, j] != Space) continue; // 空点以外はスキップ
                    // 空点なら以下
                    p.X = j; p.Y = i;
                    if (CheckLegal(color, p)) countM++; // 合法ならカウント＋１
                }
            if(countM > 0) { 
                // 置ける空点がある
                p.X = rand.Next(1, boardSize - 1);
                p.Y = rand.Next(1, boardSize - 1);
            }
            else
            {   // 置ける空点が無い
                p.X = 0; p.Y = 0;                   // パス
            }
        }
    }
}
