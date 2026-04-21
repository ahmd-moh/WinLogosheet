namespace WinLogosheet
{
    partial class Form1
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
            if (listView1 != null)
            {
                listView1.DrawColumnHeader -= ListView1_DrawColumnHeader;
                listView1.DrawSubItem -= ListView1_DrawSubItem;
                listView1.DrawItem -= ListView1_DrawItem;
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
            this.button_next = new System.Windows.Forms.Button();
            this.button_prv = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.panel_fixedError = new System.Windows.Forms.Panel();
            this.label21 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.button_copy_fromNext = new System.Windows.Forms.Button();
            this.button_copy_fromPrev = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader12 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader13 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader14 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader15 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader16 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader17 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_14 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_15 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_16 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader18 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel20 = new System.Windows.Forms.Panel();
            this.lable_date = new System.Windows.Forms.Label();
            this.label22 = new System.Windows.Forms.Label();
            this.button_OnPrint = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.button_UpdateCurrHImage = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.textBox7 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox8 = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.textBox9 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox10 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox11 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox12 = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.textBox13 = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox14 = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.textBox15 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox16 = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.textBox21 = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox22 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox23 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel7 = new System.Windows.Forms.Panel();
            this.panel8 = new System.Windows.Forms.Panel();
            this.panel9 = new System.Windows.Forms.Panel();
            this.panel10 = new System.Windows.Forms.Panel();
            this.panel11 = new System.Windows.Forms.Panel();
            this.panel12 = new System.Windows.Forms.Panel();
            this.panel13 = new System.Windows.Forms.Panel();
            this.panel14 = new System.Windows.Forms.Panel();
            this.panel15 = new System.Windows.Forms.Panel();
            this.panel16 = new System.Windows.Forms.Panel();
            this.panel17 = new System.Windows.Forms.Panel();
            this.panel18 = new System.Windows.Forms.Panel();
            this.panel19 = new System.Windows.Forms.Panel();
            this.panel22 = new System.Windows.Forms.Panel();
            this.panel24 = new System.Windows.Forms.Panel();
            this.panel25 = new System.Windows.Forms.Panel();
            this.panel23 = new System.Windows.Forms.Panel();
            this.panel21 = new System.Windows.Forms.Panel();
            this.panel26 = new System.Windows.Forms.Panel();
            this.panel38 = new System.Windows.Forms.Panel();
            this.panel39 = new System.Windows.Forms.Panel();
            this.panel40 = new System.Windows.Forms.Panel();
            this.panel41 = new System.Windows.Forms.Panel();
            this.panel42 = new System.Windows.Forms.Panel();
            this.panel43 = new System.Windows.Forms.Panel();
            this.panel44 = new System.Windows.Forms.Panel();
            this.panel45 = new System.Windows.Forms.Panel();
            this.panel46 = new System.Windows.Forms.Panel();
            this.panel47 = new System.Windows.Forms.Panel();
            this.panel48 = new System.Windows.Forms.Panel();
            this.panel49 = new System.Windows.Forms.Panel();
            this.panel32 = new System.Windows.Forms.Panel();
            this.panel33 = new System.Windows.Forms.Panel();
            this.panel34 = new System.Windows.Forms.Panel();
            this.panel35 = new System.Windows.Forms.Panel();
            this.panel36 = new System.Windows.Forms.Panel();
            this.panel37 = new System.Windows.Forms.Panel();
            this.panel27 = new System.Windows.Forms.Panel();
            this.panel28 = new System.Windows.Forms.Panel();
            this.panel29 = new System.Windows.Forms.Panel();
            this.panel30 = new System.Windows.Forms.Panel();
            this.panel31 = new System.Windows.Forms.Panel();
            this.label23 = new System.Windows.Forms.Label();
            this.textBox24 = new System.Windows.Forms.TextBox();
            this.textBox17 = new System.Windows.Forms.TextBox();
            this.label26 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button_DeleteCurrentImage = new System.Windows.Forms.Button();
            this.panel146 = new System.Windows.Forms.Panel();
            this.panel147 = new System.Windows.Forms.Panel();
            this.panel148 = new System.Windows.Forms.Panel();
            this.panel149 = new System.Windows.Forms.Panel();
            this.panel150 = new System.Windows.Forms.Panel();
            this.panel151 = new System.Windows.Forms.Panel();
            this.panel152 = new System.Windows.Forms.Panel();
            this.panel153 = new System.Windows.Forms.Panel();
            this.panel154 = new System.Windows.Forms.Panel();
            this.panel155 = new System.Windows.Forms.Panel();
            this.panel156 = new System.Windows.Forms.Panel();
            this.panel157 = new System.Windows.Forms.Panel();
            this.panel158 = new System.Windows.Forms.Panel();
            this.panel159 = new System.Windows.Forms.Panel();
            this.panel160 = new System.Windows.Forms.Panel();
            this.panel161 = new System.Windows.Forms.Panel();
            this.panel162 = new System.Windows.Forms.Panel();
            this.panel163 = new System.Windows.Forms.Panel();
            this.panel164 = new System.Windows.Forms.Panel();
            this.panel165 = new System.Windows.Forms.Panel();
            this.panel166 = new System.Windows.Forms.Panel();
            this.panel167 = new System.Windows.Forms.Panel();
            this.panel168 = new System.Windows.Forms.Panel();
            this.panel169 = new System.Windows.Forms.Panel();
            this.panel170 = new System.Windows.Forms.Panel();
            this.panel171 = new System.Windows.Forms.Panel();
            this.panel172 = new System.Windows.Forms.Panel();
            this.panel173 = new System.Windows.Forms.Panel();
            this.panel174 = new System.Windows.Forms.Panel();
            this.panel175 = new System.Windows.Forms.Panel();
            this.panel176 = new System.Windows.Forms.Panel();
            this.panel177 = new System.Windows.Forms.Panel();
            this.panel178 = new System.Windows.Forms.Panel();
            this.panel179 = new System.Windows.Forms.Panel();
            this.panel180 = new System.Windows.Forms.Panel();
            this.panel181 = new System.Windows.Forms.Panel();
            this.panel182 = new System.Windows.Forms.Panel();
            this.panel183 = new System.Windows.Forms.Panel();
            this.panel184 = new System.Windows.Forms.Panel();
            this.panel185 = new System.Windows.Forms.Panel();
            this.panel186 = new System.Windows.Forms.Panel();
            this.panel187 = new System.Windows.Forms.Panel();
            this.panel188 = new System.Windows.Forms.Panel();
            this.panel189 = new System.Windows.Forms.Panel();
            this.panel190 = new System.Windows.Forms.Panel();
            this.panel191 = new System.Windows.Forms.Panel();
            this.panel192 = new System.Windows.Forms.Panel();
            this.panel193 = new System.Windows.Forms.Panel();
            this.panel74 = new System.Windows.Forms.Panel();
            this.panel75 = new System.Windows.Forms.Panel();
            this.panel76 = new System.Windows.Forms.Panel();
            this.panel77 = new System.Windows.Forms.Panel();
            this.panel78 = new System.Windows.Forms.Panel();
            this.panel79 = new System.Windows.Forms.Panel();
            this.panel80 = new System.Windows.Forms.Panel();
            this.panel81 = new System.Windows.Forms.Panel();
            this.panel82 = new System.Windows.Forms.Panel();
            this.panel83 = new System.Windows.Forms.Panel();
            this.panel84 = new System.Windows.Forms.Panel();
            this.panel85 = new System.Windows.Forms.Panel();
            this.panel86 = new System.Windows.Forms.Panel();
            this.panel87 = new System.Windows.Forms.Panel();
            this.panel88 = new System.Windows.Forms.Panel();
            this.panel89 = new System.Windows.Forms.Panel();
            this.panel90 = new System.Windows.Forms.Panel();
            this.panel91 = new System.Windows.Forms.Panel();
            this.panel92 = new System.Windows.Forms.Panel();
            this.panel93 = new System.Windows.Forms.Panel();
            this.panel94 = new System.Windows.Forms.Panel();
            this.panel95 = new System.Windows.Forms.Panel();
            this.panel96 = new System.Windows.Forms.Panel();
            this.panel97 = new System.Windows.Forms.Panel();
            this.panel50 = new System.Windows.Forms.Panel();
            this.panel51 = new System.Windows.Forms.Panel();
            this.panel52 = new System.Windows.Forms.Panel();
            this.panel53 = new System.Windows.Forms.Panel();
            this.panel54 = new System.Windows.Forms.Panel();
            this.panel55 = new System.Windows.Forms.Panel();
            this.panel56 = new System.Windows.Forms.Panel();
            this.panel57 = new System.Windows.Forms.Panel();
            this.panel58 = new System.Windows.Forms.Panel();
            this.panel59 = new System.Windows.Forms.Panel();
            this.panel60 = new System.Windows.Forms.Panel();
            this.panel61 = new System.Windows.Forms.Panel();
            this.panel62 = new System.Windows.Forms.Panel();
            this.panel63 = new System.Windows.Forms.Panel();
            this.panel64 = new System.Windows.Forms.Panel();
            this.panel65 = new System.Windows.Forms.Panel();
            this.panel66 = new System.Windows.Forms.Panel();
            this.panel67 = new System.Windows.Forms.Panel();
            this.panel68 = new System.Windows.Forms.Panel();
            this.panel69 = new System.Windows.Forms.Panel();
            this.panel70 = new System.Windows.Forms.Panel();
            this.panel71 = new System.Windows.Forms.Panel();
            this.panel72 = new System.Windows.Forms.Panel();
            this.panel73 = new System.Windows.Forms.Panel();
            this.textBox20 = new System.Windows.Forms.TextBox();
            this.textBox19 = new System.Windows.Forms.TextBox();
            this.textBox18 = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.panel98 = new System.Windows.Forms.Panel();
            this.panel122 = new System.Windows.Forms.Panel();
            this.panel123 = new System.Windows.Forms.Panel();
            this.panel124 = new System.Windows.Forms.Panel();
            this.panel125 = new System.Windows.Forms.Panel();
            this.panel126 = new System.Windows.Forms.Panel();
            this.panel127 = new System.Windows.Forms.Panel();
            this.panel128 = new System.Windows.Forms.Panel();
            this.panel129 = new System.Windows.Forms.Panel();
            this.panel130 = new System.Windows.Forms.Panel();
            this.panel131 = new System.Windows.Forms.Panel();
            this.panel132 = new System.Windows.Forms.Panel();
            this.panel133 = new System.Windows.Forms.Panel();
            this.panel134 = new System.Windows.Forms.Panel();
            this.panel135 = new System.Windows.Forms.Panel();
            this.panel136 = new System.Windows.Forms.Panel();
            this.panel137 = new System.Windows.Forms.Panel();
            this.panel138 = new System.Windows.Forms.Panel();
            this.panel139 = new System.Windows.Forms.Panel();
            this.panel140 = new System.Windows.Forms.Panel();
            this.panel141 = new System.Windows.Forms.Panel();
            this.panel142 = new System.Windows.Forms.Panel();
            this.panel143 = new System.Windows.Forms.Panel();
            this.panel144 = new System.Windows.Forms.Panel();
            this.panel145 = new System.Windows.Forms.Panel();
            this.panel99 = new System.Windows.Forms.Panel();
            this.panel100 = new System.Windows.Forms.Panel();
            this.panel101 = new System.Windows.Forms.Panel();
            this.panel102 = new System.Windows.Forms.Panel();
            this.panel103 = new System.Windows.Forms.Panel();
            this.panel104 = new System.Windows.Forms.Panel();
            this.panel105 = new System.Windows.Forms.Panel();
            this.panel106 = new System.Windows.Forms.Panel();
            this.panel107 = new System.Windows.Forms.Panel();
            this.panel108 = new System.Windows.Forms.Panel();
            this.panel109 = new System.Windows.Forms.Panel();
            this.panel110 = new System.Windows.Forms.Panel();
            this.panel111 = new System.Windows.Forms.Panel();
            this.panel112 = new System.Windows.Forms.Panel();
            this.panel113 = new System.Windows.Forms.Panel();
            this.panel114 = new System.Windows.Forms.Panel();
            this.panel115 = new System.Windows.Forms.Panel();
            this.panel116 = new System.Windows.Forms.Panel();
            this.panel117 = new System.Windows.Forms.Panel();
            this.panel118 = new System.Windows.Forms.Panel();
            this.panel119 = new System.Windows.Forms.Panel();
            this.panel120 = new System.Windows.Forms.Panel();
            this.panel121 = new System.Windows.Forms.Panel();
            this.checkBoxYarema = new System.Windows.Forms.CheckBox();
            this.checkBoxQayara = new System.Windows.Forms.CheckBox();
            this.checkBoxT1 = new System.Windows.Forms.CheckBox();
            this.checkBoxT2 = new System.Windows.Forms.CheckBox();
            this.checkBoxT3 = new System.Windows.Forms.CheckBox();
            this.checkBoxFdd = new System.Windows.Forms.CheckBox();
            this.button_Calibrate = new System.Windows.Forms.Button();
            this.button_start = new System.Windows.Forms.Button();
            this.button_new = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.panel_fixedError.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panel20.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel19.SuspendLayout();
            this.panel22.SuspendLayout();
            this.panel24.SuspendLayout();
            this.panel26.SuspendLayout();
            this.panel38.SuspendLayout();
            this.panel39.SuspendLayout();
            this.panel40.SuspendLayout();
            this.panel41.SuspendLayout();
            this.panel45.SuspendLayout();
            this.panel46.SuspendLayout();
            this.panel32.SuspendLayout();
            this.panel33.SuspendLayout();
            this.panel34.SuspendLayout();
            this.panel27.SuspendLayout();
            this.panel28.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel146.SuspendLayout();
            this.panel147.SuspendLayout();
            this.panel148.SuspendLayout();
            this.panel149.SuspendLayout();
            this.panel150.SuspendLayout();
            this.panel151.SuspendLayout();
            this.panel155.SuspendLayout();
            this.panel156.SuspendLayout();
            this.panel160.SuspendLayout();
            this.panel161.SuspendLayout();
            this.panel162.SuspendLayout();
            this.panel166.SuspendLayout();
            this.panel167.SuspendLayout();
            this.panel171.SuspendLayout();
            this.panel172.SuspendLayout();
            this.panel173.SuspendLayout();
            this.panel174.SuspendLayout();
            this.panel178.SuspendLayout();
            this.panel179.SuspendLayout();
            this.panel183.SuspendLayout();
            this.panel184.SuspendLayout();
            this.panel185.SuspendLayout();
            this.panel189.SuspendLayout();
            this.panel190.SuspendLayout();
            this.panel74.SuspendLayout();
            this.panel75.SuspendLayout();
            this.panel76.SuspendLayout();
            this.panel77.SuspendLayout();
            this.panel78.SuspendLayout();
            this.panel82.SuspendLayout();
            this.panel83.SuspendLayout();
            this.panel87.SuspendLayout();
            this.panel88.SuspendLayout();
            this.panel89.SuspendLayout();
            this.panel93.SuspendLayout();
            this.panel94.SuspendLayout();
            this.panel50.SuspendLayout();
            this.panel51.SuspendLayout();
            this.panel52.SuspendLayout();
            this.panel53.SuspendLayout();
            this.panel54.SuspendLayout();
            this.panel58.SuspendLayout();
            this.panel59.SuspendLayout();
            this.panel63.SuspendLayout();
            this.panel64.SuspendLayout();
            this.panel65.SuspendLayout();
            this.panel69.SuspendLayout();
            this.panel70.SuspendLayout();
            this.panel98.SuspendLayout();
            this.panel122.SuspendLayout();
            this.panel123.SuspendLayout();
            this.panel124.SuspendLayout();
            this.panel125.SuspendLayout();
            this.panel126.SuspendLayout();
            this.panel130.SuspendLayout();
            this.panel131.SuspendLayout();
            this.panel135.SuspendLayout();
            this.panel136.SuspendLayout();
            this.panel137.SuspendLayout();
            this.panel141.SuspendLayout();
            this.panel142.SuspendLayout();
            this.panel99.SuspendLayout();
            this.panel100.SuspendLayout();
            this.panel101.SuspendLayout();
            this.panel102.SuspendLayout();
            this.panel106.SuspendLayout();
            this.panel107.SuspendLayout();
            this.panel111.SuspendLayout();
            this.panel112.SuspendLayout();
            this.panel113.SuspendLayout();
            this.panel117.SuspendLayout();
            this.panel118.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_next
            // 
            this.button_next.Location = new System.Drawing.Point(280, 16);
            this.button_next.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_next.Name = "button_next";
            this.button_next.Size = new System.Drawing.Size(75, 31);
            this.button_next.TabIndex = 0;
            this.button_next.TabStop = false;
            this.button_next.Text = "Next >>";
            this.button_next.UseVisualStyleBackColor = true;
            this.button_next.Click += new System.EventHandler(this.button_next_Click);
            this.button_next.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnNext);
            // 
            // button_prv
            // 
            this.button_prv.Location = new System.Drawing.Point(7, 15);
            this.button_prv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_prv.Name = "button_prv";
            this.button_prv.Size = new System.Drawing.Size(75, 31);
            this.button_prv.TabIndex = 0;
            this.button_prv.TabStop = false;
            this.button_prv.Text = "<< Perv";
            this.button_prv.UseVisualStyleBackColor = true;
            this.button_prv.Click += new System.EventHandler(this.button_prv_Click);
            this.button_prv.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnPrev);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.labelStatus);
            this.groupBox1.Controls.Add(this.button_prv);
            this.groupBox1.Controls.Add(this.button_next);
            this.groupBox1.Location = new System.Drawing.Point(13, 12);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(376, 53);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatus.Location = new System.Drawing.Point(124, 17);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(57, 20);
            this.labelStatus.TabIndex = 5;
            this.labelStatus.Text = "Hours";
            // 
            // panel_fixedError
            // 
            this.panel_fixedError.BackColor = System.Drawing.Color.Black;
            this.panel_fixedError.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel_fixedError.Controls.Add(this.label21);
            this.panel_fixedError.Controls.Add(this.label20);
            this.panel_fixedError.Controls.Add(this.button_copy_fromNext);
            this.panel_fixedError.Controls.Add(this.button_copy_fromPrev);
            this.panel_fixedError.Location = new System.Drawing.Point(21, 297);
            this.panel_fixedError.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel_fixedError.Name = "panel_fixedError";
            this.panel_fixedError.Size = new System.Drawing.Size(336, 187);
            this.panel_fixedError.TabIndex = 9;
            this.panel_fixedError.Visible = false;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.BackColor = System.Drawing.Color.Black;
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.ForeColor = System.Drawing.Color.Coral;
            this.label21.Location = new System.Drawing.Point(15, 42);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(191, 18);
            this.label21.TabIndex = 1;
            this.label21.Text = " - To fixed it do the following";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.BackColor = System.Drawing.Color.Black;
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.Coral;
            this.label20.Location = new System.Drawing.Point(13, 17);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(235, 18);
            this.label20.TabIndex = 1;
            this.label20.Text = "Error in loading or found the image";
            // 
            // button_copy_fromNext
            // 
            this.button_copy_fromNext.BackColor = System.Drawing.SystemColors.Info;
            this.button_copy_fromNext.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_copy_fromNext.Location = new System.Drawing.Point(41, 128);
            this.button_copy_fromNext.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_copy_fromNext.Name = "button_copy_fromNext";
            this.button_copy_fromNext.Size = new System.Drawing.Size(235, 38);
            this.button_copy_fromNext.TabIndex = 0;
            this.button_copy_fromNext.Text = "Copy from the next  Values";
            this.button_copy_fromNext.UseVisualStyleBackColor = false;
            this.button_copy_fromNext.Click += new System.EventHandler(this.button_copy_fromNext_Click);
            // 
            // button_copy_fromPrev
            // 
            this.button_copy_fromPrev.BackColor = System.Drawing.SystemColors.Info;
            this.button_copy_fromPrev.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_copy_fromPrev.Location = new System.Drawing.Point(41, 74);
            this.button_copy_fromPrev.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_copy_fromPrev.Name = "button_copy_fromPrev";
            this.button_copy_fromPrev.Size = new System.Drawing.Size(235, 38);
            this.button_copy_fromPrev.TabIndex = 0;
            this.button_copy_fromPrev.Text = "Copy from Pervious Values";
            this.button_copy_fromPrev.UseVisualStyleBackColor = false;
            this.button_copy_fromPrev.Click += new System.EventHandler(this.button_copy_fromPrev_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 949);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 15, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1924, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(42, 17);
            this.toolStripStatusLabel1.Text = "Status:";
            // 
            // listView1
            // 
            this.listView1.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader_2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader_6,
            this.columnHeader2,
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader_8,
            this.columnHeader8,
            this.columnHeader9,
            this.columnHeader10,
            this.columnHeader_11,
            this.columnHeader11,
            this.columnHeader12,
            this.columnHeader13,
            this.columnHeader14,
            this.columnHeader15,
            this.columnHeader16,
            this.columnHeader17,
            this.columnHeader_14,
            this.columnHeader_15,
            this.columnHeader_16,
            this.columnHeader18});
            this.listView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(391, 63);
            this.listView1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listView1.Name = "listView1";
            this.listView1.OwnerDraw = true;
            this.listView1.ShowItemToolTips = true;
            this.listView1.Size = new System.Drawing.Size(1492, 831);
            this.listView1.TabIndex = 8;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Hours";
            this.columnHeader1.Width = 40;
            // 
            // columnHeader_2
            // 
            this.columnHeader_2.Text = "Yaremja-KV";
            this.columnHeader_2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_2.Width = 70;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "A";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "WM";
            this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "WV";
            this.columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader_6
            // 
            this.columnHeader_6.Text = "Qayara-KV";
            this.columnHeader_6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_6.Width = 70;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "A";
            this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "WM";
            this.columnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "MV";
            this.columnHeader7.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader_8
            // 
            this.columnHeader_8.Text = "T1";
            this.columnHeader_8.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_8.Width = 70;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "A";
            this.columnHeader8.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "WM";
            this.columnHeader9.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "MV";
            this.columnHeader10.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader_11
            // 
            this.columnHeader_11.Text = "T2";
            this.columnHeader_11.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_11.Width = 70;
            // 
            // columnHeader11
            // 
            this.columnHeader11.Text = "A";
            this.columnHeader11.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader12
            // 
            this.columnHeader12.Text = "MW";
            this.columnHeader12.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader13
            // 
            this.columnHeader13.Text = "MV";
            this.columnHeader13.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader14
            // 
            this.columnHeader14.Text = "T3";
            this.columnHeader14.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader14.Width = 70;
            // 
            // columnHeader15
            // 
            this.columnHeader15.Text = "A";
            this.columnHeader15.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader16
            // 
            this.columnHeader16.Text = "WM";
            this.columnHeader16.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader17
            // 
            this.columnHeader17.Text = "MV";
            this.columnHeader17.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader_14
            // 
            this.columnHeader_14.Text = "Domez";
            this.columnHeader_14.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_14.Width = 70;
            // 
            // columnHeader_15
            // 
            this.columnHeader_15.Text = "Summer";
            this.columnHeader_15.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_15.Width = 70;
            // 
            // columnHeader_16
            // 
            this.columnHeader_16.Text = "[1] Salam";
            this.columnHeader_16.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader_16.Width = 70;
            // 
            // columnHeader18
            // 
            this.columnHeader18.Text = "[2]Salam";
            this.columnHeader18.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader18.Width = 70;
            // 
            // panel20
            // 
            this.panel20.Controls.Add(this.lable_date);
            this.panel20.Controls.Add(this.label22);
            this.panel20.Location = new System.Drawing.Point(404, 23);
            this.panel20.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel20.Name = "panel20";
            this.panel20.Size = new System.Drawing.Size(1113, 42);
            this.panel20.TabIndex = 9;
            // 
            // lable_date
            // 
            this.lable_date.AutoSize = true;
            this.lable_date.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lable_date.Location = new System.Drawing.Point(692, 7);
            this.lable_date.Name = "lable_date";
            this.lable_date.Size = new System.Drawing.Size(50, 24);
            this.lable_date.TabIndex = 2;
            this.lable_date.Text = "2025";
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.label22.Location = new System.Drawing.Point(435, 4);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(151, 29);
            this.label22.TabIndex = 0;
            this.label22.Text = "محطة الشمسيات";
            // 
            // button_OnPrint
            // 
            this.button_OnPrint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_OnPrint.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_OnPrint.Location = new System.Drawing.Point(408, 902);
            this.button_OnPrint.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_OnPrint.Name = "button_OnPrint";
            this.button_OnPrint.Size = new System.Drawing.Size(199, 36);
            this.button_OnPrint.TabIndex = 10;
            this.button_OnPrint.Text = "طباعة";
            this.button_OnPrint.UseVisualStyleBackColor = true;
            this.button_OnPrint.Click += new System.EventHandler(this.buttonOnPrint);
            // 
            // button_UpdateCurrHImage
            // 
            this.button_UpdateCurrHImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_UpdateCurrHImage.Location = new System.Drawing.Point(105, 833);
            this.button_UpdateCurrHImage.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_UpdateCurrHImage.Name = "button_UpdateCurrHImage";
            this.button_UpdateCurrHImage.Size = new System.Drawing.Size(269, 43);
            this.button_UpdateCurrHImage.TabIndex = 18;
            this.button_UpdateCurrHImage.Text = "Update Current Hour[Image]";
            this.button_UpdateCurrHImage.UseVisualStyleBackColor = true;
            this.button_UpdateCurrHImage.Click += new System.EventHandler(this.button_UpdateCurrHImage_Click);
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(107, 25);
            this.textBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 26);
            this.textBox1.TabIndex = 3;
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label17.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(212, 529);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(100, 21);
            this.label17.TabIndex = 2;
            this.label17.Text = "   -   MV";
            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.Location = new System.Drawing.Point(107, 59);
            this.textBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(100, 26);
            this.textBox2.TabIndex = 3;
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label16.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(212, 497);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(100, 21);
            this.label16.TabIndex = 2;
            this.label16.Text = "   -   MW";
            // 
            // textBox3
            // 
            this.textBox3.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox3.Location = new System.Drawing.Point(107, 92);
            this.textBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(100, 26);
            this.textBox3.TabIndex = 3;
            this.textBox3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label12.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(212, 399);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(100, 21);
            this.label12.TabIndex = 2;
            this.label12.Text = "   -   MV";
            // 
            // textBox4
            // 
            this.textBox4.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox4.Location = new System.Drawing.Point(107, 127);
            this.textBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(100, 26);
            this.textBox4.TabIndex = 3;
            this.textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label11.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(212, 368);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(100, 21);
            this.label11.TabIndex = 2;
            this.label11.Text = "   -   MW";
            // 
            // textBox5
            // 
            this.textBox5.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox5.Location = new System.Drawing.Point(107, 161);
            this.textBox5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(100, 26);
            this.textBox5.TabIndex = 3;
            this.textBox5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label8.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(212, 265);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 21);
            this.label8.TabIndex = 2;
            this.label8.Text = "   -   MV";
            // 
            // textBox6
            // 
            this.textBox6.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox6.Location = new System.Drawing.Point(107, 196);
            this.textBox6.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(100, 26);
            this.textBox6.TabIndex = 3;
            this.textBox6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label15.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(212, 465);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(90, 21);
            this.label15.TabIndex = 2;
            this.label15.Text = "   -   A";
            // 
            // textBox7
            // 
            this.textBox7.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox7.Location = new System.Drawing.Point(107, 229);
            this.textBox7.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox7.Name = "textBox7";
            this.textBox7.Size = new System.Drawing.Size(100, 26);
            this.textBox7.TabIndex = 3;
            this.textBox7.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label7.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(212, 231);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 21);
            this.label7.TabIndex = 2;
            this.label7.Text = "   -   MW";
            // 
            // textBox8
            // 
            this.textBox8.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox8.Location = new System.Drawing.Point(107, 263);
            this.textBox8.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox8.Name = "textBox8";
            this.textBox8.Size = new System.Drawing.Size(100, 26);
            this.textBox8.TabIndex = 3;
            this.textBox8.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label10.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(212, 335);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(90, 21);
            this.label10.TabIndex = 2;
            this.label10.Text = "   -   A";
            // 
            // textBox9
            // 
            this.textBox9.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox9.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox9.Location = new System.Drawing.Point(107, 297);
            this.textBox9.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox9.Name = "textBox9";
            this.textBox9.Size = new System.Drawing.Size(100, 26);
            this.textBox9.TabIndex = 3;
            this.textBox9.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label4.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(212, 129);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 21);
            this.label4.TabIndex = 2;
            this.label4.Text = "   -   MV";
            // 
            // textBox10
            // 
            this.textBox10.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox10.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox10.Location = new System.Drawing.Point(107, 331);
            this.textBox10.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox10.Name = "textBox10";
            this.textBox10.Size = new System.Drawing.Size(100, 26);
            this.textBox10.TabIndex = 3;
            this.textBox10.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label6.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(212, 198);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 21);
            this.label6.TabIndex = 2;
            this.label6.Text = "   -   A";
            // 
            // textBox11
            // 
            this.textBox11.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox11.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox11.Location = new System.Drawing.Point(107, 364);
            this.textBox11.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox11.Name = "textBox11";
            this.textBox11.Size = new System.Drawing.Size(100, 26);
            this.textBox11.TabIndex = 3;
            this.textBox11.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label3.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(212, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 21);
            this.label3.TabIndex = 2;
            this.label3.Text = "   -   MW";
            // 
            // textBox12
            // 
            this.textBox12.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox12.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox12.Location = new System.Drawing.Point(107, 399);
            this.textBox12.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox12.Name = "textBox12";
            this.textBox12.Size = new System.Drawing.Size(100, 26);
            this.textBox12.TabIndex = 3;
            this.textBox12.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label19.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.Location = new System.Drawing.Point(212, 770);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(90, 21);
            this.label19.TabIndex = 2;
            this.label19.Text = "F  Salam";
            // 
            // textBox13
            // 
            this.textBox13.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox13.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox13.Location = new System.Drawing.Point(107, 433);
            this.textBox13.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox13.Name = "textBox13";
            this.textBox13.Size = new System.Drawing.Size(100, 26);
            this.textBox13.TabIndex = 3;
            this.textBox13.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label18.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(212, 737);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(100, 21);
            this.label18.TabIndex = 2;
            this.label18.Text = "F  Summer";
            // 
            // textBox14
            // 
            this.textBox14.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox14.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox14.Location = new System.Drawing.Point(107, 468);
            this.textBox14.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox14.Name = "textBox14";
            this.textBox14.Size = new System.Drawing.Size(100, 26);
            this.textBox14.TabIndex = 3;
            this.textBox14.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label14.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(212, 705);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(100, 21);
            this.label14.TabIndex = 2;
            this.label14.Text = "F  Domeez";
            // 
            // textBox15
            // 
            this.textBox15.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox15.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox15.Location = new System.Drawing.Point(107, 501);
            this.textBox15.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox15.Name = "textBox15";
            this.textBox15.Size = new System.Drawing.Size(100, 26);
            this.textBox15.TabIndex = 3;
            this.textBox15.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label2.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(212, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 21);
            this.label2.TabIndex = 2;
            this.label2.Text = "   -   A";
            // 
            // textBox16
            // 
            this.textBox16.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox16.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox16.Location = new System.Drawing.Point(107, 535);
            this.textBox16.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox16.Name = "textBox16";
            this.textBox16.Size = new System.Drawing.Size(100, 26);
            this.textBox16.TabIndex = 3;
            this.textBox16.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label13.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(217, 567);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(120, 21);
            this.label13.TabIndex = 2;
            this.label13.Text = "Transform-3";
            // 
            // textBox21
            // 
            this.textBox21.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox21.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox21.Location = new System.Drawing.Point(107, 703);
            this.textBox21.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox21.Name = "textBox21";
            this.textBox21.Size = new System.Drawing.Size(100, 26);
            this.textBox21.TabIndex = 3;
            this.textBox21.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label9.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(212, 300);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(120, 21);
            this.label9.TabIndex = 2;
            this.label9.Text = "Transform-1";
            // 
            // textBox22
            // 
            this.textBox22.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox22.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox22.Location = new System.Drawing.Point(107, 736);
            this.textBox22.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox22.Name = "textBox22";
            this.textBox22.Size = new System.Drawing.Size(100, 26);
            this.textBox22.TabIndex = 3;
            this.textBox22.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label5.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(212, 164);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(110, 21);
            this.label5.TabIndex = 2;
            this.label5.Text = "Qayara  KV";
            // 
            // textBox23
            // 
            this.textBox23.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox23.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox23.Location = new System.Drawing.Point(107, 768);
            this.textBox23.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox23.Name = "textBox23";
            this.textBox23.Size = new System.Drawing.Size(100, 26);
            this.textBox23.TabIndex = 3;
            this.textBox23.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label1.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(212, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(110, 21);
            this.label1.TabIndex = 2;
            this.label1.Text = "Yaremja KV";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pictureBox1.Location = new System.Drawing.Point(7, 21);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(95, 811);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel1.Location = new System.Drawing.Point(7, 55);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(473, 1);
            this.panel1.TabIndex = 8;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel2.Location = new System.Drawing.Point(8, 90);
            this.panel2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(473, 1);
            this.panel2.TabIndex = 8;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel3.Location = new System.Drawing.Point(8, 127);
            this.panel3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(473, 1);
            this.panel3.TabIndex = 8;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel4.Location = new System.Drawing.Point(7, 159);
            this.panel4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(473, 1);
            this.panel4.TabIndex = 8;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel5.Location = new System.Drawing.Point(1, 193);
            this.panel5.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(473, 1);
            this.panel5.TabIndex = 8;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel6.Location = new System.Drawing.Point(1, 228);
            this.panel6.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(473, 1);
            this.panel6.TabIndex = 8;
            // 
            // panel7
            // 
            this.panel7.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel7.Location = new System.Drawing.Point(7, 260);
            this.panel7.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(473, 1);
            this.panel7.TabIndex = 8;
            // 
            // panel8
            // 
            this.panel8.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel8.Location = new System.Drawing.Point(1, 294);
            this.panel8.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel8.Name = "panel8";
            this.panel8.Size = new System.Drawing.Size(473, 1);
            this.panel8.TabIndex = 8;
            // 
            // panel9
            // 
            this.panel9.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel9.Location = new System.Drawing.Point(0, 329);
            this.panel9.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel9.Name = "panel9";
            this.panel9.Size = new System.Drawing.Size(473, 1);
            this.panel9.TabIndex = 8;
            // 
            // panel10
            // 
            this.panel10.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel10.Location = new System.Drawing.Point(7, 364);
            this.panel10.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(473, 1);
            this.panel10.TabIndex = 8;
            // 
            // panel11
            // 
            this.panel11.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel11.Location = new System.Drawing.Point(7, 396);
            this.panel11.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(473, 1);
            this.panel11.TabIndex = 8;
            // 
            // panel12
            // 
            this.panel12.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel12.Location = new System.Drawing.Point(7, 431);
            this.panel12.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel12.Name = "panel12";
            this.panel12.Size = new System.Drawing.Size(473, 1);
            this.panel12.TabIndex = 8;
            // 
            // panel13
            // 
            this.panel13.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel13.Location = new System.Drawing.Point(1, 465);
            this.panel13.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel13.Name = "panel13";
            this.panel13.Size = new System.Drawing.Size(473, 1);
            this.panel13.TabIndex = 8;
            // 
            // panel14
            // 
            this.panel14.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel14.Location = new System.Drawing.Point(7, 497);
            this.panel14.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel14.Name = "panel14";
            this.panel14.Size = new System.Drawing.Size(473, 1);
            this.panel14.TabIndex = 8;
            // 
            // panel15
            // 
            this.panel15.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel15.Location = new System.Drawing.Point(7, 535);
            this.panel15.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel15.Name = "panel15";
            this.panel15.Size = new System.Drawing.Size(473, 1);
            this.panel15.TabIndex = 8;
            // 
            // panel16
            // 
            this.panel16.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel16.Location = new System.Drawing.Point(8, 567);
            this.panel16.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel16.Name = "panel16";
            this.panel16.Size = new System.Drawing.Size(473, 1);
            this.panel16.TabIndex = 8;
            // 
            // panel17
            // 
            this.panel17.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel17.Location = new System.Drawing.Point(8, 602);
            this.panel17.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel17.Name = "panel17";
            this.panel17.Size = new System.Drawing.Size(473, 1);
            this.panel17.TabIndex = 8;
            // 
            // panel18
            // 
            this.panel18.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel18.Location = new System.Drawing.Point(8, 634);
            this.panel18.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel18.Name = "panel18";
            this.panel18.Size = new System.Drawing.Size(473, 1);
            this.panel18.TabIndex = 8;
            // 
            // panel19
            // 
            this.panel19.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel19.Controls.Add(this.panel22);
            this.panel19.Controls.Add(this.panel21);
            this.panel19.Location = new System.Drawing.Point(8, 668);
            this.panel19.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel19.Name = "panel19";
            this.panel19.Size = new System.Drawing.Size(473, 1);
            this.panel19.TabIndex = 8;
            // 
            // panel22
            // 
            this.panel22.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel22.Controls.Add(this.panel24);
            this.panel22.Controls.Add(this.panel23);
            this.panel22.Location = new System.Drawing.Point(0, 33);
            this.panel22.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel22.Name = "panel22";
            this.panel22.Size = new System.Drawing.Size(473, 1);
            this.panel22.TabIndex = 10;
            // 
            // panel24
            // 
            this.panel24.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel24.Controls.Add(this.panel25);
            this.panel24.Location = new System.Drawing.Point(0, -1);
            this.panel24.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel24.Name = "panel24";
            this.panel24.Size = new System.Drawing.Size(473, 1);
            this.panel24.TabIndex = 11;
            // 
            // panel25
            // 
            this.panel25.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel25.Location = new System.Drawing.Point(-1, 57);
            this.panel25.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel25.Name = "panel25";
            this.panel25.Size = new System.Drawing.Size(473, 1);
            this.panel25.TabIndex = 9;
            // 
            // panel23
            // 
            this.panel23.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel23.Location = new System.Drawing.Point(-1, 57);
            this.panel23.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel23.Name = "panel23";
            this.panel23.Size = new System.Drawing.Size(473, 1);
            this.panel23.TabIndex = 9;
            // 
            // panel21
            // 
            this.panel21.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel21.Location = new System.Drawing.Point(-1, 57);
            this.panel21.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel21.Name = "panel21";
            this.panel21.Size = new System.Drawing.Size(473, 1);
            this.panel21.TabIndex = 9;
            // 
            // panel26
            // 
            this.panel26.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel26.Controls.Add(this.panel38);
            this.panel26.Controls.Add(this.panel32);
            this.panel26.Controls.Add(this.panel27);
            this.panel26.Controls.Add(this.panel31);
            this.panel26.Location = new System.Drawing.Point(8, 668);
            this.panel26.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel26.Name = "panel26";
            this.panel26.Size = new System.Drawing.Size(473, 1);
            this.panel26.TabIndex = 8;
            // 
            // panel38
            // 
            this.panel38.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel38.Controls.Add(this.panel39);
            this.panel38.Controls.Add(this.panel45);
            this.panel38.Controls.Add(this.panel49);
            this.panel38.Location = new System.Drawing.Point(0, 59);
            this.panel38.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel38.Name = "panel38";
            this.panel38.Size = new System.Drawing.Size(473, 1);
            this.panel38.TabIndex = 12;
            // 
            // panel39
            // 
            this.panel39.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel39.Controls.Add(this.panel40);
            this.panel39.Controls.Add(this.panel44);
            this.panel39.ForeColor = System.Drawing.Color.Black;
            this.panel39.Location = new System.Drawing.Point(0, 60);
            this.panel39.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel39.Name = "panel39";
            this.panel39.Size = new System.Drawing.Size(473, 1);
            this.panel39.TabIndex = 11;
            // 
            // panel40
            // 
            this.panel40.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel40.Controls.Add(this.panel41);
            this.panel40.Controls.Add(this.panel43);
            this.panel40.Location = new System.Drawing.Point(0, 33);
            this.panel40.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel40.Name = "panel40";
            this.panel40.Size = new System.Drawing.Size(473, 1);
            this.panel40.TabIndex = 10;
            // 
            // panel41
            // 
            this.panel41.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel41.Controls.Add(this.panel42);
            this.panel41.Location = new System.Drawing.Point(0, -1);
            this.panel41.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel41.Name = "panel41";
            this.panel41.Size = new System.Drawing.Size(473, 1);
            this.panel41.TabIndex = 11;
            // 
            // panel42
            // 
            this.panel42.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel42.Location = new System.Drawing.Point(-1, 57);
            this.panel42.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel42.Name = "panel42";
            this.panel42.Size = new System.Drawing.Size(473, 1);
            this.panel42.TabIndex = 9;
            // 
            // panel43
            // 
            this.panel43.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel43.Location = new System.Drawing.Point(-1, 57);
            this.panel43.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel43.Name = "panel43";
            this.panel43.Size = new System.Drawing.Size(473, 1);
            this.panel43.TabIndex = 9;
            // 
            // panel44
            // 
            this.panel44.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel44.Location = new System.Drawing.Point(-1, 57);
            this.panel44.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel44.Name = "panel44";
            this.panel44.Size = new System.Drawing.Size(473, 1);
            this.panel44.TabIndex = 9;
            // 
            // panel45
            // 
            this.panel45.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel45.Controls.Add(this.panel46);
            this.panel45.Controls.Add(this.panel48);
            this.panel45.Location = new System.Drawing.Point(0, 33);
            this.panel45.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel45.Name = "panel45";
            this.panel45.Size = new System.Drawing.Size(473, 1);
            this.panel45.TabIndex = 10;
            // 
            // panel46
            // 
            this.panel46.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel46.Controls.Add(this.panel47);
            this.panel46.Location = new System.Drawing.Point(0, -1);
            this.panel46.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel46.Name = "panel46";
            this.panel46.Size = new System.Drawing.Size(473, 1);
            this.panel46.TabIndex = 11;
            // 
            // panel47
            // 
            this.panel47.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel47.Location = new System.Drawing.Point(-1, 57);
            this.panel47.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel47.Name = "panel47";
            this.panel47.Size = new System.Drawing.Size(473, 1);
            this.panel47.TabIndex = 9;
            // 
            // panel48
            // 
            this.panel48.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel48.Location = new System.Drawing.Point(-1, 57);
            this.panel48.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel48.Name = "panel48";
            this.panel48.Size = new System.Drawing.Size(473, 1);
            this.panel48.TabIndex = 9;
            // 
            // panel49
            // 
            this.panel49.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel49.Location = new System.Drawing.Point(-1, 57);
            this.panel49.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel49.Name = "panel49";
            this.panel49.Size = new System.Drawing.Size(473, 1);
            this.panel49.TabIndex = 9;
            // 
            // panel32
            // 
            this.panel32.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel32.Controls.Add(this.panel33);
            this.panel32.Controls.Add(this.panel37);
            this.panel32.ForeColor = System.Drawing.Color.Black;
            this.panel32.Location = new System.Drawing.Point(0, 60);
            this.panel32.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel32.Name = "panel32";
            this.panel32.Size = new System.Drawing.Size(473, 1);
            this.panel32.TabIndex = 11;
            // 
            // panel33
            // 
            this.panel33.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel33.Controls.Add(this.panel34);
            this.panel33.Controls.Add(this.panel36);
            this.panel33.Location = new System.Drawing.Point(0, 33);
            this.panel33.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel33.Name = "panel33";
            this.panel33.Size = new System.Drawing.Size(473, 1);
            this.panel33.TabIndex = 10;
            // 
            // panel34
            // 
            this.panel34.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel34.Controls.Add(this.panel35);
            this.panel34.Location = new System.Drawing.Point(0, -1);
            this.panel34.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel34.Name = "panel34";
            this.panel34.Size = new System.Drawing.Size(473, 1);
            this.panel34.TabIndex = 11;
            // 
            // panel35
            // 
            this.panel35.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel35.Location = new System.Drawing.Point(-1, 57);
            this.panel35.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel35.Name = "panel35";
            this.panel35.Size = new System.Drawing.Size(473, 1);
            this.panel35.TabIndex = 9;
            // 
            // panel36
            // 
            this.panel36.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel36.Location = new System.Drawing.Point(-1, 57);
            this.panel36.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel36.Name = "panel36";
            this.panel36.Size = new System.Drawing.Size(473, 1);
            this.panel36.TabIndex = 9;
            // 
            // panel37
            // 
            this.panel37.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel37.Location = new System.Drawing.Point(-1, 57);
            this.panel37.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel37.Name = "panel37";
            this.panel37.Size = new System.Drawing.Size(473, 1);
            this.panel37.TabIndex = 9;
            // 
            // panel27
            // 
            this.panel27.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel27.Controls.Add(this.panel28);
            this.panel27.Controls.Add(this.panel30);
            this.panel27.Location = new System.Drawing.Point(0, 33);
            this.panel27.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel27.Name = "panel27";
            this.panel27.Size = new System.Drawing.Size(473, 1);
            this.panel27.TabIndex = 10;
            // 
            // panel28
            // 
            this.panel28.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel28.Controls.Add(this.panel29);
            this.panel28.Location = new System.Drawing.Point(0, -1);
            this.panel28.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel28.Name = "panel28";
            this.panel28.Size = new System.Drawing.Size(473, 1);
            this.panel28.TabIndex = 11;
            // 
            // panel29
            // 
            this.panel29.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel29.Location = new System.Drawing.Point(-1, 57);
            this.panel29.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel29.Name = "panel29";
            this.panel29.Size = new System.Drawing.Size(473, 1);
            this.panel29.TabIndex = 9;
            // 
            // panel30
            // 
            this.panel30.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel30.Location = new System.Drawing.Point(-1, 57);
            this.panel30.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel30.Name = "panel30";
            this.panel30.Size = new System.Drawing.Size(473, 1);
            this.panel30.TabIndex = 9;
            // 
            // panel31
            // 
            this.panel31.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel31.Location = new System.Drawing.Point(-1, 57);
            this.panel31.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel31.Name = "panel31";
            this.panel31.Size = new System.Drawing.Size(473, 1);
            this.panel31.TabIndex = 9;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label23.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label23.Location = new System.Drawing.Point(212, 804);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(110, 21);
            this.label23.TabIndex = 10;
            this.label23.Text = "F  Salam 2";
            // 
            // textBox24
            // 
            this.textBox24.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox24.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox24.Location = new System.Drawing.Point(107, 800);
            this.textBox24.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox24.Name = "textBox24";
            this.textBox24.Size = new System.Drawing.Size(100, 26);
            this.textBox24.TabIndex = 11;
            this.textBox24.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox17
            // 
            this.textBox17.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox17.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox17.Location = new System.Drawing.Point(107, 571);
            this.textBox17.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox17.Name = "textBox17";
            this.textBox17.Size = new System.Drawing.Size(100, 26);
            this.textBox17.TabIndex = 12;
            this.textBox17.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label26.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label26.Location = new System.Drawing.Point(212, 671);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(100, 21);
            this.label26.TabIndex = 15;
            this.label26.Text = "   -   MV";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label25.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label25.Location = new System.Drawing.Point(212, 639);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(100, 21);
            this.label25.TabIndex = 14;
            this.label25.Text = "   -   MW";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label24.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label24.Location = new System.Drawing.Point(212, 607);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(90, 21);
            this.label24.TabIndex = 13;
            this.label24.Text = "   -   A";
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.groupBox2.Controls.Add(this.button_DeleteCurrentImage);
            this.groupBox2.Controls.Add(this.panel146);
            this.groupBox2.Controls.Add(this.panel74);
            this.groupBox2.Controls.Add(this.panel50);
            this.groupBox2.Controls.Add(this.textBox20);
            this.groupBox2.Controls.Add(this.panel_fixedError);
            this.groupBox2.Controls.Add(this.textBox19);
            this.groupBox2.Controls.Add(this.textBox18);
            this.groupBox2.Controls.Add(this.label24);
            this.groupBox2.Controls.Add(this.label25);
            this.groupBox2.Controls.Add(this.button_UpdateCurrHImage);
            this.groupBox2.Controls.Add(this.label26);
            this.groupBox2.Controls.Add(this.textBox17);
            this.groupBox2.Controls.Add(this.textBox24);
            this.groupBox2.Controls.Add(this.label23);
            this.groupBox2.Controls.Add(this.panel26);
            this.groupBox2.Controls.Add(this.panel19);
            this.groupBox2.Controls.Add(this.panel18);
            this.groupBox2.Controls.Add(this.panel17);
            this.groupBox2.Controls.Add(this.panel16);
            this.groupBox2.Controls.Add(this.panel15);
            this.groupBox2.Controls.Add(this.panel14);
            this.groupBox2.Controls.Add(this.panel13);
            this.groupBox2.Controls.Add(this.panel12);
            this.groupBox2.Controls.Add(this.panel11);
            this.groupBox2.Controls.Add(this.panel10);
            this.groupBox2.Controls.Add(this.panel9);
            this.groupBox2.Controls.Add(this.panel8);
            this.groupBox2.Controls.Add(this.panel7);
            this.groupBox2.Controls.Add(this.panel6);
            this.groupBox2.Controls.Add(this.panel5);
            this.groupBox2.Controls.Add(this.panel4);
            this.groupBox2.Controls.Add(this.panel3);
            this.groupBox2.Controls.Add(this.panel2);
            this.groupBox2.Controls.Add(this.panel1);
            this.groupBox2.Controls.Add(this.pictureBox1);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.textBox23);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.textBox22);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.textBox21);
            this.groupBox2.Controls.Add(this.label13);
            this.groupBox2.Controls.Add(this.textBox16);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.textBox15);
            this.groupBox2.Controls.Add(this.label14);
            this.groupBox2.Controls.Add(this.textBox14);
            this.groupBox2.Controls.Add(this.label18);
            this.groupBox2.Controls.Add(this.textBox13);
            this.groupBox2.Controls.Add(this.label19);
            this.groupBox2.Controls.Add(this.textBox12);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.textBox11);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.textBox10);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.textBox9);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.textBox8);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.textBox7);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Controls.Add(this.textBox6);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.textBox5);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.textBox4);
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.textBox3);
            this.groupBox2.Controls.Add(this.label16);
            this.groupBox2.Controls.Add(this.textBox2);
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.textBox1);
            this.groupBox2.Controls.Add(this.label27);
            this.groupBox2.Location = new System.Drawing.Point(12, 60);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Size = new System.Drawing.Size(377, 882);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            // 
            // button_DeleteCurrentImage
            // 
            this.button_DeleteCurrentImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_DeleteCurrentImage.Location = new System.Drawing.Point(4, 834);
            this.button_DeleteCurrentImage.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button_DeleteCurrentImage.Name = "button_DeleteCurrentImage";
            this.button_DeleteCurrentImage.Size = new System.Drawing.Size(100, 41);
            this.button_DeleteCurrentImage.TabIndex = 26;
            this.button_DeleteCurrentImage.Text = "Delete";
            this.button_DeleteCurrentImage.UseVisualStyleBackColor = true;
            this.button_DeleteCurrentImage.Click += new System.EventHandler(this.button_DeleteCurrentImage_Click);
            // 
            // panel146
            // 
            this.panel146.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel146.Controls.Add(this.panel147);
            this.panel146.Controls.Add(this.panel171);
            this.panel146.Controls.Add(this.panel183);
            this.panel146.Controls.Add(this.panel189);
            this.panel146.Controls.Add(this.panel193);
            this.panel146.Location = new System.Drawing.Point(-4, 799);
            this.panel146.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel146.Name = "panel146";
            this.panel146.Size = new System.Drawing.Size(384, 1);
            this.panel146.TabIndex = 24;
            // 
            // panel147
            // 
            this.panel147.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel147.Controls.Add(this.panel148);
            this.panel147.Controls.Add(this.panel160);
            this.panel147.Controls.Add(this.panel166);
            this.panel147.Controls.Add(this.panel170);
            this.panel147.Location = new System.Drawing.Point(9, 0);
            this.panel147.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel147.Name = "panel147";
            this.panel147.Size = new System.Drawing.Size(379, 0);
            this.panel147.TabIndex = 23;
            // 
            // panel148
            // 
            this.panel148.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel148.Controls.Add(this.panel149);
            this.panel148.Controls.Add(this.panel155);
            this.panel148.Controls.Add(this.panel159);
            this.panel148.Location = new System.Drawing.Point(0, 59);
            this.panel148.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel148.Name = "panel148";
            this.panel148.Size = new System.Drawing.Size(473, 1);
            this.panel148.TabIndex = 12;
            // 
            // panel149
            // 
            this.panel149.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel149.Controls.Add(this.panel150);
            this.panel149.Controls.Add(this.panel154);
            this.panel149.ForeColor = System.Drawing.Color.Black;
            this.panel149.Location = new System.Drawing.Point(0, 60);
            this.panel149.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel149.Name = "panel149";
            this.panel149.Size = new System.Drawing.Size(473, 1);
            this.panel149.TabIndex = 11;
            // 
            // panel150
            // 
            this.panel150.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel150.Controls.Add(this.panel151);
            this.panel150.Controls.Add(this.panel153);
            this.panel150.Location = new System.Drawing.Point(0, 33);
            this.panel150.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel150.Name = "panel150";
            this.panel150.Size = new System.Drawing.Size(473, 1);
            this.panel150.TabIndex = 10;
            // 
            // panel151
            // 
            this.panel151.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel151.Controls.Add(this.panel152);
            this.panel151.Location = new System.Drawing.Point(0, -1);
            this.panel151.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel151.Name = "panel151";
            this.panel151.Size = new System.Drawing.Size(473, 1);
            this.panel151.TabIndex = 11;
            // 
            // panel152
            // 
            this.panel152.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel152.Location = new System.Drawing.Point(-1, 57);
            this.panel152.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel152.Name = "panel152";
            this.panel152.Size = new System.Drawing.Size(473, 1);
            this.panel152.TabIndex = 9;
            // 
            // panel153
            // 
            this.panel153.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel153.Location = new System.Drawing.Point(-1, 57);
            this.panel153.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel153.Name = "panel153";
            this.panel153.Size = new System.Drawing.Size(473, 1);
            this.panel153.TabIndex = 9;
            // 
            // panel154
            // 
            this.panel154.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel154.Location = new System.Drawing.Point(-1, 57);
            this.panel154.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel154.Name = "panel154";
            this.panel154.Size = new System.Drawing.Size(473, 1);
            this.panel154.TabIndex = 9;
            // 
            // panel155
            // 
            this.panel155.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel155.Controls.Add(this.panel156);
            this.panel155.Controls.Add(this.panel158);
            this.panel155.Location = new System.Drawing.Point(0, 33);
            this.panel155.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel155.Name = "panel155";
            this.panel155.Size = new System.Drawing.Size(473, 1);
            this.panel155.TabIndex = 10;
            // 
            // panel156
            // 
            this.panel156.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel156.Controls.Add(this.panel157);
            this.panel156.Location = new System.Drawing.Point(0, -1);
            this.panel156.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel156.Name = "panel156";
            this.panel156.Size = new System.Drawing.Size(473, 1);
            this.panel156.TabIndex = 11;
            // 
            // panel157
            // 
            this.panel157.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel157.Location = new System.Drawing.Point(-1, 57);
            this.panel157.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel157.Name = "panel157";
            this.panel157.Size = new System.Drawing.Size(473, 1);
            this.panel157.TabIndex = 9;
            // 
            // panel158
            // 
            this.panel158.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel158.Location = new System.Drawing.Point(-1, 57);
            this.panel158.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel158.Name = "panel158";
            this.panel158.Size = new System.Drawing.Size(473, 1);
            this.panel158.TabIndex = 9;
            // 
            // panel159
            // 
            this.panel159.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel159.Location = new System.Drawing.Point(-1, 57);
            this.panel159.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel159.Name = "panel159";
            this.panel159.Size = new System.Drawing.Size(473, 1);
            this.panel159.TabIndex = 9;
            // 
            // panel160
            // 
            this.panel160.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel160.Controls.Add(this.panel161);
            this.panel160.Controls.Add(this.panel165);
            this.panel160.ForeColor = System.Drawing.Color.Black;
            this.panel160.Location = new System.Drawing.Point(0, 60);
            this.panel160.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel160.Name = "panel160";
            this.panel160.Size = new System.Drawing.Size(473, 1);
            this.panel160.TabIndex = 11;
            // 
            // panel161
            // 
            this.panel161.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel161.Controls.Add(this.panel162);
            this.panel161.Controls.Add(this.panel164);
            this.panel161.Location = new System.Drawing.Point(0, 33);
            this.panel161.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel161.Name = "panel161";
            this.panel161.Size = new System.Drawing.Size(473, 1);
            this.panel161.TabIndex = 10;
            // 
            // panel162
            // 
            this.panel162.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel162.Controls.Add(this.panel163);
            this.panel162.Location = new System.Drawing.Point(0, -1);
            this.panel162.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel162.Name = "panel162";
            this.panel162.Size = new System.Drawing.Size(473, 1);
            this.panel162.TabIndex = 11;
            // 
            // panel163
            // 
            this.panel163.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel163.Location = new System.Drawing.Point(-1, 57);
            this.panel163.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel163.Name = "panel163";
            this.panel163.Size = new System.Drawing.Size(473, 1);
            this.panel163.TabIndex = 9;
            // 
            // panel164
            // 
            this.panel164.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel164.Location = new System.Drawing.Point(-1, 57);
            this.panel164.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel164.Name = "panel164";
            this.panel164.Size = new System.Drawing.Size(473, 1);
            this.panel164.TabIndex = 9;
            // 
            // panel165
            // 
            this.panel165.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel165.Location = new System.Drawing.Point(-1, 57);
            this.panel165.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel165.Name = "panel165";
            this.panel165.Size = new System.Drawing.Size(473, 1);
            this.panel165.TabIndex = 9;
            // 
            // panel166
            // 
            this.panel166.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel166.Controls.Add(this.panel167);
            this.panel166.Controls.Add(this.panel169);
            this.panel166.Location = new System.Drawing.Point(0, 33);
            this.panel166.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel166.Name = "panel166";
            this.panel166.Size = new System.Drawing.Size(473, 1);
            this.panel166.TabIndex = 10;
            // 
            // panel167
            // 
            this.panel167.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel167.Controls.Add(this.panel168);
            this.panel167.Location = new System.Drawing.Point(0, -1);
            this.panel167.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel167.Name = "panel167";
            this.panel167.Size = new System.Drawing.Size(473, 1);
            this.panel167.TabIndex = 11;
            // 
            // panel168
            // 
            this.panel168.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel168.Location = new System.Drawing.Point(-1, 57);
            this.panel168.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel168.Name = "panel168";
            this.panel168.Size = new System.Drawing.Size(473, 1);
            this.panel168.TabIndex = 9;
            // 
            // panel169
            // 
            this.panel169.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel169.Location = new System.Drawing.Point(-1, 57);
            this.panel169.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel169.Name = "panel169";
            this.panel169.Size = new System.Drawing.Size(473, 1);
            this.panel169.TabIndex = 9;
            // 
            // panel170
            // 
            this.panel170.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel170.Location = new System.Drawing.Point(-1, 57);
            this.panel170.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel170.Name = "panel170";
            this.panel170.Size = new System.Drawing.Size(473, 1);
            this.panel170.TabIndex = 9;
            // 
            // panel171
            // 
            this.panel171.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel171.Controls.Add(this.panel172);
            this.panel171.Controls.Add(this.panel178);
            this.panel171.Controls.Add(this.panel182);
            this.panel171.Location = new System.Drawing.Point(0, 59);
            this.panel171.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel171.Name = "panel171";
            this.panel171.Size = new System.Drawing.Size(473, 1);
            this.panel171.TabIndex = 12;
            // 
            // panel172
            // 
            this.panel172.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel172.Controls.Add(this.panel173);
            this.panel172.Controls.Add(this.panel177);
            this.panel172.ForeColor = System.Drawing.Color.Black;
            this.panel172.Location = new System.Drawing.Point(0, 60);
            this.panel172.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel172.Name = "panel172";
            this.panel172.Size = new System.Drawing.Size(473, 1);
            this.panel172.TabIndex = 11;
            // 
            // panel173
            // 
            this.panel173.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel173.Controls.Add(this.panel174);
            this.panel173.Controls.Add(this.panel176);
            this.panel173.Location = new System.Drawing.Point(0, 33);
            this.panel173.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel173.Name = "panel173";
            this.panel173.Size = new System.Drawing.Size(473, 1);
            this.panel173.TabIndex = 10;
            // 
            // panel174
            // 
            this.panel174.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel174.Controls.Add(this.panel175);
            this.panel174.Location = new System.Drawing.Point(0, -1);
            this.panel174.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel174.Name = "panel174";
            this.panel174.Size = new System.Drawing.Size(473, 1);
            this.panel174.TabIndex = 11;
            // 
            // panel175
            // 
            this.panel175.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel175.Location = new System.Drawing.Point(-1, 57);
            this.panel175.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel175.Name = "panel175";
            this.panel175.Size = new System.Drawing.Size(473, 1);
            this.panel175.TabIndex = 9;
            // 
            // panel176
            // 
            this.panel176.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel176.Location = new System.Drawing.Point(-1, 57);
            this.panel176.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel176.Name = "panel176";
            this.panel176.Size = new System.Drawing.Size(473, 1);
            this.panel176.TabIndex = 9;
            // 
            // panel177
            // 
            this.panel177.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel177.Location = new System.Drawing.Point(-1, 57);
            this.panel177.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel177.Name = "panel177";
            this.panel177.Size = new System.Drawing.Size(473, 1);
            this.panel177.TabIndex = 9;
            // 
            // panel178
            // 
            this.panel178.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel178.Controls.Add(this.panel179);
            this.panel178.Controls.Add(this.panel181);
            this.panel178.Location = new System.Drawing.Point(0, 33);
            this.panel178.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel178.Name = "panel178";
            this.panel178.Size = new System.Drawing.Size(473, 1);
            this.panel178.TabIndex = 10;
            // 
            // panel179
            // 
            this.panel179.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel179.Controls.Add(this.panel180);
            this.panel179.Location = new System.Drawing.Point(0, -1);
            this.panel179.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel179.Name = "panel179";
            this.panel179.Size = new System.Drawing.Size(473, 1);
            this.panel179.TabIndex = 11;
            // 
            // panel180
            // 
            this.panel180.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel180.Location = new System.Drawing.Point(-1, 57);
            this.panel180.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel180.Name = "panel180";
            this.panel180.Size = new System.Drawing.Size(473, 1);
            this.panel180.TabIndex = 9;
            // 
            // panel181
            // 
            this.panel181.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel181.Location = new System.Drawing.Point(-1, 57);
            this.panel181.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel181.Name = "panel181";
            this.panel181.Size = new System.Drawing.Size(473, 1);
            this.panel181.TabIndex = 9;
            // 
            // panel182
            // 
            this.panel182.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel182.Location = new System.Drawing.Point(-1, 57);
            this.panel182.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel182.Name = "panel182";
            this.panel182.Size = new System.Drawing.Size(473, 1);
            this.panel182.TabIndex = 9;
            // 
            // panel183
            // 
            this.panel183.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel183.Controls.Add(this.panel184);
            this.panel183.Controls.Add(this.panel188);
            this.panel183.ForeColor = System.Drawing.Color.Black;
            this.panel183.Location = new System.Drawing.Point(0, 60);
            this.panel183.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel183.Name = "panel183";
            this.panel183.Size = new System.Drawing.Size(473, 1);
            this.panel183.TabIndex = 11;
            // 
            // panel184
            // 
            this.panel184.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel184.Controls.Add(this.panel185);
            this.panel184.Controls.Add(this.panel187);
            this.panel184.Location = new System.Drawing.Point(0, 33);
            this.panel184.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel184.Name = "panel184";
            this.panel184.Size = new System.Drawing.Size(473, 1);
            this.panel184.TabIndex = 10;
            // 
            // panel185
            // 
            this.panel185.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel185.Controls.Add(this.panel186);
            this.panel185.Location = new System.Drawing.Point(0, -1);
            this.panel185.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel185.Name = "panel185";
            this.panel185.Size = new System.Drawing.Size(473, 1);
            this.panel185.TabIndex = 11;
            // 
            // panel186
            // 
            this.panel186.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel186.Location = new System.Drawing.Point(-1, 57);
            this.panel186.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel186.Name = "panel186";
            this.panel186.Size = new System.Drawing.Size(473, 1);
            this.panel186.TabIndex = 9;
            // 
            // panel187
            // 
            this.panel187.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel187.Location = new System.Drawing.Point(-1, 57);
            this.panel187.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel187.Name = "panel187";
            this.panel187.Size = new System.Drawing.Size(473, 1);
            this.panel187.TabIndex = 9;
            // 
            // panel188
            // 
            this.panel188.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel188.Location = new System.Drawing.Point(-1, 57);
            this.panel188.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel188.Name = "panel188";
            this.panel188.Size = new System.Drawing.Size(473, 1);
            this.panel188.TabIndex = 9;
            // 
            // panel189
            // 
            this.panel189.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel189.Controls.Add(this.panel190);
            this.panel189.Controls.Add(this.panel192);
            this.panel189.Location = new System.Drawing.Point(0, 33);
            this.panel189.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel189.Name = "panel189";
            this.panel189.Size = new System.Drawing.Size(473, 1);
            this.panel189.TabIndex = 10;
            // 
            // panel190
            // 
            this.panel190.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel190.Controls.Add(this.panel191);
            this.panel190.Location = new System.Drawing.Point(0, -1);
            this.panel190.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel190.Name = "panel190";
            this.panel190.Size = new System.Drawing.Size(473, 1);
            this.panel190.TabIndex = 11;
            // 
            // panel191
            // 
            this.panel191.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel191.Location = new System.Drawing.Point(-1, 57);
            this.panel191.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel191.Name = "panel191";
            this.panel191.Size = new System.Drawing.Size(473, 1);
            this.panel191.TabIndex = 9;
            // 
            // panel192
            // 
            this.panel192.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel192.Location = new System.Drawing.Point(-1, 57);
            this.panel192.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel192.Name = "panel192";
            this.panel192.Size = new System.Drawing.Size(473, 1);
            this.panel192.TabIndex = 9;
            // 
            // panel193
            // 
            this.panel193.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel193.Location = new System.Drawing.Point(-1, 57);
            this.panel193.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel193.Name = "panel193";
            this.panel193.Size = new System.Drawing.Size(473, 1);
            this.panel193.TabIndex = 9;
            // 
            // panel74
            // 
            this.panel74.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel74.Controls.Add(this.panel75);
            this.panel74.Controls.Add(this.panel87);
            this.panel74.Controls.Add(this.panel93);
            this.panel74.Controls.Add(this.panel97);
            this.panel74.Location = new System.Drawing.Point(7, 735);
            this.panel74.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel74.Name = "panel74";
            this.panel74.Size = new System.Drawing.Size(473, 1);
            this.panel74.TabIndex = 22;
            // 
            // panel75
            // 
            this.panel75.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel75.Controls.Add(this.panel76);
            this.panel75.Controls.Add(this.panel82);
            this.panel75.Controls.Add(this.panel86);
            this.panel75.Location = new System.Drawing.Point(0, 59);
            this.panel75.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel75.Name = "panel75";
            this.panel75.Size = new System.Drawing.Size(473, 1);
            this.panel75.TabIndex = 12;
            // 
            // panel76
            // 
            this.panel76.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel76.Controls.Add(this.panel77);
            this.panel76.Controls.Add(this.panel81);
            this.panel76.ForeColor = System.Drawing.Color.Black;
            this.panel76.Location = new System.Drawing.Point(0, 60);
            this.panel76.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel76.Name = "panel76";
            this.panel76.Size = new System.Drawing.Size(473, 1);
            this.panel76.TabIndex = 11;
            // 
            // panel77
            // 
            this.panel77.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel77.Controls.Add(this.panel78);
            this.panel77.Controls.Add(this.panel80);
            this.panel77.Location = new System.Drawing.Point(0, 33);
            this.panel77.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel77.Name = "panel77";
            this.panel77.Size = new System.Drawing.Size(473, 1);
            this.panel77.TabIndex = 10;
            // 
            // panel78
            // 
            this.panel78.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel78.Controls.Add(this.panel79);
            this.panel78.Location = new System.Drawing.Point(0, -1);
            this.panel78.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel78.Name = "panel78";
            this.panel78.Size = new System.Drawing.Size(473, 1);
            this.panel78.TabIndex = 11;
            // 
            // panel79
            // 
            this.panel79.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel79.Location = new System.Drawing.Point(-1, 57);
            this.panel79.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel79.Name = "panel79";
            this.panel79.Size = new System.Drawing.Size(473, 1);
            this.panel79.TabIndex = 9;
            // 
            // panel80
            // 
            this.panel80.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel80.Location = new System.Drawing.Point(-1, 57);
            this.panel80.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel80.Name = "panel80";
            this.panel80.Size = new System.Drawing.Size(473, 1);
            this.panel80.TabIndex = 9;
            // 
            // panel81
            // 
            this.panel81.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel81.Location = new System.Drawing.Point(-1, 57);
            this.panel81.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel81.Name = "panel81";
            this.panel81.Size = new System.Drawing.Size(473, 1);
            this.panel81.TabIndex = 9;
            // 
            // panel82
            // 
            this.panel82.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel82.Controls.Add(this.panel83);
            this.panel82.Controls.Add(this.panel85);
            this.panel82.Location = new System.Drawing.Point(0, 33);
            this.panel82.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel82.Name = "panel82";
            this.panel82.Size = new System.Drawing.Size(473, 1);
            this.panel82.TabIndex = 10;
            // 
            // panel83
            // 
            this.panel83.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel83.Controls.Add(this.panel84);
            this.panel83.Location = new System.Drawing.Point(0, -1);
            this.panel83.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel83.Name = "panel83";
            this.panel83.Size = new System.Drawing.Size(473, 1);
            this.panel83.TabIndex = 11;
            // 
            // panel84
            // 
            this.panel84.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel84.Location = new System.Drawing.Point(-1, 57);
            this.panel84.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel84.Name = "panel84";
            this.panel84.Size = new System.Drawing.Size(473, 1);
            this.panel84.TabIndex = 9;
            // 
            // panel85
            // 
            this.panel85.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel85.Location = new System.Drawing.Point(-1, 57);
            this.panel85.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel85.Name = "panel85";
            this.panel85.Size = new System.Drawing.Size(473, 1);
            this.panel85.TabIndex = 9;
            // 
            // panel86
            // 
            this.panel86.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel86.Location = new System.Drawing.Point(-1, 57);
            this.panel86.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel86.Name = "panel86";
            this.panel86.Size = new System.Drawing.Size(473, 1);
            this.panel86.TabIndex = 9;
            // 
            // panel87
            // 
            this.panel87.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel87.Controls.Add(this.panel88);
            this.panel87.Controls.Add(this.panel92);
            this.panel87.ForeColor = System.Drawing.Color.Black;
            this.panel87.Location = new System.Drawing.Point(0, 60);
            this.panel87.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel87.Name = "panel87";
            this.panel87.Size = new System.Drawing.Size(473, 1);
            this.panel87.TabIndex = 11;
            // 
            // panel88
            // 
            this.panel88.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel88.Controls.Add(this.panel89);
            this.panel88.Controls.Add(this.panel91);
            this.panel88.Location = new System.Drawing.Point(0, 33);
            this.panel88.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel88.Name = "panel88";
            this.panel88.Size = new System.Drawing.Size(473, 1);
            this.panel88.TabIndex = 10;
            // 
            // panel89
            // 
            this.panel89.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel89.Controls.Add(this.panel90);
            this.panel89.Location = new System.Drawing.Point(0, -1);
            this.panel89.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel89.Name = "panel89";
            this.panel89.Size = new System.Drawing.Size(473, 1);
            this.panel89.TabIndex = 11;
            // 
            // panel90
            // 
            this.panel90.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel90.Location = new System.Drawing.Point(-1, 57);
            this.panel90.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel90.Name = "panel90";
            this.panel90.Size = new System.Drawing.Size(473, 1);
            this.panel90.TabIndex = 9;
            // 
            // panel91
            // 
            this.panel91.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel91.Location = new System.Drawing.Point(-1, 57);
            this.panel91.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel91.Name = "panel91";
            this.panel91.Size = new System.Drawing.Size(473, 1);
            this.panel91.TabIndex = 9;
            // 
            // panel92
            // 
            this.panel92.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel92.Location = new System.Drawing.Point(-1, 57);
            this.panel92.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel92.Name = "panel92";
            this.panel92.Size = new System.Drawing.Size(473, 1);
            this.panel92.TabIndex = 9;
            // 
            // panel93
            // 
            this.panel93.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel93.Controls.Add(this.panel94);
            this.panel93.Controls.Add(this.panel96);
            this.panel93.Location = new System.Drawing.Point(0, 33);
            this.panel93.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel93.Name = "panel93";
            this.panel93.Size = new System.Drawing.Size(473, 1);
            this.panel93.TabIndex = 10;
            // 
            // panel94
            // 
            this.panel94.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel94.Controls.Add(this.panel95);
            this.panel94.Location = new System.Drawing.Point(0, -1);
            this.panel94.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel94.Name = "panel94";
            this.panel94.Size = new System.Drawing.Size(473, 1);
            this.panel94.TabIndex = 11;
            // 
            // panel95
            // 
            this.panel95.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel95.Location = new System.Drawing.Point(-1, 57);
            this.panel95.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel95.Name = "panel95";
            this.panel95.Size = new System.Drawing.Size(473, 1);
            this.panel95.TabIndex = 9;
            // 
            // panel96
            // 
            this.panel96.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel96.Location = new System.Drawing.Point(-1, 57);
            this.panel96.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel96.Name = "panel96";
            this.panel96.Size = new System.Drawing.Size(473, 1);
            this.panel96.TabIndex = 9;
            // 
            // panel97
            // 
            this.panel97.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel97.Location = new System.Drawing.Point(-1, 57);
            this.panel97.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel97.Name = "panel97";
            this.panel97.Size = new System.Drawing.Size(473, 1);
            this.panel97.TabIndex = 9;
            // 
            // panel50
            // 
            this.panel50.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel50.Controls.Add(this.panel51);
            this.panel50.Controls.Add(this.panel63);
            this.panel50.Controls.Add(this.panel69);
            this.panel50.Controls.Add(this.panel73);
            this.panel50.Location = new System.Drawing.Point(8, 703);
            this.panel50.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel50.Name = "panel50";
            this.panel50.Size = new System.Drawing.Size(473, 1);
            this.panel50.TabIndex = 21;
            // 
            // panel51
            // 
            this.panel51.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel51.Controls.Add(this.panel52);
            this.panel51.Controls.Add(this.panel58);
            this.panel51.Controls.Add(this.panel62);
            this.panel51.Location = new System.Drawing.Point(0, 59);
            this.panel51.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel51.Name = "panel51";
            this.panel51.Size = new System.Drawing.Size(473, 1);
            this.panel51.TabIndex = 12;
            // 
            // panel52
            // 
            this.panel52.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel52.Controls.Add(this.panel53);
            this.panel52.Controls.Add(this.panel57);
            this.panel52.ForeColor = System.Drawing.Color.Black;
            this.panel52.Location = new System.Drawing.Point(0, 60);
            this.panel52.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel52.Name = "panel52";
            this.panel52.Size = new System.Drawing.Size(473, 1);
            this.panel52.TabIndex = 11;
            // 
            // panel53
            // 
            this.panel53.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel53.Controls.Add(this.panel54);
            this.panel53.Controls.Add(this.panel56);
            this.panel53.Location = new System.Drawing.Point(0, 33);
            this.panel53.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel53.Name = "panel53";
            this.panel53.Size = new System.Drawing.Size(473, 1);
            this.panel53.TabIndex = 10;
            // 
            // panel54
            // 
            this.panel54.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel54.Controls.Add(this.panel55);
            this.panel54.Location = new System.Drawing.Point(0, -1);
            this.panel54.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel54.Name = "panel54";
            this.panel54.Size = new System.Drawing.Size(473, 1);
            this.panel54.TabIndex = 11;
            // 
            // panel55
            // 
            this.panel55.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel55.Location = new System.Drawing.Point(-1, 57);
            this.panel55.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel55.Name = "panel55";
            this.panel55.Size = new System.Drawing.Size(473, 1);
            this.panel55.TabIndex = 9;
            // 
            // panel56
            // 
            this.panel56.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel56.Location = new System.Drawing.Point(-1, 57);
            this.panel56.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel56.Name = "panel56";
            this.panel56.Size = new System.Drawing.Size(473, 1);
            this.panel56.TabIndex = 9;
            // 
            // panel57
            // 
            this.panel57.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel57.Location = new System.Drawing.Point(-1, 57);
            this.panel57.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel57.Name = "panel57";
            this.panel57.Size = new System.Drawing.Size(473, 1);
            this.panel57.TabIndex = 9;
            // 
            // panel58
            // 
            this.panel58.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel58.Controls.Add(this.panel59);
            this.panel58.Controls.Add(this.panel61);
            this.panel58.Location = new System.Drawing.Point(0, 33);
            this.panel58.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel58.Name = "panel58";
            this.panel58.Size = new System.Drawing.Size(473, 1);
            this.panel58.TabIndex = 10;
            // 
            // panel59
            // 
            this.panel59.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel59.Controls.Add(this.panel60);
            this.panel59.Location = new System.Drawing.Point(0, -1);
            this.panel59.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel59.Name = "panel59";
            this.panel59.Size = new System.Drawing.Size(473, 1);
            this.panel59.TabIndex = 11;
            // 
            // panel60
            // 
            this.panel60.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel60.Location = new System.Drawing.Point(-1, 57);
            this.panel60.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel60.Name = "panel60";
            this.panel60.Size = new System.Drawing.Size(473, 1);
            this.panel60.TabIndex = 9;
            // 
            // panel61
            // 
            this.panel61.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel61.Location = new System.Drawing.Point(-1, 57);
            this.panel61.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel61.Name = "panel61";
            this.panel61.Size = new System.Drawing.Size(473, 1);
            this.panel61.TabIndex = 9;
            // 
            // panel62
            // 
            this.panel62.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel62.Location = new System.Drawing.Point(-1, 57);
            this.panel62.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel62.Name = "panel62";
            this.panel62.Size = new System.Drawing.Size(473, 1);
            this.panel62.TabIndex = 9;
            // 
            // panel63
            // 
            this.panel63.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel63.Controls.Add(this.panel64);
            this.panel63.Controls.Add(this.panel68);
            this.panel63.ForeColor = System.Drawing.Color.Black;
            this.panel63.Location = new System.Drawing.Point(0, 60);
            this.panel63.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel63.Name = "panel63";
            this.panel63.Size = new System.Drawing.Size(473, 1);
            this.panel63.TabIndex = 11;
            // 
            // panel64
            // 
            this.panel64.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel64.Controls.Add(this.panel65);
            this.panel64.Controls.Add(this.panel67);
            this.panel64.Location = new System.Drawing.Point(0, 33);
            this.panel64.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel64.Name = "panel64";
            this.panel64.Size = new System.Drawing.Size(473, 1);
            this.panel64.TabIndex = 10;
            // 
            // panel65
            // 
            this.panel65.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel65.Controls.Add(this.panel66);
            this.panel65.Location = new System.Drawing.Point(0, -1);
            this.panel65.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel65.Name = "panel65";
            this.panel65.Size = new System.Drawing.Size(473, 1);
            this.panel65.TabIndex = 11;
            // 
            // panel66
            // 
            this.panel66.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel66.Location = new System.Drawing.Point(-1, 57);
            this.panel66.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel66.Name = "panel66";
            this.panel66.Size = new System.Drawing.Size(473, 1);
            this.panel66.TabIndex = 9;
            // 
            // panel67
            // 
            this.panel67.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel67.Location = new System.Drawing.Point(-1, 57);
            this.panel67.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel67.Name = "panel67";
            this.panel67.Size = new System.Drawing.Size(473, 1);
            this.panel67.TabIndex = 9;
            // 
            // panel68
            // 
            this.panel68.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel68.Location = new System.Drawing.Point(-1, 57);
            this.panel68.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel68.Name = "panel68";
            this.panel68.Size = new System.Drawing.Size(473, 1);
            this.panel68.TabIndex = 9;
            // 
            // panel69
            // 
            this.panel69.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel69.Controls.Add(this.panel70);
            this.panel69.Controls.Add(this.panel72);
            this.panel69.Location = new System.Drawing.Point(0, 33);
            this.panel69.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel69.Name = "panel69";
            this.panel69.Size = new System.Drawing.Size(473, 1);
            this.panel69.TabIndex = 10;
            // 
            // panel70
            // 
            this.panel70.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel70.Controls.Add(this.panel71);
            this.panel70.Location = new System.Drawing.Point(0, -1);
            this.panel70.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel70.Name = "panel70";
            this.panel70.Size = new System.Drawing.Size(473, 1);
            this.panel70.TabIndex = 11;
            // 
            // panel71
            // 
            this.panel71.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel71.Location = new System.Drawing.Point(-1, 57);
            this.panel71.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel71.Name = "panel71";
            this.panel71.Size = new System.Drawing.Size(473, 1);
            this.panel71.TabIndex = 9;
            // 
            // panel72
            // 
            this.panel72.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel72.Location = new System.Drawing.Point(-1, 57);
            this.panel72.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel72.Name = "panel72";
            this.panel72.Size = new System.Drawing.Size(473, 1);
            this.panel72.TabIndex = 9;
            // 
            // panel73
            // 
            this.panel73.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel73.Location = new System.Drawing.Point(-1, 57);
            this.panel73.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel73.Name = "panel73";
            this.panel73.Size = new System.Drawing.Size(473, 1);
            this.panel73.TabIndex = 9;
            // 
            // textBox20
            // 
            this.textBox20.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox20.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox20.Location = new System.Drawing.Point(107, 671);
            this.textBox20.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox20.Name = "textBox20";
            this.textBox20.Size = new System.Drawing.Size(100, 26);
            this.textBox20.TabIndex = 18;
            this.textBox20.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox19
            // 
            this.textBox19.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox19.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox19.Location = new System.Drawing.Point(107, 638);
            this.textBox19.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox19.Name = "textBox19";
            this.textBox19.Size = new System.Drawing.Size(100, 26);
            this.textBox19.TabIndex = 17;
            this.textBox19.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox18
            // 
            this.textBox18.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.textBox18.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox18.Location = new System.Drawing.Point(107, 604);
            this.textBox18.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox18.Name = "textBox18";
            this.textBox18.Size = new System.Drawing.Size(100, 26);
            this.textBox18.TabIndex = 16;
            this.textBox18.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.BackColor = System.Drawing.Color.WhiteSmoke;
            this.label27.Font = new System.Drawing.Font("IBM Plex Mono", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label27.Location = new System.Drawing.Point(213, 436);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(120, 21);
            this.label27.TabIndex = 25;
            this.label27.Text = "Transform-2";
            // 
            // panel98
            // 
            this.panel98.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel98.Controls.Add(this.panel122);
            this.panel98.Controls.Add(this.panel99);
            this.panel98.Controls.Add(this.panel111);
            this.panel98.Controls.Add(this.panel117);
            this.panel98.Controls.Add(this.panel121);
            this.panel98.Location = new System.Drawing.Point(12, 826);
            this.panel98.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel98.Name = "panel98";
            this.panel98.Size = new System.Drawing.Size(384, 1);
            this.panel98.TabIndex = 23;
            // 
            // panel122
            // 
            this.panel122.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel122.Controls.Add(this.panel123);
            this.panel122.Controls.Add(this.panel135);
            this.panel122.Controls.Add(this.panel141);
            this.panel122.Controls.Add(this.panel145);
            this.panel122.Location = new System.Drawing.Point(9, 0);
            this.panel122.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel122.Name = "panel122";
            this.panel122.Size = new System.Drawing.Size(379, 0);
            this.panel122.TabIndex = 23;
            // 
            // panel123
            // 
            this.panel123.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel123.Controls.Add(this.panel124);
            this.panel123.Controls.Add(this.panel130);
            this.panel123.Controls.Add(this.panel134);
            this.panel123.Location = new System.Drawing.Point(0, 59);
            this.panel123.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel123.Name = "panel123";
            this.panel123.Size = new System.Drawing.Size(473, 1);
            this.panel123.TabIndex = 12;
            // 
            // panel124
            // 
            this.panel124.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel124.Controls.Add(this.panel125);
            this.panel124.Controls.Add(this.panel129);
            this.panel124.ForeColor = System.Drawing.Color.Black;
            this.panel124.Location = new System.Drawing.Point(0, 60);
            this.panel124.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel124.Name = "panel124";
            this.panel124.Size = new System.Drawing.Size(473, 1);
            this.panel124.TabIndex = 11;
            // 
            // panel125
            // 
            this.panel125.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel125.Controls.Add(this.panel126);
            this.panel125.Controls.Add(this.panel128);
            this.panel125.Location = new System.Drawing.Point(0, 33);
            this.panel125.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel125.Name = "panel125";
            this.panel125.Size = new System.Drawing.Size(473, 1);
            this.panel125.TabIndex = 10;
            // 
            // panel126
            // 
            this.panel126.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel126.Controls.Add(this.panel127);
            this.panel126.Location = new System.Drawing.Point(0, -1);
            this.panel126.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel126.Name = "panel126";
            this.panel126.Size = new System.Drawing.Size(473, 1);
            this.panel126.TabIndex = 11;
            // 
            // panel127
            // 
            this.panel127.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel127.Location = new System.Drawing.Point(-1, 57);
            this.panel127.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel127.Name = "panel127";
            this.panel127.Size = new System.Drawing.Size(473, 1);
            this.panel127.TabIndex = 9;
            // 
            // panel128
            // 
            this.panel128.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel128.Location = new System.Drawing.Point(-1, 57);
            this.panel128.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel128.Name = "panel128";
            this.panel128.Size = new System.Drawing.Size(473, 1);
            this.panel128.TabIndex = 9;
            // 
            // panel129
            // 
            this.panel129.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel129.Location = new System.Drawing.Point(-1, 57);
            this.panel129.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel129.Name = "panel129";
            this.panel129.Size = new System.Drawing.Size(473, 1);
            this.panel129.TabIndex = 9;
            // 
            // panel130
            // 
            this.panel130.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel130.Controls.Add(this.panel131);
            this.panel130.Controls.Add(this.panel133);
            this.panel130.Location = new System.Drawing.Point(0, 33);
            this.panel130.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel130.Name = "panel130";
            this.panel130.Size = new System.Drawing.Size(473, 1);
            this.panel130.TabIndex = 10;
            // 
            // panel131
            // 
            this.panel131.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel131.Controls.Add(this.panel132);
            this.panel131.Location = new System.Drawing.Point(0, -1);
            this.panel131.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel131.Name = "panel131";
            this.panel131.Size = new System.Drawing.Size(473, 1);
            this.panel131.TabIndex = 11;
            // 
            // panel132
            // 
            this.panel132.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel132.Location = new System.Drawing.Point(-1, 57);
            this.panel132.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel132.Name = "panel132";
            this.panel132.Size = new System.Drawing.Size(473, 1);
            this.panel132.TabIndex = 9;
            // 
            // panel133
            // 
            this.panel133.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel133.Location = new System.Drawing.Point(-1, 57);
            this.panel133.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel133.Name = "panel133";
            this.panel133.Size = new System.Drawing.Size(473, 1);
            this.panel133.TabIndex = 9;
            // 
            // panel134
            // 
            this.panel134.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel134.Location = new System.Drawing.Point(-1, 57);
            this.panel134.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel134.Name = "panel134";
            this.panel134.Size = new System.Drawing.Size(473, 1);
            this.panel134.TabIndex = 9;
            // 
            // panel135
            // 
            this.panel135.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel135.Controls.Add(this.panel136);
            this.panel135.Controls.Add(this.panel140);
            this.panel135.ForeColor = System.Drawing.Color.Black;
            this.panel135.Location = new System.Drawing.Point(0, 60);
            this.panel135.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel135.Name = "panel135";
            this.panel135.Size = new System.Drawing.Size(473, 1);
            this.panel135.TabIndex = 11;
            // 
            // panel136
            // 
            this.panel136.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel136.Controls.Add(this.panel137);
            this.panel136.Controls.Add(this.panel139);
            this.panel136.Location = new System.Drawing.Point(0, 33);
            this.panel136.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel136.Name = "panel136";
            this.panel136.Size = new System.Drawing.Size(473, 1);
            this.panel136.TabIndex = 10;
            // 
            // panel137
            // 
            this.panel137.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel137.Controls.Add(this.panel138);
            this.panel137.Location = new System.Drawing.Point(0, -1);
            this.panel137.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel137.Name = "panel137";
            this.panel137.Size = new System.Drawing.Size(473, 1);
            this.panel137.TabIndex = 11;
            // 
            // panel138
            // 
            this.panel138.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel138.Location = new System.Drawing.Point(-1, 57);
            this.panel138.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel138.Name = "panel138";
            this.panel138.Size = new System.Drawing.Size(473, 1);
            this.panel138.TabIndex = 9;
            // 
            // panel139
            // 
            this.panel139.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel139.Location = new System.Drawing.Point(-1, 57);
            this.panel139.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel139.Name = "panel139";
            this.panel139.Size = new System.Drawing.Size(473, 1);
            this.panel139.TabIndex = 9;
            // 
            // panel140
            // 
            this.panel140.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel140.Location = new System.Drawing.Point(-1, 57);
            this.panel140.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel140.Name = "panel140";
            this.panel140.Size = new System.Drawing.Size(473, 1);
            this.panel140.TabIndex = 9;
            // 
            // panel141
            // 
            this.panel141.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel141.Controls.Add(this.panel142);
            this.panel141.Controls.Add(this.panel144);
            this.panel141.Location = new System.Drawing.Point(0, 33);
            this.panel141.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel141.Name = "panel141";
            this.panel141.Size = new System.Drawing.Size(473, 1);
            this.panel141.TabIndex = 10;
            // 
            // panel142
            // 
            this.panel142.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel142.Controls.Add(this.panel143);
            this.panel142.Location = new System.Drawing.Point(0, -1);
            this.panel142.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel142.Name = "panel142";
            this.panel142.Size = new System.Drawing.Size(473, 1);
            this.panel142.TabIndex = 11;
            // 
            // panel143
            // 
            this.panel143.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel143.Location = new System.Drawing.Point(-1, 57);
            this.panel143.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel143.Name = "panel143";
            this.panel143.Size = new System.Drawing.Size(473, 1);
            this.panel143.TabIndex = 9;
            // 
            // panel144
            // 
            this.panel144.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel144.Location = new System.Drawing.Point(-1, 57);
            this.panel144.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel144.Name = "panel144";
            this.panel144.Size = new System.Drawing.Size(473, 1);
            this.panel144.TabIndex = 9;
            // 
            // panel145
            // 
            this.panel145.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel145.Location = new System.Drawing.Point(-1, 57);
            this.panel145.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel145.Name = "panel145";
            this.panel145.Size = new System.Drawing.Size(473, 1);
            this.panel145.TabIndex = 9;
            // 
            // panel99
            // 
            this.panel99.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel99.Controls.Add(this.panel100);
            this.panel99.Controls.Add(this.panel106);
            this.panel99.Controls.Add(this.panel110);
            this.panel99.Location = new System.Drawing.Point(0, 59);
            this.panel99.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel99.Name = "panel99";
            this.panel99.Size = new System.Drawing.Size(473, 1);
            this.panel99.TabIndex = 12;
            // 
            // panel100
            // 
            this.panel100.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel100.Controls.Add(this.panel101);
            this.panel100.Controls.Add(this.panel105);
            this.panel100.ForeColor = System.Drawing.Color.Black;
            this.panel100.Location = new System.Drawing.Point(0, 60);
            this.panel100.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel100.Name = "panel100";
            this.panel100.Size = new System.Drawing.Size(473, 1);
            this.panel100.TabIndex = 11;
            // 
            // panel101
            // 
            this.panel101.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel101.Controls.Add(this.panel102);
            this.panel101.Controls.Add(this.panel104);
            this.panel101.Location = new System.Drawing.Point(0, 33);
            this.panel101.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel101.Name = "panel101";
            this.panel101.Size = new System.Drawing.Size(473, 1);
            this.panel101.TabIndex = 10;
            // 
            // panel102
            // 
            this.panel102.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel102.Controls.Add(this.panel103);
            this.panel102.Location = new System.Drawing.Point(0, -1);
            this.panel102.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel102.Name = "panel102";
            this.panel102.Size = new System.Drawing.Size(473, 1);
            this.panel102.TabIndex = 11;
            // 
            // panel103
            // 
            this.panel103.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel103.Location = new System.Drawing.Point(-1, 57);
            this.panel103.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel103.Name = "panel103";
            this.panel103.Size = new System.Drawing.Size(473, 1);
            this.panel103.TabIndex = 9;
            // 
            // panel104
            // 
            this.panel104.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel104.Location = new System.Drawing.Point(-1, 57);
            this.panel104.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel104.Name = "panel104";
            this.panel104.Size = new System.Drawing.Size(473, 1);
            this.panel104.TabIndex = 9;
            // 
            // panel105
            // 
            this.panel105.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel105.Location = new System.Drawing.Point(-1, 57);
            this.panel105.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel105.Name = "panel105";
            this.panel105.Size = new System.Drawing.Size(473, 1);
            this.panel105.TabIndex = 9;
            // 
            // panel106
            // 
            this.panel106.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel106.Controls.Add(this.panel107);
            this.panel106.Controls.Add(this.panel109);
            this.panel106.Location = new System.Drawing.Point(0, 33);
            this.panel106.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel106.Name = "panel106";
            this.panel106.Size = new System.Drawing.Size(473, 1);
            this.panel106.TabIndex = 10;
            // 
            // panel107
            // 
            this.panel107.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel107.Controls.Add(this.panel108);
            this.panel107.Location = new System.Drawing.Point(0, -1);
            this.panel107.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel107.Name = "panel107";
            this.panel107.Size = new System.Drawing.Size(473, 1);
            this.panel107.TabIndex = 11;
            // 
            // panel108
            // 
            this.panel108.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel108.Location = new System.Drawing.Point(-1, 57);
            this.panel108.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel108.Name = "panel108";
            this.panel108.Size = new System.Drawing.Size(473, 1);
            this.panel108.TabIndex = 9;
            // 
            // panel109
            // 
            this.panel109.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel109.Location = new System.Drawing.Point(-1, 57);
            this.panel109.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel109.Name = "panel109";
            this.panel109.Size = new System.Drawing.Size(473, 1);
            this.panel109.TabIndex = 9;
            // 
            // panel110
            // 
            this.panel110.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel110.Location = new System.Drawing.Point(-1, 57);
            this.panel110.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel110.Name = "panel110";
            this.panel110.Size = new System.Drawing.Size(473, 1);
            this.panel110.TabIndex = 9;
            // 
            // panel111
            // 
            this.panel111.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel111.Controls.Add(this.panel112);
            this.panel111.Controls.Add(this.panel116);
            this.panel111.ForeColor = System.Drawing.Color.Black;
            this.panel111.Location = new System.Drawing.Point(0, 60);
            this.panel111.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel111.Name = "panel111";
            this.panel111.Size = new System.Drawing.Size(473, 1);
            this.panel111.TabIndex = 11;
            // 
            // panel112
            // 
            this.panel112.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel112.Controls.Add(this.panel113);
            this.panel112.Controls.Add(this.panel115);
            this.panel112.Location = new System.Drawing.Point(0, 33);
            this.panel112.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel112.Name = "panel112";
            this.panel112.Size = new System.Drawing.Size(473, 1);
            this.panel112.TabIndex = 10;
            // 
            // panel113
            // 
            this.panel113.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel113.Controls.Add(this.panel114);
            this.panel113.Location = new System.Drawing.Point(0, -1);
            this.panel113.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel113.Name = "panel113";
            this.panel113.Size = new System.Drawing.Size(473, 1);
            this.panel113.TabIndex = 11;
            // 
            // panel114
            // 
            this.panel114.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel114.Location = new System.Drawing.Point(-1, 57);
            this.panel114.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel114.Name = "panel114";
            this.panel114.Size = new System.Drawing.Size(473, 1);
            this.panel114.TabIndex = 9;
            // 
            // panel115
            // 
            this.panel115.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel115.Location = new System.Drawing.Point(-1, 57);
            this.panel115.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel115.Name = "panel115";
            this.panel115.Size = new System.Drawing.Size(473, 1);
            this.panel115.TabIndex = 9;
            // 
            // panel116
            // 
            this.panel116.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel116.Location = new System.Drawing.Point(-1, 57);
            this.panel116.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel116.Name = "panel116";
            this.panel116.Size = new System.Drawing.Size(473, 1);
            this.panel116.TabIndex = 9;
            // 
            // panel117
            // 
            this.panel117.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel117.Controls.Add(this.panel118);
            this.panel117.Controls.Add(this.panel120);
            this.panel117.Location = new System.Drawing.Point(0, 33);
            this.panel117.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel117.Name = "panel117";
            this.panel117.Size = new System.Drawing.Size(473, 1);
            this.panel117.TabIndex = 10;
            // 
            // panel118
            // 
            this.panel118.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel118.Controls.Add(this.panel119);
            this.panel118.Location = new System.Drawing.Point(0, -1);
            this.panel118.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel118.Name = "panel118";
            this.panel118.Size = new System.Drawing.Size(473, 1);
            this.panel118.TabIndex = 11;
            // 
            // panel119
            // 
            this.panel119.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel119.Location = new System.Drawing.Point(-1, 57);
            this.panel119.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel119.Name = "panel119";
            this.panel119.Size = new System.Drawing.Size(473, 1);
            this.panel119.TabIndex = 9;
            // 
            // panel120
            // 
            this.panel120.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel120.Location = new System.Drawing.Point(-1, 57);
            this.panel120.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel120.Name = "panel120";
            this.panel120.Size = new System.Drawing.Size(473, 1);
            this.panel120.TabIndex = 9;
            // 
            // panel121
            // 
            this.panel121.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.panel121.Location = new System.Drawing.Point(-1, 57);
            this.panel121.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.panel121.Name = "panel121";
            this.panel121.Size = new System.Drawing.Size(473, 1);
            this.panel121.TabIndex = 9;
            // 
            // checkBoxYarema
            // 
            this.checkBoxYarema.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.checkBoxYarema.AutoSize = true;
            this.checkBoxYarema.Location = new System.Drawing.Point(687, 912);
            this.checkBoxYarema.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxYarema.Name = "checkBoxYarema";
            this.checkBoxYarema.Size = new System.Drawing.Size(74, 20);
            this.checkBoxYarema.TabIndex = 24;
            this.checkBoxYarema.Text = "Yarema";
            this.checkBoxYarema.UseVisualStyleBackColor = true;
            this.checkBoxYarema.CheckedChanged += new System.EventHandler(this.checkBoxYarema_CheckedChanged);
            // 
            // checkBoxQayara
            // 
            this.checkBoxQayara.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.checkBoxQayara.AutoSize = true;
            this.checkBoxQayara.Location = new System.Drawing.Point(828, 912);
            this.checkBoxQayara.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxQayara.Name = "checkBoxQayara";
            this.checkBoxQayara.Size = new System.Drawing.Size(71, 20);
            this.checkBoxQayara.TabIndex = 24;
            this.checkBoxQayara.Text = "Qayara";
            this.checkBoxQayara.UseVisualStyleBackColor = true;
            this.checkBoxQayara.CheckedChanged += new System.EventHandler(this.checkBoxQayara_CheckedChanged);
            // 
            // checkBoxT1
            // 
            this.checkBoxT1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.checkBoxT1.AutoSize = true;
            this.checkBoxT1.Location = new System.Drawing.Point(1001, 912);
            this.checkBoxT1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxT1.Name = "checkBoxT1";
            this.checkBoxT1.Size = new System.Drawing.Size(42, 20);
            this.checkBoxT1.TabIndex = 24;
            this.checkBoxT1.Text = "T1";
            this.checkBoxT1.UseVisualStyleBackColor = true;
            this.checkBoxT1.CheckedChanged += new System.EventHandler(this.checkBoxT1_CheckedChanged);
            // 
            // checkBoxT2
            // 
            this.checkBoxT2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.checkBoxT2.AutoSize = true;
            this.checkBoxT2.Location = new System.Drawing.Point(1083, 912);
            this.checkBoxT2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxT2.Name = "checkBoxT2";
            this.checkBoxT2.Size = new System.Drawing.Size(42, 20);
            this.checkBoxT2.TabIndex = 24;
            this.checkBoxT2.Text = "T2";
            this.checkBoxT2.UseVisualStyleBackColor = true;
            this.checkBoxT2.CheckedChanged += new System.EventHandler(this.checkBoxT2_CheckedChanged);
            // 
            // checkBoxT3
            // 
            this.checkBoxT3.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.checkBoxT3.AutoSize = true;
            this.checkBoxT3.Location = new System.Drawing.Point(1173, 912);
            this.checkBoxT3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxT3.Name = "checkBoxT3";
            this.checkBoxT3.Size = new System.Drawing.Size(42, 20);
            this.checkBoxT3.TabIndex = 24;
            this.checkBoxT3.Text = "T3";
            this.checkBoxT3.UseVisualStyleBackColor = true;
            this.checkBoxT3.CheckedChanged += new System.EventHandler(this.checkBoxT3_CheckedChanged);
            // 
            // checkBoxFdd
            // 
            this.checkBoxFdd.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.checkBoxFdd.AutoSize = true;
            this.checkBoxFdd.Location = new System.Drawing.Point(1279, 912);
            this.checkBoxFdd.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBoxFdd.Name = "checkBoxFdd";
            this.checkBoxFdd.Size = new System.Drawing.Size(77, 20);
            this.checkBoxFdd.TabIndex = 24;
            this.checkBoxFdd.Text = "Feeders";
            this.checkBoxFdd.UseVisualStyleBackColor = true;
            this.checkBoxFdd.CheckedChanged += new System.EventHandler(this.checkBoxFdd_CheckedChanged);
            // 
            // button_Calibrate
            // 
            this.button_Calibrate.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.button_Calibrate.Location = new System.Drawing.Point(1805, 26);
            this.button_Calibrate.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_Calibrate.Name = "button_Calibrate";
            this.button_Calibrate.Size = new System.Drawing.Size(75, 23);
            this.button_Calibrate.TabIndex = 25;
            this.button_Calibrate.Text = "Calibrate";
            this.button_Calibrate.UseVisualStyleBackColor = true;
            this.button_Calibrate.Click += new System.EventHandler(this.buttonCalibrate_Click);
            // 
            // button_start
            // 
            this.button_start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_start.Font = new System.Drawing.Font("Microsoft Sans Serif", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_start.Location = new System.Drawing.Point(1684, 904);
            this.button_start.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(199, 36);
            this.button_start.TabIndex = 26;
            this.button_start.Text = "تشغيل";
            this.button_start.UseVisualStyleBackColor = true;
            this.button_start.Click += new System.EventHandler(this.button_Start_Click);
            // 
            // button_new
            // 
            this.button_new.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.button_new.Font = new System.Drawing.Font("Microsoft Sans Serif", 12.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_new.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.button_new.Location = new System.Drawing.Point(1595, 4);
            this.button_new.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button_new.Name = "button_new";
            this.button_new.Size = new System.Drawing.Size(98, 56);
            this.button_new.TabIndex = 27;
            this.button_new.Text = "جديد ";
            this.button_new.UseCompatibleTextRendering = true;
            this.button_new.UseVisualStyleBackColor = true;
            this.button_new.Click += new System.EventHandler(this.button_New_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1924, 971);
            this.Controls.Add(this.button_new);
            this.Controls.Add(this.button_start);
            this.Controls.Add(this.button_Calibrate);
            this.Controls.Add(this.checkBoxFdd);
            this.Controls.Add(this.checkBoxT3);
            this.Controls.Add(this.checkBoxT2);
            this.Controls.Add(this.checkBoxT1);
            this.Controls.Add(this.checkBoxQayara);
            this.Controls.Add(this.checkBoxYarema);
            this.Controls.Add(this.panel98);
            this.Controls.Add(this.button_OnPrint);
            this.Controls.Add(this.panel20);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WinLogosheet";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel_fixedError.ResumeLayout(false);
            this.panel_fixedError.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel20.ResumeLayout(false);
            this.panel20.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel19.ResumeLayout(false);
            this.panel22.ResumeLayout(false);
            this.panel24.ResumeLayout(false);
            this.panel26.ResumeLayout(false);
            this.panel38.ResumeLayout(false);
            this.panel39.ResumeLayout(false);
            this.panel40.ResumeLayout(false);
            this.panel41.ResumeLayout(false);
            this.panel45.ResumeLayout(false);
            this.panel46.ResumeLayout(false);
            this.panel32.ResumeLayout(false);
            this.panel33.ResumeLayout(false);
            this.panel34.ResumeLayout(false);
            this.panel27.ResumeLayout(false);
            this.panel28.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel146.ResumeLayout(false);
            this.panel147.ResumeLayout(false);
            this.panel148.ResumeLayout(false);
            this.panel149.ResumeLayout(false);
            this.panel150.ResumeLayout(false);
            this.panel151.ResumeLayout(false);
            this.panel155.ResumeLayout(false);
            this.panel156.ResumeLayout(false);
            this.panel160.ResumeLayout(false);
            this.panel161.ResumeLayout(false);
            this.panel162.ResumeLayout(false);
            this.panel166.ResumeLayout(false);
            this.panel167.ResumeLayout(false);
            this.panel171.ResumeLayout(false);
            this.panel172.ResumeLayout(false);
            this.panel173.ResumeLayout(false);
            this.panel174.ResumeLayout(false);
            this.panel178.ResumeLayout(false);
            this.panel179.ResumeLayout(false);
            this.panel183.ResumeLayout(false);
            this.panel184.ResumeLayout(false);
            this.panel185.ResumeLayout(false);
            this.panel189.ResumeLayout(false);
            this.panel190.ResumeLayout(false);
            this.panel74.ResumeLayout(false);
            this.panel75.ResumeLayout(false);
            this.panel76.ResumeLayout(false);
            this.panel77.ResumeLayout(false);
            this.panel78.ResumeLayout(false);
            this.panel82.ResumeLayout(false);
            this.panel83.ResumeLayout(false);
            this.panel87.ResumeLayout(false);
            this.panel88.ResumeLayout(false);
            this.panel89.ResumeLayout(false);
            this.panel93.ResumeLayout(false);
            this.panel94.ResumeLayout(false);
            this.panel50.ResumeLayout(false);
            this.panel51.ResumeLayout(false);
            this.panel52.ResumeLayout(false);
            this.panel53.ResumeLayout(false);
            this.panel54.ResumeLayout(false);
            this.panel58.ResumeLayout(false);
            this.panel59.ResumeLayout(false);
            this.panel63.ResumeLayout(false);
            this.panel64.ResumeLayout(false);
            this.panel65.ResumeLayout(false);
            this.panel69.ResumeLayout(false);
            this.panel70.ResumeLayout(false);
            this.panel98.ResumeLayout(false);
            this.panel122.ResumeLayout(false);
            this.panel123.ResumeLayout(false);
            this.panel124.ResumeLayout(false);
            this.panel125.ResumeLayout(false);
            this.panel126.ResumeLayout(false);
            this.panel130.ResumeLayout(false);
            this.panel131.ResumeLayout(false);
            this.panel135.ResumeLayout(false);
            this.panel136.ResumeLayout(false);
            this.panel137.ResumeLayout(false);
            this.panel141.ResumeLayout(false);
            this.panel142.ResumeLayout(false);
            this.panel99.ResumeLayout(false);
            this.panel100.ResumeLayout(false);
            this.panel101.ResumeLayout(false);
            this.panel102.ResumeLayout(false);
            this.panel106.ResumeLayout(false);
            this.panel107.ResumeLayout(false);
            this.panel111.ResumeLayout(false);
            this.panel112.ResumeLayout(false);
            this.panel113.ResumeLayout(false);
            this.panel117.ResumeLayout(false);
            this.panel118.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button_next;
        private System.Windows.Forms.Button button_prv;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Panel panel_fixedError;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Button button_copy_fromPrev;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Button button_copy_fromNext;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader_2;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader_6;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader_8;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.ColumnHeader columnHeader_11;
        private System.Windows.Forms.ColumnHeader columnHeader11;
        private System.Windows.Forms.ColumnHeader columnHeader12;
        private System.Windows.Forms.ColumnHeader columnHeader13;
        private System.Windows.Forms.ColumnHeader columnHeader_14;
        private System.Windows.Forms.ColumnHeader columnHeader_15;
        private System.Windows.Forms.ColumnHeader columnHeader_16;
        private System.Windows.Forms.Panel panel20;
        private System.Windows.Forms.Label lable_date;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Button button_OnPrint;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button button_UpdateCurrHImage;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox textBox7;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox8;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox textBox9;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox10;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox11;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox12;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox textBox13;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox14;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox textBox15;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox16;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox textBox21;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox22;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox23;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.Panel panel11;
        private System.Windows.Forms.Panel panel12;
        private System.Windows.Forms.Panel panel13;
        private System.Windows.Forms.Panel panel14;
        private System.Windows.Forms.Panel panel15;
        private System.Windows.Forms.Panel panel16;
        private System.Windows.Forms.Panel panel17;
        private System.Windows.Forms.Panel panel18;
        private System.Windows.Forms.Panel panel19;
        private System.Windows.Forms.Panel panel22;
        private System.Windows.Forms.Panel panel24;
        private System.Windows.Forms.Panel panel25;
        private System.Windows.Forms.Panel panel23;
        private System.Windows.Forms.Panel panel21;
        private System.Windows.Forms.Panel panel26;
        private System.Windows.Forms.Panel panel38;
        private System.Windows.Forms.Panel panel39;
        private System.Windows.Forms.Panel panel40;
        private System.Windows.Forms.Panel panel41;
        private System.Windows.Forms.Panel panel42;
        private System.Windows.Forms.Panel panel43;
        private System.Windows.Forms.Panel panel44;
        private System.Windows.Forms.Panel panel45;
        private System.Windows.Forms.Panel panel46;
        private System.Windows.Forms.Panel panel47;
        private System.Windows.Forms.Panel panel48;
        private System.Windows.Forms.Panel panel49;
        private System.Windows.Forms.Panel panel32;
        private System.Windows.Forms.Panel panel33;
        private System.Windows.Forms.Panel panel34;
        private System.Windows.Forms.Panel panel35;
        private System.Windows.Forms.Panel panel36;
        private System.Windows.Forms.Panel panel37;
        private System.Windows.Forms.Panel panel27;
        private System.Windows.Forms.Panel panel28;
        private System.Windows.Forms.Panel panel29;
        private System.Windows.Forms.Panel panel30;
        private System.Windows.Forms.Panel panel31;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox textBox24;
        private System.Windows.Forms.TextBox textBox17;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox20;
        private System.Windows.Forms.TextBox textBox19;
        private System.Windows.Forms.TextBox textBox18;
        private System.Windows.Forms.Panel panel74;
        private System.Windows.Forms.Panel panel75;
        private System.Windows.Forms.Panel panel76;
        private System.Windows.Forms.Panel panel77;
        private System.Windows.Forms.Panel panel78;
        private System.Windows.Forms.Panel panel79;
        private System.Windows.Forms.Panel panel80;
        private System.Windows.Forms.Panel panel81;
        private System.Windows.Forms.Panel panel82;
        private System.Windows.Forms.Panel panel83;
        private System.Windows.Forms.Panel panel84;
        private System.Windows.Forms.Panel panel85;
        private System.Windows.Forms.Panel panel86;
        private System.Windows.Forms.Panel panel87;
        private System.Windows.Forms.Panel panel88;
        private System.Windows.Forms.Panel panel89;
        private System.Windows.Forms.Panel panel90;
        private System.Windows.Forms.Panel panel91;
        private System.Windows.Forms.Panel panel92;
        private System.Windows.Forms.Panel panel93;
        private System.Windows.Forms.Panel panel94;
        private System.Windows.Forms.Panel panel95;
        private System.Windows.Forms.Panel panel96;
        private System.Windows.Forms.Panel panel97;
        private System.Windows.Forms.Panel panel50;
        private System.Windows.Forms.Panel panel51;
        private System.Windows.Forms.Panel panel52;
        private System.Windows.Forms.Panel panel53;
        private System.Windows.Forms.Panel panel54;
        private System.Windows.Forms.Panel panel55;
        private System.Windows.Forms.Panel panel56;
        private System.Windows.Forms.Panel panel57;
        private System.Windows.Forms.Panel panel58;
        private System.Windows.Forms.Panel panel59;
        private System.Windows.Forms.Panel panel60;
        private System.Windows.Forms.Panel panel61;
        private System.Windows.Forms.Panel panel62;
        private System.Windows.Forms.Panel panel63;
        private System.Windows.Forms.Panel panel64;
        private System.Windows.Forms.Panel panel65;
        private System.Windows.Forms.Panel panel66;
        private System.Windows.Forms.Panel panel67;
        private System.Windows.Forms.Panel panel68;
        private System.Windows.Forms.Panel panel69;
        private System.Windows.Forms.Panel panel70;
        private System.Windows.Forms.Panel panel71;
        private System.Windows.Forms.Panel panel72;
        private System.Windows.Forms.Panel panel73;
        private System.Windows.Forms.Panel panel98;
        private System.Windows.Forms.Panel panel122;
        private System.Windows.Forms.Panel panel123;
        private System.Windows.Forms.Panel panel124;
        private System.Windows.Forms.Panel panel125;
        private System.Windows.Forms.Panel panel126;
        private System.Windows.Forms.Panel panel127;
        private System.Windows.Forms.Panel panel128;
        private System.Windows.Forms.Panel panel129;
        private System.Windows.Forms.Panel panel130;
        private System.Windows.Forms.Panel panel131;
        private System.Windows.Forms.Panel panel132;
        private System.Windows.Forms.Panel panel133;
        private System.Windows.Forms.Panel panel134;
        private System.Windows.Forms.Panel panel135;
        private System.Windows.Forms.Panel panel136;
        private System.Windows.Forms.Panel panel137;
        private System.Windows.Forms.Panel panel138;
        private System.Windows.Forms.Panel panel139;
        private System.Windows.Forms.Panel panel140;
        private System.Windows.Forms.Panel panel141;
        private System.Windows.Forms.Panel panel142;
        private System.Windows.Forms.Panel panel143;
        private System.Windows.Forms.Panel panel144;
        private System.Windows.Forms.Panel panel145;
        private System.Windows.Forms.Panel panel99;
        private System.Windows.Forms.Panel panel100;
        private System.Windows.Forms.Panel panel101;
        private System.Windows.Forms.Panel panel102;
        private System.Windows.Forms.Panel panel103;
        private System.Windows.Forms.Panel panel104;
        private System.Windows.Forms.Panel panel105;
        private System.Windows.Forms.Panel panel106;
        private System.Windows.Forms.Panel panel107;
        private System.Windows.Forms.Panel panel108;
        private System.Windows.Forms.Panel panel109;
        private System.Windows.Forms.Panel panel110;
        private System.Windows.Forms.Panel panel111;
        private System.Windows.Forms.Panel panel112;
        private System.Windows.Forms.Panel panel113;
        private System.Windows.Forms.Panel panel114;
        private System.Windows.Forms.Panel panel115;
        private System.Windows.Forms.Panel panel116;
        private System.Windows.Forms.Panel panel117;
        private System.Windows.Forms.Panel panel118;
        private System.Windows.Forms.Panel panel119;
        private System.Windows.Forms.Panel panel120;
        private System.Windows.Forms.Panel panel121;
        private System.Windows.Forms.Panel panel146;
        private System.Windows.Forms.Panel panel147;
        private System.Windows.Forms.Panel panel148;
        private System.Windows.Forms.Panel panel149;
        private System.Windows.Forms.Panel panel150;
        private System.Windows.Forms.Panel panel151;
        private System.Windows.Forms.Panel panel152;
        private System.Windows.Forms.Panel panel153;
        private System.Windows.Forms.Panel panel154;
        private System.Windows.Forms.Panel panel155;
        private System.Windows.Forms.Panel panel156;
        private System.Windows.Forms.Panel panel157;
        private System.Windows.Forms.Panel panel158;
        private System.Windows.Forms.Panel panel159;
        private System.Windows.Forms.Panel panel160;
        private System.Windows.Forms.Panel panel161;
        private System.Windows.Forms.Panel panel162;
        private System.Windows.Forms.Panel panel163;
        private System.Windows.Forms.Panel panel164;
        private System.Windows.Forms.Panel panel165;
        private System.Windows.Forms.Panel panel166;
        private System.Windows.Forms.Panel panel167;
        private System.Windows.Forms.Panel panel168;
        private System.Windows.Forms.Panel panel169;
        private System.Windows.Forms.Panel panel170;
        private System.Windows.Forms.Panel panel171;
        private System.Windows.Forms.Panel panel172;
        private System.Windows.Forms.Panel panel173;
        private System.Windows.Forms.Panel panel174;
        private System.Windows.Forms.Panel panel175;
        private System.Windows.Forms.Panel panel176;
        private System.Windows.Forms.Panel panel177;
        private System.Windows.Forms.Panel panel178;
        private System.Windows.Forms.Panel panel179;
        private System.Windows.Forms.Panel panel180;
        private System.Windows.Forms.Panel panel181;
        private System.Windows.Forms.Panel panel182;
        private System.Windows.Forms.Panel panel183;
        private System.Windows.Forms.Panel panel184;
        private System.Windows.Forms.Panel panel185;
        private System.Windows.Forms.Panel panel186;
        private System.Windows.Forms.Panel panel187;
        private System.Windows.Forms.Panel panel188;
        private System.Windows.Forms.Panel panel189;
        private System.Windows.Forms.Panel panel190;
        private System.Windows.Forms.Panel panel191;
        private System.Windows.Forms.Panel panel192;
        private System.Windows.Forms.Panel panel193;
        private System.Windows.Forms.ColumnHeader columnHeader14;
        private System.Windows.Forms.ColumnHeader columnHeader15;
        private System.Windows.Forms.ColumnHeader columnHeader16;
        private System.Windows.Forms.ColumnHeader columnHeader17;
        private System.Windows.Forms.ColumnHeader columnHeader18;
        private System.Windows.Forms.CheckBox checkBoxYarema;
        private System.Windows.Forms.CheckBox checkBoxQayara;
        private System.Windows.Forms.CheckBox checkBoxT1;
        private System.Windows.Forms.CheckBox checkBoxT2;
        private System.Windows.Forms.CheckBox checkBoxT3;
        private System.Windows.Forms.CheckBox checkBoxFdd;
        private System.Windows.Forms.Button button_Calibrate;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Button button_DeleteCurrentImage;
        private System.Windows.Forms.Button button_new;
    }
}

