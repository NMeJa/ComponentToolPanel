using System;

namespace ComponentToolPanel
{
	public struct CtpExtendedEditors
	{
		public readonly Type inspectedType;
		public readonly Type inspectorType;

		public CtpExtendedEditors(Type inspectedType, Type inspectorType)
		{
			this.inspectedType = inspectedType;
			this.inspectorType = inspectorType;
		}
	}
}