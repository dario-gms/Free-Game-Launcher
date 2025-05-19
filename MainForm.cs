using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameLauncher
{
    public class MainForm : Form
    {
        // Configurações
        private const string GameExe = "MyGame.exe";
        private const string VersionFile = "version.txt";
        private const string UpdateUrl = "https://yourwebsite.com/update/latest.zip";
        private readonly string TempZip = Path.Combine(Path.GetTempPath(), "game_update.zip");

        private readonly HttpClient _httpClient = new HttpClient();
        private bool _isUpdating = false;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        public MainForm()
        {
            // Inicializa os componentes manualmente (substitui InitializeComponent)
            SetupUI();
            CheckGameFiles();
        }

        private void SetupUI()
        {
            // Configuração da janela principal
            this.Text = "My Game Launcher";
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Inicializa a barra de progresso
            _progressBar = new ProgressBar
            {
                Size = new System.Drawing.Size(580, 20),
                Location = new System.Drawing.Point(10, 350),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };
            this.Controls.Add(_progressBar);

            // Inicializa o label de status
            _statusLabel = new Label
            {
                Text = "Pronto para jogar!",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true,
                Font = new System.Drawing.Font("Arial", 10)
            };
            this.Controls.Add(_statusLabel);

            // Botão INICIAR
            var btnStart = new Button
            {
                Text = "INICIAR JOGO",
                Size = new System.Drawing.Size(200, 60),
                Location = new System.Drawing.Point(200, 150),
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold)
            };
            btnStart.Click += StartGame;
            this.Controls.Add(btnStart);

            // Botão ATUALIZAR
            var btnUpdate = new Button
            {
                Text = "ATUALIZAR",
                Size = new System.Drawing.Size(200, 60),
                Location = new System.Drawing.Point(200, 220),
                Font = new System.Drawing.Font("Arial", 12)
            };
            btnUpdate.Click += CheckForUpdates;
            this.Controls.Add(btnUpdate);
        }

        private void CheckGameFiles()
        {
            if (!File.Exists(GameExe))
                _statusLabel.Text = "Jogo não encontrado!";
        }

        private async void CheckForUpdates(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                _statusLabel.Text = "Verificando atualizações...";
                _progressBar.Visible = true;
                _progressBar.Value = 0;

                bool updateAvailable = await IsUpdateAvailable();
                if (updateAvailable)
                {
                    await DownloadAndInstallUpdate();
                    _statusLabel.Text = "Atualização concluída!";
                }
                else
                {
                    _statusLabel.Text = "Jogo atualizado!";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Erro: {ex.Message}";
            }
            finally
            {
                _isUpdating = false;
                _progressBar.Visible = false;
            }
        }

        private async Task<bool> IsUpdateAvailable()
        {
            try
            {
                if (!File.Exists(VersionFile)) return true;

                string localVersion = await File.ReadAllTextAsync(VersionFile);
                string latestVersion = await _httpClient.GetStringAsync(UpdateUrl.Replace(".zip", "/version.txt"));

                return latestVersion != localVersion;
            }
            catch
            {
                return false;
            }
        }

        private async Task DownloadAndInstallUpdate()
        {
            _statusLabel.Text = "Baixando atualização...";

            using (var response = await _httpClient.GetAsync(UpdateUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(TempZip))
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    var bytesRead = 0;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var progress = (int)(totalRead * 100 / totalBytes);
                            _progressBar.Value = progress;
                            _statusLabel.Text = $"Baixando... {progress}%";
                        }
                    }
                }
            }

            _statusLabel.Text = "Extraindo arquivos...";
            _progressBar.Value = 0;
            _progressBar.Style = ProgressBarStyle.Marquee;

            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(TempZip, Directory.GetCurrentDirectory(), true);
                File.Delete(TempZip);
            });

            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = 100;
            _statusLabel.Text = "Atualização instalada!";
        }

        private void StartGame(object sender, EventArgs e)
        {
            if (!File.Exists(GameExe))
            {
                _statusLabel.Text = "Executável não encontrado!";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GameExe,
                    UseShellExecute = true
                });
                this.Close();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Falha ao iniciar: {ex.Message}";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
                // Não há 'components' para descartar pois não usamos o designer
            }
            base.Dispose(disposing);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}