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
        // 交点の状態
        const int Space = 0;    // 空点
        const int Black = 1;    // 黒石
        const int White = 2;    // 白石
        const int Outsd = 3;    // 盤外
        const int Bdead = 4;    // 黒死に石
        const int Wdead = 5;    // 白死に石
        const int Xdame = 6;    // ダメ
        const int Barea = 7;    // 黒地
        const int Warea = 8;    // 白地
        static int[] countMk = new int[9];   // 交点の状態のカウンタ 

        // 碁盤サイズ
        static int boardSize;   // n 路 ＋ 2(盤外用)
        // コミ
        static int komi = 6;     // 持碁の場合は 白に+0.5 として、白勝ちとする;
        // プレイヤー
        static string[] player = new string[3];         // player[Black], player[White] に com hum 
        // 手番と座標の構造体
        public struct KiFu {
            public int Col;
            public Point Poi;
        }
        //                      0空点,1黒石,2白石,3盤外,4黒死,5白死,6ダメ,7黒地,8白地     // 4以上はマークの主な用途
        static char[] banChr = { '＋', '○', '●', '？', '▽', '▼', '×', '◇', '◆' };  // コンソール碁盤表示用文字
        static int[,] goban;                            // 碁盤配列
        static int move;                                // 手数
        static int[] prisoner = new int[3];             // アゲハマ、prizoner[Black], prizoner[White]
        static Point ko;                                // 劫の位置
        static int koNum;                               // コウが発生した手数
        static List<KiFu> kifuLog = new List<KiFu>();

        static int[,] chkGoban;                         // チェック用碁盤配列
        static int numSpace;                            // 空点の数(チェック用碁盤用)
        static int numStone;                            // 連の石数(チェック用碁盤用)

        static bool endFlag = false;                    // パス2回連続したらtrue
        static Random rand = new Random();


        /******************************************************************************/
        static void Main(string[] args)
        {
            GameSettings();                                 // 対局の設定
            int movColor = White;                           // 直前の手番の色
            Point movePoint = new Point(999, 999);          // 着手位置
            Point previousMovePoint = new Point(999, 999);  // ひとつ前の着手位置
            int score = 0;                                  // 黒地引く白地引くコミ

            InitializeBoard();// 碁盤の初期化
            Console.WriteLine("/////// コンソール碁盤 ///////");
            prisoner[Black] = 0;
            prisoner[White] = 0;
            move = 1;

            // 対局ループ
            while (true)
            {
                movColor = 3 - movColor; // 手番交代
                DispGoban();        // 碁盤表示

                // 着手
                ThinkMove(movColor, ref movePoint);

                // 盤サイズより大きい座標なら、投了
                if (movePoint.X > boardSize - 2 || movePoint.Y > boardSize - 2) {
                    string msgToRyo = (movColor == Black) ? "白の勝ち！ 黒は投了しました。" : "黒の勝ち！ 白は投了しました。";
                    Console.WriteLine(msgToRyo);
                    Console.ReadKey();
                    break;
                }

                // パスが2回連続したら終局処理
                if ((movePoint.X < 1 || movePoint.Y < 1) && (previousMovePoint.X < 1 || previousMovePoint.Y < 1)) {
                    endFlag = true;
                    score = CountScore();
                    Console.Write((double)(score - 0.5) + "目、");
                         if (score > 0) { Console.WriteLine("黒の勝ち！！"); }
                    else if (score < 0) { Console.WriteLine("白の勝ち！！"); }
                    else                { Console.WriteLine("持碁！白の勝ち！"); }
                    Console.ReadKey();
                    break;
                }

                // パスでなければ石を置く
                if ((movePoint.X + movePoint.Y) != 0) { SetStone(movColor, ref movePoint); }

                // 棋譜に記録
                RecordMove(movColor, movePoint);

                // 次の手番へ
                move++;
                previousMovePoint.X = movePoint.X;
                previousMovePoint.Y = movePoint.Y;
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
                    goban = new int[boardSize, boardSize];
                    chkGoban = new int[boardSize, boardSize];
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
            goban = new int[boardSize, boardSize];
            for (int i = 0; i < boardSize; i++)
            {
                goban[0, i] = 3;
                goban[i, 0] = 3;
                goban[boardSize - 1, i] = 3;
                goban[i, boardSize - 1] = 3;
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
                    Console.Write(banChr[goban[i, j]]);
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
                    InputCoordinate(color, ref input);  // 着手座標をキー入力
                }
                else {                                  // プレイヤーはコンピュータ
                    RandomXY(color, ref input);         // 思考エンジン（今は乱数）
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
            if (goban[p.Y, p.X] != Space) { return false; }                             // 空点じゃないと置けない
            if (move > 1) {                                         
                if (ko.X == p.X && ko.Y == p.Y && koNum == move - 1) { return false; }  // 一手前にコウを取られていたら置けない
            }
            if (CheckSuicide(color, p)) { return false; }                               // 自殺手なら置けない

            // 以上のチェックを通過したので、合法手だが、
            // 乱数打ちの場合、自分の目を埋める手は禁止しておくが、
            if ((goban[p.Y, p.X - 1] == color || goban[p.Y, p.X - 1] == Outsd) &&
                (goban[p.Y, p.X + 1] == color || goban[p.Y, p.X + 1] == Outsd) &&
                (goban[p.Y - 1, p.X] == color || goban[p.Y - 1, p.X] == Outsd) &&
                (goban[p.Y + 1, p.X] == color || goban[p.Y + 1, p.X] == Outsd))
            {
                // 手数が盤サイズの二乗を超えたら、コウの形を埋めるのはOK
                if (move > (boardSize - 1)* (boardSize - 1)) {
                    if (KoKaisho(color, p)) return true;// 置けます
                    return false;
                }
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
            if (goban[y, x] == Outsd) return false;
            // 周囲の 相手石or壁を、カウント＋１
            for (int i = 0; i < 9; i++) { countMk[i] = 0; }         // カウンタを初期化０
            countMk[goban[y, x + 1]]++;  // 右
            countMk[goban[y - 1, x]]++;  // 上
            countMk[goban[y + 1, x]]++;  // 下
            countMk[goban[y, x - 1]]++;  // 左

            // 相手の石と壁で3方向囲まれていれば、コウの形
            if (countMk[3 - color] + countMk[Outsd] == 3) {
                return true; 
            }
            return false;
        }

        /******************************************************************************/
        // 自殺手か調べる
        static bool CheckSuicide(int color, Point p)
        {
            int opponentColor;              // 相手の色
            goban[p.Y, p.X] = color;        // 仮に石を置く

            InitializeCheckBoard();         // チェック用碁盤と、空点数＆連石数を初期化
            if (CheckRemove(ref chkGoban, p.X, p.Y, color, ref numSpace, ref numStone)) // 囲まれているなら自殺手かもしれない
            {
                opponentColor = 3 - color;  // 相手の色

                if (goban[p.Y, p.X - 1] == opponentColor) {     // 右隣が相手の石
                    InitializeCheckBoard();
                    if (CheckRemove(ref chkGoban, p.X - 1, p.Y, opponentColor, ref numSpace, ref numStone)) {
                        goban[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                if (goban[p.Y, p.X + 1] == opponentColor) {     // 左隣が相手の石
                    InitializeCheckBoard();
                    if (CheckRemove(ref chkGoban, p.X + 1, p.Y, opponentColor, ref numSpace, ref numStone)) {
                        goban[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                if (goban[p.Y - 1, p.X] == opponentColor) {     // 上隣が相手の石
                    InitializeCheckBoard();
                    if (CheckRemove(ref chkGoban, p.X, p.Y - 1, opponentColor, ref numSpace, ref numStone)) {
                        goban[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                if (goban[p.Y + 1, p.X] == opponentColor) {     // 下隣が相手の石
                    InitializeCheckBoard();
                    if (CheckRemove(ref chkGoban, p.X, p.Y + 1, opponentColor, ref numSpace, ref numStone)) {
                        goban[p.Y, p.X] = Space;    // 仮石を元に戻す
                        return false;               // その石を取れるなら自殺手ではない
                    }
                }

                goban[p.Y, p.X] = Space;    // 仮石を元に戻す
                return true;                // 相手の石を取れないので、自殺手になる
            }
            else {
                goban[p.Y, p.X] = Space;    // 仮石を元に戻す
                return false;               // 囲まれていないので、自殺手ではない
            }
        }

        /******************************************************************************/
        // チェック用碁盤のマークをクリア
        static void InitializeCheckBoard()
        {
            Array.Copy(goban, chkGoban, goban.Length);  // 現状碁盤をチェック用碁盤にコピー
            numSpace = 0;   // 空点の数
            numStone = 0;   // 連の石数
        }

        /******************************************************************************/
        // 座標(x,y)のcolor石が相手に囲まれているか調べる。
        // 空点が無いなら true 、空点が有れば false を返す。
        private static bool CheckRemove(ref int[,] ary, int x, int y, int color, ref int numSpace, ref int numStone)
       {
            //再帰しない条件の処理
            if (ary[y, x] > 3) return false;        // チェック済 
            if (ary[y, x] != color) return false;   // 自分のcolor石が置かれていない

            //隣が空点なら、空点チェック済みマーク を入れ、空点カウント＋１
            if (ary[y, x - 1] == Space) { ary[y, x - 1] = Xdame; numSpace++; }            //左隣
            if (ary[y, x + 1] == Space) { ary[y, x + 1] = Xdame; numSpace++; }            //右隣
            if (ary[y - 1, x] == Space) { ary[y - 1, x] = Xdame; numSpace++; }            //上隣
            if (ary[y + 1, x] == Space) { ary[y + 1, x] = Xdame; numSpace++; }            //下隣
            ary[y, x] = 4; numStone++;  // チェック済みマーク 4 を入れ、連の石カウント＋１

            //再帰呼び出し
            _ = CheckRemove(ref ary, x - 1, y, color, ref numSpace, ref numStone);   //左隣
            _ = CheckRemove(ref ary, x, y + 1, color, ref numSpace, ref numStone);   //下隣
            _ = CheckRemove(ref ary, x + 1, y, color, ref numSpace, ref numStone);   //右隣
            _ = CheckRemove(ref ary, x, y - 1, color, ref numSpace, ref numStone);   //上隣
            return numSpace == 0;
        }

        /******************************************************************************/
        // 連の石を消す
        static void KeSu(int x, int y, int color)
        {
            goban[y, x] = Space;    // 座標の石を消す
            // 隣の石が同じ色なら、再帰で消す
            if (goban[y, x - 1] == color) { KeSu(x - 1, y, color); }    // 左
            if (goban[y, x + 1] == color) { KeSu(x + 1, y, color); }    // 右
            if (goban[y - 1, x] == color) { KeSu(x, y - 1, color); }    // 上
            if (goban[y + 1, x] == color) { KeSu(x, y + 1, color); }    // 下
        }

        /******************************************************************************/
        // 連続する要素（石or空点）を数えて、マークをつける
        static void Marking(int x, int y, int element, int mark, ref int[] numMark)
        {
            if (goban[y, x] != element) return; // 数える要素ではない 
            if (goban[y, x] == mark) return;    // マーク済み

            goban[y, x] = mark;                 // 座標の要素にマーキング
            numMark[element]++;

            // 周囲の マークを、カウント＋１（1:黒、2:白 のカウントが必要、他のマークのカウントは・・）
            if (goban[y, x + 1] == Black || goban[y, x + 1] == White) countMk[goban[y, x + 1]]++;  // 右
            if (goban[y - 1, x] == Black || goban[y - 1, x] == White) countMk[goban[y - 1, x]]++;  // 上
            if (goban[y + 1, x] == Black || goban[y + 1, x] == White) countMk[goban[y + 1, x]]++;  // 下
            if (goban[y, x - 1] == Black || goban[y, x - 1] == White) countMk[goban[y, x - 1]]++;  // 左

            // 隣の石が同じ要素なら、再帰でMarkingする
            Marking(x - 1, y, element, mark, ref numMark);    // 左
            Marking(x + 1, y, element, mark, ref numMark);    // 右
            Marking(x, y - 1, element, mark, ref numMark);    // 上
            Marking(x, y + 1, element, mark, ref numMark);    // 下
        }

        /******************************************************************************/
        // 勝敗の判定、
        static int CountScore()
        {
            int bScore = 0;
            int wScore = 0;

            DeadStones();   // 盤上の死に石数をアゲハマへ追加する
            // 死に石を消す
            for (int y =1; y < boardSize - 1; y++) for (int x = 1; x < boardSize - 1; x++)
                {
                    if(goban[y, x] == 4 || goban[y, x] == 5) { goban[y, x] = Space; }
                }
            DispGoban();

            // 陣地を数える
            for (int y = 1; y < boardSize - 1; y++)
            {
                for (int x = 1; x < boardSize - 1; x++)
                {
                    if (goban[y, x] != Space) { continue; }  // 空点でなければスキップ

                    // 空点なら以下の処理
                    for (int i = 0; i < 9; i++) { countMk[i] = 0; }         // カウンタを初期化０
                    Marking(x, y, Space, Xdame, ref countMk);        // 空点の数

                    if (countMk[Black] > 0 && countMk[White] == 0)
                    {   // 黒石だけに囲まれている
                        bScore += countMk[Space];
                        Marking(x, y, Xdame, Barea, ref countMk);
                    }

                    if (countMk[White] > 0 && countMk[Black] == 0)
                    {   // 白石だけに囲まれている
                        wScore += countMk[Space];
                        Marking(x, y, Xdame, Warea, ref countMk);
                    }
                }
            }
            DispGoban();
            Console.WriteLine("------結果-------");
            Console.WriteLine($"黒地{bScore}+黒浜{prisoner[Black]}");
            Console.WriteLine($"白地{wScore}+白浜{prisoner[White]}+コミ{komi}.5");

            bScore += prisoner[Black];
            wScore += prisoner[White] + komi;
            return bScore - wScore;
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
                InputCoordinate(Outsd, ref p);
                if (p.X == 99) { break; }
                color = goban[p.Y, p.X];
                if (!(color == Black || color == White)) { continue; }// 座標に石が無かったら再入力

                // 死に石のカウントとマークキング
                for (int i = 0; i < 9; i++) { countMk[i] = 0; }         // カウンタを初期化０
                Marking(p.X, p.Y, color, color + 3, ref countMk);
                prisoner[3 - color] += countMk[color];

                DispGoban();        // 碁盤表示
            }
        }

        /******************************************************************************/
        // 碁盤に石を置く
        static void SetStone(int color, ref Point p)
        {
            bool koFlag = false;
            goban[p.Y, p.X] = color;    // 座標に石を置く
            // 置いた石の4方向隣に同じ色の石があるか？
            if (goban[p.Y + 1, p.X] != color &&
                goban[p.Y - 1, p.X] != color &&
                goban[p.Y, p.X + 1] != color &&
                goban[p.Y, p.X - 1] != color) {
                // 同じ色の石が無いならコウかもしれない
                koFlag = true;  
            } else {
                koFlag = false; // コウではない
            }
            //   左のアゲハマ 、 右のアゲハマ 、 上のアゲハマ 、 下のアゲハマ
            int prisonerW = 0, prisonerE = 0, prisonerN = 0, prisonerS = 0;

            // 隣の相手の連が死んでいれば、碁盤から取り除く
            InitializeCheckBoard();             // 左
            if (CheckRemove(ref chkGoban, p.X - 1, p.Y, 3 - color, ref numSpace, ref numStone)) {
                KeSu(p.X - 1, p.Y, 3 - color);
                prisonerW = numStone;
            }
            InitializeCheckBoard();             // 右
            if (CheckRemove(ref chkGoban, p.X + 1, p.Y, 3 - color, ref numSpace, ref numStone)) {
                KeSu(p.X + 1, p.Y, 3 - color);
                prisonerE = numStone;
            }
            InitializeCheckBoard();             // 上
            if (CheckRemove(ref chkGoban, p.X, p.Y - 1, 3 - color, ref numSpace, ref numStone)) {
                KeSu(p.X, p.Y - 1, 3 - color);
                prisonerN = numStone;
            }
            InitializeCheckBoard();             // 下
            if (CheckRemove(ref chkGoban, p.X, p.Y + 1, 3 - color, ref numSpace, ref numStone)) {
                KeSu(p.X, p.Y + 1, 3 - color);
                prisonerS = numStone;
            }

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
                    if (goban[i, j] != Space) continue; // 空点以外はスキップ
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
