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
				{
					infoIcon = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x.name == InfoIconName);
				}

				if (infoIcon == null)
				{
					infoIcon = EditorGUIUtility.IconContent(InfoIconName).image as Texture2D;
				}

				if (infoIcon == null)
				{
					infoIcon = EditorGUIUtility.FindTexture(InfoIconName);
				}

				return infoIcon;
			}
		}

		public static Texture2D NonFavoriteIcon
		{
			get
			{
				if (nonFavoriteIcon == null)
				{
					nonFavoriteIcon = Resources.FindObjectsOfTypeAll<Texture2D>()
					                           .FirstOrDefault(x => x.name == NonFavoriteIconName);
				}

				if (nonFavoriteIcon == null)
				{
					nonFavoriteIcon = EditorGUIUtility.IconContent(NonFavoriteIconName).image as Texture2D;
				}

				if (nonFavoriteIcon == null)
				{
					nonFavoriteIcon = EditorGUIUtility.FindTexture(NonFavoriteIconName);
				}

				return nonFavoriteIcon;
			}
		}

		public static Texture2D FavoriteIcon
		{
			get
			{
				if (favoriteIcon == null)
				{
					favoriteIcon = Resources.FindObjectsOfTypeAll<Texture2D>()
					                        .FirstOrDefault(x => x.name == FavoriteIconName);
				}

				if (favoriteIcon == null)
				{
					favoriteIcon = EditorGUIUtility.IconContent(FavoriteIconName).image as Texture2D;
				}

				if (favoriteIcon == null)
				{
					favoriteIcon = EditorGUIUtility.FindTexture(FavoriteIconName);
				}

				return favoriteIcon;
			}
		}

		public static Texture2D CloseIcon
		{
			get
			{
				if (closeIcon == null)
				{
					closeIcon = Resources.FindObjectsOfTypeAll<Texture2D>()
					                     .FirstOrDefault(x => x.name == CloseIconName);
				}

				if (closeIcon == null)
				{
					closeIcon = EditorGUIUtility.IconContent(CloseIconName).image as Texture2D;
				}

				if (closeIcon == null)
				{
					closeIcon = EditorGUIUtility.FindTexture(CloseIconName);
				}

				return closeIcon;
			}
		}

		public static Texture2D DocumentIcon
		{
			get
			{
				if (documentIcon == null)
				{
					documentIcon = EditorGUIUtility.IconContent(DocumentIconName).image as Texture2D;
				}

				if (documentIcon == null)
				{
					documentIcon = EditorGUIUtility.FindTexture(DocumentIconName);
				}

				return documentIcon;
			}
		}

		public static Texture2D FolderIcon
		{
			get
			{
				if (folderIcon == null)
				{
					folderIcon = EditorGUIUtility.IconContent(FolderIconName).image as Texture2D;
				}

				if (folderIcon == null)
				{
					folderIcon = EditorGUIUtility.FindTexture(FolderIconName);
				}

				return folderIcon;
			}
		}

		public static Texture2D ResetIcon
		{
			get
			{
				if (resetIcon == null)
				{
					resetIcon = Resources.FindObjectsOfTypeAll<Texture2D>()
					                     .FirstOrDefault(x => x.name == ResetIconName);
				}

				if (resetIcon == null)
				{
					resetIcon = EditorGUIUtility.FindTexture(ResetIconName);
				}

				return resetIcon;
			}
		}

		public static Texture2D EditIcon
		{
			get
			{
				if (editIcon == null)
				{
					editIcon = EditorGUIUtility.FindTexture(EditIconName);
				}

				return editIcon;
			}
		}

		public static Texture2D WarningIcon
		{
			get
			{
				if (warningIcon == null)
				{
					warningIcon = Resources.FindObjectsOfTypeAll<Texture2D>()
					                       .FirstOrDefault(x => x.name == WarningIconName);
				}

				if (warningIcon == null)
				{
					warningIcon = EditorGUIUtility.IconContent(WarningIconName).image as Texture2D;
				}

				if (warningIcon == null)
				{
					warningIcon = EditorGUIUtility.FindTexture(WarningIconName);
				}

				return warningIcon;
			}
		}
	}
}