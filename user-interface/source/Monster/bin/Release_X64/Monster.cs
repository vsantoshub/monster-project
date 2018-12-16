using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Timers;

namespace WindowsFormsApplication3
{


    public partial class TelaBusca : Form
    {
        private const double c_radio_vref = 3.3;
        private const double c_fundo_escala_adc_radio = 1024.0;
        private volatile bool _kill_thr_aquisicao = false; //a volatile flag to signal to the other thread to stop
        private volatile bool problema_porta = false;
        string[] radios_registrados = new string[] { "4078CB0F", "40840144", "4078D792", "4078D78B", "4078CB1D" }; //SL de cada radio
        public int radios_conectados = 0; //numero de radios CONECTADOS, posso ter varios radios REGISTRADOS mas só alguns conectados
        string[] descricao_radio = new string[] { "Mão 1", "Pé 1", "Mão 2", "Pé 2", "Confortímetro" };
        TextBox[] addr;
        TextBox[] rssi;
        Label lb_addr;
        Label lb_gravacao;
        public bool estado_buscando = false;
        public bool estado_gravacao = false;
        Thread thr_busca;
        Thread thr_aquisicao;
        ComboBox listaCOM;
        ComboBox listaGravacao;
        Label lb_COM, label_buscando;
        Button serOpen, serClose, quit, btn_avancar, btn_log;
        ProgressBar ser_progressBar, pB_Buscando;
        SerialPort serialMestre = new SerialPort();
        System.Windows.Forms.Timer timer_blink_busca;
        System.Windows.Forms.Timer timer_stop_nodeDiscovery;
        System.Windows.Forms.Timer timer_log_arquivo;
        System.Windows.Forms.Timer timer_espera_avancar;
        int flag_thread_busca = 0;
        int flag_thread_aquisicao = 0;
        public bool stop_nodeDiscovery = false;
        Label[,] lb_adc;
        TextBox[,] adc_value;
        Label[] adc_addr;
        //constantes double usadas no processo de calibração dos sensores
        double[,] a = new double[5, 4];
        double[,] b = new double[5, 4];
        double[,] c = new double[5, 4];
        double[,] d = new double[5, 4];
        double[,] e = new double[5, 4];
        double[,] valor_calibrado = new double[5, 4];
        double[,] media_amostras = new double[5, 4]; //integral das amostras
        public ulong contador_amostras_validas = 0;

        /* variaveis utilizadas em gravacao */
        public string separador = ";"; // separados entre parametros para ser usado no arquivo salvo (; por causa da compatibilidade com o Excel )
        public string header_gravacao = "Data (dd/mm/aaaa); Tempo (HH:mm:ss); Indice; Mao1_T1 (°C);Mao1_T2 (°C);Mao1_T3 (°C);Mao1_T4 (°C);Pe1_T1 (°C);Pe1_T2 (°C);Pe1_Umidade (%); Pe1_Vazio;Mao2_T1 (°C);Mao2_T2 (°C);Mao2_T3 (°C);Mao2_T4 (°C);Pe2_T1 (°C);Pe2_T2 (°C);Pe2_Umidade (%); Pe2_Vazio; Conf_Tar(°C); Conf_TGlobo (°C); Conf_Veloc.Ar (m/s); Conf_UR (%)";
        public string caminho_e_nome, nome_arquivo; //relativos ao nome do arquivo gravado
        StreamWriter log_arquivo; // cria uma classe com esse nome
        
        //public int periodo_gravacao = 300000; //intervalo de gravacao em arquivo: 300k ms = 5 minutos
        public int periodo_gravacao = 60000; //intervalo de gravacao em arquivo: 60k ms = 1 minuto
        public int periodo_descoberta = 60000; //60k ms = 1m
        public int delay_avancar = 3; //3k ms = 3s
        int multiplicador_gravacao = 0;
        ulong contador_gravacoes = 0;

        private static int multiplicador_local = 0;

