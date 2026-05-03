using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace BeepPlayer
{
    public partial class frmBeepPlayer : Form
    {

        [DllImport("kernel32.dll")]
        public static extern bool Beep(int frequency, int duration);
        int[] freq = { 523, 587, 659, 698, 784, 880, 988, 1046 };
        int[] blackFreq = { 554, 622, 740, 831, 932 };
        int initWidth= 0;
        int initHeight = 0;
        Dictionary<string, Rect> initControl = new Dictionary<string, Rect>();
        public frmBeepPlayer()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            foreach (Control ctl in this.palMain.Controls)
            {
                if (ctl is Button btn && btn.Name.StartsWith("btn"))
                {
                    // 先減掉可能的重複綁定，再加回來，保證只會發聲一次
                    btn.Click -= btn1_Click;
                    btn.Click += btn1_Click;
                }
            }
            InitializePianoStyle(); // 套用鋼琴外觀
        }

        private void InitializePianoStyle()
        {
            foreach (Control ctl in this.palMain.Controls)
            {

                if (ctl is Button btn)
                {
                    // 根據名稱決定初始顏色
                    if (btn.Name.Contains("Black"))
                    {
                        btn.BackColor = Color.Black;
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.BackColor = Color.White;
                        btn.ForeColor = Color.Black;
                    }

                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = Color.LightGray;
                    btn.Cursor = Cursors.Hand;
                    btn.FlatAppearance.BorderSize = 0;
                }
            }
        }

        private async void btn1_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                // 1. 儲存原始顏色，以便發聲結束後還原
                Color oldColor = btn.BackColor;

                // 2. 視覺回饋：根據黑白鍵給予不同的「按下」效果
                // 如果名稱包含 "Black"，變更為深灰色 (DimGray)；否則變更為淺灰色 (Gainsboro)
                btn.BackColor = btn.Name.Contains("Black") ? Color.DimGray : Color.Gainsboro;

                // 3. 停用按鈕，防止使用者在播放期間連續點擊導致音訊堆疊或程式當掉
                btn.Enabled = false;

                try
                {
                    int frequency;

                    // 4. 根據按鈕名稱動態決定讀取哪一個頻率陣列
                    if (btn.Name.Contains("Black"))
                    {
                        // 因為黑鍵 TabIndex 是 10-14，所以要減掉 10 才能對應到 blackFreq 的 0-4
                        int index = btn.TabIndex - 10;

                        // 確保 index 在合法範圍內 (0 到 4)
                        index = Math.Max(0, Math.Min(index, blackFreq.Length - 1));
                        frequency = blackFreq[index];
                    }
                    else
                    {
                        // 白鍵 TabIndex 是 0-7，直接使用即可
                        int index = Math.Max(0, Math.Min(btn.TabIndex, freq.Length - 1));
                        frequency = freq[index];
                    }

                    // 5. 使用非同步 Task.Run 呼叫底層 API，避免 Beep 期間 UI 視窗凍結 (Not Responding)
                    // 持續時間設為 300 毫秒
                    await Task.Run(() => Beep(frequency, 300));
                }
                catch (Exception ex)
                {
                    // 捕捉可能的系統異常並顯示，增強程式健壯性
                    MessageBox.Show($"播放音效時發生錯誤：{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // 6. 無論播放成功與否，最後都要恢復按鈕狀態
                    btn.Enabled = true;
                    btn.BackColor = oldColor;
                }
            }
        }

        private void frmBeepPlayer_Load(object sender, EventArgs e)
        {
            //儲存初始視窗大小
            this.initWidth = this.palMain.Width;
            this.initHeight = this.palMain.Height;
            //儲存初始控制像位置和大小
            foreach (Control ctl in this.palMain.Controls)
            {
                this.initControl.Add(ctl.Name, new Rect(ctl.Left, ctl.Top, ctl.Width, ctl.Height));
            }
        }

        /// <summary>
        /// 當視窗改變大小 根據初始大小和控制項位置調整控制項的位置和大小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmBeepPlayer_SizeChanged(object sender, EventArgs e)
        {
            this.SuspendLayout();

            double width = this.palMain.Width;
            double height = this.palMain.Height;
            double iRatioWith = width / this.initWidth;
            double iRatioHeight = height / this.initHeight;
            foreach (Control ctl in this.palMain.Controls)
            {
                if (initControl.ContainsKey(ctl.Name))
                {
                    ctl.Left = (int)(initControl[ctl.Name].Left * iRatioWith);
                    ctl.Top = (int)(initControl[ctl.Name].Top * iRatioHeight);
                    ctl.Width = (int)(initControl[ctl.Name].Width * iRatioWith);
                    ctl.Height = (int)(initControl[ctl.Name].Height * iRatioHeight);
                }
            }
            this.ResumeLayout();
        }

        private void frmBeepPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("確定要關閉應用程式嗎？", "關閉確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                e.Cancel = true; // 取消關閉
            }
        }
    }
}
