namespace WindowsFormsApplication3
{
    partial class TelaBusca
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.serClose = new System.Windows.Forms.Button();
            this.ser_lbl = new System.Windows.Forms.Label();
            this.quit = new System.Windows.Forms.Button();
            this.serError_lbl = new System.Windows.Forms.Label();
            this.serOpen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 23);
            this.label1.TabIndex = 0;
            // 
            // serClose
            // 
            this.serClose.Location = new System.Drawing.Point(0, 0);
            this.serClose.Name = "serClose";
            this.serClose.Size = new System.Drawing.Size(75, 23);
            this.serClose.TabIndex = 0;
            // 
            // ser_lbl
            // 
            this.ser_lbl.AutoSize = true;
            this.ser_lbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ser_lbl.Location = new System.Drawing.Point(30, 14);
            this.ser_lbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ser_lbl.Name = "ser_lbl";
            this.ser_lbl.Size = new System.Drawing.Size(0, 13);
            this.ser_lbl.TabIndex = 52;
            // 
            // quit
            // 
            this.quit.Location = new System.Drawing.Point(0, 0);
            this.quit.Name = "quit";
            this.quit.Size = new System.Drawing.Size(75, 23);
            this.quit.TabIndex = 0;
            // 
            // serError_lbl
            // 
            this.serError_lbl.AutoSize = true;
            this.serError_lbl.ForeColor = System.Drawing.Color.Red;
            this.serError_lbl.Location = new System.Drawing.Point(60, 40);
            this.serError_lbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.serError_lbl.Name = "serError_lbl";
            this.serError_lbl.Size = new System.Drawing.Size(0, 13);
            this.serError_lbl.TabIndex = 7;
            this.serError_lbl.Visible = false;
            // 
            // serOpen
            // 
            this.serOpen.Location = new System.Drawing.Point(0, 0);
            this.serOpen.Name = "serOpen";
            this.serOpen.Size = new System.Drawing.Size(75, 23);
            this.serOpen.TabIndex = 0;
            // 
            // TelaBusca
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(711, 607);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "TelaBusca";
            this.Text = "Xbee LMPT Software";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label ser_lbl;
        private System.Windows.Forms.Label serError_lbl;
        private System.Windows.Forms.Label label1;
    }
}