        public TelaBusca()
        {
            InitializeComponent();
            this.Text = "LMPT UFSC - MONSTER: Monitor de Stress Térmico";
            this.FormClosing += Form1_FormClosing;//register event - usado para o thread
            //this.Icon = new Icon("../../xbeecell-icon-certified.ico");

            this.MinimumSize = new Size(820, 1000);
            this.MaximumSize = new Size(820, 1000);
            /* inicializacao das constantes de calibracao */
            // Mod 1 --- Mao 1 --------------------
            // T dedo
            a[0, 0] = 2.337752;
            b[0, 0] = -10.797279;
            c[0, 0] = 17.532968;
            d[0, 0] = 20.391128;
            e[0, 0] = -25.240525;


            // T mao
            a[0, 1] = 2.337752;
            b[0, 1] = -10.797279;
            c[0, 1] = 17.532968;
            d[0, 1] = 20.391128;
            e[0, 1] = -25.240525;

            // T costas
            a[0, 2] = 2.337752;
            b[0, 2] = -10.797279;
            c[0, 2] = 17.532968;
            d[0, 2] = 20.391128;
            e[0, 2] = -25.240525;

            // T ouvido
            a[0, 3] = 0.269183601;
            b[0, 3] = 2.81814575;
            c[0, 3] = -15.255214;
            d[0, 3] = 54.596192;
            e[0, 3] = -38.271522 + 0.15;

            // Mod 2  --- Pé 1  -----------------------
            // T dedo
            a[1, 0] = 2.337752;
            b[1, 0] = -10.797279;
            c[1, 0] = 17.532968;
            d[1, 0] = 20.391128;
            e[1, 0] = -25.240525 + 0.16;

            // T pe
            a[1, 1] = 2.337752;
            b[1, 1] = -10.797279;
            c[1, 1] = 17.532968;
            d[1, 1] = 20.391128;
            e[1, 1] = -25.240525 + 0.27;

            // Umidade
            a[1, 2] = 0;
            b[1, 2] = 0;
            c[1, 2] = 0;
            d[1, 2] = 1;
            e[1, 2] = 0;

            // Canal 4
            a[1, 3] = 0;
            b[1, 3] = 0;
            c[1, 3] = 0;
            d[1, 3] = 1;
            e[1, 3] = 0;

            // Mod 3 --- Mao 2 -------------------

            // T dedo
            a[2, 0] = 2.337752;
            b[2, 0] = -10.797279;
            c[2, 0] = 17.532968;
            d[2, 0] = 20.391128;
            e[2, 0] = -25.240525;


            // T mao
            a[2, 1] = 2.337752;
            b[2, 1] = -10.797279;
            c[2, 1] = 17.532968;
            d[2, 1] = 20.391128;
            e[2, 1] = -25.240525 - 0.14;

            // T costas
            a[2, 2] = 2.337752;
            b[2, 2] = -10.797279;
            c[2, 2] = 17.532968;
            d[2, 2] = 20.391128;
            e[2, 2] = -25.240525 + 0.06;

            // T ouvido
            a[2, 3] = 0.269183601;
            b[2, 3] = 2.81814575;
            c[2, 3] = -15.255214;
            d[2, 3] = 54.596192;
            e[2, 3] = -38.271522;

            // Mod 4  --- Pé 2  -----------------------
            // T dedo
            a[3, 0] = 2.337752;
            b[3, 0] = -10.797279;
            c[3, 0] = 17.532968;
            d[3, 0] = 20.391128;
            e[3, 0] = -25.240525 + 0.16;

            // T pe
            a[3, 1] = 2.337752;
            b[3, 1] = -10.797279;
            c[3, 1] = 17.532968;
            d[3, 1] = 20.391128;
            e[3, 1] = -25.240525 - 0.14;

            // Umidade
            a[3, 2] = 0;
            b[3, 2] = 0;
            c[3, 2] = 0;
            d[3, 2] = 1;
            e[3, 2] = 0;

            // Canal 4
            a[3, 3] = 0;
            b[3, 3] = 0;
            c[3, 3] = 0;
            d[3, 3] = 1;
            e[3, 3] = 0;

            //inicialização das constantes de calibração
            // Mod 5 = Confortimetro

            //Termistor 10K,  Vref=3.3
            // Tar
            a[4, 0] = 2.337752;
            b[4, 0] = -10.797279;
            c[4, 0] = 17.532968;
            d[4, 0] = 20.391128;
            e[4, 0] = -25.240525;

            // T globo
            a[4, 1] = 2.337752;
            b[4, 1] = -10.797279;
            c[4, 1] = 17.532968;
            d[4, 1] = 20.391128;
            e[4, 1] = -25.240525;

            // T quente
            a[4, 2] = 2.337752;
            b[4, 2] = -10.797279;
            c[4, 2] = 17.532968;
            d[4, 2] = 20.391128;
            e[4, 2] = -25.240525;

            // UR
            a[4, 3] = 0;
            b[4, 3] = 0;
            c[4, 3] = 0;
            d[4, 3] = 47.64627406;
            e[4, 3] = -23.82075472;
            /******************************************/

            //Cria a interface
            lb_COM = new Label();
            lb_COM.Visible = true;
            lb_COM.Text = "Selecione a porta COM:";
            lb_COM.Location = new Point(50, 15);
            lb_COM.AutoSize = true;
            this.Controls.Add(lb_COM);

            listaCOM = new ComboBox();
            listaCOM.Visible = true;
            listaCOM.Text = "";
            listaCOM.Location = new Point(50, 50);
            listaCOM.DropDownStyle = ComboBoxStyle.DropDownList;
            listaCOM.BackColor = System.Drawing.Color.White;
            this.Controls.Add(listaCOM);

            ser_progressBar = new ProgressBar();
            ser_progressBar.Location = new System.Drawing.Point(370, 10);
            ser_progressBar.Margin = new System.Windows.Forms.Padding(2);
            ser_progressBar.Name = "progressBar";
            ser_progressBar.Size = new System.Drawing.Size(145, 75);
            ser_progressBar.TabIndex = 6;
            this.Controls.Add(ser_progressBar);

            // 
            // serOpen
            // 
            serOpen = new Button();
            serOpen.Location = new System.Drawing.Point(210, 10);
            serOpen.Margin = new System.Windows.Forms.Padding(2);
            serOpen.Name = "serOpen";
            serOpen.Size = new System.Drawing.Size(145, 75);
            serOpen.TabIndex = 4;
            serOpen.Text = "Open";
            serOpen.UseVisualStyleBackColor = true;
            serOpen.Click += new System.EventHandler(this.serOpen_Click);
            this.Controls.Add(serOpen);

            // 
            // quit
            // 
            quit = new Button();
            quit.Location = new System.Drawing.Point(675, 8);
            quit.Margin = new System.Windows.Forms.Padding(2);
            quit.Name = "quit";
            quit.Size = new System.Drawing.Size(105, 75);
            quit.TabIndex = 8;
            quit.Text = "Quit";
            quit.UseVisualStyleBackColor = true;
            quit.Click += new System.EventHandler(this.quit_Click);
            this.Controls.Add(quit);

            // 
            // btn_log
            // 
            btn_log = new Button();
            btn_log.Location = new System.Drawing.Point(536, 55);
            btn_log.Margin = new System.Windows.Forms.Padding(2);
            btn_log.Name = "quit";
            btn_log.Size = new System.Drawing.Size(120, 30);
            btn_log.TabIndex = 8;
            btn_log.Text = "Gravar";
            btn_log.Visible = true;
            btn_log.Enabled = false;
            btn_log.UseVisualStyleBackColor = true;
            btn_log.Click += new System.EventHandler(this.btn_log_Click);
            this.Controls.Add(btn_log);

            lb_gravacao = new Label();
            lb_gravacao.Visible = true;
            lb_gravacao.Text = "Intervalo de Gravação:";
            lb_gravacao.Location = new Point(536, 8);
            lb_gravacao.AutoSize = true;
            this.Controls.Add(lb_gravacao);

            // 
            // listaGravacao
            // 
            listaGravacao = new ComboBox();
            listaGravacao.Visible = true;
            listaGravacao.Text = "";
            listaGravacao.Location = new Point(536, 25);
            listaGravacao.Items.Add("5 min");
            listaGravacao.Items.Add("10 min");
            listaGravacao.Items.Add("30 min");
            listaGravacao.SelectedIndex = 0;
            listaGravacao.DropDownStyle = ComboBoxStyle.DropDownList;
            listaGravacao.BackColor = System.Drawing.Color.White;
            listaGravacao.SelectedIndexChanged += new System.EventHandler(listaGravacao_SelectedIndexChanged);
            multiplicador_gravacao = 5; //5 minutos por default
            this.Controls.Add(listaGravacao);

            // 
            // timer_blink_busca
            // 
            timer_blink_busca = new System.Windows.Forms.Timer();
            timer_blink_busca.Interval = 250; //250ms
            timer_blink_busca.Tick += new EventHandler(timer_blink_busca_Tick);

            // 
            // timer_stop_nodeDiscovery
            // 
            timer_stop_nodeDiscovery = new System.Windows.Forms.Timer();
            timer_stop_nodeDiscovery.Interval = periodo_descoberta; 
            timer_stop_nodeDiscovery.Tick += new EventHandler(timer_stop_nodeDiscovery_Tick);

            // 
            // timer_log_arquivo
            // 
            timer_log_arquivo = new System.Windows.Forms.Timer();
            timer_log_arquivo.Interval = periodo_gravacao; //1 minuto multiplicado pelos contadores da comboBox
            timer_log_arquivo.Tick += new EventHandler(timer_gravacao_Tick);
            // 
            // timer_espera_avancar
            // 
            timer_espera_avancar = new System.Windows.Forms.Timer();
            timer_espera_avancar.Interval = 1000; //1s
            timer_espera_avancar.Tick += new EventHandler(timer_avancar_Tick);

            // 
            // btn_avancar
            // 
            btn_avancar = new Button();
            btn_avancar.Name = "btn_avancar";
            btn_avancar.Size = new System.Drawing.Size(145, 41);
            btn_avancar.TabIndex = 64;
            btn_avancar.Text = "Avançar";
            btn_avancar.UseVisualStyleBackColor = true;

            btn_avancar.Width = 100;
            btn_avancar.Height = 20;
            btn_avancar.Left = (this.ClientSize.Width - btn_avancar.Width) / 2;
            btn_avancar.Top = (this.ClientSize.Height - 50);
            this.Controls.Add(btn_avancar);
            btn_avancar.Anchor = AnchorStyles.None;
            btn_avancar.Click += new System.EventHandler(this.btn_avancar_Click);
            
            // 
            // label_buscando
            // 
            label_buscando = new Label();
            label_buscando.AutoSize = true;
            label_buscando.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label_buscando.Width = 439;
            label_buscando.Height = 26;
            label_buscando.Left = (this.ClientSize.Width - label_buscando.Width) / 2;
            label_buscando.Top = (this.ClientSize.Height - 850);
            label_buscando.Name = "label_buscando";
            label_buscando.Size = new System.Drawing.Size(label_buscando.Width, label_buscando.Height);
            label_buscando.TabIndex = 60;
            label_buscando.Text = "Procurando pelos rádios registrados na rede";
            label_buscando.TextAlign = ContentAlignment.MiddleCenter;
            label_buscando.Dock = DockStyle.None;
            this.Controls.Add(label_buscando);

            // 
            // pB_Buscando
            // 
            pB_Buscando = new ProgressBar();
            pB_Buscando.Location = new System.Drawing.Point(137, 118);
            pB_Buscando.Name = "pB_Buscando";
            pB_Buscando.Size = new System.Drawing.Size(434, 18);
            pB_Buscando.TabIndex = 65;
            this.Controls.Add(pB_Buscando);

            //comeco com as informacoes escondidas
            serClose.Enabled = false;
            label_buscando.Visible = false;
            pB_Buscando.Visible = false;

            btn_log.Enabled = false;
            btn_avancar.Enabled = false;
            timer_blink_busca.Enabled = false;
            serPortasDisponiveis();
            CultureInfo culture;
            culture = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }


