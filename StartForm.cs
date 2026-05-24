using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using move_game.Level;

namespace move_game
{
    public partial class StartForm : Form
    {
        private readonly List<Button> _menuButtons = new List<Button>();
        private readonly string[] _difficulties = { "\u7b80\u5355", "\u666e\u901a", "\u56f0\u96be" };
        private int _difficultyIndex = 0;
        private TableLayoutPanel _menuLayout;
        private Image _backgroundImage;
        private Bitmap _backgroundBuffer;

        public StartForm()
        {
            InitializeComponent();
            LoadCustomBackground();
            InitializeStartScreen();
        }

        private void InitializeStartScreen()
        {
            SuspendLayout();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();

            Text = "slimove";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(12, 20, 28);
            ClientSize = new Size(900, 510);

            var content = new BufferedTableLayoutPanel
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(0, 70, 0, 70)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));

            var title = new GradientTitleLabel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "slimove",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 64F, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            _menuLayout = new BufferedTableLayoutPanel
            {
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0, 4, 0, 4)
            };

            for (int i = 0; i < 4; i++)
            {
                _menuLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            AddMenuButton("\u5f00\u59cb\u6e38\u620f", StartGame);
            AddMenuButton("\u73a9\u6cd5\u4ecb\u7ecd", ShowHowToPlay);
            AddMenuButton(GetDifficultyText(), ChangeDifficulty);
            AddMenuButton("\u9000\u51fa\u6e38\u620f", ExitGame);

            content.Controls.Add(title, 1, 0);
            content.Controls.Add(_menuLayout, 1, 1);
            Controls.Add(content);
            ResumeLayout(true);
        }

        private void LoadCustomBackground()
        {
            // Change this path to replace the start screen background.
            // Example: Path.Combine(Application.StartupPath, "assets", "your_background.png")
            string backgroundPath = Path.Combine(Application.StartupPath, "assets", "background.png");

            if (File.Exists(backgroundPath))
            {
                _backgroundImage = Image.FromFile(backgroundPath);
                ResetBackgroundBuffer();
            }
        }

        private void AddMenuButton(string text, EventHandler clickHandler)
        {
            var button = new Button
            {
                Text = text,
                Anchor = AnchorStyles.None,
                Size = new Size(310, 42),
                Margin = new Padding(0, 6, 0, 6),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 214, 232),
                BackColor = Color.FromArgb(160, 28, 39, 52),
                Cursor = Cursors.Hand,
                TabStop = false
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(170, 178, 229, 255);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(215, 44, 69, 88);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 70, 112, 135);
            button.Click += clickHandler;
            button.MouseEnter += (sender, e) => button.ForeColor = Color.FromArgb(255, 236, 246);
            button.MouseLeave += (sender, e) => button.ForeColor = Color.FromArgb(255, 214, 232);

            _menuButtons.Add(button);
            _menuLayout.Controls.Add(button, 0, _menuButtons.Count - 1);
        }

        private string GetDifficultyText()
        {
            return "\u9009\u62e9\u96be\u5ea6\uff1a" + _difficulties[_difficultyIndex];
        }

        private void StartGame(object sender, EventArgs e)
        {
            Hide();
            var game = new Game.MainGame(this, 1, GetSelectedDifficulty());
            game.Show();
        }

        private void ShowHowToPlay(object sender, EventArgs e)
        {
            MessageBox.Show(
                "\u4f7f\u7528 W / A / S / D \u63a7\u5236\u89d2\u8272\u79fb\u52a8\u3002\n\n" +
                "\u89d2\u8272\u5f00\u59cb\u6ed1\u52a8\u540e\u4f1a\u4e00\u76f4\u524d\u8fdb\uff0c" +
                "\u78b0\u5230\u969c\u788d\u7269\u6216\u505c\u9760\u683c\u540e\u624d\u80fd\u518d\u6b21\u9009\u62e9\u65b9\u5411\u3002\n\n" +
                "\u5230\u8fbe\u7ec8\u70b9\u5373\u53ef\u83b7\u80dc\u3002",
                "\u73a9\u6cd5\u4ecb\u7ecd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ChangeDifficulty(object sender, EventArgs e)
        {
            _difficultyIndex = (_difficultyIndex + 1) % _difficulties.Length;
            ((Button)sender).Text = GetDifficultyText();
        }

        private GameDifficulty GetSelectedDifficulty()
        {
            return (GameDifficulty)_difficultyIndex;
        }

        private void ExitGame(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Bitmap buffer = GetBackgroundBuffer();
            if (buffer != null)
            {
                e.Graphics.DrawImageUnscaled(buffer, 0, 0);
                return;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            ResetBackgroundBuffer();
            base.OnResize(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: reduce child-control flicker.
                return cp;
            }
        }

        private Bitmap GetBackgroundBuffer()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return null;
            }

            if (_backgroundBuffer != null && _backgroundBuffer.Size == ClientSize)
            {
                return _backgroundBuffer;
            }

            ResetBackgroundBuffer();
            _backgroundBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);

            using (Graphics graphics = Graphics.FromImage(_backgroundBuffer))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                Rectangle bounds = new Rectangle(Point.Empty, ClientSize);

            if (_backgroundImage != null)
            {
                    graphics.DrawImage(_backgroundImage, bounds);
            }
            else
            {
                using (var brush = new LinearGradientBrush(
                        bounds,
                    Color.FromArgb(11, 18, 30),
                    Color.FromArgb(35, 62, 74),
                    LinearGradientMode.ForwardDiagonal))
                {
                        graphics.FillRectangle(brush, bounds);
                }
            }

            using (var overlay = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            {
                    graphics.FillRectangle(overlay, bounds);
                }
            }

            return _backgroundBuffer;
        }

        private void ResetBackgroundBuffer()
        {
            _backgroundBuffer?.Dispose();
            _backgroundBuffer = null;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _backgroundBuffer?.Dispose();
            _backgroundImage?.Dispose();
            base.OnFormClosed(e);
        }

        private class GradientTitleLabel : Label
        {
            public GradientTitleLabel()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.SupportsTransparentBackColor,
                    true);
                UpdateStyles();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                var titleRect = new Rectangle(0, 0, Width, (int)(Height * 0.68F));

                using (var path = CreateTextPath(titleRect, e.Graphics))
                {
                    for (int depth = 7; depth >= 1; depth--)
                    {
                        using (var depthPath = (GraphicsPath)path.Clone())
                        using (var matrix = new Matrix())
                        using (var depthBrush = new SolidBrush(Color.FromArgb(55 + depth * 10, 20, 96, 61)))
                        {
                            matrix.Translate(depth * 0.9F, depth * 1.1F);
                            depthPath.Transform(matrix);
                            e.Graphics.FillPath(depthBrush, depthPath);
                        }
                    }

                    using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 5, 28, 20)))
                    using (var shadowPath = (GraphicsPath)path.Clone())
                    {
                        using (var matrix = new Matrix())
                        {
                            matrix.Translate(3F, 4F);
                            shadowPath.Transform(matrix);
                        }
                        e.Graphics.FillPath(shadowBrush, shadowPath);
                    }

                    using (var brush = new LinearGradientBrush(
                        ClientRectangle,
                        Color.FromArgb(235, 255, 219),
                        Color.FromArgb(103, 224, 152),
                        LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }

            private GraphicsPath CreateTextPath(Rectangle bounds, Graphics graphics)
            {
                var path = new GraphicsPath();
                using (var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    float emSize = graphics.DpiY * Font.Size / 72F;
                    path.AddString(Text, Font.FontFamily, (int)Font.Style, emSize, bounds, format);
                }

                return path;
            }
        }

        private class BufferedTableLayoutPanel : TableLayoutPanel
        {
            public BufferedTableLayoutPanel()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.SupportsTransparentBackColor,
                    true);
                UpdateStyles();
            }
        }
    }
}
