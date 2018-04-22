using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Triangle3
{
	public Vector3 a;
	public Vector3 b;
	public Vector3 c;
};

public struct Triangle2
{
	public Vector2 a;
	public Vector2 b;
	public Vector2 c;
};

public static class PopTrianglePacker 
{
	struct Bounds2
	{
		public Vector2 Min;
		public Vector2 Max;
	};

	//	triangle packing!
	public static List<Triangle2> AllocateTriangleAtlases(List<Triangle3> MeshTriangles)
	{
		var TriangleCount = MeshTriangles.Count;
		var AtlasWidth = (int)Mathf.Ceil(Mathf.Sqrt((float)TriangleCount));
		var AtlasHeight = AtlasWidth;

		var AtlasTriangles = new List<Triangle2>();

		for (int ti = 0; ti < MeshTriangles.Count; ti++)
		{
			var MeshTriangle = MeshTriangles[ti];

			//	get atlas quad
			var AtlasX = ti % AtlasWidth;
			var AtlasY = ti / AtlasWidth;
			var AtlasMinu = PopMath.Range(0, AtlasWidth, AtlasX);
			var AtlasMaxu = PopMath.Range(0, AtlasWidth, AtlasX + 1);
			var AtlasMinv = PopMath.Range(0, AtlasHeight, AtlasY);
			var AtlasMaxv = PopMath.Range(0, AtlasHeight, AtlasY + 1);

			//	todo: get triangle position on it's plane (ie, it's ortho)
			var AtlasTriangle = new Triangle2();
			AtlasTriangle.a = MeshTriangle.a.xz();
			AtlasTriangle.b = MeshTriangle.b.xz();
			AtlasTriangle.c = MeshTriangle.c.xz();

			//	fit triangle to quad
			var AtlasTriangleBounds = new Bounds2();
			AtlasTriangleBounds.Min = PopMath.Min(AtlasTriangle.a, AtlasTriangle.b, AtlasTriangle.c);
			AtlasTriangleBounds.Max = PopMath.Max(AtlasTriangle.a, AtlasTriangle.b, AtlasTriangle.c);
			AtlasTriangle.a = PopMath.Range(AtlasTriangleBounds.Min, AtlasTriangleBounds.Max, AtlasTriangle.a);
			AtlasTriangle.b = PopMath.Range(AtlasTriangleBounds.Min, AtlasTriangleBounds.Max, AtlasTriangle.b);
			AtlasTriangle.c = PopMath.Range(AtlasTriangleBounds.Min, AtlasTriangleBounds.Max, AtlasTriangle.c);

			//	now put it in it's atlas pos
			AtlasTriangle.a.x = Mathf.Lerp(AtlasMinu, AtlasMaxu, AtlasTriangle.a.x);
			AtlasTriangle.a.y = Mathf.Lerp(AtlasMinv, AtlasMaxv, AtlasTriangle.a.y);
			AtlasTriangle.b.x = Mathf.Lerp(AtlasMinu, AtlasMaxu, AtlasTriangle.b.x);
			AtlasTriangle.b.y = Mathf.Lerp(AtlasMinv, AtlasMaxv, AtlasTriangle.b.y);
			AtlasTriangle.c.x = Mathf.Lerp(AtlasMinu, AtlasMaxu, AtlasTriangle.c.x);
			AtlasTriangle.c.y = Mathf.Lerp(AtlasMinv, AtlasMaxv, AtlasTriangle.c.y);

			AtlasTriangles.Add(AtlasTriangle);
		}

		return AtlasTriangles;
	}

}
