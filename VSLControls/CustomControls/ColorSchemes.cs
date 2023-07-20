using System.Drawing;

namespace CustomControls
{
	/// <summary>
	/// The color schemes.
	/// </summary>
	public class ColorSchemes
	{
		public static Color[] SelectedNormal = new Color[5]
	{
		Color.FromArgb(255, 237, 156),
		Color.FromArgb(255, 237, 156),
		Color.FromArgb(255, 216, 108),
		Color.FromArgb(255, 196, 0),
		Color.White
	};

		public static Color[] SelectedHover = new Color[5]
	{
		Color.FromArgb(255, 237, 156),
		Color.FromArgb(255, 247, 166),
		Color.FromArgb(255, 216, 118),
		Color.FromArgb(255, 230, 50),
		Color.White
	};

		public static Color[] SelectedPressed = new Color[5]
	{
		Color.FromArgb(225, 207, 126),
		Color.FromArgb(255, 227, 136),
		Color.FromArgb(255, 192, 35),
		Color.Gold,
		Color.White
	};

		public static object SelectedBorder = new SolidBrush(Color.FromArgb(194, 138, 48));

		public static Color[] UnSelectedNormal = new Color[5]
	{
		Color.White,
		Color.White,
		Color.White,
		Color.White,
		Color.White
	};

		public static Color[] UnSelectedHover = new Color[5]
	{
		Color.FromArgb(235, 244, 253),
		Color.FromArgb(221, 236, 253),
		Color.FromArgb(209, 230, 253),
		Color.FromArgb(194, 220, 253),
		Color.White
	};

		public static Color[] UnSelectedPressed = new Color[5]
	{
		Color.FromArgb(171, 210, 242),
		Color.FromArgb(194, 220, 253),
		Color.FromArgb(189, 210, 233),
		Color.FromArgb(194, 220, 253),
		Color.White
	};

		public static object UnSelectedBorder = new SolidBrush(Color.FromArgb(125, 162, 206));

		public static object DisabledBorder = new SolidBrush(Color.White);

		public static Color[] DisabledAllColor = new Color[5]
	{
		Color.White,
		Color.White,
		Color.White,
		Color.White,
		Color.White
	};
	}
}