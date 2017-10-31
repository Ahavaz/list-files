using System;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.IO.Compression;
using System.Windows.Input;

namespace ListaArquivos {
	public partial class MainWindow : Window {

		private String sPath;
		private String fn;
		private String ext;
		private Int64 total;
		private Delimon.Win32.IO.DirectoryInfo folder;
		private String baseDir = AppDomain.CurrentDomain.BaseDirectory;
		private Boolean validDir = true;
		private StringBuilder sb;
		//private StringBuilder zip;
		private StringBuilder rep;

		public MainWindow() => InitializeComponent();

		private void DirSearch(string sDir) {
			try {
				foreach (string f in Delimon.Win32.IO.Directory.GetFiles(sDir)) {
					total++;
				}

				foreach (string d in Delimon.Win32.IO.Directory.GetDirectories(sDir)) {
					DirSearch(d);
				}
			} catch { validDir = true; }
		}

		private void select_Dir(object sender, RoutedEventArgs e) {
			FolderBrowserDialog folderDialog = new FolderBrowserDialog();
			folderDialog.ShowNewFolderButton = false;
			DialogResult result = folderDialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK) {
				System.Windows.Application.Current.Dispatcher.Invoke(() => Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait);
				sPath = folderDialog.SelectedPath;
				textBox.Text = sPath;
				total = 0;
				progressBar.Minimum = 0;
				progressBar.Value = 0;
				folder = new Delimon.Win32.IO.DirectoryInfo(sPath);

				if (folder.Exists) {
					validDir = true;
					DirSearch(sPath);
					System.Windows.Application.Current.Dispatcher.Invoke(() => Mouse.OverrideCursor = null);

					if (total != 0 && validDir) {
						progressBar.Maximum = total;
						button1.IsEnabled = true;
						textBlock1.Text = "Total de arquivos: " + String.Format("{0:n0}", total);
					} else {
						progressBar.Value = 0;
						button1.IsEnabled = false;
						textBox.Text = "";
						textBlock1.Text = "";
						System.Windows.Forms.MessageBox.Show("Favor selecionar um diretório válido.", "Diretório inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void CreateList(string sDir) {
			try {
				folder = new Delimon.Win32.IO.DirectoryInfo(sDir);

				foreach (Delimon.Win32.IO.FileInfo f in folder.GetFiles()) {
					sb.Append($"{Environment.NewLine}{f.Length}\t{f.DirectoryName}\t{f.Name}\t{f.Extension.ToLower()}");
					progressBar.Dispatcher.Invoke(() => progressBar.Value++, DispatcherPriority.Background);

					if (f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
						using(ZipArchive archive = ZipFile.Open(f.FullName, ZipArchiveMode.Read, Encoding.GetEncoding(850))) {

							foreach(ZipArchiveEntry entry in archive.Entries) {

								if(entry.Name != "") {

									if(entry.Name.Contains(".")) {
										ext = entry.Name.Substring(entry.Name.LastIndexOf(".")).ToLower();
									} else {
										ext = "null";
									}
									rep = new StringBuilder(@"\");

									if(entry.FullName.Contains("/")) {
										rep.Append(entry.FullName.Substring(0, entry.FullName.LastIndexOf("/")));
										rep.Replace("/", @"\");
									} else {
										rep.Clear();
									}
									sb.Append($"{Environment.NewLine}{entry.Length}\t{f.FullName}{rep}\t{entry.Name}\t{ext}");

									if(ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
										fn = f.FullName;
										ZipList(fn, entry);
									}
								}
							}
						}
					}
				}

				foreach (string d in Delimon.Win32.IO.Directory.GetDirectories(sDir)) {
					CreateList(d);
				}
			} catch { }
		}

		private void ZipList(string fullPath, ZipArchiveEntry entry) {
			using(ZipArchive a = new ZipArchive(entry.Open())) {
				foreach(ZipArchiveEntry e in a.Entries) {

					if(e.Name != "") {

						if(e.Name.Contains(".")) {
							ext = e.Name.Substring(e.Name.LastIndexOf(".")).ToLower();
						} else {
							ext = "null";
						}
						rep = new StringBuilder(@"\");

						if(e.FullName.Contains("/")) {
							rep.Append($@"{entry.FullName}\{e.FullName.Substring(0, e.FullName.LastIndexOf("/"))}");
							rep.Replace("/", @"\");
						} else {
							rep.Append($"{entry.FullName}");
							rep.Replace("/", @"\");
						}
						sb.Append($"{Environment.NewLine}{e.Length}\t{fullPath}{rep}\t{e.Name}\t{ext}");

						if(ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
							fn = $@"{fullPath}{rep}";
							ZipList(fn, e);
						}
					}
				}
			}
		}

		private void create_List(object sender, RoutedEventArgs e) {
			progressBar.Value = 0;
			button.IsEnabled = false;
			button1.IsEnabled = false;
			sb = new StringBuilder("Tamanho (bytes)\tCaminho\tArquivo\tExtensão");
			//zip = new StringBuilder("Tamanho (bytes)\tCaminho\tArquivo\tExtensão");

			//Stopwatch stopWatch = new Stopwatch();
			//stopWatch.Start();

			CreateList(sPath);
			System.IO.File.WriteAllText($"{baseDir}lista_arquivos.txt", sb.ToString());

			if(System.IO.File.Exists($"{baseDir}lista_arquivos.zip")) System.IO.File.Delete($"{baseDir}lista_arquivos.zip");

			using(ZipArchive archive = ZipFile.Open("lista_arquivos.zip", ZipArchiveMode.Create)) {
				archive.CreateEntryFromFile($"{baseDir}lista_arquivos.txt", "lista_arquivos.txt", CompressionLevel.Fastest);
			}

			if(System.IO.File.Exists($"{baseDir}lista_arquivos.txt")) System.IO.File.Delete($"{baseDir}lista_arquivos.txt");

			//System.IO.File.WriteAllText(baseDir + @"lista_arquivos_zip.txt", zip.ToString());

			//stopWatch.Stop();
			//TimeSpan ts = stopWatch.Elapsed;
			//string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
			//Console.WriteLine("RunTime " + elapsedTime);

			button.IsEnabled = true;
			button1.IsEnabled = true;
			System.Windows.Forms.MessageBox.Show($"O arquivo 'lista_arquivos.zip' foi criado em{Environment.NewLine}{baseDir}", "Arquivos processados com sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