        void serPortasDisponiveis()
        {
            String[] portas = SerialPort.GetPortNames();
            listaCOM.Items.AddRange(portas);

        }

        private void serOpen_Click(object sender, EventArgs e)
        {
            if (listaCOM.SelectedIndex != -1)
            {
                serialMestre.PortName = listaCOM.Text;
                try
                {
                    serialMestre.Open();
                }
                catch
                {
                    problema_porta = true;
                    //sinalizar erro de porta
                }

                if (serialMestre.IsOpen == true)
                {
                    serOpen.Enabled = false;
                    ser_progressBar.Visible = true;
                    for (int counter = 0; counter <= 100; counter++)
                    {
                        ser_progressBar.Value = counter;
                    }

                    serError_lbl.Visible = false;
                    serialMestre.Close();
                    serClose.Enabled = true;
                    thr_busca = new Thread(() => nodeDiscovery_sleep(serialMestre, radios_registrados.Length));
                    thr_busca.Start();

                }
                else
                {
                    serError_lbl.Visible = true;
                }
            }
            else
            {
                listaCOM.Items.Clear();
                serPortasDisponiveis();
            }
        }

        private void btn_avancar_Click(object sender, EventArgs e)
        {
            if (serialMestre.IsOpen == true)
            {
                serialMestre.Close();
            }

            try {
                thr_busca.Abort();
                flag_thread_busca = 0;
            } //mata a tread de busca quando inicia a thread de aquisicao
            finally
            {
                this.label_buscando.Visible = false;
                //lb_rssi.Visible = false;
                //lb_addr.Visible = false;
                //this.Controls.Remove(lb_rssi); //todo: remover os demais objetos criados deste jeito
                this.Controls.Remove(lb_addr);
                for (int i = 0; i < radios_registrados.Length; ++i)
                {
                    //this.Controls.Remove(rssi[i]);
                    this.Controls.Remove(addr[i]);
                    //addr[i].Visible = false;
                    //rssi[i].Visible = false;
                }
                label_buscando.Text = "Procurando pelos rádios registrados na rede";
                thr_aquisicao = new Thread(() => rx_analogSamples(serialMestre, radios_registrados.Length));
                thr_aquisicao.Start();
                btn_avancar.Enabled = false;
                btn_avancar.Visible = false;
                btn_log.Enabled = true;
                btn_log.Visible = true;
            }
        }

