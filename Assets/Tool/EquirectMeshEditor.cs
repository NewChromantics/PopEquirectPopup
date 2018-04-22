using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[RequireComponent(typeof(EquirectMeshPlotter))]
public class EquirectMeshEditor : MonoBehaviour
{
	[Range(0, 1)]
	public float RemoveMaxDistance = 0.1f;

	EquirectMeshPlotter	Plotter	{ get { return GetComponent<EquirectMeshPlotter>(); }}


	public void AddScreenPoint(Vector2 ScreenPos)
	{
		var Ray = Plotter.Camera.GetComponent<Camera>().ScreenPointToRay(ScreenPos.xy0());
		Plotter.AddViewDirectionPoint(Ray.direction);
	}

	public void RemoveScreenPoint(Vector2 ScreenPos)
	{
		var Ray = Plotter.Camera.GetComponent<Camera>().ScreenPointToRay(ScreenPos.xy0());
		if (!Plotter.RemoveViewDirectionPoint(Ray.direction, RemoveMaxDistance))
			OnDidntRemove();
	}

	void OnDidntRemove()
	{
		UnityEditor.EditorApplication.Beep();
	}

	public void ExportMesh()
	{
		try
		{
			var Mesh = Plotter.GenerateMesh();
			string Filename;
			var WriteLine = PopX.IO.GetFileWriteLineFunction(out Filename,"Exported Obj", "Mesh", PopX.WavefrontObj.FileExtension);
			PopX.WavefrontObj.Export(WriteLine, Mesh, Matrix4x4.identity);
			UnityEditor.EditorUtility.RevealInFinder(Filename);
		}
		catch(System.Exception e)
		{
			ShowUiError.ShowError(e.Message);
		}
	}

	struct Triangle3
	{
		public Vector3 a;
		public Vector3 b;
		public Vector3 c;
	};
	struct Triangle2
	{
		public Vector2 a;
		public Vector2 b;
		public Vector2 c;
	};
	struct Bounds2
	{
		public Vector2 Min;
		public Vector2 Max;
	};

	//	triangle packing!
	List<Triangle2> AllocateTriangleAtlases(List<Triangle3> MeshTriangles)
	{
		var TriangleCount = MeshTriangles.Count;
		var AtlasWidth = (int)Mathf.Ceil(Mathf.Sqrt((float)TriangleCount));
		var AtlasHeight = AtlasWidth;

		var AtlasTriangles = new List<Triangle2>();

		for (int ti = 0; ti<MeshTriangles.Count;	ti++)
		{
			var MeshTriangle = MeshTriangles[ti];

			//	get atlas quad
			var AtlasX = ti % AtlasWidth;
			var AtlasY = ti / AtlasWidth;
			var AtlasMinu = PopMath.Range(0, AtlasWidth, AtlasX);
			var AtlasMaxu = PopMath.Range(0, AtlasWidth, AtlasX+1);
			var AtlasMinv = PopMath.Range(0, AtlasHeight, AtlasY);
			var AtlasMaxv = PopMath.Range(0, AtlasHeight, AtlasY+1);

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

	void GenerateProjectedTextureMap(Mesh mesh,out List<Vector2> VertexAtlasUvs)
	{
		//	generate atlas
		var MeshTriangles = new List<Triangle3>();
		var Positions = mesh.vertices;
		var TriangleIndexes = mesh.triangles;
		System.Action<int,int,int> AddTriangleByVertexIndexes = (a,b,c) =>
		{
			var Triangle = new Triangle3();
			Triangle.a = Positions[a];
			Triangle.b = Positions[b];
			Triangle.c = Positions[c];
			MeshTriangles.Add(Triangle);
		};
		for (int t = 0; t < TriangleIndexes.Length;	t+=3 )
		{
			var ia = TriangleIndexes[t + 0];
			var ib = TriangleIndexes[t + 1];
			var ic = TriangleIndexes[t + 2];
			AddTriangleByVertexIndexes(ia, ib, ic);
		}

		var AtlasTriangles = AllocateTriangleAtlases(MeshTriangles);

		//	put atlas back into mesh
		//	for every triangle, get atlasuv, get each vertex (assume disconnected) and write
		List<Vector2> VertexAtlasUvs

		//	render each triangle to atlas (can probably do all of these at once with an ortho camera)
		Graphics.Blit()
	}
}

