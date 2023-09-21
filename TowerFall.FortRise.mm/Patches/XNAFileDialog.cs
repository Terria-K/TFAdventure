using System.Text;
using FortRise;
using Microsoft.Xna.Framework.Graphics;
using MonoMod;

/* Cross-platform native file dialog by matching its functions name */
[MonoModIfFlag("OS:Windows")]
public static class XNAFileDialog
{
	// While this is unused, it is better to keep this as it somehow needs some compatibility
	public static GraphicsDevice GraphicsDevice;
	public static string Path;
	public static string StartDirectory;

	public static bool ShowDialogSynchronous(string title = null, string saveFile = null)
	{
		// Path = null;
		// var sb = new StringBuilder();
		// bool begin = false;
		// foreach (var t in title) 
		// {
		// 	if (t == '.') 
		// 	{
		// 		begin = true;
		// 		continue;
		// 	}
		// 	if (begin && char.IsWhiteSpace(t)) 
		// 	{
		// 		begin = false;
		// 		break;
		// 	}
			
		// 	if (begin)
		// 		sb.Append(t);
		// }
		// var filter = sb.ToString();
		// if (!string.IsNullOrEmpty(saveFile)) 
		// {
		// 	using var saveFileDialog = new SaveFileDialog() 
		// 	{
		// 		Title = title,
		// 		AddExtension = true,
		// 		Filter = $"Save a {filter} (*.{filter})|*.{filter}|All files (*.*)|*.*",
		// 		InitialDirectory = saveFile,
		// 		DefaultExt = $".{filter}",
		// 		RestoreDirectory = true
		// 	};
		// 	if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
		// 		return false;

		// 	Path = saveFileDialog.FileName;
			
		// 	return !string.IsNullOrEmpty(Path);
		// }

		// using var fileDialog = new OpenFileDialog() 
		// {
		// 	Title = title,
		// 	AddExtension = true,
		// 	CheckPathExists = true,
		// 	CheckFileExists = true,
		// 	Filter = $"Load a {filter} (*.{filter})|*.{filter}|All files (*.*)|*.*",
		// 	DefaultExt = $".{filter}",
		// 	RestoreDirectory = true,
		// 	InitialDirectory = StartDirectory ?? ""
		// };
		// if (fileDialog.ShowDialog() == DialogResult.Cancel)
		// 	return false;
		
		// Path = fileDialog.FileName;

		Logger.Warning("DEFUNCT ON WINDOWS, WAIT FOR THE REIMPLEMENTATION.");
		return !string.IsNullOrEmpty(Path);
	}
}