        private void Inicializa_Log(string path)
        {

            DateTime time_struct = DateTime.Now;
            int dia = time_struct.Day;
            int mes = time_struct.Month;
            int ano = time_struct.Year;
            int hora = time_struct.Hour;
            int min = time_struct.Minute;
            nome_arquivo = "LMPT_UFSC_Frigorifico-" + dia.ToString() + "-" + mes.ToString() + "-" + ano.ToString() + "  " + hora.ToString() + "_" + min.ToString();
            caminho_e_nome = Path.Combine(path, nome_arquivo + ".txt"); // vai escrever no diretório escolhido pelo usuário
            log_arquivo = File.CreateText(caminho_e_nome); // se quer escrever por cima de arq existente
            log_arquivo.WriteLine(header_gravacao);
            log_arquivo.Close(); // tem que fechar senão dá erro qdo tentar escrever de novo

        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private void listaGravacao_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (listaGravacao.SelectedIndex != -1)
            {
                switch (listaGravacao.SelectedIndex) {
                    case 0:
                        multiplicador_gravacao = 5;
                        Console.WriteLine("ComboBox Gravacao");
                        Console.WriteLine(multiplicador_gravacao.ToString());
                        break;

                    case 1:
                        multiplicador_gravacao = 10;
                        Console.WriteLine("ComboBox Gravacao");
                        Console.WriteLine(multiplicador_gravacao.ToString());
                        break;

                    case 2:
                        multiplicador_gravacao = 30;
                        Console.WriteLine("ComboBox Gravacao");
                        Console.WriteLine(multiplicador_gravacao.ToString());
                        break;

                    default:
                        multiplicador_gravacao = 5;
                        Console.WriteLine("ComboBox Gravacao");
                        Console.WriteLine(multiplicador_gravacao.ToString());
                        break;
                }
            }
            else
            { //Value is null }
                multiplicador_gravacao = 1; //300k ms = 5 min
                Console.WriteLine("ComboBox Gravacao");
                Console.WriteLine(multiplicador_gravacao.ToString());
            }
        }

