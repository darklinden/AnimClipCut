using System.Collections.Generic;

public class AnimClipCurveKey {
	public string path;
	public System.Type type;
	public string propertyName;
	public struct Comparer : IEqualityComparer<AnimClipCurveKey> {
		public bool Equals(AnimClipCurveKey x, AnimClipCurveKey y) {
			return string.Equals(x.path, y.path) && string.Equals(x.propertyName, y.propertyName) && System.Type.Equals(x.type, y.type);
		}

		public int GetHashCode(AnimClipCurveKey obj) {
			return (obj.path + " --- " + obj.propertyName).GetHashCode();
		}
	}
}