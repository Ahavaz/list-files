using System;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.IO.Compression;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.Generic;
using Spire.Doc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ListaArquivos {
	public partial class MainWindow : Window {
		
		private String sPath;
		private String fn;
		private String ext;
		private Int64 total;
		private Int64 wordPages;
		private Int64 wordFiles;
		private Int64 pdfPages;
		private Int64 pdfFiles;
		private Delimon.Win32.IO.DirectoryInfo folder;
		private String baseDir = AppDomain.CurrentDomain.BaseDirectory;
		private Boolean validDir = true;
		//private StringBuilder sb;
		//private StringBuilder zip;
		private StringBuilder rep;
		private System.IO.FileStream fs;
		private Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        //private String name;
        private List<String> lista = new List<String>();

		public MainWindow() {
			InitializeComponent();
			//String stringVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			Title = $"i-luminas - Lista Arquivos {version.Major}.{version.Minor}.{version.Build.ToString().Substring(0,1)}";
		}

		private void DirSearch(string sDir) {
			try {
				//total = Delimon.Win32.IO.Directory.GetFiles(sDir, "*", Delimon.Win32.IO.SearchOption.AllDirectories).Length;
				//Console.Write(total);

				foreach (string f in Delimon.Win32.IO.Directory.GetFiles(sDir)) {
					total++;
				}

				foreach (string d in Delimon.Win32.IO.Directory.GetDirectories(sDir)) {
					DirSearch(d);
				}
			} catch {
				//validDir = false;
				//System.Windows.Forms.MessageBox.Show($"{exc.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
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
				textBlock2.Text = "";
				progressBar.Minimum = 0;
				progressBar.Value = 0;
				folder = new Delimon.Win32.IO.DirectoryInfo(sPath);

				if (folder.Exists) {
					validDir = true;

					Stopwatch stopWatch = new Stopwatch();
					stopWatch.Start();

					DirSearch(sPath);

					stopWatch.Stop();
					TimeSpan ts = stopWatch.Elapsed;
					string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
					Console.WriteLine($"DirSearch RunTime {elapsedTime}");
					//System.Windows.Forms.MessageBox.Show(elapsedTime, "DirSearch RunTime", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

		//private String DecodeStream(string s) {
		//	Encoding defEnc = Encoding.Default;
		//	Encoding isoEnc = Encoding.GetEncoding(850);
		//	byte[] defBytes = defEnc.GetBytes(s);
		//	byte[] isoBytes = Encoding.Convert(defEnc, isoEnc, defBytes);
		//	name = isoEnc.GetString(isoBytes);

		//	return name;
		//}

		private void CreateList(string sDir) {
			try {
				folder = new Delimon.Win32.IO.DirectoryInfo(sDir);

				foreach(Delimon.Win32.IO.FileInfo f in folder.GetFiles()) {
                    
                    lista.Add($"{f.Length}\t{f.DirectoryName}\t{f.Name}\t{f.Extension.ToLower()}");
                    //await FileWriteAsync($"{Environment.NewLine}{f.Length}\t{f.DirectoryName}\t{f.Name}\t{f.Extension.ToLower()}");
                    //using (System.IO.StreamWriter w = Delimon.Win32.IO.File.AppendText($"{baseDir}lista_arquivos.txt")) {
                    //    w.Write($"{Environment.NewLine}{f.Length}\t{f.DirectoryName}\t{f.Name}\t{f.Extension.ToLower()}");
                    //    w.Close();
                    //    w.Dispose();
                    //}
                    //sb.Append($"{Environment.NewLine}{f.Length}\t{f.DirectoryName}\t{f.Name}\t{f.Extension.ToLower()}");
                    if(lista.Count >= 100000) {
                        System.IO.File.AppendAllLines($"{baseDir}lista_arquivos.txt", lista);
                        //List<List<String>> l = new List<List<String>>();
                        //l.Add(lista.ToList());
                        lista.Clear();
                        lista.TrimExcess();
                    }

                    progressBar.Dispatcher.Invoke(() => progressBar.Value++, DispatcherPriority.Background);

					if(f.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) {
						using (var stream = Delimon.Win32.IO.File.OpenRead(f.FullName)) {
							using (var ms = new System.IO.MemoryStream()) {
								stream.CopyTo(ms);
								ms.Position = 0;
								CountPdfPages(ms);
							}
						}
					}

					if(f.Extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)) {
						using (var stream = Delimon.Win32.IO.File.OpenRead(f.FullName)) {
							using (var ms = new System.IO.MemoryStream()) {
								stream.CopyTo(ms);
								ms.Position = 0;
								CountWordPages(ms);
							}
						}
					}

					if(f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
						//using (var fs = new System.IO.StreamReader(Delimon.Win32.IO.File.OpenRead(f.FullName), Encoding.GetEncoding(850))) {
						//using(ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read, Encoding.GetEncoding(850))) {

						//ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(fs);
						//System.IO.Stream zipStream = zipFile.GetInputStream(fs);
						fs = Delimon.Win32.IO.File.OpenRead(f.FullName);

						using(var archive = new ZipArchive(fs, ZipArchiveMode.Read, false, Encoding.GetEncoding(850))) {

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

                                    lista.Add($"{entry.Length}\t{f.FullName}{rep}\t{entry.Name}\t{ext}");
                                    //await FileWriteAsync($"{Environment.NewLine}{entry.Length}\t{f.FullName}{rep}\t{entry.Name}\t{ext}");
                                    //using (System.IO.StreamWriter w = Delimon.Win32.IO.File.AppendText($"{baseDir}lista_arquivos.txt")) {
                                    //    w.Write($"{Environment.NewLine}{entry.Length}\t{f.FullName}{rep}\t{entry.Name}\t{ext}");
                                    //    w.Close();
                                    //    w.Dispose();
                                    //}
                                    //sb.Append($"{Environment.NewLine}{entry.Length}\t{f.FullName}{rep}\t{entry.Name}\t{ext}");

                                    if(lista.Count >= 100000) {
                                        System.IO.File.AppendAllLines($"{baseDir}lista_arquivos.txt", lista);
                                        lista.Clear();
                                        lista.TrimExcess();
                                    }

									if(ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) {
										using (var stream = entry.Open()) {
											using (var ms = new System.IO.MemoryStream()) {
												stream.CopyTo(ms);
												ms.Position = 0;
												CountPdfPages(ms);
											}
										}
									}

									if(ext.Equals(".doc", StringComparison.OrdinalIgnoreCase) || ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)) {
										using (var stream = entry.Open()) {
											using (var ms = new System.IO.MemoryStream()) {
												stream.CopyTo(ms);
												ms.Position = 0;
												CountWordPages(ms);
											}
										}
									}

									if(ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
											fn = f.FullName;
											ZipList(fn, entry);
									}
								}
							}
						}
						//}
						//}
					}
				}

				foreach(string d in Delimon.Win32.IO.Directory.GetDirectories(sDir)) {
					CreateList(d);
				}
			} catch {
				//System.Windows.Forms.MessageBox.Show($"{exc.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CountPdfPages(System.IO.MemoryStream stream) {
			try {
				PdfDocument pdf = PdfReader.Open(stream);
				pdfPages += pdf.PageCount;
				pdfFiles++;
			} catch(Exception e) {
				Console.WriteLine($"{e.Message}");
				//System.Windows.Forms.MessageBox.Show($"{e.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			//throw new NotImplementedException();
		}

		private void CountWordPages(System.IO.MemoryStream stream) {
			try {
				Document doc = new Document();
				doc.LoadFromStream(stream, FileFormat.Auto);
				wordPages += doc.PageCount;
				wordFiles++;
			} catch(Exception e) {
				Console.WriteLine($"{e.Message}");
				//System.Windows.Forms.MessageBox.Show($"{e.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			//throw new NotImplementedException();
		}

		private void ZipList(string fullPath, ZipArchiveEntry e) {

			using(var archive = new ZipArchive(e.Open())) {

				foreach(ZipArchiveEntry entry in archive.Entries) {

					if(entry.Name != "") {

						if(entry.Name.Contains(".")) {
							ext = entry.Name.Substring(entry.Name.LastIndexOf(".")).ToLower();
						} else {
							ext = "null";
						}
						rep = new StringBuilder(@"\");

						if(entry.FullName.Contains("/")) {
							rep.Append($@"{e.FullName}\{entry.FullName.Substring(0, entry.FullName.LastIndexOf("/"))}");
							rep.Replace("/", @"\");
						} else {
							rep.Append($"{e.FullName}");
							rep.Replace("/", @"\");
						}
                        
                        lista.Add($"{entry.Length}\t{fullPath}{rep}\t{entry.Name}\t{ext}");
                        //await FileWriteAsync($"{Environment.NewLine}{entry.Length}\t{fullPath}{rep}\t{entry.Name}\t{ext}");
                        //using(System.IO.StreamWriter w = Delimon.Win32.IO.File.AppendText($"{baseDir}lista_arquivos.txt")) {
                        //    w.Write($"{Environment.NewLine}{entry.Length}\t{fullPath}{rep}\t{entry.Name}\t{ext}");
                        //    w.Close();
                        //    w.Dispose();
                        //}
                        //sb.Append($"{Environment.NewLine}{entry.Length}\t{fullPath}{rep}\t{entry.Name}\t{ext}");

                        if(lista.Count >= 100000) {
                            System.IO.File.AppendAllLines($"{baseDir}lista_arquivos.txt", lista);
                            lista.Clear();
                            lista.TrimExcess();
                        }

						if(ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) {
							using (var stream = entry.Open()) {
								using (var ms = new System.IO.MemoryStream()) {
									stream.CopyTo(ms);
									ms.Position = 0;
									CountPdfPages(ms);
								}
							}
						}

						if(ext.Equals(".doc", StringComparison.OrdinalIgnoreCase) || ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)) {
							using (var stream = entry.Open()) {
								using (var ms = new System.IO.MemoryStream()) {
									stream.CopyTo(ms);
									ms.Position = 0;
									CountWordPages(ms);
								}
							}
						}

                        if(ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)) {
							fn = $@"{fullPath}{rep}";
							ZipList(fn, entry);
						}
					}
				}
			}
		}

        //async public Task FileWriteAsync(string line) {

        //    using(System.IO.StreamWriter w = Delimon.Win32.IO.File.AppendText($"{baseDir}lista_arquivos.txt")) {
        //        await w.WriteAsync(line);
        //    }
        //}

        private void create_List(object sender, RoutedEventArgs e) {

            if(System.IO.File.Exists($"{baseDir}lista_arquivos.txt")) System.IO.File.Delete($"{baseDir}lista_arquivos.txt");
			progressBar.Value = 0;
			wordPages = 0;
			wordFiles = 0;
			pdfPages = 0;
			pdfFiles = 0;
			button.IsEnabled = false;
			button1.IsEnabled = false;
            //sb = new StringBuilder("Tamanho (bytes)\tCaminho\tArquivo\tExtensão");
            //zip = new StringBuilder("Tamanho (bytes)\tCaminho\tArquivo\tExtensão");
            //System.IO.File.WriteAllText(baseDir + @"lista_arquivos.txt", "Tamanho (bytes)\tCaminho\tArquivo\tExtensão");
            lista.Add("Tamanho (bytes)\tCaminho\tArquivo\tExtensão");

            Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();

			CreateList(sPath);

			stopWatch.Stop();
			TimeSpan ts = stopWatch.Elapsed;
			string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
			Console.WriteLine($"CreateList RunTime {elapsedTime}");
			//System.Windows.Forms.MessageBox.Show(elapsedTime, "CreateList RunTime", MessageBoxButtons.OK, MessageBoxIcon.Information);

			textBlock2.Text = "Total de páginas (PDF e Word): " + String.Format("{0:n0}", pdfPages + wordPages);

			//System.IO.File.WriteAllText($"{baseDir}lista_arquivos.txt", sb.ToString());

			System.IO.File.AppendAllLines($"{baseDir}lista_arquivos.txt", lista);
            lista.Clear();
            lista.TrimExcess();

            using (var zipToOpen = new System.IO.FileStream($"{baseDir}lista_arquivos.zip", System.IO.FileMode.Create)) {

				using(var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)) {

					archive.CreateEntryFromFile($"{baseDir}lista_arquivos.txt", "lista_arquivos.txt");
				}
			}

            //  using(var zipToOpen = new System.IO.FileStream($"{baseDir}lista_arquivos.zip", System.IO.FileMode.Create)) {

			//	using(var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)) {

			//		var listEntry = archive.CreateEntry("lista_arquivos.txt");

			//		using(var writer = new System.IO.StreamWriter(listEntry.Open())) {
			//			writer.WriteLine(sb);
			//		}
			//	}
			//}
            
			//System.IO.File.WriteAllText(baseDir + @"lista_arquivos_zip.txt", zip.ToString());

			button.IsEnabled = true;
			button1.IsEnabled = true;
			System.Windows.Forms.MessageBox.Show($"O arquivo 'lista_arquivos.zip' foi criado em{Environment.NewLine}{baseDir}", "Arquivos processados com sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);

			//System.Windows.Forms.MessageBox.Show($"Há {pdfFiles} documentos PDF totalizando {pdfPages} página(s).\r\nHá {wordFiles} documentos Word totalizando {wordPages} página(s).", "Páginas", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
