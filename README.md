# **Tutorial Completo: Criando um Game Launcher em C# (Sem Designer, Código Manual)**

Aqui está o tutorial completo e atualizado com as correções necessárias, incluindo a remoção do arquivo de designer e a renomeação correta do arquivo principal.

---

## **📌 Pré-requisitos**
- **Visual Studio 2022** (Community Edition é gratuita)  
- **.NET 8.0 SDK**  
- **7-Zip** (opcional, para compactar arquivos)  
- **Servidor web** (GitHub Pages, AWS S3, etc.) para hospedar atualizações  

---

## **🚀 Passo 1: Configuração Inicial do Projeto**

### **1. Criando o Projeto**
1. Abra o **Visual Studio**
2. **Novo Projeto** → **Windows Forms App (.NET)**
3. Nomeie como `GameLauncher`

### **2. Removendo o Arquivo de Designer**
1. No **Solution Explorer**, exclua:
   - `Form1.Designer.cs`
   - `Form1.resx`
2. Renomeie `Form1.cs` para `MainForm.cs`

---

## **🎨 Passo 2: Código Completo do Launcher (Sem Designer)**

### **MainForm.cs**
```csharp
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
            SetupUI(); // Configura a interface manualmente
            CheckGameFiles();
        }

        private void SetupUI()
        {
            // Configuração da janela principal
            this.Text = "My Game Launcher";
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Barra de progresso
            _progressBar = new ProgressBar
            {
                Size = new System.Drawing.Size(580, 20),
                Location = new System.Drawing.Point(10, 350),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };
            this.Controls.Add(_progressBar);

            // Label de status
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
                Text = "▶ INICIAR JOGO",
                Size = new System.Drawing.Size(200, 60),
                Location = new System.Drawing.Point(200, 150),
                Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold)
            };
            btnStart.Click += StartGame;
            this.Controls.Add(btnStart);

            // Botão ATUALIZAR
            var btnUpdate = new Button
            {
                Text = "🔄 ATUALIZAR",
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
                _statusLabel.Text = "⚠ Jogo não encontrado!";
        }

        private async void CheckForUpdates(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                _statusLabel.Text = "🔍 Verificando atualizações...";
                _progressBar.Visible = true;
                _progressBar.Value = 0;

                bool updateAvailable = await IsUpdateAvailable();
                if (updateAvailable)
                {
                    await DownloadAndInstallUpdate();
                    _statusLabel.Text = "✅ Atualização concluída!";
                }
                else
                {
                    _statusLabel.Text = "✔ Jogo atualizado!";
                }
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"❌ Erro: {ex.Message}";
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
            _statusLabel.Text = "⬇ Baixando atualização...";

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
                            _statusLabel.Text = $"⬇ Baixando... {progress}%";
                        }
                    }
                }
            }

            _statusLabel.Text = "📦 Extraindo arquivos...";
            _progressBar.Value = 0;
            _progressBar.Style = ProgressBarStyle.Marquee;

            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(TempZip, Directory.GetCurrentDirectory(), true);
                File.Delete(TempZip);
            });

            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = 100;
            _statusLabel.Text = "✔ Atualização instalada!";
        }

        private void StartGame(object sender, EventArgs e)
        {
            if (!File.Exists(GameExe))
            {
                _statusLabel.Text = "❌ Executável não encontrado!";
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
                _statusLabel.Text = $"❌ Falha ao iniciar: {ex.Message}";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
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
```

### **GameLauncher.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>disable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <StartupObject>GameLauncher.Program</StartupObject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
  </ItemGroup>

</Project>
```
### Captura de Tela:
![image](https://github.com/user-attachments/assets/6dd74af2-9f9a-4131-b67d-724eb4fe063c)

---

## **📦 Passo 3: Publicando o Launcher**
### **1. Configuração de Publicação**
1. **Botão direito no projeto → Publicar**  
2. **Escolha "Pasta"**  
3. **Configuração: Release | .NET 8.0**  
4. **Selecione "Produzir um único arquivo"**  

### **2. Compactando (Opcional)**
```sh
7z a -tzip GameLauncher.zip GameLauncher.exe version.txt
```

---

## **🌐 Passo 4: Hospedando Atualizações**
1. Crie `version.txt` (ex: `1.0.0`)  
2. Compacte os arquivos do jogo em `latest.zip`  
3. Suba para um servidor web  
4. Atualize a `UpdateUrl` no código  

---

## **📜 Licença (Creative Commons 4.0)**
Este tutorial está sob **CC BY 4.0**. Você pode usar, modificar e distribuir, desde que dê crédito.  

🔗 **Licença completa:** [https://creativecommons.org/licenses/by/4.0/](https://creativecommons.org/licenses/by/4.0/)  

---

## **🎯 Conclusão**
Agora você tem um **launcher completo** com:
✅ **Atualização automática**  
✅ **Interface limpa sem designer**  
✅ **Código otimizado para .NET 8**  

Próximos passos:
- Adicionar **sistema de login**  
- Implementar **download em partes**  
- Adicionar **notícias integradas**  

Espero que ajude! 🚀
