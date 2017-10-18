using System;
using System.Collections.Generic;
using System.Diagnostics;
// using System.IO;
using Delimon.Win32.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ListaArquivos {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private String sPath;
		private Int64 total;
		private DirectoryInfo folder;
		private String baseDir = AppDomain.CurrentDomain.BaseDirectory;
		private Boolean validDir = true;
		// Thread loadIconThread = new Thread(loadingIcon);

		public MainWindow() => InitializeComponent();

		private async Task<string> DirSearch(string sDir) {
			loadingIcon.Dispatcher.Invoke(() => loadingIcon.Visibility = Visibility.Visible, DispatcherPriority.Background);
			// loadingIcon.Visibility = Visibility.Visible;
			validDir = true;
			try {
				foreach (string f in Directory.GetFiles(sDir)) {
					total++;
				}
				foreach (string d in Directory.GetDirectories(sDir)) {
					await DirSearch2(d);
				}
				//await loadingIcon.Dispatcher.BeginInvoke((Action)(() => loadingIcon.Visibility = Visibility.Hidden));
				loadingIcon.Dispatcher.Invoke(() => loadingIcon.Visibility = Visibility.Hidden, DispatcherPriority.Background);
				return null;
			} catch {
				//validDir = false;
				//Console.WriteLine(e.Message);
				//System.Windows.Forms.MessageBox.Show($"{e.Message}{Environment.NewLine}Favor selecionar um diretório válido.", "Diretório inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
				// throw;
				// total = 0;
				return null;
			} finally {
				if(!validDir) System.Windows.Forms.MessageBox.Show("Favor selecionar um diretório válido.", "Diretório inválido", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private async Task<string> DirSearch2(string sDir) {
			try {
				foreach (string f in Directory.GetFiles(sDir)) {
					total++;
				}
				foreach (string d in Directory.GetDirectories(sDir)) {
					await DirSearch2(d);
				}
				return null;
			} catch { return null; }
		}

		private async void select_Dir(object sender, RoutedEventArgs e) {
			FolderBrowserDialog folderDialog = new FolderBrowserDialog();
			folderDialog.ShowNewFolderButton = false;
			// folderDialog.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
			DialogResult result = folderDialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK) {
				sPath = folderDialog.SelectedPath;
				textBox.Text = sPath;
				total = 0;
				progressBar.Minimum = 0;
				progressBar.Value = 0;
				// loadingIcon.Dispatcher.Invoke(() => loadingIcon.Visibility = Visibility.Visible, DispatcherPriority.Background);
				// await loadingIcon.Dispatcher.BeginInvoke((Action)(() => loadingIcon.Visibility = Visibility.Visible));
				// var files = Directory.GetFiles("C:\\path", "*.*", SearchOption.AllDirectories);

				folder = new DirectoryInfo(sPath);
				if (folder.Exists) {

					// total = System.IO.Directory.EnumerateFiles(sPath, "*", System.IO.SearchOption.AllDirectories).Count();
					await DirSearch(sPath);

					// foreach (FileInfo fileInfo in folder.GetFiles()) {
					// foreach (System.IO.FileInfo fileInfo in folder.EnumerateFiles("*", System.IO.SearchOption.AllDirectories)) {
					//	total++;
					//}
					// loadingIcon.Dispatcher.Invoke(() => loadingIcon.Visibility = Visibility.Hidden, DispatcherPriority.Background);
					if (total != 0 && validDir) {
						progressBar.Maximum = total;
						button1.IsEnabled = true;
						textBlock1.Text = "Total de arquivos: " + String.Format("{0:n0}", total);
					} else {
						progressBar.Value = 0;
						button1.IsEnabled = false;
					}
				}
			}
		}

		private async void create_List(object sender, RoutedEventArgs e) {
			// DirectoryInfo folder = new DirectoryInfo(sPath);
			String lista = "Tamanho (bytes)\tCaminho\tArquivo\tExtensão";
			StringBuilder sb = new StringBuilder(lista);
			
			if (folder.Exists) {
				foreach (FileInfo fileInfo in folder.GetFiles()) {
					// progressBar.Value += 1;
					sb.Append(Environment.NewLine);
					sb.Append(@fileInfo.Length);
					sb.Append("\t");
					sb.Append(@fileInfo.DirectoryName);
					sb.Append("\t");
					sb.Append(@fileInfo.Name);
					sb.Append("\t");
					sb.Append(@fileInfo.Extension);
					progressBar.Dispatcher.Invoke(() => progressBar.Value += 1, DispatcherPriority.Background);

					// @lista = @fileInfo.Length + "\t" + @fileInfo.DirectoryName + "\t" + @fileInfo.Name + "\t" + @fileInfo.Extension + Environment.NewLine;
					// sb.Append(@lista);
				}
			}

			File.WriteAllText(baseDir + @"lista_arquivos.txt", sb.ToString());
			System.Windows.Forms.MessageBox.Show($"O arquivo 'lista_arquivos.txt' foi criado em{Environment.NewLine}{baseDir}", "Arquivos processados com sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}

// Debug.WriteLine("#Debug: File: " + fileInfo.Name + " Date:" + fileInfo.CreationTime.ToString("dd-MM-yyy"));
// Directory.GetFiles.
// "chcp 65001 & DIR " + folderpath + " /A:-D /O:NS /S | FIND ":" > lista.txt"