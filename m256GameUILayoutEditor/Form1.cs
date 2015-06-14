﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace m256GameUILayoutEditor
{

    public partial class Form1 : Form
    {
        const string INIT_TITLE = "untitled";

        private string appliName = "";
        private string myVer = "";
        private string docTitle = INIT_TITLE;
        private string saveFilePath = "";
        private string saveDir = "";

        // キャンバスサイズ
        public int canvasW = 640;
        public int canvasH = 480;

        private Color canvasColor = Color.FromArgb(255, 160, 160, 160);

        // グリッドサイズ
        public int gridW = 16;
        public int gridH = 16;

        private Color gridColor = Color.FromArgb(128, 220, 220, 220);

        // グリッド描画用画像
        private Bitmap bgGrid = null;
        private Boolean bgGridRedraw = true;

        // マウスドラッグ処理検知用
        private Boolean buttonPressed = false;

        // スナップ有効無効
        private Boolean snapEnable = true;

        // テキスト描画用
        public string fontName = "Arial";
        public int fontSize = 24;
        public Color fontColor = Color.Blue;

        // 拡大表示指定値
        private int zoomValue = 100;

        private int oldMouseX = 0;
        private int oldMouseY = 0;

        private Boolean mouseInCanvas = false;

        // 表示オブジェクトの情報を記憶するためのクラス
        class ObjData
        {
            public int type; // 0ならImage, 1ならText
            public string name;
            public string path;
            public string text;
            public string fontName;
            public int fontSize;
            public int x;
            public int y;
            public int w;
            public int h;
            public int offsetX;
            public int offsetY;
            public Bitmap bitmap;
            public Color fontColor;
            public Boolean selected;
            public Boolean buttonDowned;

            // コンストラクタ
            public ObjData(int type, string name, string path, string text,
                int x, int y, string fontName, int fontSize, Color fontColor)
            {
                this.type = type;
                this.name = name;
                this.path = path;
                this.text = text;
                this.x = x;
                this.y = y;
                this.fontName = fontName;
                this.fontSize = fontSize;
                this.fontColor = fontColor;

                if (this.type == 0)
                {
                    // Image
                    this.bitmap = new Bitmap(CreateImage(path));
                    this.w = this.bitmap.Width;
                    this.h = this.bitmap.Height;
                }
                else
                {
                    // Text

                    // サイズ決定は仮
                    this.bitmap = null;
                    this.w = fontSize * this.text.Length;
                    this.h = fontSize;
                }

                this.offsetX = 0;
                this.offsetY = 0;
                this.selected = false;
                this.buttonDowned = false;
            }

            // 表示座標を更新
            public void setPosition(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            // マウスドラッグで表示座標を更新
            public void changePosition(int x, int y, Boolean snap, int gw, int gh)
            {
                this.x = x + this.offsetX;
                this.y = y + this.offsetY;

                if (snap) this.snapPosition(gw, gh);
            }

            // 与えられたグリッド値で座標をスナップ
            public void snapPosition(int gw, int gh)
            {
                this.x = (this.x / gw) * gw;
                this.y = (this.y / gh) * gh;
            }

            // マウス座標からのオフセット値を記録
            public void setOffset(int x, int y)
            {
                this.offsetX = this.x - x;
                this.offsetY = this.y - y;
            }

            // 画像を解放
            public void disposeImage()
            {
                if (this.bitmap != null) this.bitmap.Dispose();
            }
        }

        // 描画オブジェクトリスト
        List<ObjData> images = new List<ObjData>();

        public Form1()
        {
            InitializeComponent();

            setFormTitle();
            setStatus();
        }

        private void newLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newLayout();
        }

        private void toolStripBtnNew_Click(object sender, EventArgs e)
        {
            newLayout();
        }

        private void openLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openLayoutdata();
        }

        private void toolStripBtnOpen_Click(object sender, EventArgs e)
        {
            openLayoutdata();
        }

        private void saveLayoutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            saveLayoutDataAs(true);
        }

        private void toolStripBtnSave_Click(object sender, EventArgs e)
        {
            saveLayoutDataAs(true);
        }

        private void saveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveLayoutDataAs(false);
        }

        private void toolStripBtnSaveAs_Click(object sender, EventArgs e)
        {
            saveLayoutDataAs(false);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportLayoutData();
        }

        private void toolStripBtnExport_Click(object sender, EventArgs e)
        {
            exportLayoutData();
        }

        // 新規レイアウト作成
        private void newLayout()
        {
            clearObjData();
            docTitle = INIT_TITLE;
            setFormTitle();

            pictureBox1.Invalidate(); // PictureBoxを再ペイント
        }

        // 描画オブジェクトリストのクリア
        private void clearObjData()
        {
            if (images.Count <= 0) return;
            foreach (ObjData o in images)
            {
                o.disposeImage();
            }
            images.RemoveRange(0, images.Count);
        }

        // 自分自身のアプリ名を返す
        private string getAppliName()
        {
            if (appliName == "")
            {
                //var assm = Assembly.GetExecutingAssembly();
                //var name = assm.GetName();
                //appliName = name.Name.ToString();

                //Console.WriteLine("{0} {1}", Application.ProductName, Application.ProductVersion);
                appliName = Application.ProductName.ToString();
            }
            return appliName;
        }

        // 自分自身のバージョンを返す
        private string getMyVer()
        {
            if (myVer == "")
            {
                //System.Diagnostics.FileVersionInfo ver =
                //    System.Diagnostics.FileVersionInfo.GetVersionInfo(
                //    System.Reflection.Assembly.GetExecutingAssembly().Location);
                //myVer = ver.ToString();

                //var assm = Assembly.GetExecutingAssembly();
                //var name = assm.GetName();
                //Console.WriteLine("{0} {1}", name.Name, name.Version);
                //appliName = name.Name.ToString();
                //myVer = name.Version.ToString();

                //var assm = Assembly.GetExecutingAssembly();
                //var path = (new Uri(assm.CodeBase)).LocalPath;
                //var versionInfo = FileVersionInfo.GetVersionInfo(path);
                //Console.WriteLine("{0} {1}", versionInfo.FileName, versionInfo.FileVersion);
                //myVer = versionInfo.FileVersion.ToString();

                myVer = Application.ProductVersion.ToString();
            }
            return myVer;
        }

        // フォームのタイトルを変更
        private void setFormTitle()
        {
            this.Text = string.Format("{0} - {1} {2}", docTitle, getAppliName(), getMyVer());
        }

        // レイアウトデータ(.json)を開く
        private void openLayoutdata()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "JSON file(*.json)|*.json|ALL(*.*)|*.*";
            d.RestoreDirectory = true;
            if (d.ShowDialog() == DialogResult.OK) loadLayoutData(d.FileName);
        }

        // レイアウトデータ(.json)の読み込み
        private void loadLayoutData(string filePath)
        {
            setSaveFilePath(filePath);

            System.Text.Encoding enc = new System.Text.UTF8Encoding(false);
            string s = System.IO.File.ReadAllText(saveFilePath, enc);

            convJsonToLayoutdata(s);

            bgGridRedraw = true;
            pictureBox1.Invalidate();
            setStatus();
        }

        // 現在扱ってるレイアウトファイルのパスその他を指定
        private void setSaveFilePath(string path)
        {
            saveFilePath = path;
            saveDir = Path.GetDirectoryName(path) + "\\";
            docTitle = Path.GetFileName(path);
            setFormTitle();
        }

        // JSON文字列からオブジェクトへの変換
        private void convJsonToLayoutdata(string jsonString)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(LayoutData));
            byte[] b = Encoding.UTF8.GetBytes(jsonString);
            MemoryStream ms = new MemoryStream(b);
            LayoutData d = (LayoutData)ser.ReadObject(ms);

            clearObjData();

            canvasW = d.canvasW;
            canvasH = d.canvasH;
            gridW = d.gridW;
            gridH = d.gridH;

            List<string> errPaths = new List<string>();

            foreach (LayoutObj p in d.objs)
            {
                Color fontColor = Color.FromArgb(255, p.fontColorR, p.fontColorG, p.fontColorB);

                string path = p.path;
                if (p.type == 0)
                {
                    path = getAbsolutePath(saveDir, path);
                    if (!File.Exists(path))
                    {
                        errPaths.Add(path);
                        continue;
                    }
                }

                string name = Path.GetFileName(path);
                ObjData o = new ObjData(p.type, name, path, p.text,
                    p.x, p.y, p.fontName, p.fontSize, fontColor);
                images.Add(o);
            }

            // 見つからないファイルがあった
            if (errPaths.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error : File not found.");
                foreach (string s in errPaths)
                    sb.AppendLine(s);

                MessageBox.Show(sb.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // レイアウトデータをJSONファイルとして保存
        private void saveLayoutDataAs(Boolean forceSave)
        {
            if (!forceSave || saveFilePath == "")
            {
                string filepath = getSaveFilename(true);
                if (filepath == "") return;

                setSaveFilePath(filepath);
            }

            string s = getJsonStringFromLayoutData();

            System.Text.Encoding enc = new System.Text.UTF8Encoding(false);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(saveFilePath, false, enc);
            sw.Write(s);
            sw.Close();

            showSaveMessage(docTitle);
        }

        // 保存したことをステータスバーに表示
        private void showSaveMessage(string fn)
        {
            //MessageBox.Show("Save " + fn, "Result",
            //    MessageBoxButtons.OK, MessageBoxIcon.Information);

            toolStripStatusObjInfo.Text = string.Format("Saved [{0}] ... {1}", fn, DateTime.Now);
        }

        // ファイルを保存ダイアログを開く
        private string getSaveFilename(Boolean selectJson)
        {
            SaveFileDialog d = new SaveFileDialog();

            if (selectJson)
            {
                // JSON
                d.Filter = "JSON file(*.json)|*.json|ALL(*.*)|*.*";
                d.FileName = docTitle;
            }
            else
            {
                // CSV, etc
                d.Filter = "CSV file(*.csv)|*.csv|YAML file(*.yml;*.yaml)|*.yml;*.yaml|ALL(*.*)|*.*";
                string fn = docTitle;
                if (fn.EndsWith(".json")) fn = Path.GetFileNameWithoutExtension(fn);

                d.FileName = fn;
            }

            d.RestoreDirectory = true;
            if (d.ShowDialog() == DialogResult.OK)
                return d.FileName;
            else
                return "";
        }

        // レイアウトデータをJSON文字列に変換
        private string getJsonStringFromLayoutData()
        {
            // 保存するためのデータオブジェクトを作成
            LayoutData d = new LayoutData();
            d.canvasW = canvasW;
            d.canvasH = canvasH;
            d.gridW = gridW;
            d.gridH = gridH;

            d.objs = new LayoutObj[images.Count];

            for (int i = 0; i < images.Count; i++)
            {
                ObjData o = images[i];
                d.objs[i] = new LayoutObj();
                LayoutObj p = d.objs[i];

                p.type = o.type;
                p.name = o.name;
                p.text = o.text;

                p.x = o.x;
                p.y = o.y;
                p.w = o.w;
                p.h = o.h;

                p.fontName = o.fontName;
                p.fontSize = o.fontSize;
                p.fontColorR = o.fontColor.R;
                p.fontColorG = o.fontColor.G;
                p.fontColorB = o.fontColor.B;

                if (o.type == 0)
                    p.path = getRelativePath(saveDir, o.path); // Image
                else
                    p.path = o.path; // Text
            }

            // JSONに変換
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(LayoutData));
            ser.WriteObject(stream1, d);

            return Encoding.UTF8.GetString(stream1.ToArray());
        }

        // 絶対パスを相対パスに変換して返す
        private string getRelativePath(string dir, string path)
        {
            if (path == string.Empty) return "";
            if (!Path.IsPathRooted(path)) return path;

            if (dir == string.Empty) return "";
            if (!Path.IsPathRooted(dir)) return "";

            if (!dir.EndsWith("\\")) dir += "\\";

            string basePath = dir.Replace("%", "%25");
            string filePath = path.Replace("%", "%25");

            Uri u1 = new Uri(basePath);
            Uri u2 = new Uri(filePath);
            Uri relativeUri = u1.MakeRelativeUri(u2);
            string relativePath = relativeUri.ToString();

            relativePath = Uri.UnescapeDataString(relativePath);
            relativePath = relativePath.Replace("%25", "%");
            relativePath = relativePath.Replace('/', '\\');

            //Console.WriteLine(dir);
            //Console.WriteLine(path);
            //Console.WriteLine(" -> " + relativePath);

            return relativePath;
        }

        // 相対パスを絶対パスに変換して返す
        private string getAbsolutePath(string dir, string path)
        {
            if (path == string.Empty) return "";
            if (Path.IsPathRooted(path)) return path;

            if (dir == string.Empty) return "";
            if (!Path.IsPathRooted(dir)) return "";

            if (!dir.EndsWith("\\")) dir += "\\";

            string basePath = dir.Replace("%", "%25");
            string filePath = path.Replace("%", "%25");

            Uri u1 = new Uri(basePath);
            Uri u2 = new Uri(u1, filePath);
            string absolutePath = u2.LocalPath;

            absolutePath = absolutePath.Replace("%25", "%");

            //Console.WriteLine(dir);
            //Console.WriteLine(path);
            //Console.WriteLine(" -> " + absolutePath);

            return absolutePath;
        }

        // レイアウトデータをエクスポート
        private void exportLayoutData()
        {
            string filepath = getSaveFilename(false);
            if (filepath == "") return;
            string dir = Path.GetDirectoryName(filepath) + "\\";

            List<string> lines = new List<string>();
            if (filepath.EndsWith(".csv"))
            {
                // CSV
                foreach (ObjData o in images)
                {
                    string path = o.path;
                    if (o.type == 0) path = getRelativePath(dir, path);

                    List<string> strs = new List<string>();
                    strs.Add(string.Format("{0}", o.type));
                    strs.Add(string.Format("\"{0}\"", o.name));
                    strs.Add(string.Format("\"{0}\"", path));
                    strs.Add(string.Format("\"{0}\"", o.text));
                    strs.Add(string.Format("{0}", o.x));
                    strs.Add(string.Format("{0}", o.y));
                    strs.Add(string.Format("{0}", o.w));
                    strs.Add(string.Format("{0}", o.h));
                    strs.Add(string.Format("\"{0}\"", o.fontName));
                    strs.Add(string.Format("{0}", o.fontSize));
                    strs.Add(string.Format("{0}", o.fontColor.R));
                    strs.Add(string.Format("{0}", o.fontColor.G));
                    strs.Add(string.Format("{0}", o.fontColor.B));

                    lines.Add(string.Join(",", strs.ToArray()));
                }
            }
            else if (filepath.EndsWith(".yml") || filepath.EndsWith(".yaml"))
            {
                // YAML
                lines.Add("---");
                lines.Add(string.Format(":canvas_w: {0}", canvasW));
                lines.Add(string.Format(":canvas_h: {0}", canvasH));
                lines.Add(":objs:");
                foreach (ObjData o in images)
                {
                    string path = o.path;
                    if (o.type == 0) path = getRelativePath(dir, path);

                    lines.Add(string.Format("- :type: {0}", o.type));

                    lines.Add(string.Format("  :id: {0}", (o.name == "") ? "\'\'" : o.name));
                    lines.Add(string.Format("  :path: {0}", (path == "") ? "\'\'" : path));
                    lines.Add(string.Format("  :text: {0}", (o.text == "") ? "\'\'" : o.text));

                    lines.Add(string.Format("  :x: {0}", o.x));
                    lines.Add(string.Format("  :y: {0}", o.y));
                    lines.Add(string.Format("  :w: {0}", o.w));
                    lines.Add(string.Format("  :h: {0}", o.h));

                    lines.Add(string.Format("  :fontname: {0}", (o.fontName == "") ? "\'\'" : o.fontName));
                    lines.Add(string.Format("  :fontsize: {0}", o.fontSize));

                    lines.Add("  :fontcolor:");
                    lines.Add(string.Format("  - {0}", o.fontColor.R));
                    lines.Add(string.Format("  - {0}", o.fontColor.G));
                    lines.Add(string.Format("  - {0}", o.fontColor.B));
                }
            }

            System.Text.Encoding enc = new System.Text.UTF8Encoding(false);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(filepath, false, enc);
            foreach (string s in lines)
                sw.WriteLine(s);
            sw.Close();

            showSaveMessage(System.IO.Path.GetFileName(filepath));
        }

        // アプリ終了
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // アプリについてダイアログを表示
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                getAppliName() + Environment.NewLine + "Ver. " + getMyVer(),
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // 画像追加メニューを選択
        private void addImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectImageFiles();
        }

        // 画像追加ボタンをクリック
        private void toolStripBtnAddImage_Click(object sender, EventArgs e)
        {
            selectImageFiles();
        }

        // テキスト追加メニューを選択
        private void addTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addTextObj();
        }

        // テキスト追加ボタンをクリック
        private void toolStripBtnAddText_Click(object sender, EventArgs e)
        {
            addTextObj();
        }

        // テキストオブジェクトを追加
        // テキストオブジェクトのサイズは PictureBox の paint 時に取得する
        private void addTextObj()
        {
            FormAddText f = new FormAddText();

            f.fontName = this.fontName;
            f.fontSize = this.fontSize;
            f.fontColor = this.fontColor;
            f.textStr = "Test String";

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                this.fontName = f.fontName;
                this.fontSize = f.fontSize;
                this.fontColor = f.fontColor;
                string str = f.textStr;
                int x = 0;
                int y = 0;
                ObjData o = new ObjData(1, str, "", str,
                    x, y, this.fontName, this.fontSize, this.fontColor);
                images.Add(o);

                pictureBox1.Invalidate();
            }

            f.Dispose();
        }

        // 画像ファイルを追加するためのダイアログを開く
        private void selectImageFiles()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image File(*.png;*.gif;*.jpg;*.bmp)|*.png;*.gif;*.jpg;*.bmp|All(*.*)|*.*";
            //ofd.Title = "Select Image Files";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                addImageFiles(ofd.FileNames);
            }
        }

        // 画像ファイルを画面に追加
        private void addImageFiles(string[] files)
        {
            int x = 0;
            int y = 0;
            foreach (string path in files)
            {
                if (checkImageFormat(path))
                {
                    string name = Path.GetFileName(path);
                    ObjData o = new ObjData(0, name, path, "", x, y, "", 0, Color.Blue);
                    images.Add(o);
                    x += 16;
                    y += 16;
                }
            }
            pictureBox1.Invalidate(); // PictureBox を repaint
            setStatus(); // ステータスバー情報を更新
        }

        // 画像ファイルをストリームで開く(ファイルのロック回避)
        public static System.Drawing.Image CreateImage(string filename)
        {
            System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            System.Drawing.Image img = System.Drawing.Image.FromStream(fs);
            fs.Close();
            return img;
        }

        // 対応してる画像フォーマットかどうかを調べる
        private Boolean checkImageFormat(string fn)
        {
            string ext = System.IO.Path.GetExtension(fn);

            foreach (ImageCodecInfo ici in ImageCodecInfo.GetImageDecoders())
            {
                string[] exts = ici.FilenameExtension.Split(';');
                foreach (string s in exts)
                {
                    string fmtExt = s.Substring(s.IndexOf('.'));
                    if (fmtExt.ToUpper() == ext.ToUpper()) return true;
                }
            }
            return false;
        }

        // ステータス情報を更新
        private void setStatus()
        {
            string gridText = string.Format("{0}x{1}", gridW, gridH);
            toolStripStatusLabelGridSize.Text = string.Format("Grid {0}", gridText);
            if (toolStripComboBoxGridSize.Text != gridText)
                toolStripComboBoxGridSize.Text = gridText;

            string canvasText = string.Format("{0}x{1}", canvasW, canvasH);
            toolStripStatusLabelCanvasSize.Text = string.Format("Canvas {0}", canvasText);
            if (toolStripComboBoxCanvasSize.Text != canvasText)
                toolStripComboBoxCanvasSize.Text = canvasText;

            Boolean fg = snapToGridToolStripMenuItem.Checked;
            toolStripBtnSnapGrid.Checked = fg;
            snapEnable = fg;
        }

        // PictureBox描画
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // ニアレストネイバー法で拡大表示するように指定
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            pictureBox1.Width = canvasW * zoomValue / 100;
            pictureBox1.Height = canvasH * zoomValue / 100;

            SolidBrush b = new SolidBrush(canvasColor);
            //g.FillRectangle(Brushes.Silver, g.VisibleClipBounds);
            g.FillRectangle(b, g.VisibleClipBounds);
            b.Dispose();

            if (bgGridRedraw)
            {
                makeBgGrid();
                bgGridRedraw = false;
            }

            // オブジェクト描画
            // リストの後ろにあるオブジェクトのほうが手前に表示される
            int cnt = 0;
            foreach (ObjData o in images)
            {
                int px = o.x * zoomValue / 100;
                int py = o.y * zoomValue / 100;

                if (o.type == 0)
                {
                    // Image
                    int w = o.w * zoomValue / 100;
                    int h = o.h * zoomValue / 100;
                    g.DrawImage(o.bitmap, px, py, w, h);
                }
                else
                {
                    // Text
                    StringFormat sf = new StringFormat(StringFormat.GenericTypographic);

                    int size = o.fontSize * zoomValue / 100;
                    Font fnt = new Font(o.fontName, size);
                    SolidBrush fb = new SolidBrush(o.fontColor);

                    g.DrawString(o.text, fnt, fb, px, py, sf);

                    var sz = g.MeasureString(o.text, fnt, pictureBox1.Width, sf);
                    o.w = ((int)sz.Width) * 100 / zoomValue;
                    o.h = ((int)sz.Height) * 100 / zoomValue;

                    fb.Dispose();
                    fnt.Dispose();
                }

                // 選択枠を描画
                if (o.selected)
                {
                    Pen p = new Pen(Color.Red, 1);
                    p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    int w = o.w * zoomValue / 100;
                    int h = o.h * zoomValue / 100;
                    g.DrawRectangle(p, px, py, w, h);
                    p.Dispose();
                }
                cnt++;
            }

            // グリッド描画
            if (displayGridToolStripMenuItem1.Checked)
            {
                int w = bgGrid.Width * zoomValue / 100;
                int h = bgGrid.Height * zoomValue / 100;
                g.DrawImage(bgGrid, 0, 0, w, h);
            }

            // オブジェクトが何もなければメッセージを描画
            if (cnt == 0)
            {
                Font fnt = new Font("Arial", 20);
                g.DrawString("Drag and drop the image", fnt, Brushes.Gainsboro, 32, 32);
                fnt.Dispose();
            }
        }

        // グリッド描画用画像を生成
        private void makeBgGrid()
        {
            if (bgGrid != null)
            {
                bgGrid.Dispose();
                bgGrid = null;
            }

            bgGrid = new Bitmap(canvasW, canvasH);

            Graphics ng = Graphics.FromImage(bgGrid);
            ng.FillRectangle(Brushes.Transparent, ng.VisibleClipBounds);

            Pen p = new Pen(Color.FromArgb(128, gridColor.R, gridColor.G, gridColor.B), 1);

            for (int x = 0; x < canvasW; x += gridW)
                ng.DrawLine(p, x, 0, x, canvasH);

            for (int y = 0; y < canvasH; y += gridH)
                ng.DrawLine(p, 0, y, canvasW, y);

            p.Dispose();
            ng.Dispose();
        }

        private void toolStripBtnSnapGrid_Click(object sender, EventArgs e)
        {
            changeSnapToGrid();
        }

        private void snapToGridToolStripMenuItem_Click(object sender, EventArgs e)
        {
            changeSnapToGrid();
        }

        // グリッドスナップの有効無効を切り替え
        void changeSnapToGrid()
        {
            Boolean fg = !snapToGridToolStripMenuItem.Checked;
            snapToGridToolStripMenuItem.Checked = fg;
            toolStripBtnSnapGrid.Checked = fg;
            snapEnable = fg;
        }

        private void displayGridToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            changeDisplayGrid();
        }

        private void toolStripBtnDisplayGrid_Click(object sender, EventArgs e)
        {
            changeDisplayGrid();
        }

        // グリッド表示非表示を切り替え
        void changeDisplayGrid()
        {
            Boolean fg = !displayGridToolStripMenuItem1.Checked;
            displayGridToolStripMenuItem1.Checked = fg;
            toolStripBtnDisplayGrid.Checked = fg;
            pictureBox1.Invalidate();
        }

        // PictueBoxをクリック
        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }

        // PictureBox上でマウスボタンが押された
        // Shiftキー + クリックで複数選択可能にした
        //
        // TODO 中ボタンドラッグで移動できるようにしたい
        // TODO ホイール回転でズームできるようにしたい
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            int mx = (e.X * 100 / zoomValue);
            int my = (e.Y * 100 / zoomValue);
            Boolean multiSelect = false;

            buttonPressed = true;

            // シフトキーが押されてたら複数選択モード
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) multiSelect = true;

            // マウス座標からのオフセット値を記録
            foreach (ObjData o in images)
                o.setOffset(mx, my);

            if (multiSelect)
            {
                // 複数選択モード
                for (int i = images.Count - 1; i >= 0; i--)
                {
                    ObjData o = images[i];
                    if (checkHit(o, mx, my))
                    {
                        o.selected = !o.selected;
                        break;
                    }
                }
            }
            else
            {
                // シングル選択モード

                // 選択済みのオブジェクトがクリックされているのかを調べる
                // その場合、ユーザは複数まとめてドラッグ移動を望んでる可能性がある
                Boolean pfg = false;
                if (countSelectedObject() > 1)
                {
                    foreach (ObjData o in images)
                    {
                        if (o.selected && checkHit(o, mx, my))
                        {
                            pfg = true;
                            break;
                        }
                    }
                }

                if (!pfg) selectOneObj(mx, my); // 一つだけ選択
            }

            pictureBox1.Invalidate();
            setStatusBarObjInfo();

            oldMouseX = mx;
            oldMouseY = my;
        }

        // オブジェクト範囲内に座標があるか調べる
        private Boolean checkHit(ObjData o, int mx, int my)
        {
            int x0 = o.x;
            int y0 = o.y;
            int w = o.w;
            int h = o.h;
            int x1 = x0 + w;
            int y1 = y0 + h;
            return (x0 <= mx && mx <= x1 && y0 <= my && my <= y1);
        }

        // オブジェクトを一つだけ選択
        private void selectOneObj(int mx, int my)
        {
            Boolean fg = false;
            for (int i = images.Count - 1; i >= 0; i--)
            {
                ObjData o = images[i];
                o.selected = false;
                if (!fg && checkHit(o, mx, my))
                {
                    o.selected = true;
                    fg = true;
                }
            }
        }

        // PictureBox 上でマウスボタンが離された
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            int mx = (e.X * 100 / zoomValue);
            int my = (e.Y * 100 / zoomValue);
            Boolean multiSelect = false;

            buttonPressed = false;

            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) multiSelect = true;

            foreach (ObjData o in images)
                o.setOffset(mx, my);

            if (oldMouseX == mx && oldMouseY == my)
            {
                // 同位置でクリックされていた。
                // ドラッグ移動ではなく選択のつもりだったはず。
                // オブジェクトを一つだけ選択し直す
                if (!multiSelect) selectOneObj(mx, my);
            }

            pictureBox1.Invalidate();
            setStatusBarObjInfo();
            oldMouseX = mx;
            oldMouseY = my;
        }

        // PictureBox 上でマウスがドラッグされた
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (buttonPressed && countSelectedObject() > 0)
            {
                int mx = (e.X * 100 / zoomValue);
                int my = (e.Y * 100 / zoomValue);
                Console.WriteLine("{0},{1}", mx, my);
                foreach (ObjData o in images)
                    if (o.selected) o.changePosition(mx, my, snapEnable, gridW, gridH);

                pictureBox1.Invalidate();
                setStatusBarObjInfo();
            }
        }

        // 選択されてるオブジェクト数を返す
        private int countSelectedObject()
        {
            int cnt = 0;
            foreach (ObjData o in images)
                if (o.selected) cnt++;

            return cnt;
        }

        // 全オブジェクトの選択非選択を変更
        private void selectOrDeselectAll(Boolean fg)
        {
            foreach (ObjData o in images)
                o.selected = fg;

            pictureBox1.Invalidate();
            setStatusBarObjInfo();
        }

        // オブジェクト情報をステータスバーに表示
        private void setStatusBarObjInfo()
        {
            int n = countSelectedObject();
            if (n == 1)
            {
                // オブジェクトが一つだけ選択されてる場合
                foreach (ObjData o in images)
                    if (o.selected)
                        toolStripStatusObjInfo.Text = String.Format("x,y={0},{1}  w,h={2},{3}  [{4}]", o.x, o.y, o.w, o.h, o.name);
            }
            else if (n == 0)
                toolStripStatusObjInfo.Text = "--------";
            else
                toolStripStatusObjInfo.Text = "-- Multi --";

        }

        // 外部のウインドウからドラッグされた
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        // 外部のウインドウからドラッグアンドドロップされた
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            if (filenames.Length == 1)
            {
                string ext = Path.GetExtension(filenames[0]);
                if (ext.ToLower() == ".json")
                    loadLayoutData(filenames[0]); // レイアウトデータの読み込み
                else
                    addImageFiles(filenames); // 画像ファイル読み込み
            }
            else
                addImageFiles(filenames);
        }

        private void bringToFrontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bringObjectToFront();
        }

        private void toolStripBtnFront_Click(object sender, EventArgs e)
        {
            bringObjectToFront();
        }

        private void sendToBackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sendObjectToBack();
        }

        private void toolStripBtnBack_Click(object sender, EventArgs e)
        {
            sendObjectToBack();
        }

        // 選択されたオブジェクトを最前面に移動
        private void bringObjectToFront()
        {
            if (countSelectedObject() == 0) return;
            List<ObjData> selList = new List<ObjData>();
            foreach (ObjData o in images)
                if (o.selected) selList.Add(o);

            images.RemoveAll(checkSelected); // 選択オブジェクトを一度削除

            foreach (ObjData o in selList)
                images.Add(o);

            pictureBox1.Invalidate();
        }

        // 選択されたオブジェクトを最背面に移動
        private void sendObjectToBack()
        {
            if (countSelectedObject() == 0) return;
            List<ObjData> selList = new List<ObjData>();
            foreach (ObjData o in images)
                if (o.selected) selList.Add(o);

            images.RemoveAll(checkSelected); // 選択オブジェクトを一度削除

            foreach (ObjData o in selList)
                images.Insert(0, o);

            pictureBox1.Invalidate();
        }

        static Boolean checkSelected(ObjData o)
        {
            return o.selected == true;
        }

        private void bringToForwardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bringObjectForwards();
        }

        private void toolStripBtnForwards_Click(object sender, EventArgs e)
        {
            bringObjectForwards();
        }

        private void sendToBackwardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sendObjectBackwards();
        }

        private void toolStripBtnBackwards_Click(object sender, EventArgs e)
        {
            sendObjectBackwards();
        }

        // 選択オブジェクトを一つ前面に移動
        private void bringObjectForwards()
        {
            if (countSelectedObject() == 0) return;
            List<ObjData> selList = new List<ObjData>();
            int n = 0;
            for (int i = 0; i < images.Count; i++)
            {
                ObjData o = images[i];
                if (o.selected)
                {
                    n = i;
                    selList.Add(o);
                }
            }

            images.RemoveAll(checkSelected);

            int nn = n - selList.Count + 2;
            if (nn >= images.Count)
            {
                foreach (ObjData o in selList)
                    images.Add(o);
            }
            else
            {
                for (int i = selList.Count - 1; i >= 0; i--)
                {
                    ObjData o = selList[i];
                    images.Insert(nn, o);
                }
            }

            pictureBox1.Invalidate();
        }

        // 選択オブジェクトを一つ背面に移動
        private void sendObjectBackwards()
        {
            if (countSelectedObject() == 0) return;
            List<ObjData> selList = new List<ObjData>();
            int n = images.Count - 1;
            for (int i = 0; i < images.Count; i++)
            {
                ObjData o = images[i];
                if (o.selected)
                {
                    if (i < n) n = i;
                    selList.Add(o);
                }
            }

            images.RemoveAll(checkSelected);

            int nn = n - 1;
            if (nn < 0) nn = 0;
            for (int i = selList.Count - 1; i >= 0; i--)
            {
                ObjData o = selList[i];
                images.Insert(nn, o);
            }

            pictureBox1.Invalidate();
        }

        private void leftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            leftAlignment();
        }

        private void toolStripBtnAlignLeft_Click(object sender, EventArgs e)
        {
            leftAlignment();
        }

        private void centredToolStripMenuItem_Click(object sender, EventArgs e)
        {
            centredAlignment();
        }

        private void toolStripBtnCentred_Click(object sender, EventArgs e)
        {
            centredAlignment();
        }

        private void rightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rightAlignment();
        }

        private void toolStripBtnRight_Click(object sender, EventArgs e)
        {
            rightAlignment();
        }

        private void topToolStripMenuItem_Click(object sender, EventArgs e)
        {
            topAlignment();
        }

        private void toolStripBtnTop_Click(object sender, EventArgs e)
        {
            topAlignment();
        }

        private void centreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            middleAlignment();
        }

        private void toolStripBtnMiddle_Click(object sender, EventArgs e)
        {
            middleAlignment();
        }

        private void bottomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bottomAlignment();
        }

        private void toolStripBtnBottom_Click(object sender, EventArgs e)
        {
            bottomAlignment();
        }

        // 選択オブジェクトを左揃えで並べる
        private void leftAlignment()
        {
            if (countSelectedObject() == 0) return;
            if (countSelectedObject() == 1)
            {
                // 選択オブジェクトが一つだけならキャンバスを基準に揃える
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(0, o.y);
            }
            else
            {
                // 選択オブジェクトが複数なら一番左のオブジェクトに合わせる
                int minX = canvasW;
                foreach (ObjData o in images)
                    if (o.selected && o.x < minX) minX = o.x;

                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(minX, o.y);
            }
            pictureBox1.Invalidate();
        }

        // 選択オブジェクトを中央揃えで並べる
        private void centredAlignment()
        {
            if (countSelectedObject() == 0) return;
            if (countSelectedObject() == 1)
            {
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(((canvasW - o.w) / 2), o.y);
            }
            else
            {
                int x0 = canvasW;
                int x1 = 0;
                foreach (ObjData o in images)
                {
                    if (o.selected)
                    {
                        if (o.x < x0) x0 = o.x;
                        if ((o.x + o.w) > x1) x1 = o.x + o.w;
                    }
                }

                int cx = x0 + ((x1 - x0) / 2);
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(cx - (o.w / 2), o.y);
            }
            pictureBox1.Invalidate();
        }

        // 選択オブジェクトを右揃えで並べる
        private void rightAlignment()
        {
            if (countSelectedObject() == 0) return;
            if (countSelectedObject() == 1)
            {
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(canvasW - o.w, o.y);
            }
            else
            {
                int x1 = 0;
                foreach (ObjData o in images)
                    if (o.selected && (o.x + o.w) > x1) x1 = (o.x + o.w);

                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(x1 - o.w, o.y);
            }
            pictureBox1.Invalidate();
        }

        // 選択オブジェクトを上揃えで並べる
        private void topAlignment()
        {
            if (countSelectedObject() == 0) return;
            if (countSelectedObject() == 1)
            {
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(o.x, 0);
            }
            else
            {
                int y0 = canvasH;
                foreach (ObjData o in images)
                    if (o.selected && o.y < y0) y0 = o.y;

                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(o.x, y0);
            }
            pictureBox1.Invalidate();
        }

        // 選択オブジェクトを縦中央揃えで並べる
        private void middleAlignment()
        {
            if (countSelectedObject() == 0) return;
            if (countSelectedObject() == 1)
            {
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(o.x, ((canvasH - o.h) / 2));
            }
            else
            {
                int y0 = canvasH;
                int y1 = 0;
                foreach (ObjData o in images)
                {
                    if (o.selected)
                    {
                        if (o.y < y0) y0 = o.y;
                        if ((o.y + o.h) > y1) y1 = o.y + o.h;
                    }
                }

                int cy = y0 + ((y1 - y0) / 2);
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(o.x, cy - (o.h / 2));
            }
            pictureBox1.Invalidate();
        }

        // 選択オブジェクトを下揃えで並べる
        private void bottomAlignment()
        {
            if (countSelectedObject() == 0) return;
            if (countSelectedObject() == 1)
            {
                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(o.x, canvasH - o.h);
            }
            else
            {
                int y1 = 0;
                foreach (ObjData o in images)
                    if (o.selected && (o.y + o.h) > y1) y1 = (o.y + o.h);

                foreach (ObjData o in images)
                    if (o.selected) o.setPosition(o.x, y1 - o.h);
            }
            pictureBox1.Invalidate();
        }

        // Grid Size 設定用 ComboBox のテキストが変更された際の処理
        // 
        // TODO リストから選んだ際にフォーカスを外す方法を調べないと
        private void toolStripComboBoxGridSize_TextChanged(object sender, EventArgs e)
        {
            ToolStripComboBox cb = toolStripComboBoxGridSize;
            int selIdx = cb.SelectedIndex;
            string s = cb.Text;
            int selLen = cb.SelectionLength;
            //Console.WriteLine(string.Format("SelectedIndex = {0} ({1}) , SelectionLength = {2}", selIdx, s, selLen));
            changeCanvasOrGridSize(s, false);
        }

        // Canvas Size 設定用 ComboBox のテキストが変更された際の処理
        private void toolStripComboBoxCanvasSize_TextChanged(object sender, EventArgs e)
        {
            ToolStripComboBox cb = toolStripComboBoxCanvasSize;
            int selIdx = cb.SelectedIndex;
            string s = cb.Text;
            int selLen = cb.SelectionLength;
            //Console.WriteLine(string.Format("SelectedIndex = {0} ({1}) , SelectionLength = {2}", selIdx, s, selLen));
            changeCanvasOrGridSize(s, true);
        }

        // キャンバスまたはグリッドサイズを変更
        private void changeCanvasOrGridSize(string s, Boolean selectCanvas)
        {
            // 入力テキストが適切なフォーマットなのか調べていく
            if (s == string.Empty)
            {
                //Console.WriteLine("text is empty");
                return;
            }

            char[] delimiterChars = { ' ', ',', 'x', ':', '\t' };
            string[] ss = s.Split(delimiterChars);
            if (ss.Length != 2)
            {
                //Console.WriteLine("text split Length != 2");
                return;
            }

            if (ss[0] == string.Empty || ss[1] == string.Empty)
            {
                //Console.WriteLine("w or h is empty");
                return;
            }

            int w = int.Parse(ss[0]);
            int h = int.Parse(ss[1]);
            if (w <= 3 || w > 15360 || h <= 3 || h > 15360)
            {
                //Console.WriteLine(string.Format("{0}x{1} is Illegal Value", w, h));
                return;
            }

            // キャンバスサイズまたはグリッドサイズを変更
            if (selectCanvas)
            {
                canvasW = w;
                canvasH = h;
            }
            else
            {
                gridW = w;
                gridH = h;
            }

            bgGridRedraw = true;
            setStatus();
            pictureBox1.Invalidate();
        }

        // フォーム上でキーが押された場合
        //
        // KeyDownイベントではキー入力を取りこぼす。
        // PreviewKeyDownイベントで処理をしたら取りこぼさなくなった。
        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // Grid や Canvas の設定ボックスにフォーカスがあるなら何もせずに戻る
            //if (toolStripComboBoxGridSize.Focused) return;
            //if (toolStripComboBoxCanvasSize.Focused) return;

            // Canvasの中にマウスカーソルが入ってなければ以降の処理はしない
            if (!mouseInCanvas) return;

            // escキーが押されたら全オブジェクトを非選択状態にする
            //if (e.KeyCode == Keys.Escape) selectOrDeselectAll(false);

            int dx = 1;
            int dy = 1;

            // Shiftキーが押されていたら移動増分を増やす
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                dx *= gridW;
                dy *= gridH;
            }

            // 選択されてる全オブジェクトをカーソルキーを使ってドット単位で移動
            int x = 0;
            int y = 0;
            if (e.KeyCode == Keys.Up) y -= dy;
            if (e.KeyCode == Keys.Down) y += dy;
            if (e.KeyCode == Keys.Left) x -= dx;
            if (e.KeyCode == Keys.Right) x += dx;
            if (x != 0 || y != 0) moveSelectObjByKey(x, y);
        }

        // 選択されてる全オブジェクトをキー入力で移動
        private void moveSelectObjByKey(int dx, int dy)
        {
            foreach (ObjData o in images)
                if (o.selected) o.setPosition(o.x + dx, o.y + dy);

            pictureBox1.Invalidate();
            setStatusBarObjInfo();
        }

        // メニューからキャンバスサイズ変更
        private void setCanvasSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSetCanvasSize f = new FormSetCanvasSize();

            f.canvasWidth = this.canvasW.ToString();
            f.canvasHeight = this.canvasH.ToString();
            f.canvasColor = this.canvasColor;
            f.gridColor = this.gridColor;

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                string s = string.Format("{0}x{1}", f.canvasWidth, f.canvasHeight);
                changeCanvasOrGridSize(s, true);
                canvasColor = f.canvasColor;
                gridColor = f.gridColor;
            }
            f.Dispose();
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setZoom(100);
        }

        private void zoom25ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setZoom(25);
        }

        private void zoom50ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setZoom(50);
        }

        private void zoom200ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setZoom(200);
        }

        private void zoom400ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setZoom(400);
        }

        private void zoom800ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setZoom(800);
        }

        private void toolStripSplitButtonZoom_ButtonClick(object sender, EventArgs e)
        {
            setZoom(100);
        }

        private void toolZoom25_Click(object sender, EventArgs e)
        {
            setZoom(25);
        }

        private void toolZoom50_Click(object sender, EventArgs e)
        {
            setZoom(50);
        }

        private void toolZoom100_Click(object sender, EventArgs e)
        {
            setZoom(100);
        }

        private void toolZoom200_Click(object sender, EventArgs e)
        {
            setZoom(200);
        }

        private void toolZoom400_Click(object sender, EventArgs e)
        {
            setZoom(400);
        }

        private void toolZoom800_Click(object sender, EventArgs e)
        {
            setZoom(800);
        }

        // 拡大率を指定
        private void setZoom(int zoom)
        {
            this.zoomValue = zoom;
            toolStripSplitButtonZoom.Text = string.Format("Zoom {0}%", zoomValue);
            pictureBox1.Invalidate();
        }

        // 全選択
        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectOrDeselectAll(true);
        }

        // 選択解除
        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectOrDeselectAll(false);
        }

        // 削除メニューを選択
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            deleteSelectObj();
        }

        // 削除ボタンをクリック
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            deleteSelectObj();
        }

        // 選択オブジェクトを削除。画像解放も行う
        private void deleteSelectObj()
        {
            for (int i = images.Count - 1; i >= 0; i--)
            {
                ObjData o = images[i];
                if (o.selected)
                {
                    o.disposeImage();
                    images.Remove(images[i]);
                }
            }
            pictureBox1.Invalidate();
        }

        // グリッドサイズ ComboBoxキー入力
        private void toolStripComboBoxGridSize_KeyDown(object sender, KeyEventArgs e)
        {
            // カーソル上下キーを無効化
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                e.Handled = true;
            }
        }

        // キャンバスサイズ ComboBoxキー入力
        private void toolStripComboBoxCanvasSize_KeyDown(object sender, KeyEventArgs e)
        {
            // カーソル上下キーを無効化
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                e.Handled = true;
            }
        }

        // マウスが PictureBox の中に入った
        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            //panel1.BackColor = Color.FromArgb(255, 110, 110, 110);
            mouseInCanvas = true;
        }

        // マウスが PictureBox の外に出た
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            //panel1.BackColor = Color.DimGray;
            mouseInCanvas = false;
        }

    }
}