        public void nodeDiscovery_sleep(SerialPort xbee_serial, int n_nodes)
        {
            flag_thread_busca = 1;

            int eixo_y = 200;
            addr = new TextBox[radios_registrados.Length];
            rssi = new TextBox[radios_registrados.Length];
            StringBuilder sb_AT_Recv = new StringBuilder();
            List<string> radio_serial_low = new List<string>();
            List<string> pacotes_validos = new List<string>();
            int rx_counter = 0;
            byte[] rx_samples;
            int checksum = 0;
            int chk_sum = 0; //soma dos dados do checksum
            int num_radios = n_nodes;
            List<string> temp_radiosRegistrados = new List<string>(radios_registrados);

            string porta_aberta = xbee_serial.PortName; // pega o nome da porta já aberta, recebido como parametro de função 

            //label de endereços
            lb_addr = new Label();
            lb_addr.Visible = true;
            lb_addr.AutoSize = true;
            lb_addr.Text = "Endereços dos rádios registrados:";
            lb_addr.Location = new Point(45, 150);
            lb_addr.ForeColor = Color.Black;
            this.Invoke((MethodInvoker)delegate
            {
                this.Controls.Add(lb_addr);
            });

            //inicia a montagem da interface com o usuario, para mostrar os endereços e a potencia de cada radio
            for (int i = 0; i < radios_registrados.Length; ++i)
            {
                addr[i] = new TextBox();
                addr[i].Name = "tb_addr" + i.ToString();
                addr[i].Location = new Point(45, eixo_y);
                addr[i].Text = radios_registrados[i];
                addr[i].Visible = true;
                addr[i].ReadOnly = true;
                eixo_y = eixo_y + 65;
                addr[i].TextAlign = HorizontalAlignment.Center;
                addr[i].AutoSize = true;

                this.Invoke((MethodInvoker)delegate
                {
                    this.Controls.Add(addr[i]);
                });

            }

            this.Invoke((MethodInvoker)delegate
            {
                timer_blink_busca.Start();
                timer_stop_nodeDiscovery.Start();
            });

            serialMestre = new SerialPort(porta_aberta, 9600, Parity.None, 8, StopBits.One); //padrão da serial 9600-8-N-1
            if (serialMestre.IsOpen) serialMestre.Close();
            serialMestre.Open();

            while (num_radios > 0 && stop_nodeDiscovery == false) //tratar as condições para stop_nodeDiscovery = true
            {
                if (serialMestre.IsOpen == true)
                {
                    if (serialMestre.BytesToRead > 0)
                    {
                        String indata = serialMestre.ReadByte().ToString("X2"); //mostra a string em hexadecimal


                        if (rx_counter > 0)
                        {
                            sb_AT_Recv.Append(indata);
                            rx_counter = rx_counter + 1;

                            if (rx_counter == 32) //tamanho do frame de retorno do IO Data Sample RX Indicator
                            {
                                Console.WriteLine("ATND: HEX STRING: ");
                                Console.Write(sb_AT_Recv);
                                rx_counter = 0;
                                rx_samples = StringToByteArray(sb_AT_Recv.ToString());
                                Console.WriteLine("");
                                Console.WriteLine("ATND: HEX BYTE ARRAY: ");
                                string hex = BitConverter.ToString(rx_samples);
                                Console.Write(hex);

                                //faz o calculo do checksum
                                chk_sum = 0; //zera a soma usada para calcular o checksum, o cálculo é feito do terceiro byte até o penúltimo
                                for (int i = 3; i < (rx_samples.Length) - 1; i = i + 1)
                                {
                                    chk_sum = chk_sum + rx_samples[i];
                                }
                                chk_sum = chk_sum & 0xFF;
                                checksum = 0xFF - chk_sum;

                                Console.WriteLine("");
                                Console.WriteLine("IO Data: Tamanho Byte array: {0}", rx_samples.Length);


                                //valida o checksum
                                if (checksum == rx_samples[rx_samples.Length - 1])
                                {

                                    Console.WriteLine("IO Data: Pacote Integro");

                                    pacotes_validos.Add(sb_AT_Recv.ToString());
                                    radio_serial_low.Add(sb_AT_Recv.ToString().Substring(16, 8));
                                    Console.WriteLine("");
                                    Console.WriteLine("IO Data: Low Serial Number Adicionado:");
                                    Console.WriteLine(radio_serial_low[radio_serial_low.Count - 1]);

                                }

                                checksum = 0;

                                //limpa a string de dados recebidos
                                sb_AT_Recv.Clear();

                                //itera o vetor de radios registrados para ver se o SL atual encontra-se neste vetor
                                //melhorar essa busca
                                for (int i = 0; i < temp_radiosRegistrados.Count; i++)
                                {
                                    for (int j = 0; j < radio_serial_low.Count; j++)
                                    {
                                        if (temp_radiosRegistrados[i] == radio_serial_low[j])
                                        {
                                            Console.WriteLine("IO Data: Achou: ");
                                            Console.WriteLine(temp_radiosRegistrados[i]);
                                            var indice = Array.FindIndex(radios_registrados, row => row.Contains(temp_radiosRegistrados[i]));
                                            Console.WriteLine("IO Data: Achou 2 : ");
                                            Console.WriteLine(radios_registrados[indice]);
                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                addr[indice].BackColor = Color.Lime; // runs on UI thread
                                                                                            });
                                            radio_serial_low.RemoveAt(j);
                                            temp_radiosRegistrados.RemoveAt(i);
                                            num_radios = num_radios - 1;
                                            radios_conectados = radios_conectados + 1;
                                        }
                                    }
                                }
                                radio_serial_low.Clear(); //limpa em caso de adicionar o mesmo endereço duas vezes

                            }
                        }
                        //7E byte de inicio do frame API do radio
                        if (String.Equals(indata, "7E"))
                        {
                            rx_counter = 1;
                            sb_AT_Recv.Clear();
                            sb_AT_Recv.Append(indata);
                        }

                    }

                }
                else
                {
                    problema_porta = true;
                }

            }
            if (num_radios == 0 || stop_nodeDiscovery == true) { 
            
            this.Invoke((MethodInvoker)delegate
            {
                timer_blink_busca.Stop();
                timer_stop_nodeDiscovery.Stop();
                label_buscando.Visible = true;
                label_buscando.Text = "Rádios Encontrados";
                label_buscando.Left = (this.ClientSize.Width - label_buscando.Width) / 2;
                label_buscando.Top = (this.ClientSize.Height - 850);
                label_buscando.TextAlign = ContentAlignment.MiddleCenter;
                label_buscando.Dock = DockStyle.None;
                
                timer_espera_avancar.Start(); // runs on UI thread              
            });
            
            }
        }

        public void rx_analogSamples(SerialPort xbee_serial, int n_nodes)
        {
            flag_thread_aquisicao = 1;
            StringBuilder sb_AT_Recv = new StringBuilder();
            string radio_serial_low = "";
            List<string> pacotes_validos = new List<string>();
            int rx_counter = 0;
            byte[] rx_samples;
            int checksum = 0;
            int chk_sum = 0; //soma dos dados do checksum
            int num_radios = n_nodes;

            string porta_aberta = xbee_serial.PortName; // pega o nome da porta já aberta, recebido como parametro de função 

            lb_adc = new Label[radios_registrados.Length, 4];
            adc_value = new TextBox[radios_registrados.Length, 4];
            adc_addr = new Label[radios_registrados.Length];

            int eixo_x = 45;
            int eixo_y = 150; //eixo y

            //inicia a montagem da interface com o usuario, para mostrar os endereços e as amostras
            for (int i = 0; i < radios_registrados.Length; i++)
            {
                if (i > 0)
                {
                    if (i % 2 == 0) //nova linha
                    {
                        eixo_x = 45;
                        eixo_y = eixo_y + 50;
                    }
                    else
                    {  //nova coluna
                        eixo_x = 295;
                        eixo_y = eixo_y - 180;
                    }
                }

                //label de endereços
                adc_addr[i] = new Label();
                adc_addr[i].Visible = true;
                Console.WriteLine("IO: valor de i: ");
                Console.Write(i.ToString());
                Console.WriteLine("");

                //adc_addr[i].Text = "Rádio " + i.ToString() + ":" + radios_registrados[i];
                adc_addr[i].AutoSize = true;
                adc_addr[i].Text = descricao_radio[i] + ":" + radios_registrados[i];
                adc_addr[i].Location = new Point(eixo_x, eixo_y - 30); //120
                adc_addr[i].ForeColor = Color.Black;

                this.Invoke((MethodInvoker)delegate
                {
                    this.Controls.Add(adc_addr[i]);
                });
                Console.WriteLine("IO: RADIOS REGISTRADOS: ");
                Console.Write(radios_registrados[i]);
                Console.WriteLine("");

                for (int j = 0; j < 4; j++)
                {
                    lb_adc[i, j] = new Label();
                    lb_adc[i, j].Name = "lb_adc_" + radios_registrados[i] + "_" + j.ToString();
                    lb_adc[i, j].Location = new Point(eixo_x, eixo_y);
                    lb_adc[i, j].Text = "ADC" + j.ToString();
                    lb_adc[i, j].Visible = true;
                    lb_adc[i, j].AutoSize = true;

                    adc_value[i, j] = new TextBox();
                    adc_value[i, j].Name = "tb_adc_" + radios_registrados[i] + "_" + j.ToString();
                    adc_value[i, j].Location = new Point(eixo_x + 45, eixo_y);
                    adc_value[i, j].Text = "";
                    adc_value[i,j].TextAlign = HorizontalAlignment.Center;
                    adc_value[i, j].Visible = true;
                    adc_value[i, j].AutoSize = true;
                    adc_value[i, j].ReadOnly = true;
                    adc_value[i, j].BackColor = Color.White;
                    adc_value[i, j].ForeColor = Color.Black;
                    eixo_y = eixo_y + 45;

                    this.Invoke((MethodInvoker)delegate
                    {
                        this.Controls.Add(lb_adc[i, j]);
                        this.Controls.Add(adc_value[i, j]);
                    });

                }


            }

            serialMestre = new SerialPort(porta_aberta, 9600, Parity.None, 8, StopBits.One); //padrão da serial 9600-8-N-1
            if (serialMestre.IsOpen) serialMestre.Close();
            serialMestre.Open();


            while (!_kill_thr_aquisicao)
            {
                if (serialMestre.IsOpen == true)
                {
                    if (serialMestre.BytesToRead > 0)
                    {
                        String indata = serialMestre.ReadByte().ToString("X2"); //mostra a string em hexadecimal

                        if (rx_counter > 0)
                        {
                            sb_AT_Recv.Append(indata);
                            rx_counter = rx_counter + 1;

                            if (rx_counter == 32) //tamanho do frame de retorno do ATND
                            {
                                Console.WriteLine("IO_SAMPLE: HEX STRING: ");
                                Console.Write(sb_AT_Recv);
                                rx_counter = 0;
                                rx_samples = StringToByteArray(sb_AT_Recv.ToString());
                                Console.WriteLine("");
                                Console.WriteLine("IO_SAMPLE: HEX BYTE ARRAY: ");
                                string hex = BitConverter.ToString(rx_samples);
                                Console.Write(hex);

                                //faz o calculo do checksum
                                chk_sum = 0; //zera a soma usada para calcular o checksum, o cálculo é feito do terceiro byte até o penúltimo
                                for (int i = 3; i < (rx_samples.Length) - 1; i = i + 1)
                                {
                                    chk_sum = chk_sum + rx_samples[i];
                                }
                                chk_sum = chk_sum & 0xFF;
                                checksum = 0xFF - chk_sum;

                                Console.WriteLine("");
                                Console.WriteLine("IO_SAMPLE: Tamanho Byte array: {0}", rx_samples.Length);


                                //valida o checksum
                                if (checksum == rx_samples[rx_samples.Length - 1])
                                {

                                    Console.WriteLine("IO_SAMPLE: Pacote Integro");

                                    pacotes_validos.Add(sb_AT_Recv.ToString());
                                    radio_serial_low = sb_AT_Recv.ToString().Substring(16, 8);
                                    Console.WriteLine("");
                                    Console.WriteLine("IO_SAMPLE: Low Serial Number Adicionado:");
                                    Console.WriteLine(radio_serial_low);

                                    //incremento o contador de amostras somente se o pacote for integro
                                    contador_amostras_validas = contador_amostras_validas + 1;
                                }

                                checksum = 0;

                                //limpa a string de dados recebidos
                                sb_AT_Recv.Clear();
                                int amostra_inicial = 38;
                                //itera o vetor de radios registrados para ver se o SL atual encontra-se neste vetor
                                //melhorar essa busca
                                for (int i = 0; i < radios_registrados.Length; i++)
                                {
                                    int[,] adc_raw = new int[radios_registrados.Length, 4];
                                    double[,] adc_volt = new double[radios_registrados.Length, 4];
                                    if (radios_registrados[i] == radio_serial_low)
                                    {
                                        for (int j = 0; j < 4; j++)
                                        {
                                            adc_raw[i, j] = int.Parse(pacotes_validos[pacotes_validos.Count - 1].ToString().Substring(amostra_inicial, 4), System.Globalization.NumberStyles.HexNumber);
                                            Console.WriteLine("");
                                            Console.WriteLine("IO_SAMPLE: adc_raw_:" + j.ToString());
                                            Console.WriteLine(adc_raw[i, j]);

                                            adc_volt[i, j] = (adc_raw[i, j] * c_radio_vref) / c_fundo_escala_adc_radio;
                                            //adc_volt[i, j] = Math.Round(adc_volt[i, j], 3);
                                            //valor_calibrado = polinomio de calibracao: Ax^4 + Bx^3 + Cx^2 + Dx + E
                                            valor_calibrado[i, j] = a[i, j] * adc_volt[i, j] * adc_volt[i, j] * adc_volt[i, j] * adc_volt[i, j] +
                                          b[i, j] * adc_volt[i, j] * adc_volt[i, j] * adc_volt[i, j] +
                                          c[i, j] * adc_volt[i, j] * adc_volt[i, j] +
                                          d[i, j] * adc_volt[i, j] +
                                          e[i, j];
                                            valor_calibrado[i, j] = Math.Round(valor_calibrado[i, j], 3);
                                            media_amostras[i, j] = media_amostras[i, j] + valor_calibrado[i, j];
                                            Console.WriteLine("");
                                            Console.WriteLine("IO_SAMPLE: adc_volt:" + j.ToString());
                                            Console.WriteLine(adc_volt[i, j]);

                                            Console.WriteLine("");
                                            Console.WriteLine("IO_SAMPLE: valor_calibrado:" + j.ToString());
                                            Console.WriteLine(valor_calibrado[i, j]);

                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                //preencher as coisas
                                                //adc_value[i, j].Text = adc_volt[i, j].ToString();
                                                adc_value[i, j].Text = valor_calibrado[i, j].ToString();

                                            });
                                            amostra_inicial = amostra_inicial + 4;//itera entre as posicoes das amostras dentro do frame de dados
                                        }
                                    }

                                }
                                pacotes_validos.Clear();

                            }
                        }
                        //7E byte de inicio do frame API do radio
                        if (String.Equals(indata, "7E"))
                        {
                            rx_counter = 1;
                            pacotes_validos.Clear();
                            sb_AT_Recv.Clear();
                            sb_AT_Recv.Append(indata);
                        }

                    }

                }
                else
                {
                    problema_porta = true;
                }

            }

        }

        private void btn_log_Click(object sender, EventArgs e)
        {
            string caminho_gravacao = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Lo‌​cation);

            estado_gravacao = !estado_gravacao;

            if (estado_gravacao == true)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    btn_log.Text = "Gravando";
                    btn_log.ForeColor = Color.Green;
                });

                if ((thr_aquisicao.IsAlive.Equals(true) == false)) //testa se a thread de aquisicao está morta, se tiver, start ela
                {
                    thr_aquisicao = new Thread(() => rx_analogSamples(serialMestre, radios_registrados.Length));
                    thr_aquisicao.Start();
                }

                Inicializa_Log(caminho_gravacao);
                timer_log_arquivo.Start();
                
            }
            else
            {
                timer_log_arquivo.Stop();
                this.Invoke((MethodInvoker)delegate
                {
                    btn_log.Text = "Gravar";
                    btn_log.ForeColor = Color.Black;
                });
            }
        }

        private void timer_gravacao_Tick(Object myObject, EventArgs myEventArgs)
        {
            multiplicador_local = multiplicador_local + 1;
            Console.WriteLine("multiplicador_local");
            Console.WriteLine(multiplicador_local.ToString());
            if (multiplicador_local == multiplicador_gravacao) { 

            //ulong contador_local = contador_amostras_validas/ Convert.ToUInt64(radios_registrados.Length);
            ulong contador_local = contador_amostras_validas / Convert.ToUInt64(radios_conectados);
            
            contador_gravacoes = contador_gravacoes + 1;
            ulong indice_gravacao = Convert.ToUInt64((periodo_gravacao/60000))*contador_gravacoes; //obter o tempo (indice de gravacao) em minutos
            log_arquivo = File.AppendText(caminho_e_nome);
           
            if (contador_local > 0)
            {
                for (int i = 0; i < radios_registrados.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("GRAVACAO_ARQUIVO: sensor [" + i.ToString() + "] - media_amostra:" + j.ToString());
                        Console.WriteLine(media_amostras[i, j]);

                        media_amostras[i, j] = media_amostras[i, j] / Convert.ToDouble(contador_local);
                        media_amostras[i, j] =  Math.Round(media_amostras[i, j], 3);
                        Console.WriteLine(media_amostras[i, j]);
                    }
                }
                    //achar uma maneira de automatizar essa linha
                    log_arquivo.WriteLine(DateTime.Now.ToString("dd/MM/yyyy") + separador + DateTime.Now.ToString("HH:mm:ss") + separador + indice_gravacao + separador +
                        media_amostras[0, 0].ToString() + separador + media_amostras[0, 1].ToString() + separador + media_amostras[0, 2].ToString() + separador + media_amostras[0, 3].ToString() + separador +
                        media_amostras[1, 0].ToString() + separador + media_amostras[1, 1].ToString() + separador + media_amostras[1, 2].ToString() + separador + media_amostras[1, 3].ToString() + separador + 
                        media_amostras[2, 0].ToString() + separador + media_amostras[2, 1].ToString() + separador + media_amostras[2, 2].ToString() + separador + media_amostras[2, 3].ToString() + separador + 
                        media_amostras[3, 0].ToString() + separador + media_amostras[3, 1].ToString() + separador + media_amostras[3, 2].ToString() + separador + media_amostras[3, 3].ToString() + separador + 
                        media_amostras[4, 0].ToString() + separador + media_amostras[4, 1].ToString() + separador + media_amostras[4, 2].ToString() + separador + media_amostras[4, 3].ToString()); 

                    contador_amostras_validas = 0;

                for (int i = 0; i < radios_registrados.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        //limpa as medias
                        media_amostras[i, j] = 0;
                    }
                }

            }
            else {
                for (int i = 0; i < radios_registrados.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        media_amostras[i, j] = 0;
                        Console.WriteLine("");
                        Console.WriteLine("GRAVACAO_ARQUIVO_ZERO: sensor [" + i.ToString() + "] - media_amostra:" + j.ToString());
                        Console.WriteLine(media_amostras[i, j]);
                    }
                    log_arquivo.WriteLine(indice_gravacao + separador + media_amostras[i, 0].ToString() + separador + media_amostras[i, 1].ToString() + separador + media_amostras[i, 2].ToString() + separador + media_amostras[i, 3].ToString()); // escreve uma linha e pula

                }
                contador_amostras_validas = 0;
            }
            
            log_arquivo.Close(); // tem que fechar senão dá erro qdo tentar escrever de novo
            multiplicador_local = 0;
        }
        }


        private void quit_Click(object sender, EventArgs e)
        {

            if (flag_thread_busca == 1)
            {
                if (thr_busca.ThreadState.Equals(ThreadState.Running))
                {
                    try { thr_busca.Abort(); } finally { serialMestre.Close(); }
                }
            }

            if (flag_thread_aquisicao == 1)
            {
                // Request that the worker thread stop itself:
                _kill_thr_aquisicao = true;

                // Use the Join method to block the current thread until the object's thread terminates.
                try
                {
                    if ((thr_aquisicao.IsAlive.Equals(true) == true))
                    {
                        thr_aquisicao.Join();
                    }
                }
                finally
                {
                    serialMestre.Close();
                }
            }
            Application.Exit();

        }


        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (flag_thread_busca == 1) { 
            if (thr_busca.ThreadState.Equals(ThreadState.Running))
            {
                try { thr_busca.Abort(); } finally { serialMestre.Close(); }
            }
            }

            if (flag_thread_aquisicao == 1)
            {
                // Request that the worker thread stop itself:
                _kill_thr_aquisicao = true;

                // Use the Join method to block the current thread until the object's thread terminates.
                try
                {
                    if ((thr_aquisicao.IsAlive.Equals(true) == true))
                    {
                        thr_aquisicao.Join();
                    }
                }
                finally
                {
                    serialMestre.Close();
                }
            }
            Application.Exit();
        }

        private void timer_stop_nodeDiscovery_Tick(Object myObject, EventArgs myEventArgs)
        {
            stop_nodeDiscovery = true;
        }

        private void timer_avancar_Tick(Object myObject, EventArgs myEventArgs)
        {
            if (delay_avancar > 0)
            {
                delay_avancar = delay_avancar - 1;
            }
            this.Invoke((MethodInvoker)delegate
            {
                btn_avancar.Text = "Avançar [" + delay_avancar.ToString() + "]";
            });
            
            if (delay_avancar == 0) {
                btn_avancar.Text = "Avançar";
                this.Invoke((MethodInvoker)delegate
                {
                    timer_espera_avancar.Stop(); // runs on UI thread
                    btn_avancar.Enabled = true;
                });
            }
        }

        private void timer_blink_busca_Tick(Object myObject, EventArgs myEventArgs)
        {
            estado_buscando = !estado_buscando;

            this.Invoke((MethodInvoker)delegate
            {
                label_buscando.Visible = estado_buscando;
            });

        }

    }
}

