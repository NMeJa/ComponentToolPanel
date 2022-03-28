using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ComponentToolPanel
{
	public static class Icons
	{
		private const string InfoIconName = "console.infoicon.sml";
		private const string NonFavoriteIconName = "Favorite";
		private const string FavoriteIconName = "Favorite Icon";
		private const string CloseIconName = "TL Close button act";
		private const string DocumentIconName = "UnityEditor.ConsoleWindow";
		private const string FolderIconName = "Project";
		private const string ResetIconName = "playLoopOff";
		private const string EditIconName = "ViewToolZoom";
		private const string WarningIconName = "console.warnicon.sml";
		private static Texture2D infoIcon;
		private static Texture2D nonFavoriteIcon;
		private static Texture2D favoriteIcon;
		private static Texture2D closeIcon;
		private static Texture2D documentIcon;
		private static Texture2D folderIcon;
		private static Texture2D resetIcon;
		private static Texture2D editIcon;
		private static Texture2D warningIcon;

		public static Texture2D InfoIcon
		{
			get
			{
				if (infoIcon == null)
					infoIcon = FindTexture(InfoIconName);
				return infoIcon;
			}
		}

		public static Texture2D NonFavoriteIcon
		{
			get
			{
				if (nonFavoriteIcon == null)
					nonFavoriteIcon = FindTexture(NonFavoriteIconName);
				return nonFavoriteIcon;
			}
		}

		public static Texture2D FavoriteIcon
		{
			get
			{
				if (favoriteIcon == null)
					favoriteIcon = FindTexture(FavoriteIconName);
				return favoriteIcon;
			}
		}

		public static Texture2D CloseIcon
		{
			get
			{
				if (closeIcon == null)
					closeIcon = FindTexture(CloseIconName);
				return closeIcon;
			}
		}

		public static Texture2D DocumentIcon
		{
			get
			{
				if (documentIcon == null)
					documentIcon = FindTexture(DocumentIconName);
				return documentIcon;
			}
		}

		public static Texture2D FolderIcon
		{
			get
			{
				if (folderIcon == null)
					folderIcon = FindTexture(FolderIconName);
				return folderIcon;
			}
		}

		public static Texture2D ResetIcon
		{
			get
			{
				if (resetIcon == null)
					resetIcon = FindTexture(ResetIconName);
				return resetIcon;
			}
		}

		public static Texture2D EditIcon
		{
			get
			{
				if (editIcon == null)
					editIcon = FindTexture(EditIconName);
				return editIcon;
			}
		}

		public static Texture2D WarningIcon
		{
			get
			{
				if (warningIcon == null)
					warningIcon = FindTexture(WarningIconName);
				return warningIcon;
			}
		}

		private static Texture2D FindTexture(string iconName)
		{
			Texture2D texture2D = null;
			texture2D = EditorGUIUtility.IconContent(iconName).image as Texture2D;

			if (texture2D == null)
				texture2D = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x.name == iconName);

			if (texture2D == null)
				texture2D = EditorGUIUtility.FindTexture(iconName);

			return texture2D;
		}
	}
}