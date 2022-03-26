using System;

namespace ComponentToolPanel
{
	public struct ExtendedEditors
	{
		public readonly Type inspectedType;
		public readonly Type inspectorType;

		public ExtendedEditors(Type inspectedType, Type inspectorType)
		{
			this.inspectedType = inspectedType;
			this.inspectorType = inspectorType;
		}
	}
}